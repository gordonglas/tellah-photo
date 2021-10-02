using System;
using System.Linq;
using System.Text;
using TellahPhotoLibrary.Common;
using TellahPhotoLibrary.Config;
using TellahPhotoLibrary.File;
using TellahPhotoLibrary.Html;
using TellahPhotoLibrary.Image;
using TellahPhotoLibrary.Models;
using TellahPhotoLibrary.Models.Html;
using TellahPhotoLibrary.Video;
using TellahPhotoLibrary.Video.Models;

namespace TellahPhotoLibrary
{
    public class TellahPhotoApi
    {
        private ILogger _logger;
        private IFileInfoApi _fileInfoApi;
        private IVideoApi _videoApi;

        public TellahPhotoApi(ILogger logger)
        {
            _logger = logger;
            _fileInfoApi = new ExiftoolFileInfoApi(_logger);
            _videoApi = new FfmpegVideoApi(_logger, _fileInfoApi);
        }

        public string GetVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }

        public string GetUsage()
        {
            string usage = @"A simple tool for creating smaller versions of
photos and videos and use them to generate an HTML
album, that can be used for self-hosting.

More info and latest version can be found at:
https://github.com/gordonglas/tellah-photo

Usage:

  Version:
    tellah --v

  Process album:
    Converts photos/videos to smaller format,
    and generates .tellah.json metadata file.

    Required switches:
      -i {input-folder}
        input-folder is folder of raw photos/videos.

      -o {output-folder}
        output-folder is where album is generated.

      -album-name {album-name}
        album-name is the albums display name.

    Optional switches:
       -image-size {width}x{height}
         default: 1920x1920

       -video-size {width}x{height}
         default: Size of input video file.

       -preview-image-size {width}x{height}
         default: 640x640

       -video-preview-time-offset-seconds {seconds}
         seconds is a float of time from start of
         video where preview image is generated.
         default: 0.5

       -jpeg-quality {jpeg-quality}
         jpeg-quality is an int percent of
         jpg image quality.
         default: 70

    Example:
      tellah -i ""c:\photos\raw\public\art""
        -o ""c:\photos\generated-albums\public\art""
        -album-name ""Art""
        -image-size 1920x1920 -video-size 960x960
        -preview-image-size 420x420
        -video-preview-time-offset-seconds 0.5


  Update/sync album:
    After an album has already been processed,
    you can use this shorthand within the album folder:

    tellah --update


  Build html:
    Builds album html from processed albums.

    Required switches:
      -build-html {album-index-folder}
        album-index-folder is the parent folder
        of one or more processed albums.

      -album-index-url-path {album-index-url-path}
        album-index-url-path is the url path
        where your album's index page will be
        on your web server.

    Optional switches:
      -sort-albums-by {sort-by-type}
        sort-by-type can be one of the following values:
          AlbumEndDateDesc
          AlbumFolderNameAsc
        default: AlbumEndDateDesc

    Example:
      tellah -build-html ""c:\photos\generated-albums\public""
      -album-index-url-path ""/photos/public/""


  Set album cover:
    Change the album cover to an existing album image.
    You must rebuild html again after running this
    to see the change in your album index page.

    Required switches:
      -album-cover {album-cover-image}
        album-cover-image is a path to an existing
        image file to use as the album's cover image.

    Example:
      tellah -album-cover ""c:\photos\generated-albums\public\art\a-video_tm.jpg""";

            return usage;
        }

        private Size? ParseSizeArg(bool required, CommandLine commandLine,
            string argName, string errorMsg)
        {
            Size? size = null;
            if (commandLine.HasValue(argName))
            {
                size = Size.Parse(commandLine.GetValue(argName));
                if (size == null)
                    throw new Exception(errorMsg);
            }

            if (required && size == null)
                throw new Exception(errorMsg);

            return size;
        }

        private double? ParseDoubleArg(bool required, CommandLine commandLine,
            string argName, string errorMsg)
        {
            double? d = null;
            if (commandLine.HasValue(argName))
            {
                if (!double.TryParse(commandLine.GetValue(argName), out double value))
                    throw new Exception(errorMsg);
                d = value;
            }

            if (required && d == null)
                throw new Exception(errorMsg);

            return d;
        }

        private int? ParseIntArg(bool required, CommandLine commandLine,
            string argName, string errorMsg)
        {
            int? d = null;
            if (commandLine.HasValue(argName))
            {
                if (!int.TryParse(commandLine.GetValue(argName), out int value))
                    throw new Exception(errorMsg);
                d = value;
            }

            if (required && d == null)
                throw new Exception(errorMsg);

            return d;
        }

        private ProcessAlbumOptions GetProcessAlbumOptionsFromArgs(string[] args)
        {
            if (args == null || args.Length == 0)
                return null;

            CommandLine commandLine = new CommandLine();
            commandLine.Parse(args);

            ProcessAlbumOptions options = null;
            if (commandLine.HasFlag("update"))
            {
                // attempt to get options from .tellah.json file.
                // This allows subsequent runs of tellah (to update an album) to be quicker to type,
                // like so: "tellah --update"
                string currentDirectory = System.IO.Directory.GetCurrentDirectory();
                string tellahJsonPath = System.IO.Path.Combine(currentDirectory,
                    TELLAH_JSON_FILE_NAME);
                TellahJson json = TellahJson.ReadFromFile(tellahJsonPath);
                options = new ProcessAlbumOptions(currentDirectory, json.Settings);
                options.AlbumName = json.AlbumInfo.Overrides?.Name ?? json.AlbumInfo.Name;
            }

            if (options == null)
            {
                if (!commandLine.HasValue("i"))
                    throw new Exception("Missing \"-i {input-folder}\"");

                if (!commandLine.HasValue("o"))
                    throw new Exception("Missing \"-o {output-folder}\"");

                options = new ProcessAlbumOptions
                {
                    InputPath = commandLine.GetValue("i"),
                    OutputPath = commandLine.GetValue("o"),
                    AlbumName = commandLine.HasValue("album-name") ? commandLine.GetValue("album-name") : null,
                    KeepAspectRatio = !commandLine.HasFlag("ignore-aspect-ratio"),
                    ImageSize = ParseSizeArg(false, commandLine, "image-size", "Invalid image-size arg"),
                    VideoSize = ParseSizeArg(false, commandLine, "video-size", "Invalid video-size arg"),
                    PreviewImageSize = ParseSizeArg(false, commandLine, "preview-image-size", "Invalid preview-image-size arg"),
                    VideoPreviewTimeOffsetSeconds = ParseDoubleArg(false, commandLine, "video-preview-time-offset-seconds", "Invalid video-preview-time-offset-seconds arg"),
                    MaxVideoBitrateMbps = ParseDoubleArg(false, commandLine, "max-video-bitrate-mbps", "Invalid max-video-bitrate-mbps arg"),
                    JpegQuality = ParseIntArg(false, commandLine, "jpeg-quality", "Invalid jpeg-quality arg"),
                };
            }

            options.ForceUpdateInputFolder = commandLine.HasFlag("force-update-input-folder");

            return options;
        }

        private void DeleteFilesRecursive(string path, string pattern)
        {
            try
            {
                var dir = new System.IO.DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles(pattern, System.IO.SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        public const string TELLAH_JSON_FILE_NAME = ".tellah.json";
        private string _tellahJsonPath;

        public void BuildHtml(CommandLine commandLine)
        {
            // must pass the path portion of the url where this album will be hosted,
            // excluding domain name. Must begin with / or ~, and end with /
            string albumIndexUrlPath = commandLine.GetValue("album-index-url-path");
            if (albumIndexUrlPath == null)
                throw new Exception("Missing param album-index-url-path");
            if (!(albumIndexUrlPath.StartsWith("/") || albumIndexUrlPath.StartsWith("~")))
                throw new Exception("album-index-url-path must being with / or ~");
            if (!albumIndexUrlPath.EndsWith("/"))
                throw new Exception("album-index-url-path must end with /");

            string htmlPath = commandLine.GetValue("build-html");
            if (htmlPath == null)
                htmlPath = System.IO.Directory.GetCurrentDirectory();

            string strSortAlbumsBy = commandLine.GetValue("sort-albums-by") ?? "AlbumEndDateDesc";
            if (!Enum.TryParse(strSortAlbumsBy, out HtmlAlbumIndexSortBy sortAlbumsBy))
                throw new Exception("Invalid sort-by type");

            // make sure we're not inside an individual album folder.
            //if (System.IO.File.Exists(System.IO.Path.Combine(htmlPath, TELLAH_JSON_FILE_NAME)))
            //    throw new Exception("This is an individual album folder.\nThe 'build-html' command must be run against a parent folder.");

            var albumFiles = System.IO.Directory.GetFiles(htmlPath,
                TELLAH_JSON_FILE_NAME, System.IO.SearchOption.AllDirectories);

            if (albumFiles.Count() == 0)
                throw new Exception($"No albums found under: {htmlPath}");

            string htmlDirName = PathUtils.GetLastDirectory(htmlPath);

            // delete album html files in this dir,
            // in case we renamed or deleted an album folder.

            // not using "index.html" here, because it would potentially be dangerous.
            // delete main album index html file
            DeleteFilesRecursive(htmlPath, "album-index.html");
            // delete all individual album html files
            DeleteFilesRecursive(htmlPath, "album.html");

            // read in all info we need to form main album index html page
            HtmlAlbumIndex htmlAlbumIndex = new HtmlAlbumIndex();
            htmlAlbumIndex.AlbumIndexTitle = $"{htmlDirName.CapitalizeFirstLetter()} Albums";

            foreach (string tellahJsonFile in albumFiles)
            {
                TellahJson json = TellahJson.ReadFromFile(tellahJsonFile);

                if (json.Items.Count == 0)
                    continue;

                HtmlAlbumIndexItem htmlAlbum = new HtmlAlbumIndexItem();
                
                string albumPath = System.IO.Path.GetDirectoryName(tellahJsonFile);
                htmlAlbum.AlbumRelativePath = PathUtils.GetRelativePath(htmlPath, albumPath);

                htmlAlbum.AlbumFolderName = new System.IO.DirectoryInfo(albumPath).Name;
                htmlAlbum.AlbumName = json.AlbumInfo.Overrides?.Name ?? json.AlbumInfo.Name;
                htmlAlbum.AlbumCoverPreviewFileName = json.AlbumInfo.AlbumCoverPreviewFileName;
                htmlAlbum.AlbumCoverPreviewFileSize = json.AlbumInfo.AlbumCoverPreviewFileSize;
                htmlAlbum.AlbumStartDate = json.AlbumInfo.Overrides?.StartDate ?? json.AlbumInfo.StartDate;
                htmlAlbum.AlbumEndDate = json.AlbumInfo.Overrides?.EndDate ?? json.AlbumInfo.EndDate;
                htmlAlbum.AlbumItemCount = json.Items.Count;
                htmlAlbum.SortAlbumsBy = sortAlbumsBy;

                htmlAlbumIndex.Albums.Add(htmlAlbum);
            }

            // sort albums by AlbumInfo.EndDate desc,
            // so most recent albums show at the top of the index page.
            htmlAlbumIndex.Albums.Sort();

            // TODO: take template number (1 in this case) as command line switch.
            string albumIndexHtml = HtmlRenderer.RenderRazorTemplate("AlbumIndexTemplate1", htmlAlbumIndex);
            string albumIndexHtmlFileName = HtmlAlbumIndex.GetAlbumHtmlFileName(htmlDirName, true);
            string albumIndexHtmlFilePath = System.IO.Path.Combine(htmlPath, albumIndexHtmlFileName);
            System.IO.File.WriteAllText(albumIndexHtmlFilePath, albumIndexHtml, Encoding.UTF8);
            _logger.WriteLine($"Wrote: {albumIndexHtmlFilePath}");

            HtmlRenderer.CopyStaticWebFiles(htmlPath);

            // render html files for individual album pages
            foreach (string tellahJsonFile in albumFiles)
            {
                TellahJson json = TellahJson.ReadFromFile(tellahJsonFile);

                if (json.Items.Count == 0)
                    continue;

                HtmlAlbum htmlAlbum = new HtmlAlbum();
                htmlAlbum.AlbumIndexUrlPath = albumIndexUrlPath;
                htmlAlbum.AlbumIndexUrl = albumIndexUrlPath + albumIndexHtmlFileName;

                htmlAlbum.AlbumName = json.AlbumInfo.Overrides?.Name ?? json.AlbumInfo.Name;
                htmlAlbum.AlbumStartDate = json.AlbumInfo.Overrides?.StartDate ?? json.AlbumInfo.StartDate;
                htmlAlbum.AlbumEndDate = json.AlbumInfo.Overrides?.EndDate ?? json.AlbumInfo.EndDate;
                htmlAlbum.AlbumItemCount = json.Items.Count;

                foreach (TellahJsonAlbumItem jsonItem in json.Items)
                {
                    HtmlAlbumItem albumItem = new HtmlAlbumItem(jsonItem);                    
                    htmlAlbum.Items.Add(albumItem);
                }

                // TODO: take template number (1 in this case) as command line switch.
                string albumHtml = HtmlRenderer.RenderRazorTemplate("AlbumTemplate1", htmlAlbum);
                string albumPath = System.IO.Path.GetDirectoryName(tellahJsonFile);
                string albumHtmlFileName = HtmlAlbumIndex.GetAlbumHtmlFileName(
                    PathUtils.GetLastDirectory(albumPath), false);
                string albumHtmlFilePath = System.IO.Path.Combine(albumPath, albumHtmlFileName);
                System.IO.File.WriteAllText(albumHtmlFilePath, albumHtml, Encoding.UTF8);
                _logger.WriteLine($"Wrote: {albumHtmlFilePath}");
            }
        }

        public void SetAlbumCover(string previewImageFile)
        {
            string previewImageFilePath = System.IO.Path.GetFullPath(previewImageFile);

            // make sure previewImageFilePath exists and is not a directory
            if (System.IO.Directory.Exists(previewImageFilePath))
                throw new Exception($"{previewImageFilePath} is a directory, not a file.");
            if (!System.IO.File.Exists(previewImageFilePath))
                throw new Exception($"File not found: {previewImageFilePath}");

            // make sure we're inside an album folder by looking for a tellah json file
            string albumFolder = System.IO.Path.GetDirectoryName(previewImageFilePath);
            _tellahJsonPath = System.IO.Path.Combine(albumFolder, TELLAH_JSON_FILE_NAME);
            if (!System.IO.File.Exists(_tellahJsonPath))
                throw new Exception($"{TELLAH_JSON_FILE_NAME} file not found. \"album-cover\" option must be used with an image file that exists within a TellahPhoto album folder.");

            TellahJson json = TellahJson.ReadFromFile(_tellahJsonPath);

            previewImageFile = System.IO.Path.GetFileName(previewImageFilePath);

            // find album item that has the same file name
            TellahJsonAlbumItem itemFound = null;
            foreach (TellahJsonAlbumItem item in json.Items)
            {
                if (item.PreviewFileName == previewImageFile)
                {
                    itemFound = item;
                    break;
                }
            }

            if (itemFound == null)
                throw new Exception($"PreviewImageFile \"{previewImageFile}\" does not exist in this album.");

            json.AlbumInfo.AlbumCoverPreviewFileName = itemFound.PreviewFileName;
            json.AlbumInfo.AlbumCoverPreviewFileSize = itemFound.PreviewFileSize;

            // write updated ".tellah.json" file
            json.WriteToFile(_tellahJsonPath);
        }

        // Convert/compress images/videos in that album,
        // create thumbnail image of each video,
        // create json metadata file,
        // create html pages,
        // if raw file's size or date modified has changed, it reprocesses that file.
        // if raw file is deleted (but is in json file), remove it's album files.
        public void ProcessAlbum(string[] args, ProcessAlbumOptions options)
        {
            if (args != null && args.Length > 0)
            {
                options = GetProcessAlbumOptionsFromArgs(args);
            }

            if (options == null)
                throw new Exception("ProcessAlbum: args or options required");

            options.Validate();

            // Overall steps:
            // -look for ".tellah.json" in output folder.
            // -if json file doesn't exist,
            //   -process all input files, while added to "new json" list.
            // -else if json file exists,
            //   -form hashtable where key is base raw file-name (without path).
            //   -loop through (raw) input files
            //     -if exists in hashtable
            //       -compare input file size and date-modified to json entries.
            //         -if different (file was updated),
            //           -process file.
            //     -else doesn't exist in hashtable (new file)
            //       -process file
            //     -add to "new json" list
            //   -loop through json files
            //     -if raw file doesn't exist,
            //       -delete it's target files
            // -write ".tellah.json"
            // -write html files

            // ProcessFile steps:
            // -if image file
            //   -resize to {ImageSize} then {PreviewImageSize} and convert to jpg (pass KeepAspectRatio, upscale = true)
            //   -record filenames, sizes, and any other json metadata in memory
            // -if video file
            //   -convert to mp4, (VideoSize, KeepAspectRatio)
            //     -if no audio, add ffmpeg "-an" switch
            //   -create thumbnail (PreviewImageSize, KeepAspectRatio, VideoPreviewTimeOffsetSeconds) (see "CreateSingleVideoThumbnail")
            //   -record json metadata in memory

            _tellahJsonPath = System.IO.Path.Combine(options.OutputPath, TELLAH_JSON_FILE_NAME);

            TellahJson newJson = new TellahJson();
            options.SetJsonSettings(newJson.Settings);

            if (System.IO.File.Exists(_tellahJsonPath))
            {
                _logger.WriteLine("Checking for new or updated files...");

                TellahJson json = TellahJson.ReadFromFile(_tellahJsonPath);

                // Check to make sure the user entered the correct InputPath and OutputPath combo.
                // (Helps prevent the user from accidentally processing/modifying/deleting the wrong files.)
                if (!options.ForceUpdateInputFolder && !json.IsSameRawAlbumPath(options.InputPath))
                {
                    throw new Exception("Error: input-path does not match \".tellah.json\"'s InputPath (raw album path).\nEither you moved the folder, or you passed the wrong input-path.\nIf the new path is correct, pass \"--force-update-input-folder\" to override this check.");
                }

                newJson.AlbumInfo = json.AlbumInfo;
                // always update InputPath (raw album path) in case it was moved
                newJson.Settings.InputPath = System.IO.Path.GetFullPath(options.InputPath);

                newJson.AlbumInfo.StartDate = DateTime.MaxValue;
                newJson.AlbumInfo.EndDate = DateTime.MinValue;
                foreach (string inputFilePath in System.IO.Directory.EnumerateFiles(options.InputPath))
                {
                    FileInfo fileInfo;
                    try
                    {
                        fileInfo = _fileInfoApi.GetFileInfo(inputFilePath);
                    }
                    catch
                    {
                        _logger.WarnWriteLine($"GetFileInfo failed: {inputFilePath}");
                        continue;
                    }

                    if (fileInfo.IsUnknownFileType)
                    {
                        _logger.WarnWriteLine($"Unknown file type: {inputFilePath}");
                        continue;
                    }

                    if (!fileInfo.IsImageFile && !fileInfo.IsVideoFile)
                        continue;

                    // calculate album StartDate and EndDate based on the time the photo/video was taken
                    if (fileInfo.MediaTakenUtc.HasValue)
                    {
                        if (fileInfo.MediaTakenUtc < newJson.AlbumInfo.StartDate)
                            newJson.AlbumInfo.StartDate = fileInfo.MediaTakenUtc.Value;
                        if (fileInfo.MediaTakenUtc > newJson.AlbumInfo.EndDate)
                            newJson.AlbumInfo.EndDate = fileInfo.MediaTakenUtc.Value;
                    }

                    string inputFile = System.IO.Path.GetFileName(inputFilePath);

                    TellahJsonAlbumItem jsonAlbumItem = json.GetItemForRawFile(inputFile);
                    if (jsonAlbumItem == null)
                    {
                        // file is new
                        try
                        {
                            jsonAlbumItem = ProcessFile(inputFilePath, fileInfo, options);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorWriteLine($"ProcessFile failed for: {inputFilePath}");
                            _logger.ErrorWriteLine(ex.Message);
                            continue;
                        }
                    }
                    else
                    {
                        if (fileInfo.FileSizeBytes != jsonAlbumItem.RawFileSizeBytes ||
                            fileInfo.LastWriteTimeUtc != jsonAlbumItem.RawFileLastWriteTimeUtc)
                        {
                            // file was updated
                            try
                            {
                                jsonAlbumItem = ProcessFile(inputFilePath, fileInfo, options);
                            }
                            catch (Exception ex)
                            {
                                _logger.ErrorWriteLine($"ProcessFile failed for: {inputFilePath}");
                                _logger.ErrorWriteLine(ex.Message);
                                continue;
                            }
                        }
                    }

                    newJson.AddItem(jsonAlbumItem);
                }

                // if a raw file was deleted, delete it's corresponding album files
                _logger.WriteLine("Checking for deleted files...");
                foreach (TellahJsonAlbumItem jsonAlbumItem in json.Items)
                {
                    string rawFilePath = System.IO.Path.Combine(options.InputPath, jsonAlbumItem.RawFileName);
                    if (!System.IO.File.Exists(rawFilePath))
                    {
                        if (!json.TryDeleteFiles(options.OutputPath, jsonAlbumItem))
                        {
                            _logger.WarnWriteLine($"Delete files failed for raw file: {rawFilePath}");
                        }
                    }
                }
            }
            else
            {
                newJson.AlbumInfo.Name = string.IsNullOrEmpty(options.AlbumName) ?
                    PathUtils.GetLastDirectory(options.OutputPath) : options.AlbumName;
                newJson.Settings.InputPath = System.IO.Path.GetFullPath(options.InputPath);

                newJson.AlbumInfo.StartDate = DateTime.MaxValue;
                newJson.AlbumInfo.EndDate = DateTime.MinValue;
                foreach (string inputFilePath in System.IO.Directory.EnumerateFiles(options.InputPath))
                {
                    FileInfo fileInfo;
                    try
                    {
                        fileInfo = _fileInfoApi.GetFileInfo(inputFilePath);
                    }
                    catch
                    {
                        _logger.WarnWriteLine($"GetFileInfo failed: {inputFilePath}");
                        continue;
                    }

                    if (fileInfo.IsUnknownFileType)
                    {
                        _logger.WarnWriteLine($"Unknown file type: {inputFilePath}");
                        continue;
                    }

                    if (!fileInfo.IsImageFile && !fileInfo.IsVideoFile)
                        continue;

                    // set album StartDate and EndDate based on the time the photos/videos were taken
                    if (fileInfo.MediaTakenUtc.HasValue)
                    {
                        if (fileInfo.MediaTakenUtc < newJson.AlbumInfo.StartDate)
                            newJson.AlbumInfo.StartDate = fileInfo.MediaTakenUtc.Value;
                        if (fileInfo.MediaTakenUtc > newJson.AlbumInfo.EndDate)
                            newJson.AlbumInfo.EndDate = fileInfo.MediaTakenUtc.Value;
                    }

                    newJson.AddItem(ProcessFile(inputFilePath, fileInfo, options));
                }
            }

            newJson.SortAlbumItems();

            // make sure state of the album cover is up-to-date
            newJson.ValidateAlbumCover();

            // write updated ".tellah.json" file
            newJson.WriteToFile(_tellahJsonPath);
        }

        private TellahJsonAlbumItem ProcessFile(string inputFilePath, FileInfo fileInfo,
            ProcessAlbumOptions options)
        {
            TellahJsonAlbumItem jsonAlbumItem = new TellahJsonAlbumItem();

            string inputFile = System.IO.Path.GetFileName(inputFilePath);

            if (fileInfo.IsRotated)
                _logger.WarnWriteLine($"{inputFile} is rotated!");

            jsonAlbumItem.RawFileName = inputFile;
            jsonAlbumItem.RawFileSizeBytes = fileInfo.FileSizeBytes;
            jsonAlbumItem.RawFileLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
            jsonAlbumItem.MediaTakenUtc = fileInfo.MediaTakenUtc;

            if (fileInfo.IsVideoFile)
            {
                jsonAlbumItem.ItemType = TellahItemType.Video;

                ConvertVideoToMp4Response convertVideoResponse =
                    _videoApi.ConvertVideoToMp4(new ConvertVideoToMp4Options()
                    {
                        VideoInputFilePath = inputFilePath,
                        VideoOutputPath = options.OutputPath,
                        FileInfo = fileInfo,
                        // TODO: pass KeepMetadata via command-line option
                        KeepMetadata = DefaultValues.KeepMetadata,
                        KeepAspectRatio = options.KeepAspectRatio,
                        VideoSize = options.VideoSize,
                        MaxBitrateMbps = options.MaxVideoBitrateMbps ?? DefaultValues.MaxVideoBitrateMbps,
                    });

                jsonAlbumItem.LargeFileName = convertVideoResponse.Mp4FileName;
                jsonAlbumItem.LargeFileSize = convertVideoResponse.Mp4FileSize;

                CreateVideoThumbnailResponse createVideoThumbnailResponse =
                    _videoApi.CreateVideoThumbnail(new CreateVideoThumbnailOptions()
                    {
                        VideoInputFilePath = inputFilePath,
                        ThumbnailOutputPath = options.OutputPath,
                        FileInfo = fileInfo,
                        KeepAspectRatio = options.KeepAspectRatio,
                        ThumbnailSize = options.PreviewImageSize ?? DefaultValues.PreviewImageSize,
                        TimeOffsetSeconds = options.VideoPreviewTimeOffsetSeconds ?? DefaultValues.VideoPreviewTimeOffsetSeconds,
                    });

                jsonAlbumItem.PreviewFileName = createVideoThumbnailResponse.ThumbnailFileName;
                jsonAlbumItem.PreviewFileSize = createVideoThumbnailResponse.ThumbnailFileSize;

#if DEBUG
                if (fileInfo.MediaTakenSource == MediaTakenSourceField.FileModifyDate &&
                    fileInfo.IsModifiedAfterCreated())
                {
                    _logger.WarnWriteLine($"No MediaCreated and modified after created: {inputFilePath}");
                }
#endif

                if (/*options.KeepMetadata &&*/
                    fileInfo.MediaTakenSource != MediaTakenSourceField.None &&
                    // prevent invalid modified date from becoming the "media created"
                    !(fileInfo.MediaTakenSource == MediaTakenSourceField.FileModifyDate && fileInfo.IsModifiedAfterCreated()))
                {
                    // if new video files don't have a "media created" field, add it
                    string largeFilePath = System.IO.Path.Combine(options.OutputPath, jsonAlbumItem.LargeFileName);
                    FileInfo newFileInfo = _fileInfoApi.GetFileInfo(largeFilePath);
                    // notice that we check for "CreateDate" field for video files
                    // (video files don't typically use the DateTimeOriginal field that images tend to use)
                    if (newFileInfo.MediaTakenSource != MediaTakenSourceField.CreateDate)
                    {
                        _logger.WarnWriteLine($"SetMediaCreated: {largeFilePath}");
                        _fileInfoApi.SetMediaCreated(largeFilePath, fileInfo.MediaTakenUtc.Value);
                    }

                    // don't really care about exif data on the video preview image,
                    // but we'll set "date taken" on it anyway.
                    string previewFilePath = System.IO.Path.Combine(options.OutputPath, jsonAlbumItem.PreviewFileName);
                    newFileInfo = _fileInfoApi.GetFileInfo(previewFilePath);
                    if (newFileInfo.MediaTakenSource != MediaTakenSourceField.DateTimeOriginal)
                    {
                        _logger.WarnWriteLine($"SetDateTaken: {previewFilePath}");
                        _fileInfoApi.SetDateTaken(previewFilePath, fileInfo.MediaTakenUtc.Value.ToLocalTime());
                    }
                }
            }
            else if (fileInfo.IsImageFile)
            {
                jsonAlbumItem.ItemType = TellahItemType.Image;

                using (ImagickImage image = new ImagickImage(_logger, inputFilePath,
                    fileInfo, options.OutputPath,
                    new ImagickOptions {
                        LgWidth = (int)(options.ImageSize ?? DefaultValues.ImageSize).Width,
                        LgHeight = (int)(options.ImageSize ?? DefaultValues.ImageSize).Height,
                        SmWidth = (int)(options.PreviewImageSize ?? DefaultValues.PreviewImageSize).Width,
                        SmHeight = (int)(options.PreviewImageSize ?? DefaultValues.PreviewImageSize).Height,
                        JpegQuality = options.JpegQuality ?? DefaultValues.JpegQuality,
                        // TODO: pass options.KeepAspectRatio (and add support for when false)
                    }))
                {
                    _logger.WriteLine($"Converting image: {System.IO.Path.GetFileName(inputFilePath)}");
                    image.LoadImage();
                    image.CreateResizedImages();

                    ImagickImageInfo largeImageInfo = image.ResizedImages.Where(x => x.SizeType == ImageSizeType.Large).First();
                    ImagickImageInfo previewImageInfo = image.ResizedImages.Where(x => x.SizeType == ImageSizeType.Small).First();

                    jsonAlbumItem.LargeFileName = System.IO.Path.GetFileName(largeImageInfo.DestinationPathAndFile);
                    jsonAlbumItem.LargeFileSize = new Size((uint)largeImageInfo.Width, (uint)largeImageInfo.Height);

                    jsonAlbumItem.PreviewFileName = System.IO.Path.GetFileName(previewImageInfo.DestinationPathAndFile);
                    jsonAlbumItem.PreviewFileSize = new Size((uint)previewImageInfo.Width, (uint)previewImageInfo.Height);
                }

                // for debugging...
                if (fileInfo.MediaTakenSource == MediaTakenSourceField.FileModifyDate &&
                    fileInfo.IsModifiedAfterCreated())
                {
                    _logger.WarnWriteLine($"No DateTaken and modified after created: {inputFilePath}");
                }

                if (/*options.KeepMetadata &&*/
                    fileInfo.MediaTakenSource != MediaTakenSourceField.None &&
                    // prevent invalid modified date from becoming the "date taken"
                    !(fileInfo.MediaTakenSource == MediaTakenSourceField.FileModifyDate && fileInfo.IsModifiedAfterCreated()))
                {
                    // if new files don't have a "date taken" field, add it

                    string largeFilePath = System.IO.Path.Combine(options.OutputPath, jsonAlbumItem.LargeFileName);
                    FileInfo newFileInfo = _fileInfoApi.GetFileInfo(largeFilePath);
                    if (newFileInfo.MediaTakenSource != MediaTakenSourceField.DateTimeOriginal)
                    {
                        _logger.WarnWriteLine($"SetDateTaken: {largeFilePath}");
                        _fileInfoApi.SetDateTaken(largeFilePath, fileInfo.MediaTakenUtc.Value.ToLocalTime());
                    }

                    string previewFilePath = System.IO.Path.Combine(options.OutputPath, jsonAlbumItem.PreviewFileName);
                    newFileInfo = _fileInfoApi.GetFileInfo(previewFilePath);
                    if (newFileInfo.MediaTakenSource != MediaTakenSourceField.DateTimeOriginal)
                    {
                        _logger.WarnWriteLine($"SetDateTaken: {previewFilePath}");
                        _fileInfoApi.SetDateTaken(previewFilePath, fileInfo.MediaTakenUtc.Value.ToLocalTime());
                    }
                }
            }
            else
            {
                throw new Exception("Not an image or video");
            }

            return jsonAlbumItem;
        }
    }

    public class ProcessAlbumOptions
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string AlbumName { get; set; }
        public bool KeepAspectRatio { get; set; } = true;
        public Size? ImageSize { get; set; }
        public Size? VideoSize { get; set; }
        public Size? PreviewImageSize { get; set; }
        public double? VideoPreviewTimeOffsetSeconds { get; set; }
        public double? MaxVideoBitrateMbps { get; set; }
        public bool ForceUpdateInputFolder { get; set; }
        public int? JpegQuality { get; set; }

        public ProcessAlbumOptions()
        {
        }

        public ProcessAlbumOptions(string outputPath, TellahJsonSettings jsonSettings)
        {
            InputPath = jsonSettings.InputPath;
            OutputPath = outputPath;
            KeepAspectRatio = jsonSettings.KeepAspectRatio;
            ImageSize = jsonSettings.ImageSize;
            VideoSize = jsonSettings.VideoSize;
            PreviewImageSize = jsonSettings.PreviewImageSize;
            VideoPreviewTimeOffsetSeconds = jsonSettings.VideoPreviewTimeOffsetSeconds;
            MaxVideoBitrateMbps = jsonSettings.MaxVideoBitrateMbps;
            JpegQuality = jsonSettings.JpegQuality;
        }

        public void Validate()
        {
            if (!PathUtils.IsDirectory(InputPath))
                throw new Exception("InputPath is not a directory");

            if (System.IO.File.Exists(OutputPath))
                throw new Exception("OutputPath is not a directory");

            if (!System.IO.Directory.Exists(OutputPath))
                System.IO.Directory.CreateDirectory(OutputPath);

            // make sure input and output paths are not the same folder
            // convert to absolute paths
            InputPath = System.IO.Path.GetFullPath(InputPath);
            OutputPath = System.IO.Path.GetFullPath(OutputPath);
            if (InputPath == OutputPath)
                throw new Exception("InputPath and OutputPath cannot be the same directory");

            if (ImageSize.HasValue)
            {
                if (ImageSize.Value.Width == 0 || ImageSize.Value.Height == 0)
                    throw new Exception("ImageSize width and height cannot be 0");
            }

            if (VideoSize.HasValue)
            {
                if (VideoSize.Value.Width == 0 || VideoSize.Value.Height == 0)
                    throw new Exception("VideoSize width and height cannot be 0");
            }

            if (PreviewImageSize.HasValue)
            {
                if (PreviewImageSize.Value.Width == 0 || PreviewImageSize.Value.Height == 0)
                    throw new Exception("PreviewImageSize width and height cannot be 0");
            }

            if (VideoPreviewTimeOffsetSeconds.HasValue && VideoPreviewTimeOffsetSeconds < 0.0)
                throw new Exception("VideoPreviewTimeOffsetSeconds must 0 or greater");

            if (MaxVideoBitrateMbps.HasValue && MaxVideoBitrateMbps < 0.0)
                throw new Exception("MaxVideoBitrateMbps must 0 or greater");

            if (JpegQuality.HasValue)
            {
                if (JpegQuality <= 0 || JpegQuality > 100)
                    throw new Exception("JpegQuality must be greater than 0 and max of 100");
            }
        }

        public void SetJsonSettings(TellahJsonSettings jsonSettings)
        {
            jsonSettings.InputPath = InputPath;
            jsonSettings.KeepAspectRatio = KeepAspectRatio;
            jsonSettings.ImageSize = ImageSize;
            jsonSettings.VideoSize = VideoSize;
            jsonSettings.PreviewImageSize = PreviewImageSize;
            jsonSettings.VideoPreviewTimeOffsetSeconds = VideoPreviewTimeOffsetSeconds;
            jsonSettings.MaxVideoBitrateMbps = MaxVideoBitrateMbps;
            jsonSettings.JpegQuality = JpegQuality;
        }
    }
}

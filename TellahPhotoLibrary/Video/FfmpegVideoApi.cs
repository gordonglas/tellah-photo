using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TellahPhotoLibrary.Common;
using TellahPhotoLibrary.File;
using TellahPhotoLibrary.Models;
using TellahPhotoLibrary.Video.Models;

namespace TellahPhotoLibrary.Video
{
    public class FfmpegVideoApi : IVideoApi
    {
        private ILogger _logger;
        private IFileInfoApi _fileInfoApi;

        public FfmpegVideoApi(ILogger logger, IFileInfoApi fileInfoApi)
        {
            _logger = logger;
            _fileInfoApi = fileInfoApi;
        }

        public double GetVideoDurationSeconds(string file)
        {
            Process process = new Process();
            // using ffprobe because exiftool is not accurate for duration for some video formats
            process.StartInfo.FileName = "ffprobe.exe";
            process.StartInfo.Arguments = $"-i \"{file}\" -v error -show_entries format=duration -of csv=p=0";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // check for error
            err = err?.Trim();
            if (!string.IsNullOrEmpty(err))
            {
                throw new Exception(err);
            }

            output = output?.Trim();
            if (output == "")
            {
                throw new Exception("No output");
            }

            double duration;
            try
            {
                // output format: 1.001667
                duration = double.Parse(output);
            }
            catch
            {
                throw new Exception("Invalid duration value: " + output);
            }

            return duration;
        }

        public ConvertVideoToMp4Response ConvertVideoToMp4(ConvertVideoToMp4Options options)
        {
            if (PathUtils.IsDirectory(options.VideoInputFilePath))
                throw new Exception("InputPath is not a file");

            string inputFile = options.VideoInputFilePath;
            File.FileInfo fileInfo = options.FileInfo ?? _fileInfoApi.GetFileInfo(inputFile);

            if (!fileInfo.IsVideoFile)
                throw new Exception($"InputPath is not a video file: {inputFile}");

            _logger.WriteLine($"Converting video to mp4: {Path.GetFileName(inputFile)}");

            // Get output filepath
            string outputFile;
            if (options.VideoOutputPath == null)
            {
                outputFile = Path.Combine(Directory.GetCurrentDirectory(),
                    Path.GetFileNameWithoutExtension(inputFile) + ".mp4");
            }
            else
            {
                if (PathUtils.IsDirectory(options.VideoOutputPath))
                {
                    outputFile = Path.Combine(Path.GetFullPath(options.VideoOutputPath),
                        Path.GetFileNameWithoutExtension(inputFile) + ".mp4");
                }
                else
                {
                    throw new Exception("Can't use a single output file with an input directory.");
                }
            }

            // TODO: could make this a param (and also pass it to ImagickImage), but not using it now.
            bool allowUpscale = false;

            Size? newSize = null;
            Size? size = null;
            if (options.KeepAspectRatio && options.VideoSize.HasValue)
            {
                size = fileInfo.GetDimensions();
                if (size.HasValue)
                {
                    // calculate correct new width/height.
                    // (VideoSize are max size values)

                    /* OLD LOGIC:
                    // first try based on video width
                    Size nSize = new Size();
                    nSize.Width = options.VideoSize.Value.Width;
                    nSize.Height = (uint)((double)nSize.Width / size.Value.Width * size.Value.Height);

                    // if new height is more than requested height, recalculate based on video height
                    if (nSize.Height > options.VideoSize.Value.Height)
                    {
                        nSize.Height = options.VideoSize.Value.Height;
                        nSize.Width = (uint)((double)nSize.Height / size.Value.Height * size.Value.Width);
                    }

                    newSize = nSize;

                    if (!allowUpscale)
                    {
                        // if new size is larger than the original...
                        if (newSize.Value.Width > size.Value.Width &&
                            newSize.Value.Height > size.Value.Height)
                        {
                            // ...keep same size as original
                            newSize = null;
                        }
                    }
                    */

                    if (!allowUpscale)
                    {
                        newSize = Size.Scale(size.Value.Width, size.Value.Height,
                            options.VideoSize.Value.Width, options.VideoSize.Value.Height);
                    }
                }
            }

            double newBitrate = options.MaxBitrateMbps;
            if (fileInfo.AvgBitrate.HasValue && newBitrate > fileInfo.AvgBitrate)
                newBitrate = fileInfo.AvgBitrate.Value;

            StringBuilder args = new StringBuilder();
            // only have ffmpeg emit important errors, and auto-overwrite output file.
            args.Append($"-hide_banner -loglevel fatal -nostats -y ");
            // These are good settings for hosting the file over the web, IMHO.
            // TODO: test quotes around file on linux and mac
            args.Append($"-i \"{inputFile}\" -vcodec h264 -b:v {newBitrate:0.00}M ");
            // if no audio, add ffmpeg "-an" switch.
            // TODO: test with grandpa's old videos that don't have any sound supposedly. Else might need to pass this in for certain files. hmmm.
            if (fileInfo.HasAudio)
            {
                args.Append("-acodec aac -b:a 92k ");
            }
            else
            {
                args.Append("-an ");
            }
            // if we don't specify a new size, ffmpeg uses same size as input file
            if (newSize.HasValue && newSize.Value.Width != size.Value.Width &&
                newSize.Value.Height != size.Value.Height)
            {
                args.Append($"-filter:v scale={newSize.Value.Width}x{newSize.Value.Height} ");
            }
            if (options.KeepMetadata)
            {
                args.Append("-map_metadata 0 ");
            }
            // TODO: test quotes around file on linux and mac
            args.Append($"\"{outputFile}\"");

            Process process = new Process();
            process.StartInfo.FileName = "ffmpeg.exe";
            process.StartInfo.Arguments = args.ToString();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // TODO: async might be better for long-running processes.
            //       See: https://stackoverflow.com/a/29753402
            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // check for error
            err = err?.Trim();
            if (!string.IsNullOrEmpty(err))
                throw new Exception(err);

            // verify file exists
            if (!System.IO.File.Exists(outputFile))
                throw new Exception($"File not created: {outputFile}");

            ConvertVideoToMp4Response response = new ConvertVideoToMp4Response();
            response.Mp4FileName = Path.GetFileName(outputFile);
            // TODO: hopefully exiftool gets ImageWidth and ImageHeight reliably,
            //       else call ffprobe here.
            response.Mp4FileSize = newSize ??
                new Size(fileInfo.ImageWidth.Value, fileInfo.ImageHeight.Value);

            return response;
        }

        public CreateVideoThumbnailResponse CreateVideoThumbnail(CreateVideoThumbnailOptions options)
        {
            if (PathUtils.IsDirectory(options.VideoInputFilePath))
                throw new Exception("InputPath is not a file");

            string inputFile = options.VideoInputFilePath;
            File.FileInfo fileInfo = options.FileInfo ?? _fileInfoApi.GetFileInfo(inputFile);

            if (!fileInfo.IsVideoFile)
                throw new Exception($"InputPath is not a video file: {inputFile}");

            _logger.WriteLine($"Creating video preview image: {Path.GetFileName(inputFile)}");

            // Get output filepath
            string outputFile;
            if (options.ThumbnailOutputPath == null)
            {
                outputFile = Path.Combine(Directory.GetCurrentDirectory(),
                    Path.GetFileNameWithoutExtension(inputFile) + "_tm.jpg");
            }
            else
            {
                if (PathUtils.IsDirectory(options.ThumbnailOutputPath))
                {
                    outputFile = Path.Combine(Path.GetFullPath(options.ThumbnailOutputPath),
                        Path.GetFileNameWithoutExtension(inputFile) + "_tm.jpg");
                }
                else
                {
                    throw new Exception("Can't use a single output file with an input directory.");
                }
            }

            Size? newSize = null;
            if (options.KeepAspectRatio)
            {
                Size? size = fileInfo.GetDimensions();
                if (size.HasValue)
                {
                    // calculate correct new width/height.
                    // (ThumbnailSize are max size values)

                    // first try based on thumbnail width
                    Size nSize = new Size();
                    nSize.Width = options.ThumbnailSize.Width;
                    nSize.Height = (uint)((double)nSize.Width / size.Value.Width * size.Value.Height);

                    // if new height is more than requested height, recalculate based on thumbnail height
                    if (nSize.Height > options.ThumbnailSize.Height)
                    {
                        nSize.Height = options.ThumbnailSize.Height;
                        nSize.Width = (uint)((double)nSize.Height / size.Value.Height * size.Value.Width);
                    }

                    newSize = nSize;
                }
            }

            double timeOffsetSeconds = options.TimeOffsetSeconds;
            // if timeOffsetSeconds exceeds the length of the video, use first frame.
            try
            {
                // calls ffprobe
                double durationSeconds = GetVideoDurationSeconds(inputFile);
                if (timeOffsetSeconds > durationSeconds)
                {
                    timeOffsetSeconds = 0;
                }
                else if (durationSeconds > timeOffsetSeconds)
                {
                    // even if the duration of the video is greater than our thumbnail offset,
                    // if they are very close (within a second?), then thumbnail creation can still fail.
                    // so if they are within a second of each other, then just adjust our thumbnail offset
                    // to be the start of the video.
                    if (durationSeconds - timeOffsetSeconds < 1.0)
                        timeOffsetSeconds = 0;
                }
            }
            catch
            {
            }

            StringBuilder args = new StringBuilder();
            // only have ffmpeg emit important errors, and auto-overwrite output file.
            args.Append($"-hide_banner -loglevel fatal -nostats -y ");
            // TODO: test quotes around file on linux and mac
            args.Append($"-ss {timeOffsetSeconds} -i \"{inputFile}\" -vframes 1 ");
            // if we don't specify a new size, ffmpeg uses same size as input file
            if (newSize.HasValue)
            {
                args.Append($"-s {newSize.Value.Width}x{newSize.Value.Height} ");
            }
            // TODO: test quotes around file on linux and mac
            args.Append($"-filter:v 'yadif' \"{outputFile}\"");

            Process process = new Process();
            process.StartInfo.FileName = "ffmpeg.exe";
            process.StartInfo.Arguments = args.ToString();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // check for error
            err = err?.Trim();
            if (!string.IsNullOrEmpty(err))
                throw new Exception(err);

            // verify file exists
            if (!System.IO.File.Exists(outputFile))
                throw new Exception($"File not created: {outputFile}");

            CreateVideoThumbnailResponse response = new CreateVideoThumbnailResponse();
            response.ThumbnailFileName = Path.GetFileName(outputFile);
            // TODO: hopefully exiftool gets ImageWidth and ImageHeight reliably,
            //       else call ffprobe here.
            response.ThumbnailFileSize = newSize ??
                new Size(fileInfo.ImageWidth.Value, fileInfo.ImageHeight.Value);

            return response;
        }
    }
}

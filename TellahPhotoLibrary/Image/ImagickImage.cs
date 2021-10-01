using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using TellahPhotoLibrary.Common;
using TellahPhotoLibrary.Config;
using TellahPhotoLibrary.Models;

namespace TellahPhotoLibrary.Image
{
    // Magick.NET examples: https://github.com/dlemstra/Magick.NET/blob/master/docs/Readme.md#examples

    public enum ImageSizeType
    {
        Unchanged = 0,
        Original = 1,
        Large = 2,
        Small = 4,
    }

    public class ImagickOptions
    {
        public int LgWidth { get; set; } = (int)DefaultValues.ImageSize.Width;
        public int LgHeight { get; set; } = (int)DefaultValues.ImageSize.Height;
        public int SmWidth { get; set; } = (int)DefaultValues.PreviewImageSize.Width;
        public int SmHeight { get; set; } = (int)DefaultValues.PreviewImageSize.Height;
        public int JpegQuality { get; set; } = DefaultValues.JpegQuality; // up to 100 max
    }

    public class ImagickImageInfo
    {
        public ImageSizeType SizeType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public MagickFormat Format { get; set; }
        public int Quality { get; set; }
        public long ByteLen { get; set; }
        public string DestinationPathAndFile { get; set; }
        //public byte[] Bytes { get; set; }

        public ImagickImageInfo()
        {
        }

        public ImagickImageInfo(ImagickImageInfo src) :
            this(src.SizeType, src.Width, src.Height, src.Format,
                src.Quality, src.ByteLen, src.DestinationPathAndFile)
        {
        }

        public ImagickImageInfo(ImageSizeType sizeType, int width, int height,
            MagickFormat format, int quality, long byteLen, string destinationPathAndFile)
        {
            SizeType = sizeType;
            Width = width;
            Height = height;
            Format = format;
            Quality = quality;
            ByteLen = byteLen;
            DestinationPathAndFile = destinationPathAndFile;
        }

        public static string GetSizeTypeAbbeviation(ImageSizeType sizeType)
        {
            switch (sizeType)
            {
                case ImageSizeType.Large:
                    return "lg";
                case ImageSizeType.Small:
                    return "tm";
                default:
                    return "";
            }
        }
    }

    /// Imagick (Magick.NET) wrapper
    public class ImagickImage : IDisposable
    {
        public ImagickOptions Options { get; set; }

        public ImagickImageInfo OriginalImageInfo { get; set; }
        public ImagickImageInfo CurrentImageInfo { get; set; }

        public MagickImageCollection Images { get; set; }
        public List<ImagickImageInfo> ResizedImages { get; set; }

        private ILogger _logger;
        private string _sourcePathAndFile;
        private File.FileInfo _sourceFileInfo;
        private string _fileNameWithoutExtension;
        private string _destinationPath;

        public ImagickImage(ILogger logger, string sourcePathAndFile,
            File.FileInfo sourceFileInfo,
            string destinationPath = null, ImagickOptions options = null)
        {
            _logger = logger;
            _sourcePathAndFile = sourcePathAndFile;
            _sourceFileInfo = sourceFileInfo;
            _fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePathAndFile);
            _destinationPath = destinationPath ?? Path.GetDirectoryName(sourcePathAndFile);

            if (options != null)
                Options = options;
            else
                Options = new ImagickOptions();

            OriginalImageInfo = new ImagickImageInfo();
            CurrentImageInfo = new ImagickImageInfo();
            ResizedImages = new List<ImagickImageInfo>();
        }

        private void ReadHeaders()
        {
            // this really isn't a great way to do this,
            // because for video files, it tries to run ffmpeg.exe,
            // which I can't seem to get working even when latest
            // ffmpeg.exe is in the bin folder or path env var.
            MagickImageInfo info = new MagickImageInfo(_sourcePathAndFile);

            OriginalImageInfo.Width = info.Width;
            OriginalImageInfo.Height = info.Height;
            OriginalImageInfo.Format = info.Format;
            OriginalImageInfo.Quality = info.Quality;
            OriginalImageInfo.ByteLen = _sourceFileInfo.FileSizeBytes; //new System.IO.FileInfo(_sourcePathAndFile).Length;
            OriginalImageInfo.DestinationPathAndFile = _sourcePathAndFile;

            CurrentImageInfo = new ImagickImageInfo(OriginalImageInfo);

            #region comments
            // can also get more info this way if we need it
            //using (var img = new MagickImage())
            //{
            //    img.Ping(_filePath);
            //}

            /* ...which returns

            ? img
            {Jpeg 1920x1080 8-bit sRGB}
                AnimationDelay: 0
                AnimationIterations: 0
                AnimationTicksPerSecond: 100
                ArtifactNames: {ImageMagick.MagickImage.<get_ArtifactNames>d__49}
                AttributeNames: {ImageMagick.MagickImage.<get_AttributeNames>d__51}
                BackgroundColor: {#FFFFFFFF}
                BaseHeight: 1080
                BaseWidth: 1920
                BlackPointCompensation: false
                BorderColor: {#DFDFDFFF}
                BoundingBox: {x+1920+1080}
                ChannelCount: 0
                Channels: {ImageMagick.MagickImage.<get_Channels>d__70}
                ChromaBluePrimary: {ImageMagick.PrimaryInfo}
                ChromaGreenPrimary: {ImageMagick.PrimaryInfo}
                ChromaRedPrimary: {ImageMagick.PrimaryInfo}
                ChromaWhitePoint: {ImageMagick.PrimaryInfo}
                ClassType: Direct
                ColorFuzz: {0%}
                ColorSpace: sRGB
                ColorType: TrueColor
                ColormapSize: -1
                Comment: null
                Compose: Over
                Compression: JPEG
                Density: {0x0}
                Depth: 8
                EncodingGeometry: null
                Endian: Undefined
                FileName: "D:\\Users\\gglas\\Pictures\\Wallpaper\\earthbound02 - Copy.jpg"
                FilterType: Undefined
                Format: Jpeg
                FormatInfo: {Jpeg: Joint Photographic Experts Group JFIF format (+R+W-M)}
                Gamma: 0.45454543828964233
                GifDisposeMethod: Undefined
                HasAlpha: false
                HasClippingPath: false
                Height: 1080
                Interlace: NoInterlace
                Interpolate: Undefined
                IsDisposed: false
                IsOpaque: true
                Label: null
                MatteColor: {#BDBDBDFF}
                Orientation: Undefined
                Page: {1920x1080}
                ProfileNames: {ImageMagick.MagickImage.<get_ProfileNames>d__162}
                Quality: 100
                RenderingIntent: Perceptual
                Settings: {ImageMagick.MagickReadSettings}
                Signature: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
                TotalColors: 0
                VirtualPixelMethod: Undefined
                Width: 1920
            */
            #endregion
        }

        public bool IsSupportedImageType(bool checkFileExtension = true)
        {
            HashSet<MagickFormat> supportedFormats = new HashSet<MagickFormat>()
            {
                //MagickFormat.WebP,    // like animated gif, but supports streaming
                MagickFormat.Gif,       // possibly an animated gif (GIF89)
                MagickFormat.Gif87,     // not animated gif, single image
                MagickFormat.Jpeg,
                MagickFormat.Jpg,
                MagickFormat.Png,
                //MagickFormat.Png00,
                //MagickFormat.Png24,
                //MagickFormat.Png32,
                //MagickFormat.Png48,
                //MagickFormat.Png64,
                //MagickFormat.Png8,
            };

            if (OriginalImageInfo.Format == MagickFormat.Unknown)
            {
                if (checkFileExtension)
                {
                    string extension = Path.GetExtension(_sourcePathAndFile);
                    if (!IsSupportedFileExtension(extension))
                        return false;
                }

                ReadHeaders();
            }

            if (supportedFormats.Contains(OriginalImageInfo.Format))
                return true;

            return false;
        }

        private static bool IsSupportedFileExtension(string extension)
        {
            switch (extension?.ToLower())
            {
                case ".gif":
                case ".jpg":
                case ".jpeg":
                case ".png":
                    return true;
            }

            return false;
        }

        public void LoadImage()
        {
            Dispose();

            Images = new MagickImageCollection(_sourcePathAndFile);
        }

        public void CreateResizedImages()
        {
            if (Images == null || Images.Count == 0)
                throw new Exception("Image not loaded");

            CurrentImageInfo.SizeType = ImageSizeType.Unchanged;
            
            // imagick isn't aware about rotation exif data?
            if (_sourceFileInfo.IsRotated)
            {
                CurrentImageInfo.Width = Images[0].Height;
                CurrentImageInfo.Height = Images[0].Width;
            }
            else
            {
                CurrentImageInfo.Width = Images[0].Width;
                CurrentImageInfo.Height = Images[0].Height;
            }
            CurrentImageInfo.DestinationPathAndFile = null;

            bool multipleFrames = Images.Count > 1;
            // FYI, there are a lot of issues when resizing animated gifs:
            // https://imagemagick.org/discourse-server/viewtopic.php?f=1&t=7065
            bool animatedGif = multipleFrames && (Images[0].Format == MagickFormat.Gif ||
                Images[0].Format == MagickFormat.Gif87);

            if (animatedGif)
                Images.Coalesce(); // remove animated gif optimizations.
                                   // see: http://www.imagemagick.org/Usage/anim_basics/#coalesce

            if (!multipleFrames)
            {
                // http://www.imagemagick.org/Usage/filter/
                Images[0].FilterType = FilterType.Lanczos2;
                // per http://php.net/manual/en/imagick.resizeimage.php
                // blur factor where > 1 is blurry, < 1 is sharp
                // but in Magick.NET, it is set like so, per:
                // https://github.com/dlemstra/Magick.NET/issues/332
                //Images[0].SetArtifact("filter:blur", "1");
            }

            if (!animatedGif)
            {
                // just convert pretty much everything to jpg
                if (Images[0].Format != MagickFormat.Jpeg &&
                    Images[0].Format != MagickFormat.Jpg)
                {
                    CurrentImageInfo.Format = Images[0].Format = MagickFormat.Jpg;
                    CurrentImageInfo.Quality = Images[0].Quality = Options.JpegQuality;
                }
            }

            //if (Images[0].Format == MagickFormat.Png)
            //{
                // just always convert to jpg..

                //// if Png has transparency,
                //if (Images[0].HasAlpha)
                //{
                //    // TODO: lets try converting to gif89 (which also supports transparency)
                //    throw new Exception("PNG HasAlpha true, but conversion to gif89 not implemented yet.");
                //
                //    // if must use Png, the following works, but is still pretty big size
                //    //if (Image.Format == MagickFormat.Png)
                //    //{
                //    //    Image.SetArtifact("png:compression-level", "9");
                //    //}
                //}
                //else
                //{
                    // Png doesn't have transparency, so convert to jpg
                    //CurrentImageInfo.Format = Images[0].Format = MagickFormat.Jpg;
                    //CurrentImageInfo.Quality = Images[0].Quality = Options.JpegQuality;
                //}
            //}

            // if jpg, set quality
            if ((Images[0].Format == MagickFormat.Jpeg || Images[0].Format == MagickFormat.Jpg)
                && Images[0].Quality > Options.JpegQuality)
            {
                CurrentImageInfo.Quality = Images[0].Quality = Options.JpegQuality;
            }

            // if image is rotated, rotate it to "normal" orientation.
            // Doing this because there's currently a bug with chrome with "object-fit: cover" css on rotated images,
            // which we use in the built album html.
            // See: https://bugs.chromium.org/p/chromium/issues/detail?id=1082669
            // It also makes removing all EXIF data easier for privacy, if needed.
            NormalizeRotation(Images[0]);

            // large
            CurrentImageInfo.SizeType = ImageSizeType.Large;

            if (CurrentImageInfo.Width > Options.LgWidth ||
                CurrentImageInfo.Height > Options.LgHeight)
            {
                // https://github.com/dlemstra/Magick.NET/blob/master/docs/ResizeImage.md

                MagickGeometry geometry = new MagickGeometry(Options.LgWidth, Options.LgHeight);
                geometry.IgnoreAspectRatio = false;

                foreach (var image in Images)
                {
                    image.Resize(geometry);
                }

                CurrentImageInfo.Width = Images[0].Width;
                CurrentImageInfo.Height = Images[0].Height;

                CurrentImageInfo.DestinationPathAndFile = Path.Combine(_destinationPath,
                    $"{_fileNameWithoutExtension}_{ImagickImageInfo.GetSizeTypeAbbeviation(ImageSizeType.Large)}.{GetFileExtensionFromFormat(Images[0].Format)}");

                Images.Write(CurrentImageInfo.DestinationPathAndFile);

                CurrentImageInfo.ByteLen = new System.IO.FileInfo(CurrentImageInfo.DestinationPathAndFile).Length;

                ResizedImages.Add(new ImagickImageInfo(CurrentImageInfo));
            }
            else
            {
                // store current size as large size.
                // we might duplicate the same image size, but thats ok.
                // this way, we will always be able to access all the "different sized" image URLs.
                CurrentImageInfo.DestinationPathAndFile = Path.Combine(_destinationPath,
                    $"{_fileNameWithoutExtension}_{ImagickImageInfo.GetSizeTypeAbbeviation(ImageSizeType.Large)}.{GetFileExtensionFromFormat(Images[0].Format)}");

                Images.Write(CurrentImageInfo.DestinationPathAndFile);

                CurrentImageInfo.ByteLen = new System.IO.FileInfo(CurrentImageInfo.DestinationPathAndFile).Length;

                ResizedImages.Add(new ImagickImageInfo(CurrentImageInfo));
            }

            // small
            CurrentImageInfo.SizeType = ImageSizeType.Small;

            if (animatedGif)
            {
                // use jpg for the thumbnail for animated gifs, so we keep the
                // preview image small.
                CurrentImageInfo.Format = Images[0].Format = MagickFormat.Jpg;
                CurrentImageInfo.Quality = Images[0].Quality = Options.JpegQuality;

                // only need the first image in the gif.
                for (int i = Images.Count - 1; i > 0; i--)
                {
                    Images.RemoveAt(i);
                }
            }

            if (CurrentImageInfo.Width > Options.SmWidth ||
                CurrentImageInfo.Height > Options.SmHeight)
            {
                // https://github.com/dlemstra/Magick.NET/blob/master/docs/ResizeImage.md

                MagickGeometry geometry = new MagickGeometry(Options.SmWidth, Options.SmHeight);
                geometry.IgnoreAspectRatio = false;

                foreach (var image in Images)
                {
                    image.Resize(geometry);
                }

                CurrentImageInfo.Width = Images[0].Width;
                CurrentImageInfo.Height = Images[0].Height;

                CurrentImageInfo.DestinationPathAndFile = Path.Combine(_destinationPath,
                    $"{_fileNameWithoutExtension}_{ImagickImageInfo.GetSizeTypeAbbeviation(ImageSizeType.Small)}.{GetFileExtensionFromFormat(Images[0].Format)}");

                Images.Write(CurrentImageInfo.DestinationPathAndFile);

                CurrentImageInfo.ByteLen = new System.IO.FileInfo(CurrentImageInfo.DestinationPathAndFile).Length;

                ResizedImages.Add(new ImagickImageInfo(CurrentImageInfo));
            }
            else
            {
                // store current size as large size.
                // we might duplicate the same image size, but thats ok.
                // this way, we will always be able to access all the "different sized" image URLs.
                CurrentImageInfo.DestinationPathAndFile = Path.Combine(_destinationPath,
                    $"{_fileNameWithoutExtension}_{ImagickImageInfo.GetSizeTypeAbbeviation(ImageSizeType.Small)}.{GetFileExtensionFromFormat(Images[0].Format)}");

                Images.Write(CurrentImageInfo.DestinationPathAndFile);

                CurrentImageInfo.ByteLen = new System.IO.FileInfo(CurrentImageInfo.DestinationPathAndFile).Length;

                ResizedImages.Add(new ImagickImageInfo(CurrentImageInfo));
            }
        }

        // converted from: https://stackoverflow.com/a/31943940
        private void NormalizeRotation(IMagickImage<byte> image)
        {
            if (image.Orientation != OrientationType.TopLeft && image.Orientation != OrientationType.Undefined)
                _logger.WarnWriteLine("Imagick: normalizing rotation");

            switch (image.Orientation)
            {
                case OrientationType.TopLeft:
                    break;
                case OrientationType.TopRight:
                    image.Flop();
                    break;
                case OrientationType.BottomRight:
                    image.Rotate(180);
                    break;
                case OrientationType.BottomLeft:
                    image.Flop();
                    image.Rotate(180);
                    break;
                case OrientationType.LeftTop:
                    image.Flop();
                    image.Rotate(-90);
                    break;
                case OrientationType.RightTop:
                    image.Rotate(90);
                    break;
                case OrientationType.RightBottom:
                    image.Flop();
                    image.Rotate(90);
                    break;
                case OrientationType.LeftBotom:
                    image.Rotate(-90);
                    break;
                default:
                    break;
            }
            image.Orientation = OrientationType.TopLeft;
        }

        public static string GetFileExtensionFromFormat(MagickFormat format)
        {
            switch (format)
            {
                case MagickFormat.Gif:      // possibly and animated gif (GIF89)
                case MagickFormat.Gif87:    // not animated gif, single image
                    return "gif";
                case MagickFormat.Jpeg:
                case MagickFormat.Jpg:
                    return "jpg";
                case MagickFormat.Png:
                    return "png";
                default:
                    throw new Exception("unsupported image format: " + format);
            }
        }

        public static string GetImageTypeFromFormat(MagickFormat format)
        {
            switch (format)
            {
                case MagickFormat.Gif:      // possibly and animated gif (GIF89)
                    return "Gif";
                case MagickFormat.Gif87:    // not animated gif, single image
                    return "Gif87";
                case MagickFormat.Jpeg:
                    return "Jpeg";
                case MagickFormat.Jpg:
                    return "Jpg";
                case MagickFormat.Png:
                    return "Png";
                default:
                    throw new Exception("unsupported image format: " + format);
            }
        }

        public void Dispose()
        {
            Images?.Dispose();
        }
    }
}

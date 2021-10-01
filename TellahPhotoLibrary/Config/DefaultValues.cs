using System;
using System.Collections.Generic;
using System.Text;
using TellahPhotoLibrary.Models;

namespace TellahPhotoLibrary.Config
{
    public static class DefaultValues
    {
        public const bool KeepMetadata = true;
        public const bool KeepAspectRatio = true;
        public static readonly Size ImageSize = new Size(1920, 1920);      // 1200
        public static readonly Size PreviewImageSize = new Size(640, 640); // 420
        public const int JpegQuality = 70;
        public const double VideoPreviewTimeOffsetSeconds = 0.5;
        //public static readonly Size VideoSize = new Size(960, 960);
        public const double MaxVideoBitrateMbps = 1.5;
    }
}

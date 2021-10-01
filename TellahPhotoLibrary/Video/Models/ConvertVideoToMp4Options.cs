using System;
using System.Collections.Generic;
using System.Text;
using TellahPhotoLibrary.Config;
using TellahPhotoLibrary.Models;

namespace TellahPhotoLibrary.Video.Models
{
    public class ConvertVideoToMp4Options
    {
        public const double DEFAULT_MAX_VIDEO_BITRATE_MBPS = 1.5;

        public string VideoInputFilePath { get; set; }
        public string VideoOutputPath { get; set; }
        public File.FileInfo FileInfo { get; set; }
        public bool KeepMetadata { get; set; } = DefaultValues.KeepMetadata;
        public bool KeepAspectRatio { get; set; } = DefaultValues.KeepAspectRatio;
        // null means use original video size
        public Size? VideoSize { get; set; } = null;
        public double MaxBitrateMbps { get; set; } = DefaultValues.MaxVideoBitrateMbps;
    }
}

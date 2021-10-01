using System;
using System.Collections.Generic;
using System.Text;
using TellahPhotoLibrary.Config;
using TellahPhotoLibrary.Models;

namespace TellahPhotoLibrary.Video.Models
{
    public class CreateVideoThumbnailOptions
    {
        public string VideoInputFilePath { get; set; }
        public string ThumbnailOutputPath { get; set; }
        public File.FileInfo FileInfo { get; set; }
        public bool KeepAspectRatio { get; set; } = DefaultValues.KeepAspectRatio;
        public Size ThumbnailSize { get; set; } = DefaultValues.PreviewImageSize;
        public double TimeOffsetSeconds { get; set; } = DefaultValues.VideoPreviewTimeOffsetSeconds;
    }
}

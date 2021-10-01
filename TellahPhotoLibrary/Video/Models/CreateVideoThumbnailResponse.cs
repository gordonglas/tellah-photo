using System;
using System.Collections.Generic;
using System.Text;
using TellahPhotoLibrary.Models;

namespace TellahPhotoLibrary.Video.Models
{
    public class CreateVideoThumbnailResponse
    {
        public string ThumbnailFileName { get; set; }
        public Size ThumbnailFileSize { get; set; }
    }
}

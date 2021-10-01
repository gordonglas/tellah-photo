using System;
using System.Collections.Generic;
using System.Text;

namespace TellahPhotoLibrary.File
{
    public interface IFileInfoApi
    {
        FileInfo GetFileInfo(string file);
        string[] GetImageDateTimeParseFormats();
        string[] GetVideoDateTimeParseFormats();
        void SetDateTaken(string file, DateTime dateTakenLocal);
        void SetMediaCreated(string file, DateTime mediaCreatedUtc);
    }
}

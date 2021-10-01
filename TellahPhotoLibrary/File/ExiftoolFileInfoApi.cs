using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TellahPhotoLibrary.Common;

namespace TellahPhotoLibrary.File
{
    public class ExiftoolFileInfoApi : IFileInfoApi
    {
        private ILogger _logger;

        public ExiftoolFileInfoApi(ILogger logger)
        {
            _logger = logger;
        }

        public FileInfo GetFileInfo(string file)
        {
            Process process = new Process();
            process.StartInfo.FileName = "exiftool";
            // TODO: test quotes around file on linux and mac
            process.StartInfo.Arguments = $"-j \"{file}\"";
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
                throw new Exception("exiftool: No output");
            }

            FileInfoResponse fileInfoResponse;
            try
            {
                // output format is json array of FileInfoResponse
                List<FileInfoResponse> fileInfos = JsonSerializer.Deserialize<List<FileInfoResponse>>(output);
                if (fileInfos == null || fileInfos.Count == 0)
                    throw new Exception("Invalid json");
                fileInfoResponse = fileInfos[0];
            }
            catch
            {
                throw new Exception("Invalid GetFileInfo output: " + output);
            }

            FileInfo fileInfo = new FileInfo(fileInfoResponse, this);

            try
            {
                System.IO.FileInfo ioFileInfo = new System.IO.FileInfo(file);
                fileInfo.FileSizeBytes = ioFileInfo.Length;
                fileInfo.LastWriteTimeUtc = ioFileInfo.LastWriteTimeUtc;
                fileInfo.CreationTimeUtc = ioFileInfo.CreationTimeUtc;
            }
            catch
            {
                throw new Exception($"System.IO.FileInfo failed: {file}");
            }

            return fileInfo;
        }

        public void SetDateTaken(string file, DateTime dateTakenLocal)
        {
            Process process = new Process();
            // TODO: do we need extension on linux/mac?
            process.StartInfo.FileName = "exiftool";
            // TODO: test quotes around file on linux and mac
            process.StartInfo.Arguments = $"\"-DateTimeOriginal={dateTakenLocal:yyyy:MM:dd HH:mm:ss}\" -overwrite_original \"{file}\"";
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
                if (!err.StartsWith("Warning"))
                    throw new Exception("exiftool: " + err);
            }

            output = output?.Trim();
            // maybe check if output contains "1 image files updated"?
        }

        public void SetMediaCreated(string file, DateTime mediaCreatedUtc)
        {
            Process process = new Process();
            process.StartInfo.FileName = "exiftool";
            // TODO: test quotes around file on linux and mac
            process.StartInfo.Arguments = $"\"-CreateDate={mediaCreatedUtc:yyyy:MM:dd HH:mm:ss}\" -overwrite_original \"{file}\"";
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
                if (!err.StartsWith("Warning"))
                    throw new Exception("exiftool: " + err);
            }

            output = output?.Trim();
            // maybe check if output contains "1 image files updated"?
        }

        private readonly string[] ImageDateTimeParseFormats =
        {
            "yyyy:MM:dd HH:mm:sszzz",
            "yyyy:MM:dd HH:mm:ss",
        };

        public string[] GetImageDateTimeParseFormats()
        {
            return ImageDateTimeParseFormats;
        }

        private readonly string[] VideoDateTimeParseFormats =
        {
            "yyyy:MM:dd HH:mm:ss",
            "yyyy:MM:dd HH:mm:sszzz",
        };

        public string[] GetVideoDateTimeParseFormats()
        {
            return VideoDateTimeParseFormats;
        }
    }
}

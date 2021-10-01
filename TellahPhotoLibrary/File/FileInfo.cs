using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TellahPhotoLibrary.Models;

namespace TellahPhotoLibrary.File
{
    public enum MediaTakenSourceField
    {
        None = 0,
        CreationDate,
        CreateDate,
        TrackCreateDate,
        MediaCreateDate,
        DateTimeOriginal,
        ModifyDate,
        FileModifyDate,
    }

    // generic file-info
    public class FileInfo
    {
        public string MIMEType { get; set; }
        public uint? ImageWidth { get; set; }
        public uint? ImageHeight { get; set; }
        public uint? AudioChannels { get; set; }
        public int? Rotation { get; set; }
        public double? AvgBitrate { get; set; }

        public bool IsUnknownFileType { get; set; }
        public bool IsImageFile { get; set; }
        public bool IsVideoFile { get; set; }
        public bool HasAudio { get; set; }
        public bool IsRotated { get; set; }

        public long FileSizeBytes { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }

        public DateTime? MediaTakenUtc { get; set; }
        public MediaTakenSourceField MediaTakenSource { get; set; }

        public FileInfo()
        {
        }

        public FileInfo(FileInfoResponse info, IFileInfoApi fileInfoApi)
        {
            IsRotated = info.IsRotated();
            if (IsRotated)
            {
                ImageWidth = info.ImageHeight;
                ImageHeight = info.ImageWidth;
            }
            else
            {
                ImageWidth = info.ImageWidth;
                ImageHeight = info.ImageHeight;
            }
            MIMEType = info.MIMEType;
            AudioChannels = info.AudioChannels;
            Rotation = info.Rotation;
            AvgBitrate = info.AvgBitrateMbps();

            IsUnknownFileType = info.IsUnknownFileType();
            IsImageFile = info.IsImageFile();
            IsVideoFile = info.IsVideoFile();
            HasAudio = info.HasAudio();

            MediaTakenUtc = info.GetMediaTakenUtc(fileInfoApi,
                out MediaTakenSourceField mediaTakenSource);
            MediaTakenSource = mediaTakenSource;
        }

        public Size? GetDimensions()
        {
            Size? size = null;

            if (ImageWidth.HasValue && ImageHeight.HasValue)
            {
                size = new Size(ImageWidth.Value, ImageHeight.Value);
            }

            return size;
        }

        public bool IsModifiedAfterCreated()
        {
            return LastWriteTimeUtc > CreationTimeUtc;
        }
    }

    // exiftool specific fields/logic
    public class FileInfoResponse
    {
        public string MIMEType { get; set; }
        public uint? ImageWidth { get; set; }
        public uint? ImageHeight { get; set; }
        public uint? AudioChannels { get; set; }
        
        // rotation can be in a couple different fields. ugh.
        public int? Rotation { get; set; }
        // possible values for Orientation are:
        /*
        Horizontal (normal)
        Mirror horizontal
        Rotate 180
        Mirror vertical
        Mirror horizontal and rotate 270 CW
        Rotate 90 CW
        Mirror horizontal and rotate 90 CW
        Rotate 270 CW
        */
        public string Orientation { get; set; }

        public string AvgBitrate { get; set; }
        public string Error { get; set; }

        // since exif/meta-data date fields can sometimes be local-times or sometimes be utc,
        // we'll json-deserialize them in as strings from exiftool.exe
        // and handle conversions to local/utc DateTimes ourselves.

        // For images:
        public string DateTimeOriginal { get; set; }
        public string CreateDate { get; set; }
        public string ModifyDate { get; set; }

        // For videos:
        public string CreationDate { get; set; }
        //public string CreateDate { get; set; }
        public string TrackCreateDate { get; set; }
        public string MediaCreateDate { get; set; }

        // the final fallback:
        public string FileModifyDate { get; set; }


        // The logic/detection in these functions may need to be tweaked.

        public bool IsUnknownFileType()
        {
            return Error == "Unknown file type";
        }

        public bool IsImageFile()
        {
            return MIMEType?.Trim().ToLower().StartsWith("image/") ?? false;
        }

        public bool IsVideoFile()
        {
            return MIMEType?.Trim().ToLower().StartsWith("video/") ?? false;
        }

        public bool HasAudio()
        {
            return (AudioChannels ?? 0) > 0;
        }

        public bool IsRotated()
        {
            int rotatedDegrees = Rotation ?? 0;
            // if no Rotation exif field, there might be an Orientation string field
            if (Rotation == null && !string.IsNullOrEmpty(Orientation))
            {
                string orientation = Orientation.ToLower();
                int idx = orientation.IndexOf("rotate ");
                if (idx != -1)
                {
                    idx = idx + "rotate ".Length;
                    if (idx < orientation.Length)
                    {
                        string strRot = orientation.Substring(idx);
                        idx = strRot.IndexOf(' ');
                        if (idx != -1)
                            strRot = strRot.Substring(0, idx);

                        int.TryParse(strRot, out rotatedDegrees);
                    }
                }
            }
            return rotatedDegrees == 90 || rotatedDegrees == 270;
        }

        public double? AvgBitrateMbps()
        {
            double? avgBitrateMbps = null;

            if (IsVideoFile() && !string.IsNullOrEmpty(AvgBitrate) &&
                AvgBitrate.Contains(" Mbps"))
            {
                if (double.TryParse(AvgBitrate.Split(' ')[0], out double bitrate))
                    avgBitrateMbps = bitrate;
            }

            return avgBitrateMbps;
        }

        public DateTime? GetMediaTakenUtc(IFileInfoApi fileInfoApi,
            out MediaTakenSourceField mediaTakenSource)
        {
            // For images, this appears to be exiftool's "DateTimeOriginal" json field,
            // which is in local time, so we will convert that to utc.
            // It might be possible this data doesn't always exist, so for images,
            // look for the following fields in this order:
            //   "DateTimeOriginal" (local time), "CreateDate" (local time), "ModifyDate" (local time).
            // For videos:
            //   "CreationDate" (UTC), "CreateDate" (UTC), "TrackCreateDate" (UTC), "MediaCreateDate" (UTC)".
            // For both, fallback finally to: "FileModifyDate" (UTC)

            DateTime? dt = null;
            mediaTakenSource = MediaTakenSourceField.None;
            if (IsVideoFile())
            {
                string[] formats = fileInfoApi.GetVideoDateTimeParseFormats();

                // all date strings for videos appear to be UTC
                
                if (DateTime.TryParseExact(CreationDate, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.CreationDate;
                }
                else if (DateTime.TryParseExact(CreateDate, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.CreateDate;
                }
                else if (DateTime.TryParseExact(TrackCreateDate, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.TrackCreateDate;
                }
                else if (DateTime.TryParseExact(MediaCreateDate, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.MediaCreateDate;
                }
                else if (DateTime.TryParseExact(FileModifyDate, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.FileModifyDate;
                }
            }
            else if (IsImageFile())
            {
                string[] formats = fileInfoApi.GetImageDateTimeParseFormats();

                // all date strings for images appear to be local (not UTC)

                if (DateTime.TryParseExact(DateTimeOriginal, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out DateTime outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.DateTimeOriginal;
                }
                else if (DateTime.TryParseExact(CreateDate, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.CreateDate;
                }
                else if (DateTime.TryParseExact(ModifyDate, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.ModifyDate;
                }
                // FileModifyDate is always UTC (use video format parse search order)
                else if (DateTime.TryParseExact(FileModifyDate, fileInfoApi.GetVideoDateTimeParseFormats(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out outDate))
                {
                    dt = outDate;
                    mediaTakenSource = MediaTakenSourceField.FileModifyDate;
                }
            }

            return dt?.ToUniversalTime();
        }
    }
}

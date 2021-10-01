using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace TellahPhotoLibrary.Models
{
    // describes an album folder and it's contents
    public class TellahJson
    {
        public TellahJsonSettings Settings { get; set; }
        public TellahJsonAlbumInfo AlbumInfo { get; set; }
        public List<TellahJsonAlbumItem> Items { get; set; }

        private Dictionary<string, TellahJsonAlbumItem> _hashItems = new Dictionary<string, TellahJsonAlbumItem>();

        public TellahJson()
        {
            Settings = new TellahJsonSettings();
            AlbumInfo = new TellahJsonAlbumInfo();
            Items = new List<TellahJsonAlbumItem>();
        }

        public static TellahJson ReadFromFile(string tellahJsonPath)
        {
            if (!System.IO.File.Exists(tellahJsonPath))
                return null;

            string strJson = System.IO.File.ReadAllText(tellahJsonPath, Encoding.UTF8);
            TellahJson json = JsonSerializer.Deserialize<TellahJson>(strJson);
            foreach (var item in json.Items)
            {
                json._hashItems.Add(item.RawFileName, item);
            }

            return json;
        }

        public void WriteToFile(string tellahJsonPath)
        {
            string json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(tellahJsonPath, json, Encoding.UTF8);
        }

        public void AddItem(TellahJsonAlbumItem item)
        {
            Items.Add(item);
            _hashItems.Add(item.RawFileName, item);
        }

        public TellahJsonAlbumItem GetItemForRawFile(string rawFileName)
        {
            if (_hashItems.ContainsKey(rawFileName))
                return _hashItems[rawFileName];
            return null;
        }

        public bool IsSameRawAlbumPath(string inputPath)
        {
            return System.IO.Path.GetFullPath(inputPath) ==
                System.IO.Path.GetFullPath(Settings.InputPath);
        }

        public bool HasAlbumCover()
        {
            return AlbumInfo.AlbumCoverPreviewFileName != null;
        }

        public bool SetAlbumCoverPreviewImage(string rawFileName = null)
        {
            AlbumInfo.AlbumCoverPreviewFileName = null;
            AlbumInfo.AlbumCoverPreviewFileSize = new Size(0, 0);

            TellahJsonAlbumItem item;

            if (rawFileName == null)
            {
                // set to first item
                if (Items.Count == 0)
                    return false;

                item = Items[0];
            }
            else
            {
                item = GetItemForRawFile(rawFileName);
                if (item == null)
                    return false;
            }

            AlbumInfo.AlbumCoverPreviewFileName = item.PreviewFileName;
            AlbumInfo.AlbumCoverPreviewFileSize = item.PreviewFileSize;
            return true;
        }

        public void ValidateAlbumCover()
        {
            if (HasAlbumCover())
            {
                if (System.IO.File.Exists(AlbumInfo.AlbumCoverPreviewFileName))
                {
                    // find preview file
                    TellahJsonAlbumItem itemFound = null;
                    foreach (TellahJsonAlbumItem item in Items)
                    {
                        if (item.PreviewFileName == AlbumInfo.AlbumCoverPreviewFileName)
                        {
                            itemFound = item;
                            break;
                        }
                    }

                    if (itemFound == null)
                    {
                        SetAlbumCoverPreviewImage();
                    }
                    else
                    {
                        // make sure size matches in case it was just updated
                        AlbumInfo.AlbumCoverPreviewFileSize = itemFound.PreviewFileSize;
                    }
                }
                else
                {
                    // image no longer exists, so set to first preview image
                    SetAlbumCoverPreviewImage();
                }
            }
            else
            {
                // if no album cover, set it to first preview image
                SetAlbumCoverPreviewImage();
            }
        }

        public bool TryDeleteFiles(string outputPath, TellahJsonAlbumItem albumItem)
        {
            bool success = true;
            try
            {
                System.IO.File.Delete(System.IO.Path.Combine(outputPath, albumItem.LargeFileName));
            }
            catch
            {
                success = false;
            }

            try
            {
                // if this was the album cover, reset album cover to first image
                if (AlbumInfo.AlbumCoverPreviewFileName == albumItem.PreviewFileName)
                {
                    SetAlbumCoverPreviewImage();
                }

                System.IO.File.Delete(System.IO.Path.Combine(outputPath, albumItem.PreviewFileName));
            }
            catch
            {
                success = false;
            }

            return success;
        }

        public void SortAlbumItems()
        {
            Items.Sort();
        }
    }

    public class TellahJsonSettings
    {
        public string InputPath { get; set; }
        public bool KeepAspectRatio { get; set; }
        public Size? ImageSize { get; set; }
        public Size? VideoSize { get; set; }
        public Size? PreviewImageSize { get; set; }
        public double? VideoPreviewTimeOffsetSeconds { get; set; }
        public double? MaxVideoBitrateMbps { get; set; }
        public int? JpegQuality { get; set; }
    }

    public class TellahJsonAlbumInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string AlbumCoverPreviewFileName { get; set; }
        public Size AlbumCoverPreviewFileSize { get; set; }

        public TellahJsonAlbumInfoOverrides Overrides { get; set; }
    }

    public class TellahJsonAlbumInfoOverrides
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public enum TellahItemType
    {
        Image = 1,
        Video = 2,
    }

    public class TellahJsonAlbumItem : IComparable<TellahJsonAlbumItem>
    {
        public TellahItemType ItemType { get; set; }

        public string RawFileName { get; set; }
        public long RawFileSizeBytes { get; set; }
        public DateTime RawFileLastWriteTimeUtc { get; set; }

        public string LargeFileName { get; set; }
        public Size LargeFileSize { get; set; }
        
        public string PreviewFileName { get; set; }
        public Size PreviewFileSize { get; set; }
        
        public DateTime? MediaTakenUtc { get; set; }
        
        public string Title { get; set; }
        public string Description { get; set; }

        // for sorting album items by MediaTakenUtc
        public int CompareTo(TellahJsonAlbumItem other)
        {
            DateTime sortDate = MediaTakenUtc ?? RawFileLastWriteTimeUtc;
            DateTime otherSortDate = other.MediaTakenUtc ?? other.RawFileLastWriteTimeUtc;

            return sortDate.CompareTo(otherSortDate);
        }
    }
}

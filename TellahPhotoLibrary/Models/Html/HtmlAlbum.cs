using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TellahPhotoLibrary.Models.Html
{
    public class HtmlAlbum
    {
        public string AlbumIndexUrlPath { get; set; }
        public string AlbumIndexUrl { get; set; }

        public string AlbumName { get; set; }
        public DateTime AlbumStartDate { get; set; }
        public DateTime AlbumEndDate { get; set; }
        public int AlbumItemCount { get; set; }

        public List<HtmlAlbumItem> Items { get; set; }

        public HtmlAlbum()
        {
            Items = new List<HtmlAlbumItem>();
        }

        public string ItemsToJson()
        {
            return JsonSerializer.Serialize(Items);
        }
    }

    public class HtmlAlbumItem
    {
        [JsonPropertyName("ty")]
        public TellahItemType ItemType { get; set; }

        [JsonPropertyName("lgf")]
        public string LargeFileName { get; set; }

        [JsonPropertyName("lgw")]
        public uint LargeFileWidth { get; set; }

        [JsonPropertyName("lgh")]
        public uint LargeFileHeight { get; set; }

        [JsonPropertyName("pf")]
        public string PreviewFileName { get; set; }

        [JsonPropertyName("pfw")]
        public uint PreviewFileWidth { get; set; }

        [JsonPropertyName("pfh")]
        public uint PreviewFileHeight { get; set; }

        [JsonPropertyName("mtu")]
        public DateTime MediaTakenUtc { get; set; }

        [JsonPropertyName("t")]
        public string Title { get; set; }

        [JsonPropertyName("d")]
        public string Description { get; set; }

        public HtmlAlbumItem()
        {
        }

        public HtmlAlbumItem(TellahJsonAlbumItem json)
        {
            ItemType = json.ItemType;
            LargeFileName = json.LargeFileName;
            LargeFileWidth = json.LargeFileSize.Width;
            LargeFileHeight = json.LargeFileSize.Height;
            PreviewFileName = json.PreviewFileName;
            PreviewFileWidth = json.PreviewFileSize.Width;
            PreviewFileHeight = json.PreviewFileSize.Height;
            MediaTakenUtc = json.MediaTakenUtc ?? json.RawFileLastWriteTimeUtc;
            Title = json.Title;
            Description = json.Description;
        }
    }
}

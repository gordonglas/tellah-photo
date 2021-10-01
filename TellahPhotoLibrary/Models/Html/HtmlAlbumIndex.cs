using System;
using System.Collections.Generic;
using System.Text;
using TellahPhotoLibrary.Common;

namespace TellahPhotoLibrary.Models.Html
{
    public enum HtmlAlbumIndexSortBy
    {
        AlbumEndDateDesc = 0,
        AlbumFolderNameAsc = 1,
    }

    public class HtmlAlbumIndex
    {
        public string AlbumIndexTitle { get; set; }
        public List<HtmlAlbumIndexItem> Albums { get; set; }

        public HtmlAlbumIndex()
        {
            Albums = new List<HtmlAlbumIndexItem>();
        }

        public static string GetAlbumHtmlFileName(string prefix, bool isAlbumIndex)
        {
            /*
            if (string.IsNullOrEmpty(prefix))
                throw new Exception("prefix param not set");

            string pathSafePrefix = PathUtils.ReplaceConsecutiveWhitespace(prefix, "-")?.Trim();
            pathSafePrefix = PathUtils.RemoveInvalidFileNameChars(pathSafePrefix)?.Trim();
            if (string.IsNullOrEmpty(pathSafePrefix))
                throw new Exception("Invalid prefix for html file name");

            return $"{pathSafePrefix.ToLowerInvariant()}-album{(isAlbumIndex ? "-index" : "")}.html";
            */

            return isAlbumIndex ? "album-index.html" : "album.html";
        }
    }

    public class HtmlAlbumIndexItem : IComparable<HtmlAlbumIndexItem>
    {
        public string AlbumRelativePath { get; set; }
        public string AlbumFolderName { get; set; }
        // used to generate album's html file name, with path-safe chars
        public string AlbumName { get; set; }
        public string AlbumCoverPreviewFileName { get; set; }
        public Size AlbumCoverPreviewFileSize { get; set; }
        public DateTime AlbumStartDate { get; set; }
        public DateTime AlbumEndDate { get; set; }
        public int AlbumItemCount { get; set; }
        public HtmlAlbumIndexSortBy SortAlbumsBy { get; set; }

        public string GetAlbumHtmlFileName()
        {
            return HtmlAlbumIndex.GetAlbumHtmlFileName(AlbumName, false);
        }

        // Needed so the html pages contain the image heights before
        // masonry.js runs, otherwise masonry doesn't know the height
        // of the grid items and they would overlap on-load.
        public string GetPreviewImageHeight(int maxWidth)
        {
            Size curSize = AlbumCoverPreviewFileSize;

            if (curSize.Width <= maxWidth)
            {
                return curSize.Height + "px";
            }
            else
            {
                // get height at maxWidth
                int newHeight = (int)(curSize.Height * maxWidth / (double)curSize.Width);
                return newHeight + "px";
            }
        }

        public int CompareTo(HtmlAlbumIndexItem other)
        {
            if (SortAlbumsBy == HtmlAlbumIndexSortBy.AlbumEndDateDesc)
            {
                // sort by AlbumEndDate desc, AlbumStartDate desc
                int endDateDesc = other.AlbumEndDate.CompareTo(AlbumEndDate);
                if (endDateDesc != 0)
                    return endDateDesc;
                return other.AlbumStartDate.CompareTo(AlbumStartDate);
            }
            else if (SortAlbumsBy == HtmlAlbumIndexSortBy.AlbumFolderNameAsc)
            {
                // sort by AlbumFolderName desc
                return other.AlbumFolderName.CompareTo(AlbumFolderName);
            }
            else
            {
                throw new Exception("unhandled sort-album-by type");
            }
        }
    }
}

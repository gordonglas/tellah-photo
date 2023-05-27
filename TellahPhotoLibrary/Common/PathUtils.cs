using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TellahPhotoLibrary.Common
{
    public static class PathUtils
    {
        public static bool IsDirectory(string path)
        {
            return (System.IO.File.GetAttributes(path) & FileAttributes.Directory)
                 == FileAttributes.Directory;
        }

        public static string GetLastDirectory(string path)
        {
            return new DirectoryInfo(path).Name;
        }

        public static string ReplaceConsecutiveWhitespace(string input, string replaceWith)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return Regex.Replace(input, @"\s+", replaceWith);
        }

        public static string RemoveInvalidFileNameChars(string proposedFileName)
        {
            if (string.IsNullOrEmpty(proposedFileName))
                return proposedFileName;

            StringBuilder sbFileName = new StringBuilder(proposedFileName);
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                sbFileName.Replace(c.ToString(), "");
            }

            return sbFileName.ToString();
        }

        public static string GetMainExePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            // TODO: if/when upgrading TellahPhotoLibrary from .NET Standard 2.0 to .NET 5, can remove this function and instead use System.IO.Path.GetRelativePath().

            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            relativePath = RemoveTrailingDirectorySeparatorChar(relativePath);

            return relativePath;
        }

        // Handles case when there are multiple files in the same album with the same base name but different extensions.
        // For example: "IMG_0959.jpg" and "IMG_0959.mov". "IMG_0959.mov" will become "IMG_0959_mov.mp4" in this case.
        public static string CalculateFileNameWithoutExtension(string filePath)
        {
            string ext = Path.GetExtension(filePath) ?? "";
            if (ext.Length > 1)
            {
                ext = "_" + ext.Substring(1, ext.Length - 1);
            }
            else
            {
                ext = "";
            }

            return $"{Path.GetFileNameWithoutExtension(filePath)}{ext}";
        }

        private static string RemoveTrailingDirectorySeparatorChar(string relativePath)
        {
            if (relativePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 1);
            }

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (IsDirectory(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
    }
}

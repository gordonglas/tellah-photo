using System;
using System.Collections.Generic;
using System.Text;

namespace TellahPhotoLibrary.Models
{
    public struct Size
    {
        public uint Width { get; set; }
        public uint Height { get; set; }

        public Size(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        public static Size? Parse(string str)
        {
            Size? size;
            try
            {
                string[] tokens = str.Split('x');
                size = new Size(uint.Parse(tokens[0]), uint.Parse(tokens[1]));
            }
            catch
            {
                size = null;
            }

            return size;
        }

        /// <summary>
        /// Calculates new size with max size, keeping aspect ratio. No upscale.
        /// </summary>
        public static Size Scale(uint width, uint height, uint maxWidth, uint maxHeight)
        {
            if (maxWidth == 0 || maxHeight == 0)
                throw new Exception("Size.Scale: maxWidth and maxHeight cannot be 0");

            if (maxWidth >= width && maxHeight >= height)
            {
                // no resize needed
                return new Size(width, height);
            }

            double ratioX = (double)maxWidth / width;
            double ratioY = (double)maxHeight / height;
            double ratio = Math.Min(ratioX, ratioY);

            uint newWidth = (uint)Math.Floor(width * ratio);
            uint newHeight = (uint)Math.Floor(height * ratio);

            return new Size(newWidth, newHeight);
        }
    }
}

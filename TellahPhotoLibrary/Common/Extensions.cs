using System;
using System.Collections.Generic;
using System.Text;

namespace TellahPhotoLibrary.Common
{
    public static class Extensions
    {
        public static string CapitalizeFirstLetter(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            string newStr = char.ToUpperInvariant(str[0]).ToString();
            if (str.Length > 1)
                newStr += str.Substring(1);

            return newStr;
        }
    }
}

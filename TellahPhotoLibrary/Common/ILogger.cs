using System;
using System.Collections.Generic;
using System.Text;

namespace TellahPhotoLibrary.Common
{
    public interface ILogger
    {
        void Write(string msg);
        void WriteLine(string msg);
        void Flush();

        void WarnWrite(string msg);
        void WarnWriteLine(string msg);
        void WarnFlush();

        void ErrorWrite(string err);
        void ErrorWriteLine(string err);
        void ErrorFlush();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace TellahPhotoLibrary.Common
{
    // args that start with 2 dashes are boolean flags.
    // args that start with 1 dash expect 1 value after it,
    // separated by a space.
    public class CommandLine
    {
        private HashSet<string> _flags = new HashSet<string>();
        private Dictionary<string, string> _values = new Dictionary<string, string>();

        public void Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.StartsWith("--"))
                {
                    string name = arg.Substring(2);
                    if (name != "")
                    {
                        _flags.Add(name);
                    }
                    else
                    {
                        throw new Exception("Missing arg name");
                    }

                    continue;
                }

                if (arg.StartsWith("-"))
                {
                    string name = arg.Substring(1);
                    if (name != "")
                    {
                        i++;
                        if (i < args.Length)
                        {
                            _values.Add(name, args[i]);
                        }
                        else
                        {
                            throw new Exception("Missing arg value");
                        }
                    }
                    else
                    {
                        throw new Exception("Missing arg name");
                    }

                    continue;
                }
            }
        }

        public bool HasFlag(string flag)
        {
            return _flags.Contains(flag);
        }

        public bool HasValue(string name)
        {
            return _values.ContainsKey(name);
        }

        public string GetValue(string name)
        {
            if (HasValue(name))
                return _values[name];
            return null;
        }
    }
}

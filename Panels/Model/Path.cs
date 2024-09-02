using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.Model
{
    class Path
    {
        public static string GetCurrentDirectory()
        {
            return System.IO.Directory.GetCurrentDirectory();
        }

        public static string GetFullPath(string path)
        {
            return System.IO.Path.GetFullPath(path);
        }

        public static string GetApplicationDirectory()
        {
            return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
    }
}

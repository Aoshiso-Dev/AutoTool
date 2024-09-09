using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.Model.List.Type
{
    public class ConditionType
    {
        public static readonly string True = "True";
        public static readonly string False = "False";
        public static readonly string ImageExists = "ImageExists";
        public static readonly string ImageNotExists = "ImageNotExists";

        public static IEnumerable<string> GetTypes()
        {
            return new List<string>
            {
                True,
                False,
                ImageExists,
                ImageNotExists
            };
        }
    }
}
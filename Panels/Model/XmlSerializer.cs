using System;
using System.IO;
using System.Runtime.Serialization;

namespace Panels.Model
{
    public class XmlSerializer
    {
        public static void Serialize<T>(T obj, string path)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            var serializer = new DataContractSerializer(typeof(T));
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                serializer.WriteObject(stream, obj);
            }
        }

        public static T? Deserialize<T>(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return default;
            }

            var serializer = new DataContractSerializer(typeof(T));
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                T? deserialized = (T?)serializer.ReadObject(stream);
                return deserialized;
            }
        }
    }
}
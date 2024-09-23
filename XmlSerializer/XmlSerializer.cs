using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlSerializer;

namespace XmlSerializer
{
    public class XmlSerializer
    {
        public static string Serialize<T>(T obj)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, obj);
            return writer.ToString();
        }

        public static T? Deserialize<T>(string xml)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var reader = new System.IO.StringReader(xml);
            return (T?)serializer.Deserialize(reader);
        }

        public static void SerializeToFile<T>(T obj, string path)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var writer = new System.IO.StreamWriter(path);
            serializer.Serialize(writer, obj);
        }

        public static T? DeserializeFromFile<T>(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return default;
            }

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var reader = new System.IO.StreamReader(path);
            return (T?)serializer.Deserialize(reader);
        }
    }
}

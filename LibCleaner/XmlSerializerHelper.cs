using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LibCleaner
{
    public static class XmlSerializerHelper
    {
        #region general methods

        public static void SerializeToFile(string filePath, object value, bool useFormatting = true)
        {
            File.WriteAllText(filePath, Serialize(value, useFormatting));
        }

        public static string Serialize(object value, bool useFormatting = false)
        {
            if (value == null) return null;
            var xmlFormatting = new XmlWriterSettings {OmitXmlDeclaration = true};
            if (useFormatting)
            {
                xmlFormatting.ConformanceLevel = ConformanceLevel.Document;
                xmlFormatting.Indent = true;
                xmlFormatting.NewLineOnAttributes = true;
            }
            var builder = new StringBuilder();
            using (var writer = XmlWriter.Create(builder, xmlFormatting))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                new XmlSerializer(value.GetType()).Serialize(writer, value, ns);
                return builder.ToString();
            }
        }

        public static T DeserializeFile<T>(string fileName) where T : class
        {
            T result = null;
            if (File.Exists(fileName))
                result = Deserialize<T>(File.ReadAllText(fileName));
            return result;
        }

        public static T Deserialize<T>(string str) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(str))
                    return null;
                var reader = new StringReader(str);
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static T Deserialize<T>(byte[] bytes) where T : class
        {
            try
            {
                if (bytes == null || bytes.Length == 0)
                    return null;
                return Deserialize<T>(Encoding.UTF8.GetString(bytes));
            }
            catch
            {
                return null;
            }
        }

        #endregion general methods

        #region object extensions

        public static string ToXml(this object obj, bool format = false)
        {
            return obj == null ? null : Serialize(obj, format);
        }

        public static T ConvertTo<T>(this object item) where T : class
        {
            return Deserialize<T>(Serialize(item));
        }

        #endregion object extensions
    }
}
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Fb2Kindle
{
    public static class XmlSerializerHelper
    {
        #region general methods

        public static T DeserialzeUTF8<T>(string serializedXmlString)
        {
            T result;
            var serializer = new XmlSerializer(typeof(T));
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(serializedXmlString)))
            {
                result = (T)serializer.Deserialize(memStream);
            }
            return result;
        }

        public static void WriteObjectToFile(string filePath, object value, bool useFormatting = false)
        {
            var fileData = WriteObjectToString(value, useFormatting);
            File.WriteAllText(filePath, fileData);
        }

        public static string WriteObjectToString(object value, bool useFormatting = false)
        {
            if (value == null) return null;
            var builder = new StringBuilder();
            var xmlFormatting = new XmlWriterSettings {OmitXmlDeclaration = true};
            if (useFormatting)
            {
                xmlFormatting.ConformanceLevel = ConformanceLevel.Document;
                xmlFormatting.Indent = true;
                xmlFormatting.NewLineOnAttributes = true;
            }
            using (var writer = XmlWriter.Create(builder, xmlFormatting))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                new XmlSerializer(value.GetType()).Serialize(writer, value, ns);
                return builder.ToString();
            }
//            var writer = new StringWriter();
//            var serializer = new XmlSerializer(value.GetType());
//            serializer.Serialize(writer, value);
//            return writer.ToString();
        }

        public static T ReadObjectFromFile<T>(string fileName) where T : class
        {
            try
            {
                if (!File.Exists(fileName))
                    return null;
                var fileData = File.ReadAllText(fileName);
                return ReadObjectFromString<T>(fileData);
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        public static T ReadObjectFromString<T>(string str) where T : class
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

        #endregion general methods

        #region attributes methods

        public static XmlNode SaveString(XmlNode node, string name, string value)
        {
            var attr = node.OwnerDocument.CreateAttribute(name);
            node.Attributes.Append(attr).Value = value;
            return node;
        }

        public static XmlNode SaveInt(XmlNode node, string name, int value)
        {
            var attr = node.OwnerDocument.CreateAttribute(name);
            node.Attributes.Append(attr).Value = value.ToString();
            return node;
        }

        public static string LoadString(XmlNode node, string name, string def)
        {
            try
            {
                if (node == null || node.Attributes == null)
                    return def;
                var attribute = node.Attributes[name];
                return attribute != null ? attribute.Value : def;
            }
            catch { return def; }
        }

        public static string LoadString(XmlNode node, string name)
        {
            return node.Attributes[name].Value;
        }

        public static double LoadDouble(XmlNode node, string name)
        {
            Double tmp;
            if (Double.TryParse(node.Attributes[name].Value, out tmp))
                return tmp;
            throw new Exception(String.Format("Parse error with attribute {0}", name));
        }

        public static int LoadInt(XmlNode node, string name)
        {
            int tmp;

            if (int.TryParse(node.Attributes[name].Value, out tmp))
                return tmp;
            throw new Exception(String.Format("Parse error with attribute {0}", name));
        }

        public static int LoadInt(XmlNode node, string name, int def)
        {
            try
            {
                if (node == null || node.Attributes == null)
                    return def;
                var attribute = node.Attributes[name];
                return attribute != null ? int.Parse(attribute.Value) : def;
            }
            catch { return def; }
        }

        #endregion attributes methods
    }
}
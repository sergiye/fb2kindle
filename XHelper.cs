using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Fb2Kindle
{
    public sealed class XHelper
    {
        private static readonly List<XName> _xnamesCache = new List<XName>();

        public static XName Name(string name, string ns = "")
        {
            var item = _xnamesCache.Find(f => f.Namespace == ns && f.LocalName == name);
            if (item != null)
                return item;
            item = XNamespace.Get(ns).GetName(name);
            _xnamesCache.Add(item);
            return item;
        }

        public static string Value(IEnumerable<XElement> source)
        {
            foreach (var element in source)
                return element.Value;
            return null;
        }

        public static string AttributeValue(XElement source, XName name)
        {
            return (string) source.Attribute(name);
        }

        public static string AttributeValue(IEnumerable<XElement> source, XName name)
        {
            foreach (var element in source)
                return (string) element.Attribute(name);
            return null;
        }

        public static XElement First(IEnumerable<XElement> source)
        {
            foreach (var item in source)
                return item;
            return null;
        }

        public static int Count(IEnumerable<XElement> source)
        {

            var result = 0;
            foreach (var item in source)
                result++;
            return result;
        }

        public static XAttribute CreateAttribute(XName name, object value)
        {
            return value == null ? null : new XAttribute(name, value);
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
                if (String.IsNullOrEmpty(str))
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
    }
}
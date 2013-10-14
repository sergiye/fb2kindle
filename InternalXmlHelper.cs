using System.Collections.Generic;
using System.Xml.Linq;

namespace Fb2Kindle
{
    public sealed class InternalXmlHelper
    {
        public static XName GetXName(string name, string ns = "")
        {
            return XNamespace.Get(ns).GetName(name);
        }

        public static string get_Value(IEnumerable<XElement> source)
        {
            foreach (var element in source)
                return element.Value;
            return null;
        }

        public static string get_AttributeValue(XElement source, XName name)
        {
            return (string)source.Attribute(name);
        }

        public static string get_AttributeValue(IEnumerable<XElement> source, XName name)
        {
            foreach (var element in source)
                return (string) element.Attribute(name);
            return null;
        }

        public static XAttribute CreateAttribute(XName name, object value)
        {
            return value == null ? null : new XAttribute(name, value);
        }
    }
}
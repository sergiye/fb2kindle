﻿using System.Collections.Generic;
using System.Xml.Linq;

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
    }
}
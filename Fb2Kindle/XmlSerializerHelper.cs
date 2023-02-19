using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Fb2Kindle {

  public static class XmlSerializerHelper {

    #region general methods

    public static string Serialize(object value, bool useFormatting = false) {
      if (value == null) return null;
      var xmlFormatting = new XmlWriterSettings { OmitXmlDeclaration = true };
      if (useFormatting) {
        xmlFormatting.ConformanceLevel = ConformanceLevel.Document;
        xmlFormatting.Indent = true;
        xmlFormatting.NewLineOnAttributes = true;
      }
      var builder = new StringBuilder();
      using (var writer = XmlWriter.Create(builder, xmlFormatting)) {
        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");
        new XmlSerializer(value.GetType()).Serialize(writer, value, ns);
        return builder.ToString();
      }
    }

    public static T Deserialize<T>(string str) where T : class {
      try {
        if (string.IsNullOrEmpty(str))
          return null;
        var reader = new StringReader(str);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(reader);
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
        return null;
      }
    }

    #endregion general methods

    #region customization

    public static void SerializeToFile(string filePath, object value, bool useFormatting = true) {
      File.WriteAllText(filePath, value.ToXml(useFormatting));
    }

    public static T DeserializeFile<T>(string fileName) where T : class {
      T result = null;
      if (File.Exists(fileName))
        result = File.ReadAllText(fileName).FromXml<T>();
      return result;
    }

    public static byte[] SerializeToBytes(object value, bool useFormatting = false) {
      try {
        return value == null ? null : new UTF8Encoding().GetBytes(value.ToXml(useFormatting));
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
        throw;
      }
    }

    public static T Deserialize<T>(byte[] bytes) where T : class {
      try {
        if (bytes == null || bytes.Length == 0)
          return null;
        return Encoding.UTF8.GetString(bytes).FromXml<T>();
      }
      catch {
        return null;
      }
    }

    #endregion customization

    #region object extensions

    public static string ToXml(this object obj, bool format = false) {
      return obj == null ? null : Serialize(obj, format);
    }

    public static T FromXml<T>(this string data) where T : class {
      return Deserialize<T>(data);
    }

    public static void ToXmlFile(this object obj, string fileName, bool format = false) {
      SerializeToFile(fileName, obj, format);
    }

    public static T XmlCopy<T>(this T item) where T : class {
      return item.ToXml().FromXml<T>();
    }

    public static T XmlCopy<T>(this object item) where T : class {
      return item.ToXml().FromXml<T>();
    }

    public static byte[] ToBytes(this object obj, bool format = false) {
      return obj == null ? null : SerializeToBytes(obj, format);
    }

    public static T ConvertTo<T>(this object item) where T : class {
      return item.ToXml().FromXml<T>();
    }

    #endregion object extensions
  }
}
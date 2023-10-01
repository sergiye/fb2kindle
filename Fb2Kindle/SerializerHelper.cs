using System.IO;
using System.Web.Script.Serialization;

namespace Fb2Kindle {
  internal static class SerializerHelper {

    internal static string ToJson(this object value) {
      var serializer = new JavaScriptSerializer();
      return serializer.Serialize(value);
    }

    internal static T FromJson<T>(this string json) {
      var serializer = new JavaScriptSerializer();
      return serializer.Deserialize<T>(json);
    }

    internal static void ToJsonFile(this object value, string filePath) {
      File.WriteAllText(filePath, value.ToJson());
    }

    internal static T ReadJsonFile<T>(string fileName) where T : class {
      T result = null;
      if (File.Exists(fileName))
        result = File.ReadAllText(fileName).FromJson<T>();
      return result;
    }
  }
}
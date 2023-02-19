using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Fb2Kindle {
  internal class XmlEncodeWriter : StringWriter {
    public override Encoding Encoding { get; }

    public XmlEncodeWriter(Encoding encoding) {
      Encoding = encoding;
    }

    public override string ToString() {
      var optimized = Regex.Replace(base.ToString(), @"\s{2,}", " ");
      return optimized;
    }
  }
}
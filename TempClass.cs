using System.Xml.Linq;

namespace Fb2Kindle
{
    public class TempClass
    {
        public XName br;
        public XName firstName;
        public XName h2;
        public XName lastName;
        public XName middleName;

        public XElement Process(XElement avtorbook)
        {
            var element2 = new XElement(h2);
            element2.Add(InternalXmlHelper.get_Value(avtorbook.Elements(lastName)));
            var content = new XElement(br);
            element2.Add(content);
            element2.Add(InternalXmlHelper.get_Value(avtorbook.Elements(firstName)));
            content = new XElement(br);
            element2.Add(content);
            element2.Add(InternalXmlHelper.get_Value(avtorbook.Elements(middleName)));
            content = new XElement(br);
            element2.Add(content);
            return element2;
        }
    }
}
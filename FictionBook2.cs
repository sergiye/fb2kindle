using System;
using System.ComponentModel;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Fb2Kindle
{
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    [XmlRoot(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0", IsNullable = false)]
    public class FictionBook
    {
        [XmlElement("binary")] public FictionBookBinary[] binary;
        [XmlElement("body")] public FictionBookBody[] body;
        public FictionBookDescription description;
        [XmlElement("stylesheet")] public FictionBookStylesheet[] stylesheet;
    }

    public class FictionBookStylesheet
    {
        [XmlText] public string Value;
        [XmlAttribute] public string type;
    }

    public class dateType
    {
        [XmlText] public string Value;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        [XmlAttribute(DataType = "date")] public DateTime value;
        [XmlIgnore] public bool valueSpecified;
    }

    public class sequenceType
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        [XmlAttribute] public string name;
        [XmlAttribute(DataType = "integer")] public string number;
        [XmlElement("sequence")] public sequenceType[] sequence;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class tableType
    {
        [XmlElement("tr")] public tableTypeTR[] tr;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class tableTypeTR
    {
        [XmlAttribute] [DefaultValue(alignType.left)] public alignType align;
        [XmlElement("td")] public pType[] td;
        public tableTypeTR()
        {
            align = alignType.left;
        }
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class pType : styleType
    {
        [XmlAttribute(DataType = "ID")] public string id;
        [XmlAttribute] public string style;
    }

    [XmlInclude(typeof (pType))]
    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class styleType
    {
        [XmlElement("a", typeof (linkType))] [XmlElement("emphasis", typeof (styleType))] [XmlElement("image", typeof (imageType))] [XmlElement("strong", typeof (styleType))] [XmlElement("style", typeof (namedStyleType))] [XmlChoiceIdentifier("ItemsElementName")] public object[] Items;
        [XmlElement("ItemsElementName")] [XmlIgnore] public ItemsChoiceType4[] ItemsElementName;
        [XmlText] public string[] Text;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class linkType
    {
        [XmlElement("emphasis", typeof (styleLinkType))] [XmlElement("strong", typeof (styleLinkType))] [XmlElement("style", typeof (styleLinkType))] [XmlChoiceIdentifier("ItemsElementName")] public styleLinkType[] Items;
        [XmlElement("ItemsElementName")] [XmlIgnore] public ItemsChoiceType2[] ItemsElementName;
        [XmlText] public string[] Text;
        [XmlAttribute(DataType = "token")] public string type;
        [XmlAttribute(Form = XmlSchemaForm.Qualified)] public string xlinkhref;
        [XmlAttribute(Form = XmlSchemaForm.Qualified)] public string xlinktype;

        public linkType()
        {
            xlinktype = "simple";
        }
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class styleLinkType
    {
        [XmlElement("emphasis", typeof (styleLinkType))] [XmlElement("strong", typeof (styleLinkType))] [XmlElement("style", typeof (styleLinkType))] [XmlChoiceIdentifier("ItemsElementName")] public styleLinkType[] Items;
        [XmlElement("ItemsElementName")] [XmlIgnore] public ItemsChoiceType1[] ItemsElementName;
        [XmlText] public string[] Text;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0", IncludeInSchema = false)]
    public enum ItemsChoiceType1
    {
        emphasis,
        strong,
        style,
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0", IncludeInSchema = false)]
    public enum ItemsChoiceType2
    {
        emphasis,
        strong,
        style,
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class imageType
    {
        [XmlAttribute] public string alt;
        [XmlAttribute(Form = XmlSchemaForm.Qualified)] public string xlinkhref;
        [XmlAttribute(Form = XmlSchemaForm.Qualified)] public string xlinktype;

        public imageType()
        {
            xlinktype = "simple";
        }
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class namedStyleType
    {
        [XmlElement("a", typeof (linkType))] [XmlElement("emphasis", typeof (styleType))] [XmlElement("image", typeof (imageType))] [XmlElement("strong", typeof (styleType))] [XmlElement("style", typeof (namedStyleType))] [XmlChoiceIdentifier("ItemsElementName")] public object[] Items;
        [XmlElement("ItemsElementName")] [XmlIgnore] public ItemsChoiceType3[] ItemsElementName;
        [XmlText] public string[] Text;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        [XmlAttribute(DataType = "token")] public string name;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0", IncludeInSchema = false)]
    public enum ItemsChoiceType3
    {
        a,
        emphasis,
        image,
        strong,
        style,
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0", IncludeInSchema = false)]
    public enum ItemsChoiceType4
    {
        a,
        emphasis,
        image,
        strong,
        style,
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public enum alignType
    {
        left,
        right,
        center,
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class sectionType
    {
        [XmlElement("cite", typeof (citeType), Order = 4)] [XmlElement("empty-line", typeof (object), Order = 4)] [XmlElement("image", typeof (imageType), Order = 4)] [XmlElement("p", typeof (pType), Order = 4)] [XmlElement("poem", typeof (poemType), Order = 4)] [XmlElement("section", typeof (sectionType), Order = 4)] [XmlElement("subtitle", typeof (pType), Order = 4)] [XmlElement("table", typeof (tableType), Order = 4)] [XmlChoiceIdentifier("ItemsElementName")] public object[] Items;
        [XmlElement("ItemsElementName", Order = 5)] [XmlIgnore] public ItemsChoiceType5[] ItemsElementName;
        [XmlElement(Order = 3)] public annotationType annotation;
        [XmlElement("epigraph", Order = 1)] public epigraphType[] epigraph;
        [XmlAttribute(DataType = "ID")] public string id;
        [XmlElement(Order = 2)] public imageType image;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        [XmlElement(Order = 0)] public titleType title;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class titleType
    {
        [XmlElement("empty-line", typeof (object))] [XmlElement("p", typeof (pType))] public object[] Items;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class epigraphType
    {
        [XmlElement("cite", typeof (citeType))] [XmlElement("empty-line", typeof (object))] [XmlElement("p", typeof (pType))] [XmlElement("poem", typeof (poemType))] public object[] Items;
        [XmlAttribute(DataType = "ID")] public string id;
        [XmlElement("text-author")] public textFieldType[] textauthor;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class citeType
    {
        [XmlElement("empty-line", typeof (object))] [XmlElement("p", typeof (pType))] [XmlElement("poem", typeof (poemType))] public object[] Items;
        [XmlAttribute(DataType = "ID")] public string id;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        [XmlElement("text-author")] public textFieldType[] textauthor;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class poemType
    {
        public dateType date;
        [XmlElement("epigraph")] public epigraphType[] epigraph;
        [XmlAttribute(DataType = "ID")] public string id;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        [XmlElement("stanza")] public poemTypeStanza[] stanza;
        [XmlElement("text-author")] public textFieldType[] textauthor;
        public titleType title;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class poemTypeStanza
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        public pType subtitle;
        public titleType title;
        [XmlElement("v")] public pType[] v;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class textFieldType
    {
        [XmlText] public string Value;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class annotationType
    {
        [XmlElement("cite", typeof (citeType))] [XmlElement("empty-line", typeof (object))] [XmlElement("p", typeof (pType))] [XmlElement("poem", typeof (poemType))] public object[] Items;
        [XmlAttribute(DataType = "ID")] public string id;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0", IncludeInSchema = false)]
    public enum ItemsChoiceType5
    {
        cite,
        [XmlEnum("empty-line")] emptyline,
        image,
        p,
        poem,
        section,
        subtitle,
        table,
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class authorType
    {
        [XmlElement("email", typeof (string))] [XmlElement("first-name", typeof (textFieldType))] [XmlElement("home-page", typeof (string))] [XmlElement("last-name", typeof (textFieldType))] [XmlElement("middle-name", typeof (textFieldType))] [XmlElement("nickname", typeof (textFieldType))] [XmlChoiceIdentifier("ItemsElementName")] public object[] Items;
        [XmlElement("ItemsElementName")] [XmlIgnore] public ItemsChoiceType[] ItemsElementName;
    }

    [Serializable]
    [XmlType(Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0", IncludeInSchema = false)]
    public enum ItemsChoiceType
    {
        email,
        [XmlEnum("first-name")] firstname,
        [XmlEnum("home-page")] homepage,
        [XmlEnum("last-name")] lastname,
        [XmlEnum("middle-name")] middlename,
        nickname,
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookDescription
    {
        [XmlElement("custom-info")] public FictionBookDescriptionCustominfo[] custominfo;
        [XmlElement("document-info")] public FictionBookDescriptionDocumentinfo documentinfo;
        [XmlElement("publish-info")] public FictionBookDescriptionPublishinfo publishinfo;
        [XmlElement("title-info")] public FictionBookDescriptionTitleinfo titleinfo;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookDescriptionTitleinfo
    {
        public annotationType annotation;
        [XmlElement("author")] public FictionBookDescriptionTitleinfoAuthor[] author;
        [XmlElement("book-title")] public textFieldType booktitle;
        [XmlArrayItem("image", IsNullable = false)] public imageType[] coverpage;
        public dateType date;
        [XmlElement("genre")] public string[] genre;
        public textFieldType keywords;
        [XmlElement(DataType = "language")] public string lang;
        [XmlElement("sequence")] public sequenceType[] sequence;
        [XmlElement("src-lang", DataType = "language")] public string srclang;
        [XmlElement("translator")] public authorType[] translator;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookDescriptionTitleinfoAuthor : authorType
    {
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookDescriptionDocumentinfo
    {
        [XmlElement("author")] public authorType[] author;
        public dateType date;
        public annotationType history;
        [XmlElement(DataType = "token")] public string id;
        [XmlElement("program-used")] public textFieldType programused;
        [XmlElement("src-ocr")] public textFieldType srcocr;
        [XmlElement("src-url")] public string[] srcurl;
        public float version;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookDescriptionPublishinfo
    {
        [XmlElement("book-name")] public textFieldType bookname;
        public textFieldType city;
        public textFieldType isbn;
        public textFieldType publisher;
        [XmlElement("sequence")] public sequenceType[] sequence;
        [XmlElement(DataType = "gYear")] public string year;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookDescriptionCustominfo : textFieldType
    {
        [XmlAttribute("info-type")] public string infotype;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookBody
    {
        [XmlElement("epigraph")] public epigraphType[] epigraph;
        public imageType image;
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")] public string lang;
        [XmlAttribute] public string name;
        [XmlElement("section")] public sectionType[] section;
        public titleType title;
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0")]
    public class FictionBookBinary
    {
        [XmlText(DataType = "base64Binary")] public byte[] Value;
        [XmlAttribute("content-type")] public string contenttype;
        [XmlAttribute(DataType = "ID")] public string id;
    }
}
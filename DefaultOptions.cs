using System;
using System.Xml.Serialization;

namespace Fb2Kindle
{
    [Serializable]
    public class DefaultOptions
    {
        public DefaultOptions()
        {
            defaultCSS = "styles.css";
            DropCap = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧЩШЭЮЯ";
        }

        public bool deleteOrigin { get; set; }
        public bool noBig { get; set; }
        public bool noChapters { get; set; }
        public bool nh { get; set; }
        public bool noImages { get; set; }
        public bool ntoc { get; set; }
        public bool nstitle { get; set; }
        public bool ntitle0 { get; set; }
        public bool dztitle { get; set; } //del zero title
        public string defaultCSS { get; set; }
        public string DropCap { get; set; }
        public bool ContentOf { get; set; }
        public bool nbox { get; set; } //note box
        [XmlIgnore]public bool save { get; set; } //note box
    }
}

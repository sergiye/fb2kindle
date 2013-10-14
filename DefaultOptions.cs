using System;

namespace Fb2Kindle
{
    [Serializable]
    public class DefaultOptions
    {
        public DefaultOptions()
        {
            d = "True";
            nh = "True";
            defaultCSS = "styles.css";
            DropCap = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧЩШЭЮЯ";
            TabReplace = "";
        }

        public string d { get; set; }
        public bool deleteOrigin { get { return d.Equals("True", StringComparison.OrdinalIgnoreCase); } }

        public string nb { get; set; }
        public string nc { get; set; }
        public string nh { get; set; }
        public string ntoc { get; set; }
        public string nstitle { get; set; }
        public bool nstitleb { get { return nstitle.Equals("True", StringComparison.OrdinalIgnoreCase); } }
        public string ntitle0 { get; set; }
        public string DelZeroTitle { get; set; }
        public string defaultCSS { get; set; }
        public string DropCap { get; set; }
        public string ContentOf { get; set; }
        public string NoteBox { get; set; }
        public bool NoteBoxb { get { return NoteBox.Equals("True", StringComparison.OrdinalIgnoreCase); } }
        public string TabReplace { get; set; }
    }
}

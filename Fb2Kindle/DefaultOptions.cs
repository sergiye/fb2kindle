using System;

namespace Fb2Kindle
{
    [Serializable]
    internal class DefaultOptions
    {
        public bool DeleteOriginal { get; set; }
        public bool Chapters { get; set; }
        public bool NoImages { get; set; }
        public bool NoToc { get; set; }
        public bool Compression { get; set; }
        public bool Sequence { get; set; }
        public bool Grayscaled { get; set; }
        public bool Jpeg { get; set; }
    }
}
using System;

namespace Fb2Kindle
{
    internal enum ConverterCleanupMode
    {
        Full = 0,
        Partial, //keep html files, styles & images
        No //for debug
    }

    [Serializable]
    internal class DefaultOptions
    {
        public bool DeleteOriginal { get; set; }
        public bool NoChapters { get; set; }
        public bool NoImages { get; set; }
        public bool NoToc { get; set; }
        public bool Compression { get; set; }
        public bool Sequence { get; set; }
        public bool Grayscaled { get; set; }
        public bool Jpeg { get; set; }
        public ConverterCleanupMode CleanupMode { get; set; }
        public bool UseSourceAsTempFolder { get; set; }
    }
}
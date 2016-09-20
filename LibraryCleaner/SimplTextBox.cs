using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryCleaner
{
    public class SimplTextBox : RichTextBox
    {
        private const short WmPaint = 0x00f;
        private const int Maxsize = 409600;
        private const int Dropsize = Maxsize / 4;

        private bool _skipPainting;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmPaint && _skipPainting)
                m.Result = IntPtr.Zero;
            else
                base.WndProc(ref m);
        }

        public void AppendLine(string text, Color color, bool lineEnding = true)
        {
            if (Text.Length > Maxsize)
            {
                _skipPainting = true;
                var endmarker = Text.IndexOf('\n', Dropsize) + 1;
                if (endmarker < Dropsize)
                    endmarker = Dropsize;
                Select(0, endmarker);//Select(0, GetFirstCharIndexFromLine(1000));
                var prevReadOnly = ReadOnly;
                ReadOnly = false;
                SelectedText = string.Empty;
                ReadOnly = prevReadOnly;
                _skipPainting = false;
            }
            SelectionStart = Text.Length;
            SelectionLength = 0;
            SelectionColor = color;
            AppendText(text.Trim());
            if (lineEnding)
                AppendText(Environment.NewLine);
            //ScrollToCaret();
        }
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryCleaner
{
    public class SimplTextBox : RichTextBox
    {
        private const short WM_PAINT = 0x00f;
        private const int maxsize = 409600;
        private const int dropsize = maxsize / 4;

        private bool _skipPainting;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PAINT && _skipPainting)
                m.Result = IntPtr.Zero;
            else
                base.WndProc(ref m);
        }

        public void AppendLine(string text, Color color)
        {
            if (Text.Length > maxsize)
            {
                _skipPainting = true;
                var endmarker = Text.IndexOf('\n', dropsize) + 1;
                if (endmarker < dropsize)
                    endmarker = dropsize;
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
            AppendText(text.Trim() + Environment.NewLine);
            //ScrollToCaret();
        }
    }
}
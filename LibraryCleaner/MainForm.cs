using System;
using System.Windows.Forms;
using LibCleaner;

namespace LibraryCleaner
{
    public partial class MainForm : Form
    {
        private readonly Cleaner _cleaner;

        public MainForm()
        {
            InitializeComponent();
        
            _cleaner = new Cleaner(null);
            _cleaner.OnStateChanged += s => AddToLog(s);
        }

        private void AddToLog(string message, bool newLine = true)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, bool>(AddToLog), new object[] { message, newLine });
                return;
            }

            if (newLine)
                message += "\n";
            txtLog.AppendLine(message, txtLog.ForeColor);
            Application.DoEvents();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Filter = "Database file (*.db)|*.db";
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtDatabase.Text = dlg.FileName;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _cleaner.DatabasePath = txtDatabase.Text;

            if (!_cleaner.CheckParameters())
            {
                AddToLog("Please check input parameters and start again!");
                return;
            }
            
            _cleaner.PrepareStatistics();

            if (MessageBox.Show("Start database cleaning?", "Confirmation", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _cleaner.Start();

            AddToLog("Finished!");
        }
    }
}

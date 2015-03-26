using System;
using System.Linq;
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

            clsGenres.Items.Clear();
            var genres = GenresListContainer.GetDefaultItems();
            foreach (var genre in genres)
            {
                clsGenres.Items.Add(genre, genre.Selected);
            }

            cbxRemoveForeign.Checked = _cleaner.RemoveForeign;
            cbxRemoveDeleted.Checked = _cleaner.RemoveDeleted;
        }

        #region GUI helper methods

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

        private void btnAllGenres_Click(object sender, EventArgs e)
        {
            for (var i = 0; i < clsGenres.Items.Count; i++)
                clsGenres.SetItemChecked(i, true);
        }

        private void btnNoneGenres_Click(object sender, EventArgs e)
        {
            for (var i = 0; i < clsGenres.Items.Count; i++)
                clsGenres.SetItemChecked(i, false);
        }

        #endregion GUI helper methods

        private void btnStart_Click(object sender, EventArgs e)
        {
            //get selected genres list
            var genresToRemove = clsGenres.CheckedItems.Cast<Genres>().Select(s => s.Code).ToArray();
            _cleaner.GenresToRemove = genresToRemove;
            _cleaner.DatabasePath = txtDatabase.Text;
            _cleaner.RemoveForeign = cbxRemoveForeign.Checked;
            _cleaner.RemoveDeleted = cbxRemoveDeleted.Checked;

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

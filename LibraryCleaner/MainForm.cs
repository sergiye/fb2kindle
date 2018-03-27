using System;
using System.Diagnostics;
using System.Drawing;
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

            Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);

            _cleaner = new Cleaner(null);
            _cleaner.OnStateChanged += AddToLog;

            clsGenres.Items.Clear();
            var genres = GenresListContainer.GetDefaultItems();
            foreach (var genre in genres)
            {
                clsGenres.Items.Add(genre, genre.Selected);
            }

            cbxRemoveForeign.Checked = _cleaner.RemoveForeign;
            cbxRemoveDeleted.Checked = _cleaner.RemoveDeleted;
            cbxRemoveMissedArchives.Checked = _cleaner.RemoveMissingArchivesFromDb;

            txtDatabase.Text = @"d:\media\library\myrulib_flibusta\myrulib.db";
        }

        #region GUI helper methods

        private void AddToLog(string message, Cleaner.StateKind state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, Cleaner.StateKind>(AddToLog), message, state);
                return;
            }
            txtLog.AppendLine(string.Format("{0:T} - ", DateTime.Now), Color.LightGray, false);
            switch (state)
            {
                case Cleaner.StateKind.Error:
                    txtLog.AppendLine(message, Color.Red);
                    break;
                case Cleaner.StateKind.Warning:
                    txtLog.AppendLine(message, Color.Orange);
                    break;
                case Cleaner.StateKind.Message:
                    txtLog.AppendLine(message, Color.LimeGreen);
                    break;
                default:
                    txtLog.AppendLine(message, txtLog.ForeColor);
                    break;
            }
            txtLog.ScrollToCaret();
            Application.DoEvents();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog {CheckFileExists = true, Filter = "Database file (*.db)|*.db"};
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtDatabase.Text = dlg.FileName;
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtOutput.Text = dlg.SelectedPath;
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

        private void ProcessCleanupTasks(bool analyzeOnly)
        {
            var startedTime = DateTime.Now;
            SetStartedState();
            try
            {
                //get selected genres list
                var genresToRemove = clsGenres.CheckedItems.Cast<Genres>().Select(s => s.Code).ToArray();
                _cleaner.GenresToRemove = genresToRemove;
                _cleaner.DatabasePath = txtDatabase.Text;
                _cleaner.ArchivesOutputPath = txtOutput.Text;
                _cleaner.RemoveForeign = cbxRemoveForeign.Checked;
                _cleaner.RemoveDeleted = cbxRemoveDeleted.Checked;
                _cleaner.RemoveMissingArchivesFromDb = cbxRemoveMissedArchives.Checked;// && !analyzeOnly;

                if (!_cleaner.CheckParameters())
                {
                    AddToLog("Please check input parameters and start again!", Cleaner.StateKind.Warning);
                    SetFinishedState(startedTime);
                    return;
                }

                _cleaner.PrepareStatistics(() =>
                {
                    if (!analyzeOnly && MessageBox.Show("Start database cleaning?", "Confirmation", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _cleaner.Start(() =>
                        {
                            AddToLog("Finished!", Cleaner.StateKind.Log);
                            SetFinishedState(startedTime);
                        });
                    }
                    else
                        SetFinishedState(startedTime);
                });
            }
            catch (Exception ex)
            {
                AddToLog(ex.Message, Cleaner.StateKind.Error);
                SetFinishedState(startedTime);
            }
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            ProcessCleanupTasks(true);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            ProcessCleanupTasks(false);
        }

        private void SetStartedState()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(SetStartedState));
                return;
            }
            btnOptimizeArchives.Enabled = false;
            btnAnalyze.Enabled = false;
            btnStart.Enabled = false;
            Cursor = Cursors.WaitCursor;
        }

        private void SetFinishedState(DateTime startedTime)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DateTime>(SetFinishedState), startedTime);
                return;
            }
            var timeWasted = DateTime.Now - startedTime;
            AddToLog(string.Format("Time wasted: {0:G}", timeWasted), Cleaner.StateKind.Log);
            btnOptimizeArchives.Enabled = true;
            btnAnalyze.Enabled = true;
            btnStart.Enabled = true;
            Cursor = Cursors.Default;
        }

        private void btnOptimizeArchives_Click(object sender, EventArgs e)
        {
            var startedTime = DateTime.Now;
            SetStartedState();
            try
            {
                _cleaner.DatabasePath = txtDatabase.Text;

                if (!_cleaner.CheckParameters())
                {
                    AddToLog("Please check input parameters and start again!", Cleaner.StateKind.Warning);
                    SetFinishedState(startedTime);
                    return;
                }

                _cleaner.OptimizeArchivesByHash(() => SetFinishedState(startedTime));
            }
            catch (Exception ex)
            {
                AddToLog(ex.Message, Cleaner.StateKind.Error);
                SetFinishedState(startedTime);
            }
        }
    }
}

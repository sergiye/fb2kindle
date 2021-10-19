using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryCleaner {
    public partial class MainForm : Form {
        private readonly Cleaner _cleaner;

        public MainForm() {
            InitializeComponent();

            var module = Process.GetCurrentProcess().MainModule;
            if (module != null)
                Icon = Icon.ExtractAssociatedIcon(module.FileName);

            var asm = Assembly.GetExecutingAssembly().GetName();
            var mainTitleText = $"{asm.Name} Version: {asm.Version.ToString(3)}";
            var timer = new Timer {Interval = 1000, Enabled = true};
            timer.Tick += (sender, args) => {
                var dt = DateTime.Now;
                var build = dt.Subtract(new DateTime(2000, 1, 1)).Days;
                var revision = (dt.Second + dt.Minute * 60 + dt.Hour * 60 * 60) / 2;
                Text = $"{mainTitleText}; Now: {build}.{revision}";
            };

            _cleaner = new Cleaner(null);
            _cleaner.OnStateChanged += AddToLog;

            clsGenres.Items.Clear();
            var genres = GenresListContainer.GetDefaultItems();
            foreach (var genre in genres) {
                clsGenres.Items.Add(genre, genre.Selected);
            }

            cbxUpdateHashes.Checked = _cleaner.UpdateHashInfo;
            cbxRemoveDeleted.Checked = _cleaner.RemoveDeleted;
            cbxRemoveMissedArchives.Checked = _cleaner.RemoveMissingArchivesFromDb;

            txtDatabase.Text = @"d:\media\library\myrulib_flibusta\myrulib.db";
        }

        #region GUI helper methods

        private void AddToLog(string message, Cleaner.StateKind state) {
            if (InvokeRequired) {
                Invoke(new Action<string, Cleaner.StateKind>(AddToLog), message, state);
                return;
            }

            txtLog.AppendLine($"{DateTime.Now:T} - ", Color.LightGray, false);
            switch (state) {
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

        private void btnBrowse_Click(object sender, EventArgs e) {
            var dlg = new OpenFileDialog {CheckFileExists = true, Filter = "Database file (*.db)|*.db"};
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtDatabase.Text = dlg.FileName;
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e) {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtOutput.Text = dlg.SelectedPath;
        }

        private void btnDeletedFile_Click(object sender, EventArgs e) {
            var dlg = new OpenFileDialog {
                CheckFileExists = true,
                Filter = "Csv file (*.csv)|*.csv|Text file (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtDeletedFile.Text = dlg.FileName;
        }

        private void btnAllGenres_Click(object sender, EventArgs e) {
            for (var i = 0; i < clsGenres.Items.Count; i++)
                clsGenres.SetItemChecked(i, true);
        }

        private void btnNoneGenres_Click(object sender, EventArgs e) {
            for (var i = 0; i < clsGenres.Items.Count; i++)
                clsGenres.SetItemChecked(i, false);
        }

        #endregion GUI helper methods

        private void ProcessCleanupTasks(bool analyzeOnly) {
            var startedTime = DateTime.Now;
            SetStartedState();
            Task.Factory.StartNew(async () => {
                try {
                    //get selected genres list
                    var genresToRemove = clsGenres.CheckedItems.Cast<Genres>().Select(s => s.Code).ToArray();
                    _cleaner.GenresToRemove = genresToRemove;
                    _cleaner.DatabasePath = txtDatabase.Text;
                    _cleaner.ArchivesOutputPath = txtOutput.Text;
                    _cleaner.UpdateHashInfo = cbxUpdateHashes.Checked;
                    _cleaner.RemoveDeleted = cbxRemoveDeleted.Checked;
                    _cleaner.RemoveNotRegisteredFilesFromZip = cbxRemoveDeleted.Checked;
                    _cleaner.RemoveMissingArchivesFromDb = cbxRemoveMissedArchives.Checked; // && !analyzeOnly;
                    _cleaner.MinFilesToUpdateZip = (int) edtMinFilesToSave.Value;
                    _cleaner.FileWithDeletedBooksIds = txtDeletedFile.Text;

                    if (!await _cleaner.CheckParameters().ConfigureAwait(false)) {
                        AddToLog("Please check input parameters and start again!", Cleaner.StateKind.Warning);
                        SetFinishedState(startedTime);
                        return;
                    }

                    await _cleaner.CalculateStats().ConfigureAwait(false);

                    if (!analyzeOnly) {
                        await _cleaner.CompressLibrary().ConfigureAwait(false);
                        AddToLog("Finished!", Cleaner.StateKind.Log);
                    }
                    
                    SetFinishedState(startedTime);
                }
                catch (Exception ex) {
                    AddToLog(ex.Message, Cleaner.StateKind.Error);
                    SetFinishedState(startedTime);
                }
            });
        }

        private void btnAnalyze_Click(object sender, EventArgs e) {
            ProcessCleanupTasks(true);
        }

        private void btnStart_Click(object sender, EventArgs e) {
            ProcessCleanupTasks(false);
        }

        private void SetStartedState() {
            panSettings.Visible = false;
            btnAnalyze.Enabled = false;
            btnStart.Enabled = false;
            Cursor = Cursors.WaitCursor;
        }

        private void SetFinishedState(DateTime startedTime) {
            if (InvokeRequired) {
                Invoke(new Action<DateTime>(SetFinishedState), startedTime);
                return;
            }

            var timeWasted = DateTime.Now - startedTime;
            AddToLog($"Time wasted: {timeWasted:G}", Cleaner.StateKind.Log);
            panSettings.Visible = true;
            btnAnalyze.Enabled = true;
            btnStart.Enabled = true;
            Cursor = Cursors.Default;
        }

    }
}
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MOBIeditor {
	public partial class MainForm : Form {
		protected MOBIFormat _mobi;
		protected string _currentFilepath = null;

		protected bool _isChanged { get { return Text.EndsWith("*"); } }

		public MainForm() {
			InitializeComponent();
		}

		private void Form1_Shown(object sender, EventArgs e) {
			openMOBIToolStripMenuItem_Click(this, EventArgs.Empty);
		}

		private void openMOBIToolStripMenuItem_Click(object sender, EventArgs e) {
			CheckChanged();
			if (openFileMOBI.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
			OpenMOBIFile(openFileMOBI.FileName);
		}

		private void openNextFileToolStripMenuItem_Click(object sender, EventArgs e) {
			CheckChanged();

			FileInfo[] files = new DirectoryInfo(Path.GetDirectoryName(_currentFilepath)).GetFiles("*.mobi");
			for (int i = 0; i < files.Length; i++) {
				if (files[i].FullName == _currentFilepath && i < files.Length - 1) {
					OpenMOBIFile(files[i + 1].FullName);
					return;
				}
			}

			MessageBox.Show("Couldn't find next file.");
		}

		private void openPreviousFileToolStripMenuItem_Click(object sender, EventArgs e) {
			CheckChanged();

			FileInfo[] files = new DirectoryInfo(Path.GetDirectoryName(_currentFilepath)).GetFiles("*.mobi");
			for (int i = 0; i < files.Length; i++) {
				if (files[i].FullName == _currentFilepath && i > 0) {
					OpenMOBIFile(files[i - 1].FullName);
					return;
				}
			}

			MessageBox.Show("Couldn't find previous file.");
		}
		
		private void OpenMOBIFile(string filename) {
			_currentFilepath = filename;
			_mobi = new MOBIFormat(new FileInfo(filename));

			panel1.Controls.Clear();
			foreach (MOBIFormat.ExthHeaderFormat.ExthHeaderRecordFormat exthRecord in _mobi.ExthHeader.Records.OrderBy(o => o.SortOrder)) {
				EXTHProperty exthPropControl = new EXTHProperty() {
					Name = "exthProperty_" + exthRecord.Id, EXTHRecord = exthRecord, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Width = panel1.Width
				};
				exthPropControl.DeleteProperty += new DeletePropertyHandler(exthPropControl_DeleteProperty);
				panel1.Controls.Add(exthPropControl);
			}
			Text = filename;
			panel1.Refresh();
			Controls[0].Focus();

			saveMOBIToolStripMenuItem.Enabled = saveMOBIAsToolStripMenuItem.Enabled = addPropertyToolStripMenuItem.Enabled = addNewPropertyToolStripMenuItem.Enabled = true;
		}
	
		/// <summary> If any properties have changed gives the eu a chance to save the file. </summary>
		private void CheckChanged() {
			if (!_isChanged) return;
			if (MessageBox.Show("The current MOBI properties have changed. Do you want to save this file before opening a new one?", "File Changed", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
				saveMOBIToolStripMenuItem_Click(null, EventArgs.Empty);
		}

		private void addNewPropertyToolStripMenuItem_Click(object sender, EventArgs e) {
			addPropertyToolStripMenuItem_Click(sender, e);
		}

		private void addPropertyToolStripMenuItem_Click(object sender, EventArgs e) {
			AddEXTHProperty addForm = new AddEXTHProperty();
			if (addForm.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

			MOBIFormat.ExthHeaderFormat.ExthHeaderRecordFormat newRecord = new MOBIFormat.ExthHeaderFormat.ExthHeaderRecordFormat();
			newRecord.Type = (int)((ListViewItem)addForm.comboBoxPropertyType.SelectedItem).Tag;
			switch ((string)addForm.comboBoxValueType.SelectedItem) {
				case "String":
					newRecord.Data = ASCIIEncoding.ASCII.GetBytes("New Property");
					break;
				case "Number":
					newRecord.Data = new byte[] { 0, 0, 0, 0 };
					break;
				default:
					newRecord.Data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
					break;
			}
			_mobi.ExthHeader.Records.Add(newRecord);

			EXTHProperty exthPropControl = new EXTHProperty() {
				Name = "exthProperty_" + newRecord.Id, EXTHRecord = newRecord, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
			};
			exthPropControl.DeleteProperty += new DeletePropertyHandler(exthPropControl_DeleteProperty);
			panel1.Controls.Add(exthPropControl);
			panel1.Refresh();
		}

		void exthPropControl_DeleteProperty(Guid id) {
			panel1.Controls.RemoveByKey("exthProperty_" + id);
			_mobi.ExthHeader.Records.Remove(_mobi.ExthHeader.Records.Single(o => o.Id == id));
			if (!Text.EndsWith("*")) Text += "*";
		}

		private void panel1_Paint(object sender, PaintEventArgs e) {
			if (panel1.Controls.Count == 0) return;

			for (int i = 0; i < panel1.Controls.Count; i++)
				panel1.Controls[i].Top = (i == 0 ? 0 : panel1.Controls[i - 1].Bottom);
		}

		protected bool AnyInvalidControls() {
			Control defaultControl = ActiveControl;
			if (Controls.Count > 0) Controls[1].Focus();
			Controls[0].Focus();
			if (defaultControl != null) defaultControl.Focus();

			foreach (EXTHProperty exthProp in panel1.Controls) if (!exthProp.IsValid) return true;
			return false;
		}

		private void saveMOBIToolStripMenuItem_Click(object sender, EventArgs e) {
			if (AnyInvalidControls()) return;

			if (_currentFilepath == null) {
				saveMOBIAsToolStripMenuItem_Click(sender, EventArgs.Empty);
				return;
			}

			Cursor.Current = Cursors.WaitCursor;
			_mobi.Save(new FileInfo(_currentFilepath));
			Text = Path.GetFileName(_currentFilepath);
			Cursor.Current = Cursors.Default;
		}

		private void saveMOBIAsToolStripMenuItem_Click(object sender, EventArgs e) {
			if (AnyInvalidControls()) return;

			if (saveFileMOBI.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

			Cursor.Current = Cursors.WaitCursor;
			_currentFilepath = saveFileMOBI.FileName;
			_mobi.Save(new FileInfo(_currentFilepath));
			Text = Path.GetFileName(_currentFilepath);
			Cursor.Current = Cursors.Default;
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
			MessageBox.Show("Hello");
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			if (_isChanged)
				if (MessageBox.Show("The current MOBI properties have changed. Do you want to save this file before closing?", "File Changed", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
					saveMOBIToolStripMenuItem_Click(sender, EventArgs.Empty);
		}


	}
}

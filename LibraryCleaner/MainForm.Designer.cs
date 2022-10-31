namespace LibraryCleaner
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
      this.clsGenres = new System.Windows.Forms.CheckedListBox();
      this.panMain = new System.Windows.Forms.Panel();
      this.lblFileWithDeleted = new System.Windows.Forms.Label();
      this.txtDeletedFile = new System.Windows.Forms.TextBox();
      this.btnDeletedFile = new System.Windows.Forms.Button();
      this.lblMinFilesToUpdate = new System.Windows.Forms.Label();
      this.edtMinFilesToSave = new System.Windows.Forms.NumericUpDown();
      this.btnAnalyze = new System.Windows.Forms.Button();
      this.cbxUpdateHashes = new System.Windows.Forms.CheckBox();
      this.cbxRemoveMissedArchives = new System.Windows.Forms.CheckBox();
      this.lblOutput = new System.Windows.Forms.Label();
      this.btnStart = new System.Windows.Forms.Button();
      this.txtOutput = new System.Windows.Forms.TextBox();
      this.cbxRemoveDeleted = new System.Windows.Forms.CheckBox();
      this.btnBrowseOutput = new System.Windows.Forms.Button();
      this.lblDBPath = new System.Windows.Forms.Label();
      this.txtDatabase = new System.Windows.Forms.TextBox();
      this.btnBrowse = new System.Windows.Forms.Button();
      this.btnNoneGenres = new System.Windows.Forms.Button();
      this.btnAllGenres = new System.Windows.Forms.Button();
      this.panLog = new System.Windows.Forms.Panel();
      this.txtLog = new LibraryCleaner.SimplTextBox();
      this.tabControlConfig = new System.Windows.Forms.TabControl();
      this.tabPageMain = new System.Windows.Forms.TabPage();
      this.tabPageGenres = new System.Windows.Forms.TabPage();
      this.panGenresContainer = new System.Windows.Forms.Panel();
      this.panMain.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.edtMinFilesToSave)).BeginInit();
      this.panLog.SuspendLayout();
      this.tabControlConfig.SuspendLayout();
      this.tabPageMain.SuspendLayout();
      this.tabPageGenres.SuspendLayout();
      this.panGenresContainer.SuspendLayout();
      this.SuspendLayout();
      // 
      // clsGenres
      // 
      this.clsGenres.CheckOnClick = true;
      this.clsGenres.Dock = System.Windows.Forms.DockStyle.Fill;
      this.clsGenres.FormattingEnabled = true;
      this.clsGenres.Items.AddRange(new object[] {
            "to be filled on app start..."});
      this.clsGenres.Location = new System.Drawing.Point(3, 40);
      this.clsGenres.Name = "clsGenres";
      this.clsGenres.Size = new System.Drawing.Size(244, 282);
      this.clsGenres.TabIndex = 0;
      // 
      // panMain
      // 
      this.panMain.Controls.Add(this.lblFileWithDeleted);
      this.panMain.Controls.Add(this.txtDeletedFile);
      this.panMain.Controls.Add(this.btnDeletedFile);
      this.panMain.Controls.Add(this.lblMinFilesToUpdate);
      this.panMain.Controls.Add(this.edtMinFilesToSave);
      this.panMain.Controls.Add(this.btnAnalyze);
      this.panMain.Controls.Add(this.cbxUpdateHashes);
      this.panMain.Controls.Add(this.cbxRemoveMissedArchives);
      this.panMain.Controls.Add(this.lblOutput);
      this.panMain.Controls.Add(this.btnStart);
      this.panMain.Controls.Add(this.txtOutput);
      this.panMain.Controls.Add(this.cbxRemoveDeleted);
      this.panMain.Controls.Add(this.btnBrowseOutput);
      this.panMain.Controls.Add(this.lblDBPath);
      this.panMain.Controls.Add(this.txtDatabase);
      this.panMain.Controls.Add(this.btnBrowse);
      this.panMain.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panMain.Location = new System.Drawing.Point(3, 3);
      this.panMain.Name = "panMain";
      this.panMain.Size = new System.Drawing.Size(244, 319);
      this.panMain.TabIndex = 12;
      // 
      // lblFileWithDeleted
      // 
      this.lblFileWithDeleted.AutoSize = true;
      this.lblFileWithDeleted.Location = new System.Drawing.Point(9, 81);
      this.lblFileWithDeleted.Name = "lblFileWithDeleted";
      this.lblFileWithDeleted.Size = new System.Drawing.Size(121, 13);
      this.lblFileWithDeleted.TabIndex = 6;
      this.lblFileWithDeleted.Text = "Use file with deleted IDs";
      // 
      // txtDeletedFile
      // 
      this.txtDeletedFile.Location = new System.Drawing.Point(10, 98);
      this.txtDeletedFile.Name = "txtDeletedFile";
      this.txtDeletedFile.ReadOnly = true;
      this.txtDeletedFile.Size = new System.Drawing.Size(200, 20);
      this.txtDeletedFile.TabIndex = 4;
      this.txtDeletedFile.Text = "lib.md5.txt";
      // 
      // btnDeletedFile
      // 
      this.btnDeletedFile.Location = new System.Drawing.Point(213, 95);
      this.btnDeletedFile.Name = "btnDeletedFile";
      this.btnDeletedFile.Size = new System.Drawing.Size(23, 23);
      this.btnDeletedFile.TabIndex = 5;
      this.btnDeletedFile.Text = "...";
      this.btnDeletedFile.UseVisualStyleBackColor = true;
      this.btnDeletedFile.Click += new System.EventHandler(this.btnDeletedFile_Click);
      // 
      // lblMinFilesToUpdate
      // 
      this.lblMinFilesToUpdate.AutoSize = true;
      this.lblMinFilesToUpdate.Location = new System.Drawing.Point(9, 174);
      this.lblMinFilesToUpdate.Name = "lblMinFilesToUpdate";
      this.lblMinFilesToUpdate.Size = new System.Drawing.Size(135, 13);
      this.lblMinFilesToUpdate.TabIndex = 8;
      this.lblMinFilesToUpdate.Text = "Changes to modify archive:";
      // 
      // edtMinFilesToSave
      // 
      this.edtMinFilesToSave.Location = new System.Drawing.Point(180, 167);
      this.edtMinFilesToSave.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
      this.edtMinFilesToSave.Name = "edtMinFilesToSave";
      this.edtMinFilesToSave.Size = new System.Drawing.Size(56, 20);
      this.edtMinFilesToSave.TabIndex = 9;
      this.edtMinFilesToSave.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
      // 
      // btnAnalyze
      // 
      this.btnAnalyze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnAnalyze.Location = new System.Drawing.Point(10, 291);
      this.btnAnalyze.Name = "btnAnalyze";
      this.btnAnalyze.Size = new System.Drawing.Size(75, 23);
      this.btnAnalyze.TabIndex = 11;
      this.btnAnalyze.Text = "Analyze";
      this.btnAnalyze.UseVisualStyleBackColor = true;
      this.btnAnalyze.Click += new System.EventHandler(this.btnAnalyze_Click);
      // 
      // cbxUpdateHashes
      // 
      this.cbxUpdateHashes.AutoSize = true;
      this.cbxUpdateHashes.Location = new System.Drawing.Point(10, 124);
      this.cbxUpdateHashes.Name = "cbxUpdateHashes";
      this.cbxUpdateHashes.Size = new System.Drawing.Size(137, 17);
      this.cbxUpdateHashes.TabIndex = 6;
      this.cbxUpdateHashes.Text = "Update dates and sizes";
      this.cbxUpdateHashes.UseVisualStyleBackColor = true;
      // 
      // cbxRemoveMissedArchives
      // 
      this.cbxRemoveMissedArchives.AutoSize = true;
      this.cbxRemoveMissedArchives.Checked = true;
      this.cbxRemoveMissedArchives.CheckState = System.Windows.Forms.CheckState.Checked;
      this.cbxRemoveMissedArchives.Location = new System.Drawing.Point(10, 198);
      this.cbxRemoveMissedArchives.Name = "cbxRemoveMissedArchives";
      this.cbxRemoveMissedArchives.Size = new System.Drawing.Size(146, 17);
      this.cbxRemoveMissedArchives.TabIndex = 10;
      this.cbxRemoveMissedArchives.Text = "Remove Missed Archives";
      this.cbxRemoveMissedArchives.UseVisualStyleBackColor = true;
      // 
      // lblOutput
      // 
      this.lblOutput.AutoSize = true;
      this.lblOutput.Location = new System.Drawing.Point(9, 41);
      this.lblOutput.Name = "lblOutput";
      this.lblOutput.Size = new System.Drawing.Size(152, 13);
      this.lblOutput.TabIndex = 3;
      this.lblOutput.Text = "Output (empty if same as input)";
      // 
      // btnStart
      // 
      this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnStart.Location = new System.Drawing.Point(102, 291);
      this.btnStart.Name = "btnStart";
      this.btnStart.Size = new System.Drawing.Size(75, 23);
      this.btnStart.TabIndex = 12;
      this.btnStart.Text = "Start";
      this.btnStart.UseVisualStyleBackColor = true;
      this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
      // 
      // txtOutput
      // 
      this.txtOutput.Location = new System.Drawing.Point(10, 58);
      this.txtOutput.Name = "txtOutput";
      this.txtOutput.ReadOnly = true;
      this.txtOutput.Size = new System.Drawing.Size(200, 20);
      this.txtOutput.TabIndex = 2;
      // 
      // cbxRemoveDeleted
      // 
      this.cbxRemoveDeleted.AutoSize = true;
      this.cbxRemoveDeleted.Location = new System.Drawing.Point(10, 149);
      this.cbxRemoveDeleted.Name = "cbxRemoveDeleted";
      this.cbxRemoveDeleted.Size = new System.Drawing.Size(136, 17);
      this.cbxRemoveDeleted.TabIndex = 7;
      this.cbxRemoveDeleted.Text = "Remove deleted books";
      this.cbxRemoveDeleted.UseVisualStyleBackColor = true;
      // 
      // btnBrowseOutput
      // 
      this.btnBrowseOutput.Location = new System.Drawing.Point(213, 55);
      this.btnBrowseOutput.Name = "btnBrowseOutput";
      this.btnBrowseOutput.Size = new System.Drawing.Size(23, 23);
      this.btnBrowseOutput.TabIndex = 3;
      this.btnBrowseOutput.Text = "...";
      this.btnBrowseOutput.UseVisualStyleBackColor = true;
      this.btnBrowseOutput.Click += new System.EventHandler(this.btnBrowseOutput_Click);
      // 
      // lblDBPath
      // 
      this.lblDBPath.AutoSize = true;
      this.lblDBPath.Location = new System.Drawing.Point(9, 0);
      this.lblDBPath.Name = "lblDBPath";
      this.lblDBPath.Size = new System.Drawing.Size(53, 13);
      this.lblDBPath.TabIndex = 0;
      this.lblDBPath.Text = "Database";
      // 
      // txtDatabase
      // 
      this.txtDatabase.Location = new System.Drawing.Point(10, 17);
      this.txtDatabase.Name = "txtDatabase";
      this.txtDatabase.ReadOnly = true;
      this.txtDatabase.Size = new System.Drawing.Size(200, 20);
      this.txtDatabase.TabIndex = 0;
      // 
      // btnBrowse
      // 
      this.btnBrowse.Location = new System.Drawing.Point(213, 14);
      this.btnBrowse.Name = "btnBrowse";
      this.btnBrowse.Size = new System.Drawing.Size(23, 23);
      this.btnBrowse.TabIndex = 1;
      this.btnBrowse.Text = "...";
      this.btnBrowse.UseVisualStyleBackColor = true;
      this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
      // 
      // btnNoneGenres
      // 
      this.btnNoneGenres.Location = new System.Drawing.Point(29, 7);
      this.btnNoneGenres.Name = "btnNoneGenres";
      this.btnNoneGenres.Size = new System.Drawing.Size(23, 23);
      this.btnNoneGenres.TabIndex = 15;
      this.btnNoneGenres.Text = "-";
      this.btnNoneGenres.UseVisualStyleBackColor = true;
      this.btnNoneGenres.Click += new System.EventHandler(this.btnNoneGenres_Click);
      // 
      // btnAllGenres
      // 
      this.btnAllGenres.Location = new System.Drawing.Point(3, 7);
      this.btnAllGenres.Name = "btnAllGenres";
      this.btnAllGenres.Size = new System.Drawing.Size(23, 23);
      this.btnAllGenres.TabIndex = 14;
      this.btnAllGenres.Text = "+";
      this.btnAllGenres.UseVisualStyleBackColor = true;
      this.btnAllGenres.Click += new System.EventHandler(this.btnAllGenres_Click);
      // 
      // panLog
      // 
      this.panLog.Controls.Add(this.txtLog);
      this.panLog.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panLog.Location = new System.Drawing.Point(258, 0);
      this.panLog.Name = "panLog";
      this.panLog.Size = new System.Drawing.Size(534, 351);
      this.panLog.TabIndex = 1;
      // 
      // txtLog
      // 
      this.txtLog.BackColor = System.Drawing.Color.Black;
      this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
      this.txtLog.ForeColor = System.Drawing.Color.White;
      this.txtLog.Location = new System.Drawing.Point(0, 0);
      this.txtLog.Name = "txtLog";
      this.txtLog.ReadOnly = true;
      this.txtLog.Size = new System.Drawing.Size(534, 351);
      this.txtLog.TabIndex = 0;
      this.txtLog.Text = "";
      // 
      // tabControlConfig
      // 
      this.tabControlConfig.Controls.Add(this.tabPageMain);
      this.tabControlConfig.Controls.Add(this.tabPageGenres);
      this.tabControlConfig.Dock = System.Windows.Forms.DockStyle.Left;
      this.tabControlConfig.Location = new System.Drawing.Point(0, 0);
      this.tabControlConfig.Name = "tabControlConfig";
      this.tabControlConfig.SelectedIndex = 0;
      this.tabControlConfig.Size = new System.Drawing.Size(258, 351);
      this.tabControlConfig.TabIndex = 1;
      // 
      // tabPageMain
      // 
      this.tabPageMain.Controls.Add(this.panMain);
      this.tabPageMain.Location = new System.Drawing.Point(4, 22);
      this.tabPageMain.Name = "tabPageMain";
      this.tabPageMain.Padding = new System.Windows.Forms.Padding(3);
      this.tabPageMain.Size = new System.Drawing.Size(250, 325);
      this.tabPageMain.TabIndex = 0;
      this.tabPageMain.Text = "Settings";
      this.tabPageMain.UseVisualStyleBackColor = true;
      // 
      // tabPageGenres
      // 
      this.tabPageGenres.Controls.Add(this.clsGenres);
      this.tabPageGenres.Controls.Add(this.panGenresContainer);
      this.tabPageGenres.Location = new System.Drawing.Point(4, 22);
      this.tabPageGenres.Name = "tabPageGenres";
      this.tabPageGenres.Padding = new System.Windows.Forms.Padding(3);
      this.tabPageGenres.Size = new System.Drawing.Size(250, 325);
      this.tabPageGenres.TabIndex = 1;
      this.tabPageGenres.Text = "Genres to remove";
      this.tabPageGenres.UseVisualStyleBackColor = true;
      // 
      // panGenresContainer
      // 
      this.panGenresContainer.Controls.Add(this.btnAllGenres);
      this.panGenresContainer.Controls.Add(this.btnNoneGenres);
      this.panGenresContainer.Dock = System.Windows.Forms.DockStyle.Top;
      this.panGenresContainer.Location = new System.Drawing.Point(3, 3);
      this.panGenresContainer.Name = "panGenresContainer";
      this.panGenresContainer.Size = new System.Drawing.Size(244, 37);
      this.panGenresContainer.TabIndex = 0;
      // 
      // MainForm
      // 
      this.AcceptButton = this.btnAnalyze;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(792, 351);
      this.Controls.Add(this.panLog);
      this.Controls.Add(this.tabControlConfig);
      this.MinimumSize = new System.Drawing.Size(800, 300);
      this.Name = "MainForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "Library Cleaner";
      this.panMain.ResumeLayout(false);
      this.panMain.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.edtMinFilesToSave)).EndInit();
      this.panLog.ResumeLayout(false);
      this.tabControlConfig.ResumeLayout(false);
      this.tabPageMain.ResumeLayout(false);
      this.tabPageGenres.ResumeLayout(false);
      this.panGenresContainer.ResumeLayout(false);
      this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panLog;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblDBPath;
        private SimplTextBox txtLog;
        private System.Windows.Forms.CheckedListBox clsGenres;
        private System.Windows.Forms.Button btnNoneGenres;
        private System.Windows.Forms.Button btnAllGenres;
        private System.Windows.Forms.CheckBox cbxUpdateHashes;
        private System.Windows.Forms.CheckBox cbxRemoveDeleted;
        private System.Windows.Forms.CheckBox cbxRemoveMissedArchives;
        private System.Windows.Forms.Panel panMain;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.NumericUpDown edtMinFilesToSave;
        private System.Windows.Forms.Label lblMinFilesToUpdate;
        private System.Windows.Forms.Label lblFileWithDeleted;
        private System.Windows.Forms.TextBox txtDeletedFile;
        private System.Windows.Forms.Button btnDeletedFile;
        private System.Windows.Forms.TabControl tabControlConfig;
        private System.Windows.Forms.TabPage tabPageMain;
        private System.Windows.Forms.TabPage tabPageGenres;
        private System.Windows.Forms.Panel panGenresContainer;
    }
}


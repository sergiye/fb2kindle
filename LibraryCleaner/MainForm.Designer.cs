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
            this.panSettings = new System.Windows.Forms.Panel();
            this.clsGenres = new System.Windows.Forms.CheckedListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblMinFilesToUpdate = new System.Windows.Forms.Label();
            this.edtMinFilesToSave = new System.Windows.Forms.NumericUpDown();
            this.btnAnalyze = new System.Windows.Forms.Button();
            this.cbxRemoveForeign = new System.Windows.Forms.CheckBox();
            this.cbxRemoveMissedArchives = new System.Windows.Forms.CheckBox();
            this.lblOutput = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.cbxRemoveDeleted = new System.Windows.Forms.CheckBox();
            this.btnBrowseOutput = new System.Windows.Forms.Button();
            this.lblDBPath = new System.Windows.Forms.Label();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.btnNoneGenres = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnAllGenres = new System.Windows.Forms.Button();
            this.panLog = new System.Windows.Forms.Panel();
            this.txtLog = new LibraryCleaner.SimplTextBox();
            this.lblFileWithDeleted = new System.Windows.Forms.Label();
            this.txtDeletedFile = new System.Windows.Forms.TextBox();
            this.btnDeletedFile = new System.Windows.Forms.Button();
            this.panSettings.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.edtMinFilesToSave)).BeginInit();
            this.panLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // panSettings
            // 
            this.panSettings.Controls.Add(this.clsGenres);
            this.panSettings.Controls.Add(this.panel2);
            this.panSettings.Dock = System.Windows.Forms.DockStyle.Left;
            this.panSettings.Location = new System.Drawing.Point(0, 0);
            this.panSettings.Name = "panSettings";
            this.panSettings.Size = new System.Drawing.Size(239, 465);
            this.panSettings.TabIndex = 0;
            // 
            // clsGenres
            // 
            this.clsGenres.CheckOnClick = true;
            this.clsGenres.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clsGenres.FormattingEnabled = true;
            this.clsGenres.Items.AddRange(new object[] {
            "firest",
            "second",
            "last"});
            this.clsGenres.Location = new System.Drawing.Point(0, 214);
            this.clsGenres.Name = "clsGenres";
            this.clsGenres.Size = new System.Drawing.Size(239, 251);
            this.clsGenres.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.lblFileWithDeleted);
            this.panel2.Controls.Add(this.txtDeletedFile);
            this.panel2.Controls.Add(this.btnDeletedFile);
            this.panel2.Controls.Add(this.lblMinFilesToUpdate);
            this.panel2.Controls.Add(this.edtMinFilesToSave);
            this.panel2.Controls.Add(this.btnAnalyze);
            this.panel2.Controls.Add(this.cbxRemoveForeign);
            this.panel2.Controls.Add(this.cbxRemoveMissedArchives);
            this.panel2.Controls.Add(this.lblOutput);
            this.panel2.Controls.Add(this.btnStart);
            this.panel2.Controls.Add(this.txtOutput);
            this.panel2.Controls.Add(this.cbxRemoveDeleted);
            this.panel2.Controls.Add(this.btnBrowseOutput);
            this.panel2.Controls.Add(this.lblDBPath);
            this.panel2.Controls.Add(this.txtDatabase);
            this.panel2.Controls.Add(this.btnNoneGenres);
            this.panel2.Controls.Add(this.btnBrowse);
            this.panel2.Controls.Add(this.btnAllGenres);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(239, 214);
            this.panel2.TabIndex = 12;
            // 
            // lblMinFilesToUpdate
            // 
            this.lblMinFilesToUpdate.AutoSize = true;
            this.lblMinFilesToUpdate.Location = new System.Drawing.Point(99, 194);
            this.lblMinFilesToUpdate.Name = "lblMinFilesToUpdate";
            this.lblMinFilesToUpdate.Size = new System.Drawing.Size(75, 13);
            this.lblMinFilesToUpdate.TabIndex = 16;
            this.lblMinFilesToUpdate.Text = "Min files count";
            // 
            // edtMinFilesToSave
            // 
            this.edtMinFilesToSave.Location = new System.Drawing.Point(180, 192);
            this.edtMinFilesToSave.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.edtMinFilesToSave.Name = "edtMinFilesToSave";
            this.edtMinFilesToSave.Size = new System.Drawing.Size(56, 20);
            this.edtMinFilesToSave.TabIndex = 17;
            this.edtMinFilesToSave.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btnAnalyze
            // 
            this.btnAnalyze.Location = new System.Drawing.Point(162, 134);
            this.btnAnalyze.Name = "btnAnalyze";
            this.btnAnalyze.Size = new System.Drawing.Size(75, 23);
            this.btnAnalyze.TabIndex = 11;
            this.btnAnalyze.Text = "Analyze";
            this.btnAnalyze.UseVisualStyleBackColor = true;
            this.btnAnalyze.Click += new System.EventHandler(this.btnAnalyze_Click);
            // 
            // cbxRemoveForeign
            // 
            this.cbxRemoveForeign.AutoSize = true;
            this.cbxRemoveForeign.Checked = true;
            this.cbxRemoveForeign.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxRemoveForeign.Location = new System.Drawing.Point(10, 124);
            this.cbxRemoveForeign.Name = "cbxRemoveForeign";
            this.cbxRemoveForeign.Size = new System.Drawing.Size(133, 17);
            this.cbxRemoveForeign.TabIndex = 9;
            this.cbxRemoveForeign.Text = "Remove foreign books";
            this.cbxRemoveForeign.UseVisualStyleBackColor = true;
            // 
            // cbxRemoveMissedArchives
            // 
            this.cbxRemoveMissedArchives.AutoSize = true;
            this.cbxRemoveMissedArchives.Checked = true;
            this.cbxRemoveMissedArchives.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxRemoveMissedArchives.Location = new System.Drawing.Point(10, 170);
            this.cbxRemoveMissedArchives.Name = "cbxRemoveMissedArchives";
            this.cbxRemoveMissedArchives.Size = new System.Drawing.Size(146, 17);
            this.cbxRemoveMissedArchives.TabIndex = 12;
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
            this.btnStart.Location = new System.Drawing.Point(162, 163);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 13;
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
            this.txtOutput.TabIndex = 4;
            // 
            // cbxRemoveDeleted
            // 
            this.cbxRemoveDeleted.AutoSize = true;
            this.cbxRemoveDeleted.Location = new System.Drawing.Point(10, 147);
            this.cbxRemoveDeleted.Name = "cbxRemoveDeleted";
            this.cbxRemoveDeleted.Size = new System.Drawing.Size(104, 17);
            this.cbxRemoveDeleted.TabIndex = 10;
            this.cbxRemoveDeleted.Text = "Remove deleted";
            this.cbxRemoveDeleted.UseVisualStyleBackColor = true;
            // 
            // btnBrowseOutput
            // 
            this.btnBrowseOutput.Location = new System.Drawing.Point(213, 55);
            this.btnBrowseOutput.Name = "btnBrowseOutput";
            this.btnBrowseOutput.Size = new System.Drawing.Size(23, 23);
            this.btnBrowseOutput.TabIndex = 5;
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
            this.txtDatabase.TabIndex = 1;
            // 
            // btnNoneGenres
            // 
            this.btnNoneGenres.Location = new System.Drawing.Point(36, 189);
            this.btnNoneGenres.Name = "btnNoneGenres";
            this.btnNoneGenres.Size = new System.Drawing.Size(23, 23);
            this.btnNoneGenres.TabIndex = 15;
            this.btnNoneGenres.Text = "-";
            this.btnNoneGenres.UseVisualStyleBackColor = true;
            this.btnNoneGenres.Click += new System.EventHandler(this.btnNoneGenres_Click);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(213, 14);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(23, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnAllGenres
            // 
            this.btnAllGenres.Location = new System.Drawing.Point(10, 189);
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
            this.panLog.Location = new System.Drawing.Point(239, 0);
            this.panLog.Name = "panLog";
            this.panLog.Size = new System.Drawing.Size(553, 465);
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
            this.txtLog.Size = new System.Drawing.Size(553, 465);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
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
            this.txtDeletedFile.TabIndex = 7;
            // 
            // btnDeletedFile
            // 
            this.btnDeletedFile.Location = new System.Drawing.Point(213, 95);
            this.btnDeletedFile.Name = "btnDeletedFile";
            this.btnDeletedFile.Size = new System.Drawing.Size(23, 23);
            this.btnDeletedFile.TabIndex = 8;
            this.btnDeletedFile.Text = "...";
            this.btnDeletedFile.UseVisualStyleBackColor = true;
            this.btnDeletedFile.Click += new System.EventHandler(this.btnDeletedFile_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 465);
            this.Controls.Add(this.panLog);
            this.Controls.Add(this.panSettings);
            this.MinimumSize = new System.Drawing.Size(800, 300);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Library Cleaner";
            this.panSettings.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.edtMinFilesToSave)).EndInit();
            this.panLog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panSettings;
        private System.Windows.Forms.Panel panLog;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblDBPath;
        private SimplTextBox txtLog;
        private System.Windows.Forms.CheckedListBox clsGenres;
        private System.Windows.Forms.Button btnNoneGenres;
        private System.Windows.Forms.Button btnAllGenres;
        private System.Windows.Forms.CheckBox cbxRemoveForeign;
        private System.Windows.Forms.CheckBox cbxRemoveDeleted;
        private System.Windows.Forms.CheckBox cbxRemoveMissedArchives;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.NumericUpDown edtMinFilesToSave;
        private System.Windows.Forms.Label lblMinFilesToUpdate;
        private System.Windows.Forms.Label lblFileWithDeleted;
        private System.Windows.Forms.TextBox txtDeletedFile;
        private System.Windows.Forms.Button btnDeletedFile;
    }
}


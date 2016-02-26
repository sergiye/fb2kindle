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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.panSettings = new System.Windows.Forms.Panel();
            this.clsGenres = new System.Windows.Forms.CheckedListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblDBPath = new System.Windows.Forms.Label();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.btnNoneGenres = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnAllGenres = new System.Windows.Forms.Button();
            this.lblGenres = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnAnalyze = new System.Windows.Forms.Button();
            this.cbxRemoveForeign = new System.Windows.Forms.CheckBox();
            this.cbxRemoveMissedArchives = new System.Windows.Forms.CheckBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.cbxRemoveDeleted = new System.Windows.Forms.CheckBox();
            this.panLog = new System.Windows.Forms.Panel();
            this.txtLog = new LibraryCleaner.SimplTextBox();
            this.lblOutput = new System.Windows.Forms.Label();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.btnBrowseOutput = new System.Windows.Forms.Button();
            this.panSettings.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // panSettings
            // 
            this.panSettings.Controls.Add(this.clsGenres);
            this.panSettings.Controls.Add(this.panel2);
            this.panSettings.Controls.Add(this.panel1);
            this.panSettings.Dock = System.Windows.Forms.DockStyle.Left;
            this.panSettings.Location = new System.Drawing.Point(0, 0);
            this.panSettings.Name = "panSettings";
            this.panSettings.Size = new System.Drawing.Size(239, 362);
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
            this.clsGenres.Location = new System.Drawing.Point(0, 105);
            this.clsGenres.Name = "clsGenres";
            this.clsGenres.Size = new System.Drawing.Size(239, 186);
            this.clsGenres.TabIndex = 4;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.lblOutput);
            this.panel2.Controls.Add(this.txtOutput);
            this.panel2.Controls.Add(this.btnBrowseOutput);
            this.panel2.Controls.Add(this.lblDBPath);
            this.panel2.Controls.Add(this.txtDatabase);
            this.panel2.Controls.Add(this.btnNoneGenres);
            this.panel2.Controls.Add(this.btnBrowse);
            this.panel2.Controls.Add(this.btnAllGenres);
            this.panel2.Controls.Add(this.lblGenres);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(239, 105);
            this.panel2.TabIndex = 12;
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
            this.txtDatabase.Size = new System.Drawing.Size(200, 20);
            this.txtDatabase.TabIndex = 2;
            // 
            // btnNoneGenres
            // 
            this.btnNoneGenres.Location = new System.Drawing.Point(113, 80);
            this.btnNoneGenres.Name = "btnNoneGenres";
            this.btnNoneGenres.Size = new System.Drawing.Size(49, 23);
            this.btnNoneGenres.TabIndex = 7;
            this.btnNoneGenres.Text = "None";
            this.btnNoneGenres.UseVisualStyleBackColor = true;
            this.btnNoneGenres.Click += new System.EventHandler(this.btnNoneGenres_Click);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(213, 14);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(23, 23);
            this.btnBrowse.TabIndex = 3;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnAllGenres
            // 
            this.btnAllGenres.Location = new System.Drawing.Point(58, 80);
            this.btnAllGenres.Name = "btnAllGenres";
            this.btnAllGenres.Size = new System.Drawing.Size(49, 23);
            this.btnAllGenres.TabIndex = 6;
            this.btnAllGenres.Text = "All";
            this.btnAllGenres.UseVisualStyleBackColor = true;
            this.btnAllGenres.Click += new System.EventHandler(this.btnAllGenres_Click);
            // 
            // lblGenres
            // 
            this.lblGenres.AutoSize = true;
            this.lblGenres.Location = new System.Drawing.Point(11, 87);
            this.lblGenres.Name = "lblGenres";
            this.lblGenres.Size = new System.Drawing.Size(41, 13);
            this.lblGenres.TabIndex = 5;
            this.lblGenres.Text = "Genres";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnAnalyze);
            this.panel1.Controls.Add(this.cbxRemoveForeign);
            this.panel1.Controls.Add(this.cbxRemoveMissedArchives);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Controls.Add(this.cbxRemoveDeleted);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 291);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(239, 71);
            this.panel1.TabIndex = 11;
            // 
            // btnAnalyze
            // 
            this.btnAnalyze.Location = new System.Drawing.Point(155, 13);
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
            this.cbxRemoveForeign.Location = new System.Drawing.Point(3, 3);
            this.cbxRemoveForeign.Name = "cbxRemoveForeign";
            this.cbxRemoveForeign.Size = new System.Drawing.Size(133, 17);
            this.cbxRemoveForeign.TabIndex = 8;
            this.cbxRemoveForeign.Text = "Remove foreign books";
            this.cbxRemoveForeign.UseVisualStyleBackColor = true;
            // 
            // cbxRemoveMissedArchives
            // 
            this.cbxRemoveMissedArchives.AutoSize = true;
            this.cbxRemoveMissedArchives.Checked = true;
            this.cbxRemoveMissedArchives.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxRemoveMissedArchives.Location = new System.Drawing.Point(3, 49);
            this.cbxRemoveMissedArchives.Name = "cbxRemoveMissedArchives";
            this.cbxRemoveMissedArchives.Size = new System.Drawing.Size(146, 17);
            this.cbxRemoveMissedArchives.TabIndex = 10;
            this.cbxRemoveMissedArchives.Text = "Remove Missed Archives";
            this.cbxRemoveMissedArchives.UseVisualStyleBackColor = true;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(155, 42);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // cbxRemoveDeleted
            // 
            this.cbxRemoveDeleted.AutoSize = true;
            this.cbxRemoveDeleted.Checked = true;
            this.cbxRemoveDeleted.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxRemoveDeleted.Location = new System.Drawing.Point(3, 26);
            this.cbxRemoveDeleted.Name = "cbxRemoveDeleted";
            this.cbxRemoveDeleted.Size = new System.Drawing.Size(104, 17);
            this.cbxRemoveDeleted.TabIndex = 9;
            this.cbxRemoveDeleted.Text = "Remove deleted";
            this.cbxRemoveDeleted.UseVisualStyleBackColor = true;
            // 
            // panLog
            // 
            this.panLog.Controls.Add(this.txtLog);
            this.panLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panLog.Location = new System.Drawing.Point(239, 0);
            this.panLog.Name = "panLog";
            this.panLog.Size = new System.Drawing.Size(545, 362);
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
            this.txtLog.Size = new System.Drawing.Size(545, 362);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
            // 
            // lblOutput
            // 
            this.lblOutput.AutoSize = true;
            this.lblOutput.Location = new System.Drawing.Point(9, 41);
            this.lblOutput.Name = "lblOutput";
            this.lblOutput.Size = new System.Drawing.Size(152, 13);
            this.lblOutput.TabIndex = 8;
            this.lblOutput.Text = "Output (empty if same as input)";
            // 
            // txtOutput
            // 
            this.txtOutput.Location = new System.Drawing.Point(10, 58);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.Size = new System.Drawing.Size(200, 20);
            this.txtOutput.TabIndex = 9;
            // 
            // btnBrowseOutput
            // 
            this.btnBrowseOutput.Location = new System.Drawing.Point(213, 55);
            this.btnBrowseOutput.Name = "btnBrowseOutput";
            this.btnBrowseOutput.Size = new System.Drawing.Size(23, 23);
            this.btnBrowseOutput.TabIndex = 10;
            this.btnBrowseOutput.Text = "...";
            this.btnBrowseOutput.UseVisualStyleBackColor = true;
            this.btnBrowseOutput.Click += new System.EventHandler(this.btnBrowseOutput_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 362);
            this.Controls.Add(this.panLog);
            this.Controls.Add(this.panSettings);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(800, 300);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Library Cleaner";
            this.panSettings.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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
        private System.Windows.Forms.Label lblGenres;
        private System.Windows.Forms.CheckedListBox clsGenres;
        private System.Windows.Forms.Button btnNoneGenres;
        private System.Windows.Forms.Button btnAllGenres;
        private System.Windows.Forms.CheckBox cbxRemoveForeign;
        private System.Windows.Forms.CheckBox cbxRemoveDeleted;
        private System.Windows.Forms.CheckBox cbxRemoveMissedArchives;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnBrowseOutput;
    }
}


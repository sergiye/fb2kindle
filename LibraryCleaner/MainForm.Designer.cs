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
            this.cbxRemoveDeleted = new System.Windows.Forms.CheckBox();
            this.cbxRemoveForeign = new System.Windows.Forms.CheckBox();
            this.btnNoneGenres = new System.Windows.Forms.Button();
            this.btnAllGenres = new System.Windows.Forms.Button();
            this.lblGenres = new System.Windows.Forms.Label();
            this.clsGenres = new System.Windows.Forms.CheckedListBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.lblDBPath = new System.Windows.Forms.Label();
            this.panLog = new System.Windows.Forms.Panel();
            this.txtLog = new LibraryCleaner.SimplTextBox();
            this.cbxRemoveMissedArchives = new System.Windows.Forms.CheckBox();
            this.panSettings.SuspendLayout();
            this.panLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // panSettings
            // 
            this.panSettings.Controls.Add(this.cbxRemoveMissedArchives);
            this.panSettings.Controls.Add(this.cbxRemoveDeleted);
            this.panSettings.Controls.Add(this.cbxRemoveForeign);
            this.panSettings.Controls.Add(this.btnNoneGenres);
            this.panSettings.Controls.Add(this.btnAllGenres);
            this.panSettings.Controls.Add(this.lblGenres);
            this.panSettings.Controls.Add(this.clsGenres);
            this.panSettings.Controls.Add(this.btnBrowse);
            this.panSettings.Controls.Add(this.txtDatabase);
            this.panSettings.Controls.Add(this.btnStart);
            this.panSettings.Controls.Add(this.lblDBPath);
            this.panSettings.Dock = System.Windows.Forms.DockStyle.Left;
            this.panSettings.Location = new System.Drawing.Point(0, 0);
            this.panSettings.Name = "panSettings";
            this.panSettings.Size = new System.Drawing.Size(301, 362);
            this.panSettings.TabIndex = 0;
            // 
            // cbxRemoveDeleted
            // 
            this.cbxRemoveDeleted.AutoSize = true;
            this.cbxRemoveDeleted.Checked = true;
            this.cbxRemoveDeleted.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxRemoveDeleted.Location = new System.Drawing.Point(12, 330);
            this.cbxRemoveDeleted.Name = "cbxRemoveDeleted";
            this.cbxRemoveDeleted.Size = new System.Drawing.Size(104, 17);
            this.cbxRemoveDeleted.TabIndex = 9;
            this.cbxRemoveDeleted.Text = "Remove deleted";
            this.cbxRemoveDeleted.UseVisualStyleBackColor = true;
            // 
            // cbxRemoveForeign
            // 
            this.cbxRemoveForeign.AutoSize = true;
            this.cbxRemoveForeign.Checked = true;
            this.cbxRemoveForeign.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxRemoveForeign.Location = new System.Drawing.Point(12, 307);
            this.cbxRemoveForeign.Name = "cbxRemoveForeign";
            this.cbxRemoveForeign.Size = new System.Drawing.Size(133, 17);
            this.cbxRemoveForeign.TabIndex = 8;
            this.cbxRemoveForeign.Text = "Remove foreign books";
            this.cbxRemoveForeign.UseVisualStyleBackColor = true;
            // 
            // btnNoneGenres
            // 
            this.btnNoneGenres.Location = new System.Drawing.Point(115, 62);
            this.btnNoneGenres.Name = "btnNoneGenres";
            this.btnNoneGenres.Size = new System.Drawing.Size(49, 23);
            this.btnNoneGenres.TabIndex = 7;
            this.btnNoneGenres.Text = "None";
            this.btnNoneGenres.UseVisualStyleBackColor = true;
            this.btnNoneGenres.Click += new System.EventHandler(this.btnNoneGenres_Click);
            // 
            // btnAllGenres
            // 
            this.btnAllGenres.Location = new System.Drawing.Point(60, 62);
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
            this.lblGenres.Location = new System.Drawing.Point(13, 69);
            this.lblGenres.Name = "lblGenres";
            this.lblGenres.Size = new System.Drawing.Size(41, 13);
            this.lblGenres.TabIndex = 5;
            this.lblGenres.Text = "Genres";
            // 
            // clsGenres
            // 
            this.clsGenres.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clsGenres.CheckOnClick = true;
            this.clsGenres.FormattingEnabled = true;
            this.clsGenres.Items.AddRange(new object[] {
            "firest",
            "second",
            "last"});
            this.clsGenres.Location = new System.Drawing.Point(3, 85);
            this.clsGenres.Name = "clsGenres";
            this.clsGenres.Size = new System.Drawing.Size(283, 214);
            this.clsGenres.TabIndex = 4;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(272, 33);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(23, 23);
            this.btnBrowse.TabIndex = 3;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtDatabase
            // 
            this.txtDatabase.Location = new System.Drawing.Point(14, 36);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(252, 20);
            this.txtDatabase.TabIndex = 2;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(220, 327);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // lblDBPath
            // 
            this.lblDBPath.AutoSize = true;
            this.lblDBPath.Location = new System.Drawing.Point(13, 15);
            this.lblDBPath.Name = "lblDBPath";
            this.lblDBPath.Size = new System.Drawing.Size(53, 13);
            this.lblDBPath.TabIndex = 0;
            this.lblDBPath.Text = "Database";
            // 
            // panLog
            // 
            this.panLog.Controls.Add(this.txtLog);
            this.panLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panLog.Location = new System.Drawing.Point(301, 0);
            this.panLog.Name = "panLog";
            this.panLog.Size = new System.Drawing.Size(483, 362);
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
            this.txtLog.Size = new System.Drawing.Size(483, 362);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
            // 
            // cbxRemoveMissedArchives
            // 
            this.cbxRemoveMissedArchives.AutoSize = true;
            this.cbxRemoveMissedArchives.Checked = true;
            this.cbxRemoveMissedArchives.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxRemoveMissedArchives.Location = new System.Drawing.Point(151, 307);
            this.cbxRemoveMissedArchives.Name = "cbxRemoveMissedArchives";
            this.cbxRemoveMissedArchives.Size = new System.Drawing.Size(146, 17);
            this.cbxRemoveMissedArchives.TabIndex = 10;
            this.cbxRemoveMissedArchives.Text = "Remove Missed Archives";
            this.cbxRemoveMissedArchives.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 362);
            this.Controls.Add(this.panLog);
            this.Controls.Add(this.panSettings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Library Cleaner";
            this.panSettings.ResumeLayout(false);
            this.panSettings.PerformLayout();
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
    }
}


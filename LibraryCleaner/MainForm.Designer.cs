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
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.lblDBPath = new System.Windows.Forms.Label();
            this.panLog = new System.Windows.Forms.Panel();
            this.txtLog = new LibraryCleaner.SimplTextBox();
            this.panSettings.SuspendLayout();
            this.panLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // panSettings
            // 
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
    }
}


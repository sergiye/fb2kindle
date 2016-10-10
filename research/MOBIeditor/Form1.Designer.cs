namespace MOBIeditor {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.openFileMOBI = new System.Windows.Forms.OpenFileDialog();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openMOBIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveMOBIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveMOBIAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mOBIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.addPropertyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveFileMOBI = new System.Windows.Forms.SaveFileDialog();
			this.panel1 = new System.Windows.Forms.Panel();
			this.contextMenuStripPanel = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.addNewPropertyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openNextFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openPreviousFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.contextMenuStripPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// openFileMOBI
			// 
			this.openFileMOBI.Filter = "MOBI|*.mobi";
			this.openFileMOBI.Title = "Open MOBI File";
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.mOBIToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(702, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMOBIToolStripMenuItem,
            this.saveMOBIToolStripMenuItem,
            this.saveMOBIAsToolStripMenuItem,
            this.openNextFileToolStripMenuItem,
            this.openPreviousFileToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openMOBIToolStripMenuItem
			// 
			this.openMOBIToolStripMenuItem.Name = "openMOBIToolStripMenuItem";
			this.openMOBIToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openMOBIToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
			this.openMOBIToolStripMenuItem.Text = "&Open MOBI";
			this.openMOBIToolStripMenuItem.Click += new System.EventHandler(this.openMOBIToolStripMenuItem_Click);
			// 
			// saveMOBIToolStripMenuItem
			// 
			this.saveMOBIToolStripMenuItem.Enabled = false;
			this.saveMOBIToolStripMenuItem.Name = "saveMOBIToolStripMenuItem";
			this.saveMOBIToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveMOBIToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
			this.saveMOBIToolStripMenuItem.Text = "&Save MOBI";
			this.saveMOBIToolStripMenuItem.Click += new System.EventHandler(this.saveMOBIToolStripMenuItem_Click);
			// 
			// saveMOBIAsToolStripMenuItem
			// 
			this.saveMOBIAsToolStripMenuItem.Enabled = false;
			this.saveMOBIAsToolStripMenuItem.Name = "saveMOBIAsToolStripMenuItem";
			this.saveMOBIAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.saveMOBIAsToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
			this.saveMOBIAsToolStripMenuItem.Text = "Save MOBI &As";
			this.saveMOBIAsToolStripMenuItem.Click += new System.EventHandler(this.saveMOBIAsToolStripMenuItem_Click);
			// 
			// mOBIToolStripMenuItem
			// 
			this.mOBIToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addPropertyToolStripMenuItem});
			this.mOBIToolStripMenuItem.Name = "mOBIToolStripMenuItem";
			this.mOBIToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
			this.mOBIToolStripMenuItem.Text = "&MOBI";
			// 
			// addPropertyToolStripMenuItem
			// 
			this.addPropertyToolStripMenuItem.Enabled = false;
			this.addPropertyToolStripMenuItem.Name = "addPropertyToolStripMenuItem";
			this.addPropertyToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.addPropertyToolStripMenuItem.Text = "&Add Property";
			this.addPropertyToolStripMenuItem.Click += new System.EventHandler(this.addPropertyToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.aboutToolStripMenuItem.Text = "&About";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// saveFileMOBI
			// 
			this.saveFileMOBI.Filter = "MOBI|*.mobi";
			this.saveFileMOBI.Title = "Save MOBI File";
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.ContextMenuStrip = this.contextMenuStripPanel;
			this.panel1.Location = new System.Drawing.Point(12, 27);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(678, 359);
			this.panel1.TabIndex = 1;
			this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
			// 
			// contextMenuStripPanel
			// 
			this.contextMenuStripPanel.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addNewPropertyToolStripMenuItem});
			this.contextMenuStripPanel.Name = "contextMenuStripPanel";
			this.contextMenuStripPanel.Size = new System.Drawing.Size(172, 26);
			// 
			// addNewPropertyToolStripMenuItem
			// 
			this.addNewPropertyToolStripMenuItem.Enabled = false;
			this.addNewPropertyToolStripMenuItem.Name = "addNewPropertyToolStripMenuItem";
			this.addNewPropertyToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
			this.addNewPropertyToolStripMenuItem.Text = "&Add New Property";
			this.addNewPropertyToolStripMenuItem.Click += new System.EventHandler(this.addNewPropertyToolStripMenuItem_Click);
			// 
			// openNextFileToolStripMenuItem
			// 
			this.openNextFileToolStripMenuItem.Name = "openNextFileToolStripMenuItem";
			this.openNextFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Right)));
			this.openNextFileToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
			this.openNextFileToolStripMenuItem.Text = "Open &Next File";
			this.openNextFileToolStripMenuItem.Click += new System.EventHandler(this.openNextFileToolStripMenuItem_Click);
			// 
			// openPreviousFileToolStripMenuItem
			// 
			this.openPreviousFileToolStripMenuItem.Name = "openPreviousFileToolStripMenuItem";
			this.openPreviousFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Left)));
			this.openPreviousFileToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
			this.openPreviousFileToolStripMenuItem.Text = "Open &Previous File";
			this.openPreviousFileToolStripMenuItem.Click += new System.EventHandler(this.openPreviousFileToolStripMenuItem_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(702, 398);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "MOBI Editor";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Shown += new System.EventHandler(this.Form1_Shown);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.contextMenuStripPanel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileMOBI;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openMOBIToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveMOBIToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveMOBIAsToolStripMenuItem;
		private System.Windows.Forms.SaveFileDialog saveFileMOBI;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ToolStripMenuItem mOBIToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem addPropertyToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip contextMenuStripPanel;
		private System.Windows.Forms.ToolStripMenuItem addNewPropertyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openNextFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openPreviousFileToolStripMenuItem;
	}
}


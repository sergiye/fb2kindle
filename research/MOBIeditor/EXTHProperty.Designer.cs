namespace MOBIeditor {
	partial class EXTHProperty {
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.labelName = new System.Windows.Forms.Label();
			this.textBoxValue = new System.Windows.Forms.TextBox();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.displayValueAsStringToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.displayValueAsLongStringToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.displayValueAsNumberToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.displayValueAsByteArrayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.deletePropertyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// labelName
			// 
			this.labelName.AutoSize = true;
			this.labelName.Location = new System.Drawing.Point(3, 6);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(35, 13);
			this.labelName.TabIndex = 0;
			this.labelName.Text = "Name";
			// 
			// textBoxValue
			// 
			this.textBoxValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxValue.Location = new System.Drawing.Point(125, 3);
			this.textBoxValue.Name = "textBoxValue";
			this.textBoxValue.Size = new System.Drawing.Size(550, 20);
			this.textBoxValue.TabIndex = 1;
			this.textBoxValue.TextChanged += new System.EventHandler(this.textBoxValue_TextChanged);
			this.textBoxValue.Leave += new System.EventHandler(this.textBoxValue_Leave);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.displayValueAsStringToolStripMenuItem,
            this.displayValueAsLongStringToolStripMenuItem,
            this.displayValueAsNumberToolStripMenuItem,
            this.displayValueAsByteArrayToolStripMenuItem,
            this.toolStripSeparator1,
            this.deletePropertyToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(223, 120);
			// 
			// displayValueAsStringToolStripMenuItem
			// 
			this.displayValueAsStringToolStripMenuItem.Name = "displayValueAsStringToolStripMenuItem";
			this.displayValueAsStringToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.displayValueAsStringToolStripMenuItem.Text = "Display Value as &String";
			this.displayValueAsStringToolStripMenuItem.Click += new System.EventHandler(this.displayValueAsStringToolStripMenuItem_Click);
			// 
			// displayValueAsLongStringToolStripMenuItem
			// 
			this.displayValueAsLongStringToolStripMenuItem.Name = "displayValueAsLongStringToolStripMenuItem";
			this.displayValueAsLongStringToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
			this.displayValueAsLongStringToolStripMenuItem.Text = "Display Value as &Long String";
			this.displayValueAsLongStringToolStripMenuItem.Click += new System.EventHandler(this.displayValueAsLongStringToolStripMenuItem_Click);
			// 
			// displayValueAsNumberToolStripMenuItem
			// 
			this.displayValueAsNumberToolStripMenuItem.Name = "displayValueAsNumberToolStripMenuItem";
			this.displayValueAsNumberToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.displayValueAsNumberToolStripMenuItem.Text = "Display Value as &Number";
			this.displayValueAsNumberToolStripMenuItem.Click += new System.EventHandler(this.displayValueAsNumberToolStripMenuItem_Click);
			// 
			// displayValueAsByteArrayToolStripMenuItem
			// 
			this.displayValueAsByteArrayToolStripMenuItem.Name = "displayValueAsByteArrayToolStripMenuItem";
			this.displayValueAsByteArrayToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.displayValueAsByteArrayToolStripMenuItem.Text = "Display Value as &Byte Array";
			this.displayValueAsByteArrayToolStripMenuItem.Click += new System.EventHandler(this.displayValueAsByteArrayToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(212, 6);
			// 
			// deletePropertyToolStripMenuItem
			// 
			this.deletePropertyToolStripMenuItem.Name = "deletePropertyToolStripMenuItem";
			this.deletePropertyToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
			this.deletePropertyToolStripMenuItem.Text = "&Delete Property";
			this.deletePropertyToolStripMenuItem.Click += new System.EventHandler(this.deletePropertyToolStripMenuItem_Click);
			// 
			// EXTHProperty
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ContextMenuStrip = this.contextMenuStrip1;
			this.Controls.Add(this.textBoxValue);
			this.Controls.Add(this.labelName);
			this.Name = "EXTHProperty";
			this.Size = new System.Drawing.Size(678, 27);
			this.Load += new System.EventHandler(this.EXTHProperty_Load);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelName;
		private System.Windows.Forms.TextBox textBoxValue;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem displayValueAsStringToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem displayValueAsNumberToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem displayValueAsByteArrayToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem deletePropertyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem displayValueAsLongStringToolStripMenuItem;
	}
}

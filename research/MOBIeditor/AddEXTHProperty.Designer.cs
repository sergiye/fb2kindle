namespace MOBIeditor {
	partial class AddEXTHProperty {
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
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxValueType = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxPropertyType = new System.Windows.Forms.ComboBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(31, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Type";
			// 
			// comboBoxValueType
			// 
			this.comboBoxValueType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxValueType.FormattingEnabled = true;
			this.comboBoxValueType.Items.AddRange(new object[] {
            "String",
            "Number",
            "Byte Array"});
			this.comboBoxValueType.Location = new System.Drawing.Point(49, 33);
			this.comboBoxValueType.Name = "comboBoxValueType";
			this.comboBoxValueType.Size = new System.Drawing.Size(149, 21);
			this.comboBoxValueType.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 36);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(34, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Value";
			// 
			// comboBoxPropertyType
			// 
			this.comboBoxPropertyType.DisplayMember = "Text";
			this.comboBoxPropertyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxPropertyType.FormattingEnabled = true;
			this.comboBoxPropertyType.Location = new System.Drawing.Point(49, 6);
			this.comboBoxPropertyType.Name = "comboBoxPropertyType";
			this.comboBoxPropertyType.Size = new System.Drawing.Size(149, 21);
			this.comboBoxPropertyType.TabIndex = 1;
			this.comboBoxPropertyType.ValueMember = "Text";
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(49, 60);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(71, 23);
			this.buttonOK.TabIndex = 3;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(126, 60);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(72, 23);
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// AddEXTHProperty
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(210, 96);
			this.ControlBox = false;
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.comboBoxPropertyType);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.comboBoxValueType);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "AddEXTHProperty";
			this.ShowInTaskbar = false;
			this.Text = "Add Property";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		public System.Windows.Forms.ComboBox comboBoxValueType;
		public System.Windows.Forms.ComboBox comboBoxPropertyType;
	}
}
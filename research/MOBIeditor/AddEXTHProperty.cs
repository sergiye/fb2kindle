using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MOBIeditor {
	public partial class AddEXTHProperty : Form {
		public AddEXTHProperty() {
			InitializeComponent();

			comboBoxPropertyType.Items.AddRange(MOBIFormat.ExthHeaderFormat.ExthHeaderRecordFormat.RecordTypes.Select(o => new ListViewItem(o.Value) { Tag = o.Key }).ToArray());
			comboBoxPropertyType.SelectedIndex = 0;

			comboBoxValueType.SelectedIndex = 0;
		}

		private void buttonOK_Click(object sender, EventArgs e) {
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

	}
}

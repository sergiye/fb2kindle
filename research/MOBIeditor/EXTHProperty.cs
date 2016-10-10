using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MOBIeditor {
	public delegate void DeletePropertyHandler(Guid id);

	public partial class EXTHProperty : UserControl {
		public enum ValueDisplayType { String, Number, ByteArray };
		public event DeletePropertyHandler DeleteProperty;

		public MOBIFormat.ExthHeaderFormat.ExthHeaderRecordFormat EXTHRecord;
		public ValueDisplayType Display;

		public bool IsValid { get { return (textBoxValue.BackColor != Color.Red); } }

		public EXTHProperty() {
			InitializeComponent();
		}

		private void EXTHProperty_Load(object sender, EventArgs e) {
			labelName.Text = EXTHRecord.TypeName;
			if (EXTHRecord.Data[0] == 0) {
				if (EXTHRecord.Data.Length == 4 || EXTHRecord.Data.Length == 2) displayValueAsNumberToolStripMenuItem_Click(sender, EventArgs.Empty);
				else displayValueAsByteArrayToolStripMenuItem_Click(sender, EventArgs.Empty);
			} else displayValueAsStringToolStripMenuItem_Click(sender, EventArgs.Empty);
		}

		private void textBoxValue_Leave(object sender, EventArgs e) {
			switch (Display) {
				case ValueDisplayType.String:
					EXTHRecord.Data = ASCIIEncoding.ASCII.GetBytes(textBoxValue.Text);
					textBoxValue.BackColor = Color.White;
					break;

				case ValueDisplayType.Number:
					textBoxValue.BackColor = Color.Red;
					if (EXTHRecord.Data.Length == 4) {
						int result;
						if (Int32.TryParse(textBoxValue.Text, out result)) {
							EXTHRecord.Data = BitConverter.GetBytes(result);
							EXTHRecord.Data = EXTHRecord.Data.Reverse().ToArray(); // endian swap
							textBoxValue.BackColor = Color.White;
						}
					} else if (EXTHRecord.Data.Length == 2) {
						short result;
						if (short.TryParse(textBoxValue.Text, out result)) {
							EXTHRecord.Data = BitConverter.GetBytes(result);
							EXTHRecord.Data = EXTHRecord.Data.Reverse().ToArray(); // endian swap
							textBoxValue.BackColor = Color.White;
						}
					}
					break;

				case ValueDisplayType.ByteArray:
					try {
						textBoxValue.Text = System.Text.RegularExpressions.Regex.Replace(textBoxValue.Text, @"\s+", " ").Trim();
						EXTHRecord.Data = textBoxValue.Text.Split(' ').Select(o => Convert.ToByte(o)).ToArray();
						textBoxValue.BackColor = Color.White;
					} catch {
						textBoxValue.BackColor = Color.Red;
					}
					break;
			}
		}

		private void displayValueAsStringToolStripMenuItem_Click(object sender, EventArgs e) {
			// not if the first byte is 0 - there'd be nothing to display, anyway
			if (EXTHRecord.Data[0] == 0) return;

			textBoxValue.Text = ASCIIEncoding.ASCII.GetString(EXTHRecord.Data);
			Display = ValueDisplayType.String;
			//textBoxValue.Width = Width - 131;

			if (textBoxValue.Multiline) {
				Height -= 60;
				textBoxValue.Multiline = false;
				textBoxValue.Height -= 60;
			}

			Parent.Refresh();
		}

		private void displayValueAsLongStringToolStripMenuItem_Click(object sender, EventArgs e) {
			// not if the first byte is 0 - there'd be nothing to display, anyway
			if (EXTHRecord.Data[0] == 0) return;

			textBoxValue.Text = ASCIIEncoding.ASCII.GetString(EXTHRecord.Data);

			Display = ValueDisplayType.String;

			if (!textBoxValue.Multiline) {
				Height += 60;
				textBoxValue.Multiline = true;
				textBoxValue.Height += 60;
			}

			Parent.Refresh();
		}

		private void displayValueAsNumberToolStripMenuItem_Click(object sender, EventArgs e) {
			// only 16-bit and 32-bit numbers are supported
			if (EXTHRecord.Data.Length != 2 && EXTHRecord.Data.Length != 4) return;

			if (EXTHRecord.Data.Length == 4) {
				textBoxValue.Text = MOBIFormat.Get_BigEndian_Int32_FromByteArray(EXTHRecord.Data, 0).ToString();
			} else if (EXTHRecord.Data.Length == 2) {
				textBoxValue.Text = MOBIFormat.Get_BigEndian_Short_FromByteArray(EXTHRecord.Data, 0).ToString();
			}

			Display = ValueDisplayType.Number;

			if (textBoxValue.Multiline) {
				Height -= 60;
				textBoxValue.Multiline = false;
				textBoxValue.Height -= 60;
			}

			Parent.Refresh();
		}

		private void displayValueAsByteArrayToolStripMenuItem_Click(object sender, EventArgs e) {
			string s = string.Join(" ", EXTHRecord.Data.Select(o => o.ToString()));
			textBoxValue.Text = s;

			Display = ValueDisplayType.ByteArray;

			if (textBoxValue.Multiline) {
				Height -= 60;
				textBoxValue.Multiline = false;
				textBoxValue.Height -= 60;
			}

			Parent.Refresh();
		}

		private void deletePropertyToolStripMenuItem_Click(object sender, EventArgs e) {
			if (DeleteProperty != null) DeleteProperty(EXTHRecord.Id);
		}

		private void textBoxValue_TextChanged(object sender, EventArgs e) {
			if (!ParentForm.Text.EndsWith("*")) ParentForm.Text += "*";
		}

	}
}

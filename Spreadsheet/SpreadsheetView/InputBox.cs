using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpreadsheetView.Properties;

namespace SpreadsheetView
{
    /// <summary>
    /// Form used to retreive spreadsheet names and passwords.
    /// </summary>
    public partial class InputBox : Form
    {
        private bool _enteringPassword;
        private string _spreadsheetName;
        private string _password;
        private SpreadsheetForm owner; 

        /// <summary>
        /// Initializes input box.
        /// </summary>
        public InputBox(SpreadsheetForm f, bool newSpreadsheet)
        {
            InitializeComponent();
            owner = f;
            Text = newSpreadsheet ? "Create New Spreadsheet" : "Open Existing Spreadsheet";
            textLabel.Text = newSpreadsheet ? Resources.InputBoxTextNew : Resources.InputBoxTextEdit;
            namePasswordLabel.Text = Resources.InputBoxLabelNameText;
            _enteringPassword = false;
            namePasswordTextBox.UseSystemPasswordChar = false;
            namePasswordTextBox.Focus();
        }

        /// <summary>
        /// Ok button handler
        /// </summary>
        private void OkButtonClick(object sender, EventArgs e)
        {
            namePasswordTextBox.Enabled = false;

            //Check if entering password
            if (!_enteringPassword)
            {
                _spreadsheetName = namePasswordTextBox.Text;
                namePasswordTextBox.UseSystemPasswordChar = true;
                namePasswordTextBox.Text = string.Empty;
                _enteringPassword = true;
                namePasswordLabel.Text = Resources.InputBoxLabelPasswordText;
                namePasswordTextBox.Enabled = true;
                namePasswordTextBox.Focus();
            }
            else
            {
                _password = namePasswordTextBox.Text;
                if (owner != null)
                {
                    owner.SetSpreadsheetNameAndPassword(_spreadsheetName, _password);
                    DialogResult = System.Windows.Forms.DialogResult.OK;
                }
                Close();
            }

        }

        /// <summary>
        /// Cancel button handler
        /// </summary>
        private void CancelButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Enter button handler
        /// </summary>
        private void TextBoxPressEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                okButton.Focus();
        }
    }
}

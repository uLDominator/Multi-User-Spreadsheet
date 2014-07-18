using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SpreadsheetView
{
    /// <summary>
    /// Form used to connect to the server.
    /// </summary>
    public partial class ConnectBox : Form
    {
        private SpreadsheetForm owner;
        private bool connected;

        /// <summary>
        /// Initializes connect box
        /// </summary>
        public ConnectBox(SpreadsheetForm f)
        {
            InitializeComponent();
            owner = f;
            connected = false;
            IPBox.Focus();
        }

        /// <summary>
        /// Connect button handler
        /// </summary>
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                owner.ConnectToServer(IPBox.Text);
                connected = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to server. Exception: " + ex.ToString() + ".", "Unable to Connect");
            }
        }

        /// <summary>
        /// Cancel button handler
        /// </summary>
        private void CancelButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Please connect to the server to create or join a spreadsheet");
        }
        
        /// <summary>
        /// Close event handler
        /// </summary>
        private void ConnectBox_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(!connected)
                owner.Close();
        }

        /// <summary>
        /// Enter button handler
        /// </summary>
        private void PressEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ConnectButton.Focus();
        }
    }
}

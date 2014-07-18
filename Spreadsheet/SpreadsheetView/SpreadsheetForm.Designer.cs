using SpreadsheetUtilities;

namespace SpreadsheetView
{
    sealed partial class SpreadsheetForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpreadsheetForm));
            this.spreadsheetMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMainMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ConnectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editMainMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMainMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpContentsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectedCellLabel = new System.Windows.Forms.Label();
            this.selectedCellTextBox = new System.Windows.Forms.TextBox();
            this.cellValueLabel = new System.Windows.Forms.Label();
            this.cellValueTextBox = new System.Windows.Forms.TextBox();
            this.cellContentsLabel = new System.Windows.Forms.Label();
            this.cellContentsTextBox = new System.Windows.Forms.TextBox();
            this.cellInfoGroupBox = new System.Windows.Forms.GroupBox();
            this.mainSpreadsheetPanel = new SpreadsheetView.SpreadsheetPanel();
            this.spreadsheetMenuStrip.SuspendLayout();
            this.cellInfoGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // spreadsheetMenuStrip
            // 
            this.spreadsheetMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMainMenuItem,
            this.editMainMenuItem,
            this.helpMainMenuItem});
            this.spreadsheetMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.spreadsheetMenuStrip.Name = "spreadsheetMenuStrip";
            this.spreadsheetMenuStrip.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.spreadsheetMenuStrip.Size = new System.Drawing.Size(880, 24);
            this.spreadsheetMenuStrip.TabIndex = 1;
            this.spreadsheetMenuStrip.Text = "menuStrip1";
            // 
            // fileMainMenuItem
            // 
            this.fileMainMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ConnectMenuItem,
            this.saveMenuItem,
            this.newMenuItem,
            this.openMenuItem,
            this.closeMenuItem,
            this.exitMenuItem});
            this.fileMainMenuItem.Name = "fileMainMenuItem";
            this.fileMainMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileMainMenuItem.Text = "File";
            // 
            // saveMenuItem
            // 
            this.saveMenuItem.Name = "saveMenuItem";
            this.saveMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveMenuItem.Text = "Save";
            this.saveMenuItem.Click += new System.EventHandler(this.SaveMenuItemClick);
            // 
            // newMenuItem
            // 
            this.newMenuItem.Name = "newMenuItem";
            this.newMenuItem.Size = new System.Drawing.Size(152, 22);
            this.newMenuItem.Text = "New";
            this.newMenuItem.Click += new System.EventHandler(this.NewMenuItemClick);
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openMenuItem.Text = "Open...";
            this.openMenuItem.Click += new System.EventHandler(this.OpenMenuMenuItemClick);
            // 
            // closeMenuItem
            // 
            this.closeMenuItem.Name = "closeMenuItem";
            this.closeMenuItem.Size = new System.Drawing.Size(152, 22);
            this.closeMenuItem.Text = "Close";
            this.closeMenuItem.Click += new System.EventHandler(this.CloseMenuItemClick);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.ExitMenuItemClick);
            // 
            // ConnectMenuItem
            // 
            this.ConnectMenuItem.Name = "ConnectMenuItem";
            this.ConnectMenuItem.Size = new System.Drawing.Size(152, 22);
            this.ConnectMenuItem.Text = "Connect...";
            this.ConnectMenuItem.Click += new System.EventHandler(this.ConnectMenuItemClick);
            // 
            // editMainMenuItem
            // 
            this.editMainMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoMenuItem});
            this.editMainMenuItem.Name = "editMainMenuItem";
            this.editMainMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editMainMenuItem.Text = "Edit";
            // 
            // undoMenuItem
            // 
            this.undoMenuItem.Name = "undoMenuItem";
            this.undoMenuItem.Size = new System.Drawing.Size(152, 22);
            this.undoMenuItem.Text = "Undo";
            this.undoMenuItem.Click += new System.EventHandler(this.UndoMenuItemClick);
            // 
            // helpMainMenuItem
            // 
            this.helpMainMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpContentsMenuItem,
            this.aboutMenuItem});
            this.helpMainMenuItem.Name = "helpMainMenuItem";
            this.helpMainMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpMainMenuItem.Text = "Help";
            // 
            // helpContentsMenuItem
            // 
            this.helpContentsMenuItem.Name = "helpContentsMenuItem";
            this.helpContentsMenuItem.Size = new System.Drawing.Size(159, 22);
            this.helpContentsMenuItem.Text = "Help Contents...";
            this.helpContentsMenuItem.Click += new System.EventHandler(this.HelpContentsMenuItemClick);
            // 
            // aboutMenuItem
            // 
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(159, 22);
            this.aboutMenuItem.Text = "About...";
            this.aboutMenuItem.Click += new System.EventHandler(this.AboutMenuItemClick);
            // 
            // selectedCellLabel
            // 
            this.selectedCellLabel.AutoSize = true;
            this.selectedCellLabel.Location = new System.Drawing.Point(5, 18);
            this.selectedCellLabel.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.selectedCellLabel.Name = "selectedCellLabel";
            this.selectedCellLabel.Size = new System.Drawing.Size(75, 13);
            this.selectedCellLabel.TabIndex = 2;
            this.selectedCellLabel.Text = "Selected Cell: ";
            // 
            // selectedCellTextBox
            // 
            this.selectedCellTextBox.Location = new System.Drawing.Point(82, 15);
            this.selectedCellTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 30, 3);
            this.selectedCellTextBox.Name = "selectedCellTextBox";
            this.selectedCellTextBox.ReadOnly = true;
            this.selectedCellTextBox.Size = new System.Drawing.Size(38, 20);
            this.selectedCellTextBox.TabIndex = 3;
            this.selectedCellTextBox.TabStop = false;
            // 
            // cellValueLabel
            // 
            this.cellValueLabel.AutoSize = true;
            this.cellValueLabel.Location = new System.Drawing.Point(152, 18);
            this.cellValueLabel.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.cellValueLabel.Name = "cellValueLabel";
            this.cellValueLabel.Size = new System.Drawing.Size(37, 13);
            this.cellValueLabel.TabIndex = 4;
            this.cellValueLabel.Text = "Value:";
            // 
            // cellValueTextBox
            // 
            this.cellValueTextBox.Location = new System.Drawing.Point(191, 15);
            this.cellValueTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 30, 3);
            this.cellValueTextBox.Name = "cellValueTextBox";
            this.cellValueTextBox.ReadOnly = true;
            this.cellValueTextBox.Size = new System.Drawing.Size(188, 20);
            this.cellValueTextBox.TabIndex = 5;
            this.cellValueTextBox.TabStop = false;
            // 
            // cellContentsLabel
            // 
            this.cellContentsLabel.AutoSize = true;
            this.cellContentsLabel.Location = new System.Drawing.Point(412, 18);
            this.cellContentsLabel.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.cellContentsLabel.Name = "cellContentsLabel";
            this.cellContentsLabel.Size = new System.Drawing.Size(52, 13);
            this.cellContentsLabel.TabIndex = 6;
            this.cellContentsLabel.Text = "Contents:";
            // 
            // cellContentsTextBox
            // 
            this.cellContentsTextBox.Location = new System.Drawing.Point(466, 15);
            this.cellContentsTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 30, 3);
            this.cellContentsTextBox.Name = "cellContentsTextBox";
            this.cellContentsTextBox.Size = new System.Drawing.Size(376, 20);
            this.cellContentsTextBox.TabIndex = 0;
            this.cellContentsTextBox.TextChanged += new System.EventHandler(this.CellContentsTextBoxTextChanged);
            this.cellContentsTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ChangeSelectedCell);
            // 
            // cellInfoGroupBox
            // 
            this.cellInfoGroupBox.Controls.Add(this.cellContentsLabel);
            this.cellInfoGroupBox.Controls.Add(this.cellContentsTextBox);
            this.cellInfoGroupBox.Controls.Add(this.selectedCellLabel);
            this.cellInfoGroupBox.Controls.Add(this.selectedCellTextBox);
            this.cellInfoGroupBox.Controls.Add(this.cellValueLabel);
            this.cellInfoGroupBox.Controls.Add(this.cellValueTextBox);
            this.cellInfoGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.cellInfoGroupBox.Location = new System.Drawing.Point(0, 24);
            this.cellInfoGroupBox.Margin = new System.Windows.Forms.Padding(2);
            this.cellInfoGroupBox.Name = "cellInfoGroupBox";
            this.cellInfoGroupBox.Padding = new System.Windows.Forms.Padding(2);
            this.cellInfoGroupBox.Size = new System.Drawing.Size(880, 46);
            this.cellInfoGroupBox.TabIndex = 8;
            this.cellInfoGroupBox.TabStop = false;
            // 
            // mainSpreadsheetPanel
            // 
            this.mainSpreadsheetPanel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.mainSpreadsheetPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainSpreadsheetPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSpreadsheetPanel.Location = new System.Drawing.Point(0, 70);
            this.mainSpreadsheetPanel.Name = "mainSpreadsheetPanel";
            this.mainSpreadsheetPanel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.mainSpreadsheetPanel.Size = new System.Drawing.Size(880, 548);
            this.mainSpreadsheetPanel.TabIndex = 7;
            this.mainSpreadsheetPanel.TabStop = false;
            // 
            // SpreadsheetForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(880, 618);
            this.Controls.Add(this.mainSpreadsheetPanel);
            this.Controls.Add(this.cellInfoGroupBox);
            this.Controls.Add(this.spreadsheetMenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.spreadsheetMenuStrip;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SpreadsheetForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Spreadsheet";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CloseSpreadsheet);
            this.spreadsheetMenuStrip.ResumeLayout(false);
            this.spreadsheetMenuStrip.PerformLayout();
            this.cellInfoGroupBox.ResumeLayout(false);
            this.cellInfoGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SpreadsheetPanel mainSpreadsheetPanel;
        private System.Windows.Forms.MenuStrip spreadsheetMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMainMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.Label selectedCellLabel;
        private System.Windows.Forms.TextBox selectedCellTextBox;
        private System.Windows.Forms.Label cellValueLabel;
        private System.Windows.Forms.TextBox cellValueTextBox;
        private System.Windows.Forms.Label cellContentsLabel;
        private System.Windows.Forms.TextBox cellContentsTextBox;
        private System.Windows.Forms.GroupBox cellInfoGroupBox;
        private System.Windows.Forms.ToolStripMenuItem helpMainMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpContentsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editMainMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ConnectMenuItem;
    }
}


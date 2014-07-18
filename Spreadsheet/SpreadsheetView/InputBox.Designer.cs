namespace SpreadsheetView
{
    partial class InputBox
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
            this.namePasswordTextBox = new System.Windows.Forms.TextBox();
            this.textLabel = new System.Windows.Forms.Label();
            this.namePasswordLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // namePasswordTextBox
            // 
            this.namePasswordTextBox.Location = new System.Drawing.Point(83, 45);
            this.namePasswordTextBox.Margin = new System.Windows.Forms.Padding(5);
            this.namePasswordTextBox.Name = "namePasswordTextBox";
            this.namePasswordTextBox.Size = new System.Drawing.Size(242, 20);
            this.namePasswordTextBox.TabIndex = 0;
            this.namePasswordTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxPressEnter);
            // 
            // textLabel
            // 
            this.textLabel.AutoSize = true;
            this.textLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.textLabel.Location = new System.Drawing.Point(0, 0);
            this.textLabel.Name = "textLabel";
            this.textLabel.Padding = new System.Windows.Forms.Padding(10);
            this.textLabel.Size = new System.Drawing.Size(329, 33);
            this.textLabel.TabIndex = 1;
            this.textLabel.Text = "Please enter the name of the spreadsheet you would like to edit.";
            this.textLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // namePasswordLabel
            // 
            this.namePasswordLabel.AutoSize = true;
            this.namePasswordLabel.Location = new System.Drawing.Point(12, 48);
            this.namePasswordLabel.Name = "namePasswordLabel";
            this.namePasswordLabel.Padding = new System.Windows.Forms.Padding(2, 0, 4, 0);
            this.namePasswordLabel.Size = new System.Drawing.Size(44, 13);
            this.namePasswordLabel.TabIndex = 2;
            this.namePasswordLabel.Text = "Name:";
            this.namePasswordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(83, 83);
            this.okButton.Name = "okButton";
            this.okButton.Padding = new System.Windows.Forms.Padding(5);
            this.okButton.Size = new System.Drawing.Size(101, 32);
            this.okButton.TabIndex = 3;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButtonClick);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(225, 83);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Padding = new System.Windows.Forms.Padding(5);
            this.cancelButton.Size = new System.Drawing.Size(100, 32);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButtonClick);
            // 
            // InputBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(392, 127);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.namePasswordLabel);
            this.Controls.Add(this.textLabel);
            this.Controls.Add(this.namePasswordTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputBox";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select Spreadsheet";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox namePasswordTextBox;
        private System.Windows.Forms.Label textLabel;
        private System.Windows.Forms.Label namePasswordLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
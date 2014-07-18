using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SpreadsheetModel;
using SpreadsheetUtilities;
using SpreadsheetView.Properties;
using System.Collections;
using System.Threading;

namespace SpreadsheetView
{
    /// <summary>
    /// Spreadsheet Form Application.
    /// </summary>
    public sealed partial class SpreadsheetForm : Form
    {
        #region Member Variables and Constructors

        private readonly SpreadsheetClientModel _model; //used to communicate with the server
        private SpreadsheetContainer _spreadSheet; //used to keep track of the cells and their information internally
        private string _host; //used to keep track of hostname that the client is connected to

        KeyValuePair<string, string> _ssCredentials; //used to keep track of spreadsheet name and password
        KeyValuePair<string, string> _newFormCredentials; //use to keep track of spreadsheet name and password to open in a new form
        private int _spreadsheetVersion; //used to keep track of the spreadsheet's version number

        private int _pendingSelectedColumn; //used to keep track of the pending column number after leaving a cell
        private int _pendingSelectedRow; //used to keep track of the pending column number after leaving a cell
        private bool _pendingChanges; //used to indicate if we are waiting for the server to respond

        private int _selectedColumn; //used to keep track of the column number of the currently selected cell
        private int _selectedRow; //used to keep track of the row number of the currently selected cell

        private string _selectedCellName; //used to keep track of the name of the currently selected cell
        private bool _selectedCellContentsChanged; //used to keep track of when the spreadsheet has been changed since being saved or opened

        private bool _connected; //used to keep track of server connection
        private bool _readonly; //used to indicate if the spreadsheet is currently readonly

        private readonly object lockObj = new object();
        
        /// <summary>
        /// Creates an empty spreadsheet form.
        /// </summary>
        public SpreadsheetForm()
        {
            //Initialize client
            InitializeComponent();
            _model = new SpreadsheetClientModel();
            _model.IncomingLineEvent += MessageReceived;

            //Display connect box
            ConnectBox connectBox = new ConnectBox(this);
            connectBox.ShowDialog();

            //Populate client interface
            _spreadsheetVersion = -1;
            _spreadSheet = new SpreadsheetContainer(FormHelper.IsValidCellName, FormHelper.NormalizeCellName, _spreadsheetVersion.ToString());
            mainSpreadsheetPanel.SelectionChanged += DisplaySelectedCellInformation;
            mainSpreadsheetPanel.SetSelection(0, 0);
            _selectedCellContentsChanged = false;
            _selectedCellName = "A1";
            DisplaySelectedCellInformation(mainSpreadsheetPanel);

            //Make readonly
            SetReadOnly();

            // check if form handler has been created already
            if (!IsHandleCreated)
                CreateHandle();
        }

        /// <summary>
        /// Opens a spreadsheet form in a new window and either requests
        /// a new spreadsheet or requests to edit an existing spreadsheet.
        /// </summary>
        public SpreadsheetForm(string host, string name, string password, bool newSpreadsheet)
        {
            //Initialize client
            InitializeComponent();
            _model = new SpreadsheetClientModel();
            _model.IncomingLineEvent += MessageReceived;

            //Connect to the server
            ConnectToServer(host);

            //Populate client interface
            _ssCredentials = new KeyValuePair<string, string>(name, password);
            _spreadsheetVersion = -1;
            _spreadSheet = new SpreadsheetContainer(FormHelper.IsValidCellName, FormHelper.NormalizeCellName, _spreadsheetVersion.ToString());
            mainSpreadsheetPanel.SelectionChanged += DisplaySelectedCellInformation;
            mainSpreadsheetPanel.SetSelection(0, 0);
            _selectedCellContentsChanged = false;
            _selectedCellName = "A1";
            DisplaySelectedCellInformation(mainSpreadsheetPanel);

            //Make readonly
            SetReadOnly();

            // check if form handler has been created already
            if (!IsHandleCreated)
                CreateHandle();

            //Check if connected
            if (!_connected)
                return;

            //Send create if new Spreadsheet, otherwise send join request
            if (newSpreadsheet)
                _model.Create(name, password);
            else
                _model.Join(name, password);    
        }

        #endregion

        #region Server Communication Methods

        /// <summary>
        /// Connects the client to the spreadsheet server.
        /// </summary>
        public void ConnectToServer(string ip)
        {
            try
            {
                //Attempt to connect to server
                _host = ip;
                _model.Connect(_host);
                _connected = true;
                ConnectMenuItem.Enabled = false;
            }
            catch (Exception ex)
            {
                //Display error message
                MessageBox.Show(this, "Error connecting to the server: " + ex, "Server Connection Error");
                _model.Disconnect();
                _connected = false;
                ConnectMenuItem.Enabled = true;
            }
        }

        /// <summary>
        /// Sets the spreadsheet name and password.
        /// </summary>
        public void SetSpreadsheetNameAndPassword(string name, string password)
        {
            // Are we creating a new spreadsheet or
            // opening our first existing spreadsheet?
            if (_spreadsheetVersion < 0)
                _ssCredentials = new KeyValuePair<string, string>(name, password);
            
            //Are we already editing a spreadsheet and want to open another one?
            else
                _newFormCredentials = new KeyValuePair<string, string>(name, password);
        }

        /// <summary>
        /// Handles messages from the server.
        /// </summary>
        private void MessageReceived(ArrayList list)
        {
            if (list == null)
                return;

            // check if form handler has been created already
            
            if (!IsHandleCreated)
                CreateHandle();

            // check which reponse was received
            switch(list[0].ToString())
            {
                case "CREATE":
                    {
                        // if CREATE OK was received
                        if (list[1].ToString().ToLower().Equals("true"))
                        {
                            Invoke(new Action(() => { MessageBox.Show("Spreadsheet '" + list[2] + "' was successfully created!\nYou must open it to be able to edit it.", "File Creation Successful"); }));
                        }
                        // if CREATE FAIL was received
                        else 
                        {
                            // display the informative message when an error occurs
                            Invoke(new Action(() => { MessageBox.Show(Resources.ErrorDialogTextSavingFile + ": " + list[3].ToString(), Resources.ErrorDialogTitle); })); 
                        }
                        break;
                    }

                case "JOIN":
                    {
                        // if JOIN OK was received
                        if (list[1].ToString().ToLower().Equals("true"))
                        {
                            int ver = 0;
                            if (!int.TryParse(list[3].ToString(), out ver))
                            {
                                Invoke(new Action(() => { MessageBox.Show("Error: Spreadsheet version is not a valid number.", Resources.ErrorDialogTitle); }));
                                break;
                            }
                            
                            // loads cells from the xml string
                            try
                            {
                                _spreadSheet.LoadCellsFromXML(list[5].ToString());
                            }
                            catch(SpreadsheetReadWriteException)
                            {
                                //Show simple error message to the user that the file could not be read
                                Invoke(new Action(() => { MessageBox.Show(this, Resources.ErrorDialogTextSavingFile, Resources.ErrorDialogTitle); }));
                                break;
                            }
                            // saves correct version number
                            _spreadsheetVersion = ver;
                            // allow editing and populate cell values
                            Invoke(new Action(() => { EnableEditing(); }));
                            Invoke(new Action(() => { PopulateCells(); }));
                            Invoke(new Action(() => { Text = _ssCredentials.Key; }));
                        }
                        // if JOIN FAIL was received
                        else
                        {
                            // display the informative message when an error occurs
                            Invoke(new Action(() => { MessageBox.Show(Resources.ErrorDialogTextOpeningFile + ": " + list[3].ToString(), Resources.ErrorDialogTitle); }));
                            _ssCredentials = new KeyValuePair<string,string>();
                            _spreadsheetVersion = -1;
                        }
                        break;
                    }

                case "CHANGE":
                    {
                        // if CHANGE OK was received
                        if (list[1].Equals("OK"))
                        {
                            Invoke(new Action(() => { CommitChanges(); }));
                      
                            int ver = 0;
                            if (!int.TryParse(list[3].ToString(), out ver))
                            {
                                Invoke(new Action(() => { MessageBox.Show("Error: Spreadsheet version is not a number.", Resources.ErrorDialogTitle); }));
                                break;
                            }
                            // saves correct version number
                            _spreadsheetVersion = ver;
                        }
                        // if CHANGE WAIT was received
                        else if (list[1].Equals("WAIT")) 
                        {
                            int ver = 0;
                            if (!int.TryParse(list[3].ToString(), out ver))
                            {
                                Invoke(new Action(() => { MessageBox.Show("Error: Spreadsheet version is not a number.", Resources.ErrorDialogTitle); }));
                                break;
                            }
                            // saves correct version number
                            _spreadsheetVersion = ver;
                            _pendingChanges = false;
                        }
                        // if CHANGE FAIL was received
                        else if (list[1].Equals("FAIL"))
                        {
                            //display the informative message when an error occurs
                            Invoke(new Action(() => { MessageBox.Show("Error occurred updating cell: " + list[3].ToString(), Resources.ErrorDialogTitle); }));
                        }
                        break;
                    }

                case "UNDO":
                    {
                        // if UNDO OK was received
                        if (list[1].Equals("OK"))
                        {
                            Invoke(new Action(() => { RefreshSelectedCell(list[4].ToString(), list[6].ToString()); }));

                            int ver = 0;
                            if (!int.TryParse(list[3].ToString(), out ver))
                            {
                                Invoke(new Action(() => { MessageBox.Show("Error: Spreadsheet version is not a number.", Resources.ErrorDialogTitle); }));
                                break;
                            }
                            // saves correct version number
                            _spreadsheetVersion = ver;
                        }
                        // if UNDO WAIT was received
                        else if (list[1].Equals("WAIT"))
                        {
                            int ver = 0;
                            if (!int.TryParse(list[3].ToString(), out ver))
                            {
                                Invoke(new Action(() => { MessageBox.Show("Error: Spreadsheet version is not a number.", Resources.ErrorDialogTitle); }));
                                break;
                            }
                            // saves correct version number
                            _spreadsheetVersion = ver;
                        }
                        // if UNDO FAIL was received
                        else if (list[1].Equals("FAIL"))
                        {
                            // display the informative message when an error occurs
                            Invoke(new Action(() => { MessageBox.Show(Resources.ErrorDialogTextUndoUnknown + ": " + list[3].ToString(), Resources.ErrorDialogTitle); })); 
                        }
                        // if UNDO END was received
                        else if (list[1].Equals("END")) { }
                        break;
                    }

                case "UPDATE":
                    {
                        // update the cell
                        RefreshSelectedCell(list[3].ToString(), list[5].ToString());
                        int ver = 0;
                        if (!int.TryParse(list[2].ToString(), out ver))
                        {
                            Invoke(new Action(() => { MessageBox.Show("Error: Spreadsheet version is not a number.", Resources.ErrorDialogTitle); }));
                            break;
                        }
                        // saves correct version number
                        _spreadsheetVersion = ver;
                        break;
                    }

                case "SAVE":
                    {
                        // if SAVE OK was received
                        if (list[1].ToString().ToLower().Equals("true"))
                        { Invoke(new Action(() => { MessageBox.Show("Save Successful", "Save Successful"); })); }
                        // if SAVE FAIL was received
                        else
                        {
                            // display the informative message when an error occurs
                            Invoke(new Action(() => { MessageBox.Show(Resources.ErrorDialogTextSavingFile + ": " + list[3].ToString(), Resources.ErrorDialogTitle); })); 
                        }
                        break;
                    }
                case "ERROR":
                    {
                        // display the informative message when an error occurs
                        Invoke(new Action(() => { MessageBox.Show("Invalid command sent to server", Resources.ErrorDialogTitle); }));
                        break;
                    }
            }
        }

        #endregion

        #region Menu Item Click Event Methods

        /// <summary>
        /// Saves changes to the current spreadsheet if already saved to a file
        /// or prompts the user to save it to a file.
        /// </summary>
        private void SaveMenuItemClick(object sender, EventArgs e)
        {
            try
            {
                //No changes have been made
                if (!_spreadSheet.Changed)
                    return;

                _model.Save(_ssCredentials.Key);
            }
            catch (SpreadsheetReadWriteException)
            {
                //Show simple error message to the user that the file could not be saved
                MessageBox.Show(this, Resources.ErrorDialogTextSavingFile, Resources.ErrorDialogTitle);
            }
            catch (Exception)
            {
                //Show simple error message to the user that the file could not be saved
                MessageBox.Show(this, Resources.ErrorDialogTextSavingFileUnknown, Resources.ErrorDialogTitle);
            }

        }

        /// <summary>
        /// Opens a new spreadsheet form.
        /// </summary>
        private void NewMenuItemClick(object sender, EventArgs e)
        {
            try
            {
                //Check if connected
                if (!_connected)
                {
                    MessageBox.Show(this, "Please connect to a server first.", "Not Connected");
                    return;
                }

                //Prompt user for spreadsheet name and password
                InputBox input = new InputBox(this, true);
                var result = input.ShowDialog(this);

                //Check if user canceled
                if (result != System.Windows.Forms.DialogResult.OK)
                    return;

                //Check if we are already editing a spreadsheet
                if (_spreadsheetVersion < 0)
                    _model.Create(_ssCredentials.Key, _ssCredentials.Value);
                else
                    SpreadSheetApplicationContext.GetSpreadsheetAppContext().RunForm(new SpreadsheetForm(_host, _newFormCredentials.Key, _newFormCredentials.Value, true));
            }
            catch (Exception)
            {
                //Show simple error message to the user that a new spreadsheet could not be opened
                MessageBox.Show(this, Resources.ErrorDialogTextUnknown + Resources.ErrorDialogTextCouldNotOpenNew, Resources.ErrorDialogTitle);
            }

        }

        /// <summary>
        /// Prompts the user to open an existing spreadsheet file.
        /// </summary>
        private void OpenMenuMenuItemClick(object sender, EventArgs e)
        {
            try
            {
                //Check if connected
                if (!_connected)
                {
                    MessageBox.Show(this, "Please connect to a server first.", "Not Connected");
                    return;
                }

                //Prompt user for spreadsheet name and password
                InputBox input = new InputBox(this, false);
                var result = input.ShowDialog(this);

                //Check if user canceled
                if (result != System.Windows.Forms.DialogResult.OK)
                    return;

                //Check if we are already editing a spreadsheet
                if (_spreadsheetVersion < 0)
                    _model.Join(_ssCredentials.Key, _ssCredentials.Value);
                else
                    SpreadSheetApplicationContext.GetSpreadsheetAppContext().RunForm(new SpreadsheetForm(_host, _newFormCredentials.Key, _newFormCredentials.Value, false));
            }
            catch (SpreadsheetReadWriteException)
            {
                //Show simple error message to the user that the file could not be opened
                MessageBox.Show(this, Resources.ErrorDialogTextOpeningFile, Resources.ErrorDialogTitle);
            }
            catch (Exception)
            {
                //Show simple error message to the user that the file could not be opened
                MessageBox.Show(this, Resources.ErrorDialogTextOpeningFileUnknown, Resources.ErrorDialogTitle);
            }
        }

        /// <summary>
        /// Closes the current spreadsheet form (after appropriately 
        /// prompting the user if they want to save changes if needed).
        /// </summary>
        private void CloseMenuItemClick(object sender, EventArgs e)
        {
            //This will raise the close form handler 
            //event (CloseSpreadsheet) for the current form
            Close();
        }

        /// <summary>
        /// Closes the current spreadsheet form (after appropriately 
        /// prompting the user if they want to save changes if needed).
        /// </summary>
        private void CloseSpreadsheet(object sender, FormClosingEventArgs e)
        {
            //Check if in an editing session
            if(_spreadsheetVersion >= 0)
                _model.Leave(_ssCredentials.Key);
            e.Cancel = false;
        }

        /// <summary>
        /// Closes any open forms (while appropriately prompting users if
        /// changes need to be saved) and exits the spreadsheet application.
        /// </summary>
        private void ExitMenuItemClick(object sender, EventArgs e)
        {
            //This will raise the close form handler 
            //event (CloseSpreadsheet) for all open forms
            Application.Exit();
        }

        /// <summary>
        /// Undoes the previous action committed by the user.
        /// </summary>
        private void UndoMenuItemClick(object sender, EventArgs e)
        {
            try
            {
                _model.Undo(_ssCredentials.Key, _spreadsheetVersion);
            }
            catch (Exception)
            {
                //Show simple error message to the user that the previous action could not be undone
                MessageBox.Show(this, Resources.ErrorDialogTextUndoUnknown, Resources.ErrorDialogTitle);
            }
        }

        /// <summary>
        /// Displays the help contents in a message box
        /// indicating to the user how to use the application.
        /// </summary>
        private void HelpContentsMenuItemClick(object sender, EventArgs e)
        {
            string helpContents = Resources.HelpDialogTextContents1 + "\n\n" +
                                  Resources.HelpDialogTextContents2 + "\n\n" +
                                  Resources.HelpDialogTextContents3;

            MessageBox.Show(this, helpContents, Resources.HelpDialogTitle);
        }

        /// <summary>
        /// Displays information about the application in a message box 
        /// including the version number and the authors. 
        /// </summary>
        private void AboutMenuItemClick(object sender, EventArgs e)
        {
            string about = Resources.HelpDialogTextAbout1 + "\n" +
                           Resources.HelpDialogTextAbout2 + "\n\n" +
                           Resources.HelpDialogTextAbout3 + "\n" +
                           Resources.HelpDialogTextAbout4 + "\n\n" +
                           Resources.HelpDialogTextAbout5;

            MessageBox.Show(this, about, Resources.HelpDialogTitle);
        }

        /// <summary>
        /// Represents Connect button click. Displays a connect boxs that prompts the user to entr a server ip.
        /// </summary>
        private void ConnectMenuItemClick(object sender, EventArgs e)
        {
            if (!_connected)
            {
                ConnectBox connectBox = new ConnectBox(this);
                connectBox.ShowDialog();
            }
        }

        #endregion

        #region Cell and Text Box Change Event Methods

        /// <summary>
        /// Displays the name, the value and the contents of
        /// the cell, currently selected in the spreadsheet panel, in
        /// the form textboxes. 
        /// </summary>
        private void DisplaySelectedCellInformation(SpreadsheetPanel ss)
        {
            int row, col;
            ss.GetSelection(out col, out row);

            _selectedColumn = col;
            _selectedRow = row;

            //Get the cell name and its value and content
            _selectedCellName = FormHelper.GetCellName(col, row);
            string selectedCellValue = GetSelectedCellValue(_selectedCellName);
            string selectCellContents = GetSelectedCellContents(_selectedCellName);

            //Display the cell information
            mainSpreadsheetPanel.SetValue(_selectedColumn, _selectedRow, selectedCellValue);
            selectedCellTextBox.Text = _selectedCellName;
            cellValueTextBox.Text = selectedCellValue;
            cellContentsTextBox.Text = selectCellContents;
            cellContentsTextBox.Focus();
            _selectedCellContentsChanged = false;
        }

        /// <summary>
        /// Used to identify if the contents of the currently selected cell 
        /// have been changed by the user.
        /// </summary>
        private void CellContentsTextBoxTextChanged(object sender, EventArgs e)
        {
            _selectedCellContentsChanged = true;
        }

        /// <summary>
        /// Changes the selected cell of the spreadsheet panel. 
        /// </summary>
        private void ChangeSelectedCell(object sender, KeyEventArgs e)
        {
            //Do not respond if the spreadsheet is readonly or if we are pending changes
            if (_readonly || _pendingChanges)
                return;

            //Check if the arrow keys or Enter were pressed and respond accordingly
            int newRow, newCol;
            switch (e.KeyCode)
            {
                case Keys.Return:
                case Keys.Down:
                    newRow = _selectedRow + 1; //Move down a cell
                    if (newRow > FormHelper.MaxRowCount)
                        break;
                    _pendingSelectedColumn = _selectedColumn;
                    _pendingSelectedRow = newRow;
                    break;
                case Keys.Right:
                    newCol = _selectedColumn + 1; // Move to the cell on the right
                    if (newCol > FormHelper.MaxColCount)
                        break;
                    _pendingSelectedColumn = newCol;
                    _pendingSelectedRow = _selectedRow;
                    break;
                case Keys.Up:
                    newRow = _selectedRow - 1; //Move up a cell
                    if (newRow < 0)
                        break;
                    _pendingSelectedColumn = _selectedColumn;
                    _pendingSelectedRow = newRow;
                    break;
                case Keys.Left:
                    newCol = _selectedColumn - 1; //Move to the cell on the left
                    if (newCol < 0)
                        break;
                    _pendingSelectedColumn = newCol;
                    _pendingSelectedRow = _selectedRow;
                    break;
                default: 
                    return;
            }

            SendChangeRequest();

            // Check if we are pending changes, if not we can go to the next cell
            if (!_pendingChanges)
                CommitChanges();
        }

        #endregion

        #region Helper Methods

        private void SendChangeRequest()
        {
            // Send a change request if one has not been sent yet
            if (!_pendingChanges && _selectedCellContentsChanged)
            {
                //Get initial value in case the cell was modifed to something invalid
                string temp = GetSelectedCellContents(_selectedCellName);

                // Check if the change will be valid
                Exception e = null;
                if (!_spreadSheet.SetContentsOfCellIsSafe(_selectedCellName, cellContentsTextBox.Text, ref e))
                {
                    if (e is FormulaFormatException)
                    {
                        //Show simple error message to the user about the formula format
                        MessageBox.Show(this, Resources.ErrorDialogTextFormulaFormat, Resources.ErrorDialogTitle);
                    }
                    else if (e is CircularException)
                    {
                        //Show simple error message to the user about a circular reference
                        MessageBox.Show(this, Resources.ErrorDialogTextCircularReference, Resources.ErrorDialogTitle);
                    }
                    else
                    {
                        //Show simple error message to the user that an unknown error occurred
                        MessageBox.Show(this, Resources.ErrorDialogTextUnknown + " " + Resources.ErrorDialogTextNoChangesMade, Resources.ErrorDialogTitle);
                    }
                    cellContentsTextBox.Text = temp;
                    _selectedCellContentsChanged = false;
                    return;
                }
                _model.Change(_ssCredentials.Key, _spreadsheetVersion, _selectedCellName, cellContentsTextBox.Text.Length, cellContentsTextBox.Text);
                _pendingChanges = true;
            }
        }

        /// <summary>
        /// Saves pending changes to spreadsheet after the server approves the changes.
        /// </summary>
        private void CommitChanges()
        {
            //update the cell information after approval
            RefreshSelectedCell(_selectedCellName, cellContentsTextBox.Text);
            mainSpreadsheetPanel.SetSelection(_pendingSelectedColumn, _pendingSelectedRow);
            _pendingChanges = false;
            DisplaySelectedCellInformation(mainSpreadsheetPanel);
        }

        /// <summary>
        /// Evaluates the contents of a cell and displays 
        /// its resulting value in the spreadsheet panel.
        /// </summary>
        private void RefreshSelectedCell(string cellName, string contents)
        {
            try
            {
                //set the contents of the cell and recalculate all dependencies
                var cellsToRecalculate = _spreadSheet.SetContentsOfCell(cellName, contents);
                RecalculateCells(cellsToRecalculate);
                string selectedCellValue = GetSelectedCellValue(cellName);
                int col, row;
                FormHelper.GetCellCoordinates(cellName, out col, out row);
                mainSpreadsheetPanel.SetValue(col, row, selectedCellValue);
            }
            catch (FormulaFormatException)
            {
                //Show simple error message to the user about the formula format
                MessageBox.Show(this, Resources.ErrorDialogTextFormulaFormat, Resources.ErrorDialogTitle);
            }
            catch (CircularException)
            {
                //Show simple error message to the user about a circular reference
                MessageBox.Show(this, Resources.ErrorDialogTextCircularReference, Resources.ErrorDialogTitle);
            }
            catch (Exception)
            {
                //Show simple error message to the user that an unknown error occurred
                MessageBox.Show(this, Resources.ErrorDialogTextUnknown + " " + Resources.ErrorDialogTextNoChangesMade, Resources.ErrorDialogTitle);
            }
        }

        /// <summary>
        /// Retrieves the value of the given cell name 
        /// as a string.
        /// </summary>
        private string GetSelectedCellValue(string cellName)
        {
            //Get the cell value
            var val = _spreadSheet.GetCellValue(cellName);
            if (val is double || val is string)
                return val.ToString();
            if (val is FormulaError)
            {
                var formError = (FormulaError)val;
                return formError.Reason;
            }

            return string.Empty; //must be empty            
        }

        /// <summary>
        /// Retrieves the contents of the given cell name 
        /// as a string.
        /// </summary>
        private string GetSelectedCellContents(string cellName)
        {
            //Get the cell contents
            var content = _spreadSheet.GetCellContents(cellName);
            if (content is double || content is string)
                return content.ToString();
            if (content is Formula)
                return "=" + content;

            return string.Empty; //must be empty
        }

        /// <summary>
        /// Displays the values of all the cells in 
        /// the actual spreadhseet panel after opening a 
        /// saved spreadsheet file.
        /// </summary>
        private void PopulateCells()
        {
            //clear the panel before repopulating the spreadsheet
            mainSpreadsheetPanel.Clear();

            //display the value of each non-empty cell in the spreadsheet panel
            foreach (var cell in _spreadSheet.GetNamesOfAllNonemptyCells())
            {
                int col, row;
                FormHelper.GetCellCoordinates(cell, out col, out row);
                if (col == -1 || row == -1)
                {
                    //Indicate to the user that an error occurred while trying to populate the cells
                    MessageBox.Show(this, Resources.ErrorDialogTextFileLoad, Resources.ErrorDialogTitle);
                    return;
                }
                mainSpreadsheetPanel.SetValue(col, row, GetSelectedCellValue(cell));
            }

            //recalculate the cells now that all variables are loaded
            RecalculateCells(_spreadSheet.GetNamesOfAllNonemptyCells());

            //set the currently selected cell to the top left corner
            mainSpreadsheetPanel.SetSelection(0, 0);
            DisplaySelectedCellInformation(mainSpreadsheetPanel);
        }

        /// <summary>
        /// Recalculates the cells that are directly or indirectly affected
        /// by the cell that most recently had its value changed.
        /// </summary>
        private void RecalculateCells(IEnumerable<string> cellsToRecalculate)
        {
            if (cellsToRecalculate == null)
                return;

            //iterate through every cell to recalculate
            foreach (var cell in cellsToRecalculate)
            {
                //verify it is a formula
                var cellContents = _spreadSheet.GetCellContents(cell);
                if (!(cellContents is Formula))
                    continue;

                //reset the contents of the cell to recalculate
                //its value if necessary
                string contents = GetSelectedCellContents(cell);
                _spreadSheet.SetContentsOfCell(cell, contents);

                //display the recalculated value in the spreadsheet panel
                int col, row;
                FormHelper.GetCellCoordinates(cell, out col, out row);
                if (col == -1 || row == -1)
                {
                    //Indicate to the user that an error occurred while trying to recalculate the cells
                    MessageBox.Show(this, Resources.ErrorDialogTextRecalculatingCells, Resources.ErrorDialogTitle);
                    return;
                }
                mainSpreadsheetPanel.SetValue(col, row, GetSelectedCellValue(cell));
            }
        }

        /// <summary>
        /// Make the spreadsheet readonly.
        /// </summary>
        private void SetReadOnly()
        {
            _readonly = true;
            mainSpreadsheetPanel.Enabled = false;
            saveMenuItem.Enabled = false;
            undoMenuItem.Enabled = false;
            cellContentsTextBox.ReadOnly = true;
        }

        /// <summary>
        /// Make the spreadsheet editable.
        /// </summary>
        private void EnableEditing()
        {
            _readonly = false;
            mainSpreadsheetPanel.Enabled = true;
            saveMenuItem.Enabled = true;
            undoMenuItem.Enabled = true;
            cellContentsTextBox.ReadOnly = false;
            cellContentsTextBox.Focus();
        }

        #endregion
    }
}

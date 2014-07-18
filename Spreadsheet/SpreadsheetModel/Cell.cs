using System;
using System.Globalization;
using SpreadsheetUtilities;

namespace SpreadsheetModel
{
    /// <summary>
    /// Represents cells in a spreadsheet.  Cells have a name, a value and content.
    /// 
    /// The contents of a cell can be (1) a string, (2) a double, or (3) a Formula.  If the
    /// contents is an empty string, we say that the cell is empty.  (By analogy, the contents
    /// of a cell in Excel is what is displayed on the editing line when the cell is selected.)
    /// 
    /// In a new spreadsheet, the contents of every cell is the empty string.
    ///  
    /// The value of a cell can be (1) a string, (2) a double, or (3) a FormulaError.  
    /// (By analogy, the value of an Excel cell is what is displayed in that cell's position
    /// in the grid.)
    /// 
    /// If a cell's contents is a string, its value is that string.
    /// 
    /// If a cell's contents is a double, its value is that double.
    /// 
    /// If a cell's contents is a Formula, its value is either a double or a FormulaError,
    /// as reported by the Evaluate method of the Formula class.  The value of a Formula,
    /// of course, can depend on the values of variables.  The value of a variable is the 
    /// value of the spreadsheet cell it names (if that cell's value is a double) or 
    /// is undefined (otherwise).
    /// </summary>
    internal class Cell
    {
        #region Members and Properties

        //The Cell class uses nested classes to represent the structure of a cell 
        //including its contents and its value.  There are three nested classes. One 
        //that represents the structure of the cell when its contents contain a string, one 
        //when its contents contain a double and one when it conatins a Formula. 
        //Each nested class has a boolean property that indicates if it is the being used 
        //to represent the cell's structure. This boolean value is used as the condition to determine 
        //what type of content and value to return when requested.  Thus when a cell is constucted
        //each nested class is initialized but only one is set at any given time.

        private readonly string _cellName;                 //Keeps track of the cell name
        private readonly TextContents _textContents;   //Keeps track of the contents as a string
        private readonly DoubleContents _doubleContents;   //Keeps track of the contents as a double
        private readonly FormulaContents _formulaContents; //Keeps track of the contents as a formula
        

        /// <summary>
        /// The name of the cell.
        /// </summary>
        public string CellName { get { return _cellName; } }

        /// <summary>
        /// The contents of the cell. 
        /// </summary>
        public object CellContents
        {
            get
            {
                if (_textContents.IsSet)
                    return _textContents.Content;
                if (_doubleContents.IsSet)
                    return _doubleContents.Content;
                return _formulaContents.IsSet ? _formulaContents.Content : null;
            }
        }

        /// <summary>
        /// The value of the cell.
        /// </summary>
        public object CellValue
        {
            get
            {
                if (_textContents.IsSet)
                    return _textContents.Value;
                if (_doubleContents.IsSet)
                    return _doubleContents.Value;
                return _formulaContents.IsSet ? _formulaContents.Value : null;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an empty cell.
        /// </summary>
        public Cell(string cellName)
        {
            _cellName = cellName;
            _textContents = new TextContents(String.Empty, true); //contains an empty string
            _doubleContents = new DoubleContents(0, false);
            _formulaContents = new FormulaContents(null, null, false);
        }

        /// <summary>
        /// Constructs a cell with a string as its content.
        /// </summary>
        public Cell(string cellName, string content)
        {
            _cellName = cellName;
            _textContents = new TextContents(content, true); //contains a string
            _doubleContents = new DoubleContents(0, false);
            _formulaContents = new FormulaContents(null, null, false);
        }

        /// <summary>
        /// Constructs a cell with a double as its content.
        /// </summary>
        public Cell(string cellName, double content)
        {
            _cellName = cellName;
            _textContents = new TextContents(String.Empty, false);
            _doubleContents = new DoubleContents(content, true); //contains a double
            _formulaContents = new FormulaContents(null, null, false);
        }

        /// <summary>
        /// Constructs a cell with a Formula as its content.
        /// </summary>
        public Cell(string cellName, Formula content, object value)
        {
            _cellName = cellName;
            _textContents = new TextContents(String.Empty, false);
            _doubleContents = new DoubleContents(0, false);
            _formulaContents = new FormulaContents(content, value, true); //contains a formula
        }

        #endregion

        #region Set Cell Contents Methods

        /// <summary>
        /// Clears the cell's contents.
        /// </summary>
        public void SetCellContent()
        {
            _textContents.SetFields(String.Empty, true); //contains an empty string
            _doubleContents.SetFields(0, false);
            _formulaContents.SetFields(null, null, false);
        }

        /// <summary>
        /// Sets a cell's content to a string.
        /// </summary>
        public void SetCellContent(string content)
        {
            _textContents.SetFields(content, true); //contains a string
            _doubleContents.SetFields(0, false);
            _formulaContents.SetFields(null, null, false);
        }

        /// <summary>
        /// Sets a cell's content to a double.
        /// </summary>
        public void SetCellContent(double content)
        {
            _textContents.SetFields(String.Empty, false);
            _doubleContents.SetFields(content, true); //contains a double
            _formulaContents.SetFields(null, null, false);
        }

        /// <summary>
        /// Sets a cell's content to a Formula.
        /// </summary>
        public void SetCellContent(Formula content, object value)
        {
            _textContents.SetFields(String.Empty, false);
            _doubleContents.SetFields(0, false);
            _formulaContents.SetFields(content, value, true); //contains a formula
        }

        #endregion

        #region Private Cell Extension Classes

        /// <summary>
        /// Represents the contents of the cell when it is a string.
        /// </summary>
        private class TextContents
        {
            public string Content { get; private set; }
            public string Value { get; private set; }
            public bool IsSet { get; private set; }

            /// <summary>
            /// Constructor for StringContents.
            /// </summary>
            public TextContents(string content, bool cellContainsString)
            {
                Content = content;
                Value = content;
                IsSet = cellContainsString;
            }

            /// <summary>
            /// Updates properties of StringContents.
            /// </summary>
            public void SetFields(string content, bool cellContainsString)
            {
                Content = content;
                Value = content;
                IsSet = cellContainsString;
            }
        }

        /// <summary>
        /// Represents the contents of the cell when it is a double.
        /// </summary>
        private class DoubleContents
        {
            public double Content { get; private set; }
            public double Value { get; private set; }
            public bool IsSet { get; private set; }

            /// <summary>
            /// Constructor for DoubleContents.
            /// </summary>
            public DoubleContents(double content, bool cellContainsDouble)
            {
                Content = content;
                Value = content;
                IsSet = cellContainsDouble;
            }

            /// <summary>
            /// Updates properties of DoubleContents.
            /// </summary>
            public void SetFields(double content, bool cellContainsDouble)
            {
                Content = content;
                Value = content;
                IsSet = cellContainsDouble;
            }
        }

        /// <summary>
        /// Represents the contents of the cell when it is a Formula.
        /// </summary>
        private class FormulaContents
        {
            public Formula Content { get; private set; }
            public object Value { get; private set; }
            public bool IsSet { get; private set; }

            /// <summary>
            /// Constructor for FormulaContents.
            /// </summary>
            public FormulaContents(Formula content, object value, bool cellContainsFormula)
            {
                Content = content;
                Value = value;
                IsSet = cellContainsFormula;
            }

            /// <summary>
            /// Updates properties of FormulaContents.
            /// </summary>
            public void SetFields(Formula content, object value, bool cellContainsFormula)
            {
                Content = content;
                Value = value;
                IsSet = cellContainsFormula;
            }
        }

        #endregion
    }
}

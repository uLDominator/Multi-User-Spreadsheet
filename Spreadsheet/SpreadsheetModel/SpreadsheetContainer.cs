using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SpreadsheetUtilities;
using System.IO;

namespace SpreadsheetModel
{
    /// <summary>
    /// An AbstractSpreadsheet object represents the state of a simple spreadsheet.  A 
    /// spreadsheet consists of an infinite number of named cells.
    /// 
    /// A string is a cell name if and only if it consists of one or more letters,
    /// followed by one or more digits AND it satisfies the predicate IsValid.
    /// For example, "A15", "a15", "XY032", and "BC7" are cell names so long as they
    /// satisfy IsValid.  On the other hand, "Z", "X_", and "hello" are not cell names,
    /// regardless of IsValid.
    /// 
    /// Any valid incoming cell name, whether passed as a parameter or embedded in a formula,
    /// must be normalized with the Normalize method before it is used by or saved in 
    /// this spreadsheet.  For example, if Normalize is s => s.ToUpper(), then
    /// the Formula "x3+a5" should be converted to "X3+A5" before use.
    /// 
    /// A spreadsheet contains a cell corresponding to every possible cell name.  
    /// In addition to a name, each cell has a contents and a value.  The distinction is
    /// important.
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
    /// 
    /// Spreadsheets are never allowed to contain a combination of Formulas that establish
    /// a circular dependency.  A circular dependency exists when a cell depends on itself.
    /// For example, suppose that A1 contains B1*2, B1 contains C1*2, and C1 contains A1*2.
    /// A1 depends on B1, which depends on C1, which depends on A1.  That's a circular
    /// dependency.
    /// </summary>
    public class SpreadsheetContainer : AbstractSpreadsheetContainer
    {
        #region Members and Properties

        private const string DefaultVersion = "0"; //default version number

        //XML tag and attribute constants for xml file read/write
        private const string SpreadsheetXmlTag = "spreadsheet";
        private const string VersionXmlAttribute = "version";
        private const string CellXmlTag = "cell";
        private const string NameXmlTag = "name";
        private const string ContentsXmlTag = "contents";

        //data structures for the internal representation of spreadsheet
        private readonly DependencyGraph _dg; //used to maintain a relationship between cells that are dependent on each other
        private readonly LinkedList<string> _nonEmptyCellNames; //list that is maintained as cells are updated to keep track of non-empty cells
        private readonly HashSet<Cell> _cells; //set of cells that have had content entered into them
        private bool _changed; //used for the Changed property to avoid potential issues with using a virtual member call in the constructors

        /// <summary>
        /// True if this spreadsheet has been modified since it was created or saved                  
        /// (whichever happened most recently); false otherwise.
        /// </summary>
        public override bool Changed { get { return _changed; } protected set { _changed = value; } }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty spreadsheet that imposes no extra validity conditions other than the default cell
        /// name requirements (one or more letters followed by one or more digits), normalizes every cell name 
        /// using the default normalizer (capitalizes every letter in a cell name), and uses the default version 
        /// number (1.0).
        /// </summary>
        public SpreadsheetContainer()
            : base(SpreadsheetHelper.IsValidCellName, SpreadsheetHelper.NormalizeCellName, DefaultVersion)
        {
            //initialize all private members
            _dg = new DependencyGraph();
            _cells = new HashSet<Cell>();
            _nonEmptyCellNames = new LinkedList<string>();
            _changed = false;
        }

        /// <summary>
        /// Creates an empty spreadsheet that imposes the given validity conditions in addition to the default cell
        /// name requirements (one or more letters followed by one or more digits), normalizes every cell name 
        /// using the given normalizer, and uses the given version information.
        /// </summary>
        public SpreadsheetContainer(Func<string, bool> isValid, Func<string, string> normalize, string version) :
            base(isValid, normalize, version)
        {
            //initialize all private members
            _dg = new DependencyGraph();
            _cells = new HashSet<Cell>();
            _nonEmptyCellNames = new LinkedList<string>();
            _changed = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        public override IEnumerable<String> GetNamesOfAllNonemptyCells()
        {
            //Simply return the list of non-empty cell names that is populated when a cell has its contents set.
            return _nonEmptyCellNames;
        }

        /// <summary>
        /// If content is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if content parses as a double, the contents of the named
        /// cell becomes that double.
        /// 
        /// Otherwise, if content begins with the character '=', an attempt is made
        /// to parse the remainder of content into a Formula f using the Formula
        /// constructor.  There are then three possibilities:
        /// 
        ///   (1) If the remainder of content cannot be parsed into a Formula, a 
        ///       SpreadsheetUtilities.FormulaFormatException is thrown.
        ///       
        ///   (2) Otherwise, if changing the contents of the named cell to be f
        ///       would cause a circular dependency, a CircularException is thrown.
        ///       
        ///   (3) Otherwise, the contents of the named cell becomes f.
        /// 
        /// Otherwise, the contents of the named cell becomes content.
        /// 
        /// If an exception is not thrown, the method returns a set consisting of
        /// name plus the names of all other cells whose value depends, directly
        /// or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        public override ISet<string> SetContentsOfCell(string name, string content)
        {
            //Error checking
            CheckNameForErrors(ref name);

            //check if content is a double
            double number;
            if (double.TryParse(content, out number))
                return SetCellContents(name, number);

            //check if content is text
            if (!SpreadsheetHelper.ContainsEqualsSign(content))
                return SetCellContents(name, content);

            String temp = "";
            //check if content is a formula
            try
            {
                temp = GetCellContents(name).ToString();
                string formula = SpreadsheetHelper.RemoveEqualsSign(content);
                
                var f = new Formula(SpreadsheetHelper.NormalizeFormula(formula, Normalize)); //will throw a FormulaFormatException if not a valid formula
                return SetCellContents(name, f); //will throw a CirularException if the formula causes a circular dependency
            }
            catch (FormulaFormatException)
            {
                SetContentsOfCell(name, temp);
                throw;
            }
            catch (CircularException)
            {
                SetContentsOfCell(name, temp);
                throw;
            }
        }

        /// <summary>
        /// Previews if updating the the given cell with the 
        /// given contents is safe and will not cause any exceptions.
        /// Returns true if the cell is valid to update.
        /// </summary>
        public bool SetContentsOfCellIsSafe(string name, string content, ref Exception exception)
        {
            //Error checking
            CheckNameForErrors(ref name);

            //check if content is a double
            double number;
            if (double.TryParse(content, out number))
                return true;

            //check if content is text
            if (!SpreadsheetHelper.ContainsEqualsSign(content))
                return true;

            string toKeep = GetCellContents(name).ToString();
            
            //check if content is a formula
            try
            {
                string formula = SpreadsheetHelper.RemoveEqualsSign(content);

                var f = new Formula(SpreadsheetHelper.NormalizeFormula(formula, Normalize)); //will throw a FormulaFormatException if not a valid formula
                SetCellContents(name, f); //will throw a CirularException if the formula causes a circular dependency
            }
            catch (FormulaFormatException f)
            {
                exception = f;
                SetContentsOfCell(name, toKeep);
                return false;
            }
            catch (CircularException c)
            {
                exception = c;
                SetContentsOfCell(name, toKeep);
                return false;
            }
            catch (Exception e)
            {
                exception = e;
                SetContentsOfCell(name, toKeep);
                return false;
            }

            //If no errors were thrown, revert it back to the previous version and return true
            SetContentsOfCell(name, toKeep);
            return true;

        }


        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
        /// value should be either a string, a double, or a Formula.
        /// </summary>
        public override object GetCellContents(String name)
        {
            //Error checking
            CheckNameForErrors(ref name);

            //First check if _nonEmptyCellNames contains the given cell name.
            //If so, find the cell with the given name 
            //and return the contents of its corresponding cell.
            //Otherwise return an empty string since it must be empty.
            return _nonEmptyCellNames.Contains(name) ?
                _cells.Where(cell => cell.CellName == name).Select(cell => cell.CellContents).FirstOrDefault() : String.Empty;
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the value (as opposed to the contents) of the named cell.  The return
        /// value should be either a string, a double, or a SpreadsheetUtilities.FormulaError.
        /// </summary>
        public override object GetCellValue(string name)
        {
            //Error checking
            CheckNameForErrors(ref name);

            //First check if _nonEmptyCellNames contains the given cell name.
            //If so, find the cell with the given name 
            //and return the value of its corresponding cell.
            //Otherwise return an empty string since it must be empty.
            return _nonEmptyCellNames.Contains(name) ?
                _cells.Where(cell => cell.CellName == name).Select(cell => cell.CellValue).FirstOrDefault() : String.Empty;
        }

        /// <summary>
        /// Writes the contents of this spreadsheet to the named file using an XML format.
        /// The XML elements should be structured as follows:
        /// 
        /// <spreadsheet version="version information goes here">
        /// 
        /// <cell>
        /// <name>
        /// cell name goes here
        /// </name>
        /// <contents>
        /// cell contents goes here
        /// </contents>    
        /// </cell>
        /// 
        /// </spreadsheet>
        /// 
        /// There should be one cell element for each non-empty cell in the spreadsheet.  
        /// If the cell contains a string, it should be written as the contents.  
        /// If the cell contains a double d, d.ToString() should be written as the contents.  
        /// If the cell contains a Formula f, f.ToString() with "=" prepended should be written as the contents.
        /// 
        /// If there are any problems opening, writing, or closing the file, the method should throw a
        /// SpreadsheetReadWriteException with an explanatory message.
        /// </summary>
        public override void Save(string filename)
        {
            //Error checking
            if (string.IsNullOrWhiteSpace(Version))
                throw new SpreadsheetReadWriteException("Version number cannot be null or empty");
            if (!SpreadsheetHelper.IsValidFilePath(ref filename))
                throw new SpreadsheetReadWriteException("Cannot use filenames with the following characters: " + SpreadsheetHelper.InvalidFileNameCharacters);

            try
            {
                using (XmlWriter w = XmlWriter.Create(filename))
                {
                    w.WriteStartDocument();
                    w.WriteStartElement(SpreadsheetXmlTag); //start spreadsheet
                    w.WriteAttributeString(VersionXmlAttribute, Version);

                    foreach (var cell in _cells)
                    {
                        object cellContents = cell.CellContents;

                        //if it is an empty cell, don't worry about saving it to the XML file
                        if (cellContents as String == String.Empty)
                            continue;

                        w.WriteStartElement(CellXmlTag);
                        w.WriteElementString(NameXmlTag, cell.CellName);

                        //if it is a formula prepend "="
                        if (cellContents as Formula != null)
                            w.WriteElementString(ContentsXmlTag, "=" + cellContents);
                        else
                            w.WriteElementString(ContentsXmlTag, cellContents.ToString());

                        w.WriteEndElement();
                    }

                    w.WriteEndElement();
                    w.WriteEndDocument();
                    w.Close();
                }
                Changed = false; //just saved so now it is false
            }
            catch (Exception e)
            {
                //catch any unhandled errors
                throw new SpreadsheetReadWriteException("An error occurred trying to save spreadsheet." + e);
            }

        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes number.  The method returns a
        /// set consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        protected override ISet<String> SetCellContents(String name, double number)
        {
            //Error checking
            CheckNameForErrors(ref name);

            //Update the cell if it was already used in the spreadsheet
            bool updatingCell = false; //boolean value that indicates if the cell is being updated or added
            foreach (var cell in _cells.Where(cell => cell.CellName == name))
            {                
                RemoveExistingDependencies(name); //Remove any current dependencies before updating the cell
                cell.SetCellContent(number); //Update the cell's content
                if (!_nonEmptyCellNames.Contains(name)) //Add the cell to the _nonEmptyCellNames list if it was not there previously
                    _nonEmptyCellNames.AddLast(name);
                
                updatingCell = true;
                break;
            }

            //Otherwise add "new" cell to the spreadheet
            if (!updatingCell)
            {
                _cells.Add(new Cell(name.Trim(), number));
                _nonEmptyCellNames.AddLast(name); //Add the cell to the _nonEmptyCellNames list
            }

            //the spreadsheet has been changed
            Changed = true;

            //find dependent cells on the cell that was updated
            return SpreadsheetHelper.BuildHashSetFromEnumerable(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// If text is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes text.  The method returns a
        /// set consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        protected override ISet<String> SetCellContents(String name, String text)
        {
            //Error checking
            if (text == null)
                throw new ArgumentNullException();
            CheckNameForErrors(ref name);

            //Update the cell if it was already used in the spreadsheet
            bool updatingCell = false; //boolean value that indicates if the cell is being updated or added
            foreach (var cell in _cells.Where(cell => cell.CellName == name))
            {                
                RemoveExistingDependencies(name); //Remove any current dependencies before updating the cell
                cell.SetCellContent(text); //Update the cell's content
                if (!_nonEmptyCellNames.Contains(name) && !String.IsNullOrWhiteSpace(text))
                    _nonEmptyCellNames.AddLast(name); //Add the cell to the _nonEmptyCellNames list if it was not there previously
                else if (_nonEmptyCellNames.Contains(name) && String.IsNullOrWhiteSpace(text))
                    _nonEmptyCellNames.Remove(name); //Remove the cell from the _nonEmptyCellNames list if it is being set to empty
                
                updatingCell = true;
                break;
            }

            //Otherwise add "new" cell to the spreadheet
            if (!updatingCell)
            {                
                _cells.Add(new Cell(name.Trim(), text.Trim()));
                if (!String.IsNullOrWhiteSpace(text)) //Add the cell to the _nonEmptyCellNames list if the text is not empty
                    _nonEmptyCellNames.AddLast(name);
            }

            //the spreadsheet has been changed
            Changed = true;

            //find dependent cells on the cell that was updated
            return SpreadsheetHelper.BuildHashSetFromEnumerable(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// If formula parameter is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if changing the contents of the named cell to be the formula would cause a 
        /// circular dependency, throws a CircularException.
        /// 
        /// Otherwise, the contents of the named cell becomes formula.  The method returns a
        /// Set consisting of name plus the names of all other cells whose value depends,
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        protected override ISet<String> SetCellContents(String name, Formula formula)
        {
            //Error checking
            if (formula == null)
                throw new ArgumentNullException();
            CheckNameForErrors(ref name);

            //Update the cell if it was already used in the spreadsheet
            bool updatingCell = false; //boolean value that indicates if the cell is being updated or added
            foreach (var cell in _cells.Where(cell => cell.CellName == name))
            {               
                RemoveExistingDependencies(name); //Remove any current dependencies before updating the cell
                cell.SetCellContent(formula, formula.Evaluate(LookupCellValue)); //Update the cell's content
                if (!_nonEmptyCellNames.Contains(name)) //Add the cell to the _nonEmptyCellNames list if it was not there previously
                    _nonEmptyCellNames.AddLast(name);
                
                updatingCell = true;
                break;
            }

            //Otherwise add "new" cell to the spreadheet
            if (!updatingCell)
            {                
                _cells.Add(new Cell(name.Trim(), formula, formula.Evaluate(LookupCellValue)));
                _nonEmptyCellNames.AddLast(name); //Add the cell to the _nonEmptyCellNames list     
            }

            //Add new dependencies for the formula
            var dependees = formula.GetVariables();
            foreach (var dependee in dependees)
                _dg.AddDependency(dependee, name);

            //the spreadsheet has been changed
            Changed = true;

            //find dependent cells on the cell that was updated
            return SpreadsheetHelper.BuildHashSetFromEnumerable(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// If name is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name isn't a valid cell name, throws an InvalidNameException.
        /// 
        /// Otherwise, returns an enumeration, without duplicates, of the names of all cells whose
        /// values depend directly on the value of the named cell.  In other words, returns
        /// an enumeration, without duplicates, of the names of all cells that contain
        /// formulas containing name.
        /// 
        /// For example, suppose that
        /// A1 contains 3
        /// B1 contains the formula A1 * A1
        /// C1 contains the formula B1 + A1
        /// D1 contains the formula B1 - C1
        /// The direct dependents of A1 are B1 and C1
        /// </summary>
        protected override IEnumerable<String> GetDirectDependents(String name)
        {
            //Error checking
            if (name == null)
                throw new ArgumentNullException();
            CheckNameForErrors(ref name);

            //Simply return the list of dependents for the given cell name
            return _dg.GetDependents(name);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Recreates a spreadsheet object using the spreedsheet information
        /// contained in the provided xml string.
        /// </summary>
        public void LoadCellsFromXML(string xml)
        {
            //Error checking    
            if (string.IsNullOrWhiteSpace(xml))
                throw new SpreadsheetReadWriteException("Wrong spreadsheet file syntax.");

            try
            {
                using (XmlReader r = XmlReader.Create(new StringReader(xml)))
                {
                    while (r.Read())
                    {
                        if (!r.IsStartElement())
                            continue;

                        switch (r.Name)
                        {
                            case SpreadsheetXmlTag:
                                continue;
                            case CellXmlTag:
                                while (r.Name != NameXmlTag) { r.Read(); }
                                string cellName = r.ReadElementContentAsString();
                                while (r.Name != ContentsXmlTag) { r.Read(); }
                                string cellContents = r.ReadElementContentAsString();
                                SetContentsOfCell(cellName, cellContents);
                                break;
                        }
                    }
                    r.Close(); //close the file
                }
            }
            catch (Exception)
            {
                //catch any unhandled errors
                throw new SpreadsheetReadWriteException("An error occurred trying to load cell values from saved version of spreadsheet");
            }
        }


        /// <summary>
        /// Used as the lookup method for evaluating a formula.
        /// </summary>
        private double LookupCellValue(string name)
        {
            //Error checking
            CheckNameForErrors(ref name);

            try
            {
                foreach (var cell in _cells.Where(cell => cell.CellName == name))
                    return (double)cell.CellValue;

            }
            catch (Exception)
            {
                //if any exceptions occur when trying to evaluate
                //throw an argument exception
                throw new ArgumentException();
            }

            //if we get to this point then the value could not be found 
            //so throw an argument exception - this will result in returning 
            //a FormulaError being set as the value of the cell
            throw new ArgumentException();
        }

        /// <summary>
        /// Removes any existing dependencies whose dependent is the given cell name. 
        /// </summary>
        private void RemoveExistingDependencies(string name)
        {
            //Error checking
            CheckNameForErrors(ref name);

            //list that will contain the dependess to remove
            var dependeesToRemove = new LinkedList<String>();

            //first retrieve the dependees of the given cell name
            var formulaDependees = _dg.GetDependees(name);
            foreach (var dependee in formulaDependees)
                dependeesToRemove.AddLast(dependee);

            //remove each dependency where the cell is a dependent
            foreach (var dependee in dependeesToRemove)
                _dg.RemoveDependency(dependee, name);
        }

        /// <summary>
        /// Performs all standard checks against a cell name to determine
        /// if it is valid.  If the name is valid, returns the normalized 
        /// cell name.  Otherwise, throws the appropriate exception. 
        /// </summary>
        private void CheckNameForErrors(ref string name)
        {
            //Perform initial error checking
            if (name == null || !SpreadsheetHelper.IsValidCellName(name))
                throw new InvalidNameException();

            //Normalize the cell name and do final name check using the provided IsValid 
            //method. The try block is in case the Normalize or IsValid methods cause 
            //exceptions, in which case the method just throws an InvalidNameException
            try
            {
                name = Normalize(name);
                if (!IsValid(name))
                    throw new InvalidNameException();
            }
            catch (Exception)
            {
                throw new InvalidNameException();
            }
        }

        #endregion
    }
}

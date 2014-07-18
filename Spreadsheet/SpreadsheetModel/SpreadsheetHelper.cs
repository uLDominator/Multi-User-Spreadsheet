using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetModel
{
    /// <summary>
    /// Helper Class for Spreadsheet.
    /// </summary>
    internal static class SpreadsheetHelper
    {
        public const string InvalidFileNameCharacters = @"\/:*?""<>|";

        /// <summary>
        /// Determines if a given name is a valid cell name. (Default Isvalid method)
        /// </summary>
        /// <param name="name">Cell name</param>
        /// <returns>True if the given cell name is valid, False otherwise</returns>
        public static bool IsValidCellName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                return false;

            const string variablePattern = @"^[a-zA-Z][1-9][0-9]?$";
            return Regex.IsMatch(name.Trim(), variablePattern);
        }

        /// <summary>
        /// Default Normalize method that makes every cell letter uppercase
        /// </summary>
        public static string NormalizeCellName(string name)
        {
            return String.IsNullOrWhiteSpace(name) ? String.Empty : name.ToUpper().Trim();
        }

        /// <summary>
        /// Determines if the provided string has an equals sign at the front
        /// </summary>
        public static bool ContainsEqualsSign(string content)
        {
            if (String.IsNullOrWhiteSpace(content)) return false;

            return content.Trim()[0] == '=';
        }

        /// <summary>
        /// Removes the equal sign from a formula
        /// </summary>
        public static string RemoveEqualsSign(string formula)
        {
            return ContainsEqualsSign(formula) ? formula.Trim().Remove(0, 1).Trim() : formula;
        }

        /// <summary>
        /// Checks if the provided filepath is valid according Windows OS file name standards
        /// </summary>
        public static bool IsValidFilePath(ref string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
                return false;

            var files = Regex.Split(filePath, Regex.Escape("\\")).ToList();

            // remove the drive portion if a full path is given 
            //for error checking (e.g. remove C: from C:\filename)
            if (files.Count > 1 && files.First().Contains(":"))
                files.Remove(files.First());

            //standard windows machines don't allow the use of these characters \/:*?"<>|
            var invalidCharArray = new[] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|" };
            if ((from fileLocation in files from c in invalidCharArray where fileLocation.Contains(c) select fileLocation).Any())
            {
                //if any of the folder names or the end file name contains invalid 
                //characters return false
                return false;
            }

            //append .ss to the file name if it needs it
            if (!files.Last().ToLower().Contains(".ss"))
                filePath += ".ss";
            return true;
        }

        /// <summary>
        /// Appends .xml to a file name if it is missing
        /// </summary>
        public static void AppendSsFileTag(ref string filename)
        {
            if (!filename.ToLower().Contains(".ss"))
                filename += ".ss";
        }

        /// <summary>
        /// Builds a hash set out of an IEnumerable list.
        /// </summary>
        public static ISet<String> BuildHashSetFromEnumerable(IEnumerable<String> list)
        {
            if (list == null)
                return null;

            var set = new HashSet<String>();
            foreach (var item in list)
                set.Add(item);

            return set;
        }

        /// <summary>
        /// Given a formula, enumerates the tokens that compose it.  Tokens are left paren,
        /// right paren, one of the four operator symbols, a string consisting of one or more
        /// letters followed by one or more digits, a double literal, and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        public static string NormalizeFormula(String formula, Func<string, string> normalize)
        {
            // Patterns for individual tokens
            const String lpPattern = @"\(";
            const String rpPattern = @"\)";
            const String opPattern = @"[\+\-*/]";
            const String varPattern = @"[a-zA-Z]+\d+";
            const String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: e[\+-]?\d+)?";
            const String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            //Enumerate matching tokens that don't consist solely of white space.
            var tokens = Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace).Where(s => !Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline)).ToArray();

            var normalizedFormula = new StringBuilder();
            for (int i = 0; i < tokens.Length; i++)
            {
                normalizedFormula.Append(Regex.IsMatch(tokens[i].Trim(), varPattern) ? normalize(tokens[i]) : tokens[i]);
            }

            return normalizedFormula.ToString();
        }
    }
}

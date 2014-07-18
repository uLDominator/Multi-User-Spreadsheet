using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpreadsheetView
{
    /// <summary>
    /// Class with methods that assist the functionality of 
    /// thh Spreadhseet Form. 
    /// </summary>
    internal static class FormHelper
    {
        public const int MaxColCount = 26;
        public const int MaxColIndex = 25;
        public const int MaxRowCount = 99;
        public const int MaxRowIndex = 98;

        /// <summary>
        /// Determines if the given cell name is valid.
        /// </summary>
        public static bool IsValidCellName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                return false;

            const string variablePattern = @"^[a-zA-Z][1-9][0-9]?$";
            return Regex.IsMatch(name.Trim(), variablePattern);
        }

        /// <summary>
        /// Normalizes the cell name.
        /// </summary>
        public static string NormalizeCellName(string name)
        {
            return String.IsNullOrWhiteSpace(name) ? String.Empty : name.ToUpper().Trim();
        }

        /// <summary>
        /// Returns the cell coordinates using the given cell name.
        /// </summary>
        public static void GetCellCoordinates(string cellName, out int col, out int row)
        {
            //Error checking
            if (string.IsNullOrWhiteSpace(cellName))
            {
                col = -1;
                row = -1;
                return;
            }

            //cell name regex pattern
            const string pattern = @"([a-zA-Z]+) | (\d+)";

            //split the cell name into components
            var nameComponents = Regex.Split(cellName, pattern, RegexOptions.IgnorePatternWhitespace).Where(s => !Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline)).ToArray();
            if (nameComponents.Length < 2)
            {
                col = -1;
                row = -1;
                return;
            }

            col = GetColumnNumber(nameComponents[0]);
            row = GetRowNumber(nameComponents[1]);
        }

        /// <summary>
        /// Returns the cell name using the given coordinates.
        /// </summary>
        public static string GetCellName(int col, int row)
        {
            try
            {
                string letter = (col < 0 || col > MaxColIndex) ? string.Empty : ((Alphabet)col).ToString();
                return (row < 0 || row > MaxRowIndex) ? letter : letter + (row + 1).ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the column number.
        /// </summary>
        private static int GetColumnNumber(string col)
        {
            if (string.IsNullOrWhiteSpace(col))
                return -1;

            var alphabet = Enum.GetNames(typeof(Alphabet)).ToArray();
            for (int i = 0; i < alphabet.Length; i++)
            {
                if (col.ToUpper() == alphabet[i])
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the row number.
        /// </summary>
        private static int GetRowNumber(string row)
        {
            if (string.IsNullOrWhiteSpace(row))
                return -1;

            int num;
            if (int.TryParse(row, out num))
            {
                if (num < 1 || num > MaxRowCount)
                    return -1;

                return num - 1;
            }

            return -1;
        }

        /// <summary>
        /// Enum for the alphabet.
        /// </summary>
        private enum Alphabet { A = 0, B = 1, C = 2, D = 3, E = 4, F = 5, G = 6, H = 7, I = 8, J = 9, K = 10, L = 11, M = 12, N = 13, O = 14, P = 15, Q = 16, R = 17, S = 18, T = 19, U = 20, V = 21, W = 22, X = 23, Y = 24, Z = 25 }
    }
}

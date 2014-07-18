// Skeleton written by Joe Zachary for CS 3500, September 2012
// Version 1.1.  (Edited by Joe Zachary) Clarified the specification of ToString and added Equals, operator==, operator!=, and GetHashCode.
// Final Implementation by John Skyler Chase for CS 3500, September 18, 2012

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using floating-point
    /// syntax, variables that consist of one or more letters followed by one or more 
    /// digits, parentheses, and the four operator symbols +, -, *, and /.
    /// </summary>
    public class Formula
    {
        //in order to make operations with the formula more efficient, use a private
        // IEnumerable object to keep track of teh tokens and a private string that
        // represents the formula without any whitespace
        private readonly IEnumerable<string> _tokens; //contains the tokens of the formula
        private readonly string _formulaExpression; //represents the formula without any whitespace

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntacticaly invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// </summary>
        public Formula(String formula)
        {
            //verify that the formula expression is not null or empty or whitespace
            if (String.IsNullOrWhiteSpace(formula))
                throw new FormulaFormatException("Formula expression cannot be null or empty.");

            //separate the formula into tokens
            _tokens = GetTokens(formula);

            //check if the formula is valid
            KeyValuePair<bool, string> validate = ValidateFormula(_tokens);

            //if the returned "key" in the key value pair is false, then the formula is invalid 
            // and the returned "value" contains an explanation why
            if (!validate.Key)
                throw new FormulaFormatException(validate.Value);

            //otherwise, the returned "value" is the string representation of the 
            // formula withoutwhitespace
            _formulaExpression = validate.Value;
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  
        /// 
        /// Given a variable symbol as its parameter, lookup returns the
        /// variable's value (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            try
            {
                //evaluate the formula and use the given lookup method to find any variable values 
                return Evaluator.Evaluate(_tokens, lookup);
            }
            catch (ArgumentException)
            {
                //should be thrown when a variable value cannot be located
                return new FormulaError("*ERROR: Cannot find valid values for variables.");
            }
            catch (DivideByZeroException)
            {
                //should be thrown when an attempt to divide by zero occurs
                return new FormulaError("*ERROR: Cannot divide by zero.");
            }
            catch (NullReferenceException)
            {
                //should be thrown when an attempt to evaluate a null expression occurs
                return new FormulaError("*ERROR: Cannot evaluate null values.");
            }
            catch (Exception)
            {
                return new FormulaError("*ERROR: Cannot evaluate expression.");
            }
        }

        /// <summary>
        /// Enumerates all of the variables that occur in this formula.  No variable
        /// may appear more than once in the enumeration, even if it appears more than
        /// once in this Formula.
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            if (_tokens == null)
                return null;

            //While iterating through the tokens, add variable items to the 
            //list if they are are already not contained in it
            List<string> variables = new List<string>();
            foreach (string token in _tokens.Where(token => IsVariable(token) && !variables.Contains(token)))
                variables.Add(token);

            return variables;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).
        /// </summary>
        public override string ToString()
        {
            //simply return the string representation of the formula generated while validating it
            return _formulaExpression;
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  All tokens are compared as strings except for numeric tokens,
        /// which are compared as doubles.
        /// 
        /// Here are some examples.  
        /// new Formula("x1+y2").Equals(new Formula("x1  +  y2")) is true
        /// new Formula("x1+y2").Equalas(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            //return false if parameter is null
            if (obj == null)
                return false;

            //return false if parameter cannot be cast into Formula
            Formula compareFormula = obj as Formula;
            if ((Object)compareFormula == null)
                return false;

            //Do an initial comparison of the strings
            if (_formulaExpression == compareFormula._formulaExpression)
                return true;

            //Otherwise iterate through all of the tokens to verify equality
            string[] thisObject = _tokens.ToArray();
            string[] compareObject = compareFormula._tokens.ToArray();

            //if the number of tokens doesn't match then they cannot be equal
            if (thisObject.Length != compareObject.Length)
                return false;

            for (int i = 0; i < thisObject.Length; i++)
            {
                if (IsNonNumber(thisObject[i]))
                {
                    //each processed token that is not a number must equal the other at any given time
                    if (thisObject[i] != compareObject[i])
                        return false;
                    continue;
                }

                //each processed token must be a number if this part of the loop is executed
                double thisObjectNumber;
                double compareObjectNumber;
                if (!(IsNumber(thisObject[i], out thisObjectNumber) && IsNumber(compareObject[i], out compareObjectNumber)))
                    return false;

                //each number must equal the other
                if (Math.Abs(thisObjectNumber - compareObjectNumber) > 0 || Math.Abs(thisObjectNumber - compareObjectNumber) < 0)
                    return false;
            }

            //the objects must be equal if the method reaches this point
            return true;
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            //if f1 is not null then just run the Equals method
            if ((object)f1 != null)
                return f1.Equals(f2);

            //otherwise verify if f2 is null too
            return (object)f2 == null;
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            //if f1 is not null then just run the Equals method and invert the result
            if ((object)f1 != null)
                return !f1.Equals(f2);

            //otherwise verify if f2 is null too
            return (object)f2 != null;
        }

        /// <summary>
        /// Returns a hash code for this Formula.
        /// </summary>
        public override int GetHashCode()
        {
            //use unchecked to prevent throwing overflow exception
            unchecked
            {
                //use prime numbers for calculations
                int hashCode = 127;
                const int prime = 113;
                string[] tokenArray = _tokens.ToArray();

                //In order to maintain equality between hash codes of formulas that are considered equal, 
                // get the hash values of the actual numbers that the token strings represent if they are a number
                // instead of getting their string hash value (e.g. 4.00 would not equal 4e0 as a string but as doubles
                // they are the same), otherwise get the string hashcode.  Use these hash values in conjunction
                // with calculating the overall hash code for this formula.  Use the position of the token as well 
                // to verify that formulas that are "the same" but ordered differently get different hash values 
                // (e.g. 5 + 4 should be different from 4 + 5). 
                for (int i = 0; i < tokenArray.Length; i++)
                {
                    double number;
                    if (IsNumber(tokenArray[i], out number))
                        hashCode *= prime + i * number.GetHashCode(); //get hash value of the number
                    else
                        hashCode *= prime + i * tokenArray[i].GetHashCode(); //get hash value of the string
                }
                return hashCode;
            }
        }

        #region Private Methods

        /// <summary>
        /// Given a formula, enumerates the tokens that compose it.  Tokens are left paren,
        /// right paren, one of the four operator symbols, a string consisting of one or more
        /// letters followed by one or more digits, a double literal, and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
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
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }
        }

        /// <summary>
        /// Iterates through the tokens of the formula and determines if it is valid or not.
        /// If it is valid, returns a formatted version of the formula without whitespace.
        /// Otherwise returns a reason why it is invalid.
        /// </summary>
        private static KeyValuePair<bool, string> ValidateFormula(IEnumerable<string> tokens)
        {
            //First check if the tokens are null
            if (tokens == null)
                return new KeyValuePair<bool, string>(false, "Formula expression cannot be null or empty.");

            StringBuilder formula = new StringBuilder();
            int openParanthesesCount = 0;
            int closeParanthesesCount = 0;
            string previous = String.Empty;
            var tokenList = tokens.ToArray();

            //Check if the length of the token array is greater than zero
            if (tokenList.Length <= 0)
                return new KeyValuePair<bool, string>(false, "Formula expression cannot be null or empty.");

            for (int i = 0; i < tokenList.Length; i++)
            {
                string current = tokenList[i].Trim();

                //Check if the token is valid
                if (!(IsNumberOrVariable(current) || IsOperator(current) || IsOpeningParanthesis(current) || IsClosingParanthesis(current)))
                    return new KeyValuePair<bool, string>(false, "Formula expression contains invalid operators, numbers or variables.");

                //Check if the first token is a number, variable or opening paranthesis
                if (i == 0 && !(IsNumberOrVariable(current) || IsOpeningParanthesis(current)))
                    return new KeyValuePair<bool, string>(false, "Formula expression cannot begin with ')', '+', '-', '*' or '/'. It must begin with '(', a variable or a number.");

                //Check if the last token is a number, variable or closing paranthesis
                if ((i == tokenList.Length - 1) && !(IsNumberOrVariable(current) || IsClosingParanthesis(current)))
                    return new KeyValuePair<bool, string>(false, "Formula expression cannot end with '(', '+', '-', '*' or '/'. It must end with ')', a variable or a number.");

                //Increment the count for opening parantheses
                if (IsOpeningParanthesis(current))
                    openParanthesesCount++;

                //Increment the count for closing parantheses
                if (IsClosingParanthesis(current))
                    closeParanthesesCount++;

                //Check if the number of closing parantheses is greater than the number of opening parantheses
                if (closeParanthesesCount > openParanthesesCount)
                    return new KeyValuePair<bool, string>(false, "Formula expression cannot have more closing parantheses than open parantheses at any point in the expression.");

                //Check if a number or operning paranthesis comes after an opening paranthesis or operator
                if ((IsOpeningParanthesis(previous) || IsOperator(previous)) && !(IsOpeningParanthesis(current) || IsNumberOrVariable(current)))
                    return new KeyValuePair<bool, string>(false, "Formula expression must have an opening paranthesis, number or variable following an opening paranthesis or operator.");

                //Check if a closing paranthesis or operator comes after a closing paranthesis or number
                if ((IsClosingParanthesis(previous) || IsNumberOrVariable(previous)) && !(IsClosingParanthesis(current) || IsOperator(current)))
                    return new KeyValuePair<bool, string>(false, "Formula expression must have a closing paranthesis or operator following a closing paranthesis, number or variable.");

                //Build a string that represents the formula without any white space for comparison
                formula.Append(current);

                //use for comparison in next iteration
                previous = current;
            }

            //Check if the number of parantheses are equal
            if (openParanthesesCount != closeParanthesesCount)
                return new KeyValuePair<bool, string>(false, "Formula expression must contain the same number of opening and closing parantheses.");

            //The formula is valid at this point - return the formatted version of the formula without whitespace
            return new KeyValuePair<bool, string>(true, formula.ToString());
        }

        /// <summary>
        /// Determines if a token is an opening paranthesis
        /// </summary>
        private static bool IsOpeningParanthesis(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                return false;
            return s.Trim() == "(";
        }

        /// <summary>
        /// Determines if a token is an closing paranthesis
        /// </summary>
        private static bool IsClosingParanthesis(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                return false;
            return s.Trim() == ")";
        }

        /// <summary>
        /// Determines if a token is an operator (+, -, * or /)
        /// </summary>
        private static bool IsOperator(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                return false;
            return (s.Trim() == "+" || s.Trim() == "-" || s.Trim() == "*" || s.Trim() == "/");
        }

        /// <summary>
        /// Determines if a token is a number and parses it
        /// </summary>
        private static bool IsNumber(string s, out double number)
        {
            if (String.IsNullOrWhiteSpace(s))
            {
                number = -1.0;
                return false;
            }

            return double.TryParse(s, out number);
        }

        /// <summary>
        /// Determines if a token is a variable
        /// </summary>
        private static bool IsVariable(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                return false;

            const String variablePattern = @"[a-zA-Z]+\d+";
            return Regex.IsMatch(s.Trim(), variablePattern);
        }

        /// <summary>
        /// Determines if a token is a number or a variable
        /// </summary>
        private static bool IsNumberOrVariable(string s)
        {
            double number;
            return IsNumber(s, out number) || IsVariable(s);
        }

        /// <summary>
        /// Determines if a token is an opening or closing paranthesis, an operator or a variable (not a number)
        /// </summary>
        private static bool IsNonNumber(string s)
        {
            return IsVariable(s) || IsOperator(s) || IsOpeningParanthesis(s) || IsClosingParanthesis(s);
        }

        #endregion
    }

    #region Classes

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }

    #endregion
}


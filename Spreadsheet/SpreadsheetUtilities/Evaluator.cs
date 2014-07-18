using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Class that evaluates mathematical expressions using infix notation.
    /// </summary>
    internal static class Evaluator
    {
        private static OperandValueStack _valStack; //Stack for the operands
        private static OperatorStack _opStack; //Stack for the operators

        /// <summary>
        /// Evaluates the mathematical expression contained in the given expression tokens
        /// parameter and uses the given Lookup method to determine the numeric values 
        /// of any variables contained in the given expression.  Accepts variables 
        /// consisting of one or more letters followed by one or more digits (e.g. A1, XYZ10, etc.)
        /// </summary>
        /// <param name="expressionTokens">Tokens of the mathematical expression to evaluate</param>
        /// <param name="variableEvaluator">Delegate method used to look up the value of a variable</param>
        /// <returns>The resulting value of the mathematical expression</returns>
        internal static double Evaluate(IEnumerable<string> expressionTokens, Func<string, double> variableEvaluator)
        {
            //Check if the expression is null or empty
            if (expressionTokens == null)
                throw new NullReferenceException("Cannot evaluate a null or empty expression.");

            //Initialize stack variables
            _valStack = new OperandValueStack();
            _opStack = new OperatorStack();

            //Iterate through the expression tokens
            foreach (string token in expressionTokens)
            {
                string oper; double val;
                if (IsOperator(token, out oper)) //Check if it is an operator
                    ExecuteOperatorLogic(oper);
                if (IsOperandValue(token, variableEvaluator, out val)) //Check if it is an operand
                    ExecuteValueLogic(val);
            }

            //Preliminary check after each token has been processed
            if (_opStack.Count() == 1 && _valStack.Count() == 2)
            {
                switch (_opStack.Peek())
                {
                    case "+": //if the top value on the operator stack is for addition then add
                        Add();
                        break;
                    case "-": //if the top value on the operator stack is for subtraction then subtract
                        Subtract();
                        break;
                }
            }

            return _valStack.Pop(); //return the end result
        }

        #region Private Class Methods

        /// <summary>
        /// Executes the logic in the algorithm for evaluating an infix 
        /// expression when the processed token is an operator symbol. 
        /// </summary>
        /// <param name="op">Operator token</param>
        private static void ExecuteOperatorLogic(string op)
        {
            //if the given operator is a multiply, divide or left parantheses symbol push it onto the operator stack
            if (op == "*" || op == "/" || op == "(")
            {
                _opStack.Push(op);
                return;
            }

            //if the given operator is a plus(+) or minus(-)
            if (op == "+" || op == "-")
            {
                switch (_opStack.Peek())
                {
                    case "+": //if the top value on the operator stack is for addition then add
                        Add();
                        break;
                    case "-": //if the top value on the operator stack is for subtraction then subtract
                        Subtract();
                        break;
                }

                _opStack.Push(op); //push the operator onto the stack
                return;
            }

            //the given operator is a right parentheses at this point
            switch (_opStack.Peek())
            {
                case "+": //if the top value on the operator stack is for addition then add
                    Add();
                    break;
                case "-": //if the top value on the operator stack is for subtraction then subtract
                    Subtract();
                    break;
            }

            _opStack.Pop(); //remove the left parentheses from the stack

            switch (_opStack.Peek())
            {
                case "*": //if the top value on the operator stack is for multiplication then multiply
                    Multiply();
                    break;
                case "/": //if the top value on the operator stack is for division then divide
                    Divide();
                    break;
            }

        }

        /// <summary>
        /// Executes the logic in the algorithm for evaluating an infix 
        /// expression when the processed token is a value. 
        /// </summary>
        /// <param name="v">Value token</param>
        private static void ExecuteValueLogic(double v)
        {
            switch (_opStack.Peek())
            {
                case "*": //if the top value on the operator stack is for multiplication then multiply
                    Multiply(v);
                    break;
                case "/": //if the top value on the operator stack is for division then divide
                    Divide(v);
                    break;
                default: //otherwise push the value onto the operand value stack
                    _valStack.Push(v);
                    break;
            }
        }

        /// <summary>
        /// Checks if the given token is an operator.
        /// </summary>
        /// <param name="s">Given operator token</param>
        /// <param name="op">Operator to use</param>
        /// <returns>True if the given token is a valid operator, False otherwise</returns>
        private static bool IsOperator(string s, out string op)
        {
            if (String.IsNullOrWhiteSpace(s)) { op = String.Empty; return false; } //null or empty check
            op = s.Trim();
            return (op == "+" || op == "-" || op == "*" || op == "/" || op == "(" || op == ")");
        }

        /// <summary>
        /// Checks if a given token is a value.  If the value contains a variable, executes
        /// the given Lookup method to determine what the variable value is.
        /// </summary>
        /// <param name="s">Given value token</param>
        /// <param name="eval">Method to determine variable values</param>
        /// <param name="v">Value to use</param>
        /// <returns>True if the given token is a valid value, False otherwise</returns>
        private static bool IsOperandValue(string s, Func<string, double> eval, out double v)
        {
            //Check if it is null or empty or whitespace
            if (String.IsNullOrWhiteSpace(s))
            {
                v = -1;
                return false;
            }

            //Check if the given value can be parsed to a double, if so use the parsed result and return true
            if (double.TryParse(s.Trim(), out v))
                return true;

            //Check if it is an operator
            string op;
            if (IsOperator(s.Trim(), out op))
            {
                v = -1;
                return false;
            }

            //If the program gets here then the token should be a variable so check if the variable has a valid form
            const string variablePattern = @"[a-zA-Z]+\d+";
            if (!Regex.IsMatch(s.Trim(), variablePattern))
                throw new ArgumentException(string.Format("Variable: {0} is not an accepted variable format.", s));

            //The variable has a valid form if this point is reached    
            try
            {
                //Use the Lookup function to get the variable's value
                v = eval(s.Trim());
                return true;
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(string.Format("Could not find the value of variable: {0}.", s) + ex);
            }
        }

        #region Arithmetic Methods

        /// <summary>
        /// Finds the sum of the top two values from the operand value stack.
        /// </summary>
        private static void Add()
        {
            _opStack.Pop(); //pop the addition operator from the operator stack
            _valStack.Push(_valStack.Pop() + _valStack.Pop()); //push the sum onto the operand value stack
        }

        /// <summary>
        /// Finds the difference of the top two values from the operand value stack.
        /// </summary>
        private static void Subtract()
        {
            _opStack.Pop(); //pop the subtraction operator from the operator stack
            _valStack.Push(-_valStack.Pop() + _valStack.Pop()); //push the difference onto the operand value stack
        }

        /// <summary>
        /// Finds the product of the top two values from the operand value stack.
        /// </summary>
        private static void Multiply()
        {
            Multiply(_valStack.Pop());
        }

        /// <summary>
        /// Finds the product of the top value from the operand value stack and the given multiplier. 
        /// </summary>
        /// <param name="v">Multiplier</param>
        private static void Multiply(double v)
        {
            _opStack.Pop(); //pop the multiplication operator from the operator stack
            _valStack.Push(_valStack.Pop() * v); //push the product onto the operand value stack
        }

        /// <summary>
        /// Finds the quotient of the top two values from the operand value stack.
        /// </summary>
        private static void Divide()
        {
            Divide(_valStack.Pop());
        }

        /// <summary>
        /// Finds the quotient of the top value from the operand value stack and the given divisor.
        /// </summary>
        /// <param name="v">Divisor</param>
        private static void Divide(double v)
        {
            if (Math.Abs(v - 0) <= 0) throw new DivideByZeroException(); //check for divide by zero error
            _opStack.Pop(); //pop the division operator from the operator stack
            _valStack.Push(_valStack.Pop() / v); //push the quotient onto the operand value stack
        }

        #endregion

        #endregion
    }
}

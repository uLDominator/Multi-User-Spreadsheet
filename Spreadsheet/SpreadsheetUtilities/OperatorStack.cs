using System.Collections.Generic;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Stack Object that stores string values that represent 
    /// the operators in mathematical expressions using standard infix
    /// notation including plus(+), minus(-), multiply(*), divide(/), 
    /// left parentheses and right parentheses. 
    /// </summary>
    internal class OperatorStack
    {
        #region Class Members and Constructors

        private readonly LinkedList<string> _operatorStack; //Linked List that implements the Operator Stack operations

        /// <summary>
        /// Constructor for the OperatorStack Class.
        /// </summary>
        internal OperatorStack()
        {
            _operatorStack = new LinkedList<string>();
        }

        #endregion

        #region internal Class Methods

        /// <summary>
        /// Adds a new object to the last position of the Operator Stack.
        /// </summary>
        /// <param name="s">Object to be added to the Operator Stack</param>
        internal void Push(string s)
        {
            _operatorStack.AddLast(s);
        }

        /// <summary>
        /// Removes and returns the last object of the Operator Stack.
        /// </summary>
        /// <returns>The last object of the Operator Stack if it is not empty, null otherwise</returns>
        internal string Pop()
        {
            if (IsEmpty())
                return null;

            string op = _operatorStack.Last.Value;
            _operatorStack.RemoveLast();
            return op;
        }

        /// <summary>
        /// Returns the last object of the Operator Stack without removing it.
        /// </summary>
        /// <returns>The last object of the Operator Stack if it is not empty, null otherwise</returns>
        internal string Peek()
        {
            return !IsEmpty() ? _operatorStack.Last.Value : null;
        }

        /// <summary>
        /// Validates if the Operator Stack is empty.
        /// </summary>
        /// <returns>True if the Operator Stack is empty, False otherwise</returns>
        internal bool IsEmpty()
        {
            return _operatorStack.Count <= 0;
        }

        /// <summary>
        /// Returns the number of items on the Operator Stack.
        /// </summary>
        /// <returns>The number of items on the Operator Stack</returns>
        internal int Count()
        {
            return _operatorStack.Count;
        }

        #endregion
    }
}

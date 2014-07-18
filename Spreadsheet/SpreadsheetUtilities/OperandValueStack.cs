using System.Collections.Generic;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Stack Object that stores double values that represent 
    /// the operands in mathematical expressions using standard 
    /// infix notation.
    /// </summary>
    internal class OperandValueStack
    {
        #region Class Members and Constructors

        private readonly LinkedList<double> _valueStack; //Linked List that implements the Operand Value Stack operations

        /// <summary>
        /// Constructor for the Operand Value Stack Class.
        /// </summary>
        internal OperandValueStack()
        {
            _valueStack = new LinkedList<double>();
        }

        #endregion

        #region internal Class Methods

        /// <summary>
        /// Adds a new object to the last position of the Operand Value Stack.
        /// </summary>
        /// <param name="i">Object to be added to the Operand Value Stack</param>
        internal void Push(double i)
        {
            _valueStack.AddLast(i);
        }

        /// <summary>
        /// Removes and returns the last object of the Operand Value Stack.
        /// </summary>
        /// <returns>The last object of the Operand Value Stack if it is not empty, -1 otherwise</returns>
        internal double Pop()
        {
            if (!IsEmpty())
            {
                double value = _valueStack.Last.Value;
                _valueStack.RemoveLast();
                return value;
            }
            return -1;
        }

        /// <summary>
        /// Returns the last object of the Operand Value Stack without removing it.
        /// </summary>
        /// <returns>The last object of the Operand Value Stack if it is not empty, -1 otherwise</returns>
        internal double Peek()
        {
            return !IsEmpty() ? _valueStack.Last.Value : -1;
        }

        /// <summary>
        /// Validates if the Operand Value Stack is empty.
        /// </summary>
        /// <returns>True if the Operand Value Stack is empty, False otherwise</returns>
        internal bool IsEmpty()
        {
            return _valueStack.Count <= 0;
        }

        /// <summary>
        /// Returns the number of items on the Operand Value Stack.
        /// </summary>
        /// <returns>The number of items on the Operand Value Stack</returns>
        internal int Count()
        {
            return _valueStack.Count;
        }

        #endregion
    }
}

// Written by Joe Zachary for CS 3500, September 2011.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SpreadsheetView
{


    /// <summary>
    /// The type of delegate used to register for SelectionChanged events
    /// </summary>
    /// <param name="sender"></param>

    public delegate void SelectionChangedHandler(SpreadsheetPanel sender);


    /// <summary>
    /// A panel that displays a spreadsheet with 26 columns (labeled A-Z) and 99 rows
    /// (labeled 1-99).  Each cell on the grid can display a non-editable string.  One 
    /// of the cells is always selected (and highlighted).  When the selection changes, a 
    /// SelectionChanged event is fired.  Clients can register to be notified of
    /// such events.
    /// 
    /// None of the cells are editable.  They are for display purposes only.
    /// </summary>

    public partial class SpreadsheetPanel : UserControl
    {

        // The SpreadsheetPanel is composed of a DrawingPanel (where the grid is drawn),
        // a horizontal scroll bar, and a vertical scroll bar.
        private readonly DrawingPanel _drawingPanel;
        private readonly HScrollBar _hScroll;
        private readonly VScrollBar _vScroll;

        // These constants control the layout of the spreadsheet grid.  The height and
        // width measurements are in pixels.
        private const int DataColWidth = 80;
        private const int DataRowHeight = 20;
        private const int LabelColWidth = 30;
        private const int LabelRowHeight = 30;
        private const int PADDING = 2;
        private const int ScrollbarWidth = 20;
        private const int ColCount = 26;
        private const int RowCount = 99;


        /// <summary>
        /// Creates an empty SpreadsheetPanel
        /// </summary>

        public SpreadsheetPanel()
        {

            InitializeComponent();

            // The DrawingPanel is quite large, since it has 26 columns and 99 rows.  The
            // SpreadsheetPanel itself will usually be smaller, which is why scroll bars
            // are necessary.
            _drawingPanel = new DrawingPanel(this) { Location = new Point(0, 0), AutoScroll = false };

            // A custom vertical scroll bar.  It is designed to scroll in multiples of rows.
            _vScroll = new VScrollBar { SmallChange = 1, Maximum = RowCount };

            // A custom horizontal scroll bar.  It is designed to scroll in multiples of columns.
            _hScroll = new HScrollBar { SmallChange = 1, Maximum = ColCount };

            // Add the drawing panel and the scroll bars to the SpreadsheetPanel.
            Controls.Add(_drawingPanel);
            Controls.Add(_vScroll);
            Controls.Add(_hScroll);

            // Arrange for the drawing panel to be notified when it needs to scroll itself.
            _hScroll.Scroll += _drawingPanel.HandleHScroll;
            _vScroll.Scroll += _drawingPanel.HandleVScroll;

        }


        /// <summary>
        /// Clears the display.
        /// </summary>

        public void Clear()
        {
            _drawingPanel.Clear();
        }


        /// <summary>
        /// If the zero-based column and row are in range, sets the value of that
        /// cell and returns true.  Otherwise, returns false.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="value"></param>
        /// <returns></returns>

        public bool SetValue(int col, int row, string value)
        {
            return _drawingPanel.SetValue(col, row, value);
        }


        /// <summary>
        /// If the zero-based column and row are in range, assigns the value
        /// of that cell to the out parameter and returns true.  Otherwise,
        /// returns false.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="value"></param>
        /// <returns></returns>

        public bool GetValue(int col, int row, out string value)
        {
            return _drawingPanel.GetValue(col, row, out value);
        }


        /// <summary>
        /// If the zero-based column and row are in range, uses them to set
        /// the current selection and returns true.  Otherwise, returns false.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <returns></returns>

        public bool SetSelection(int col, int row)
        {
            return _drawingPanel.SetSelection(col, row);
        }


        /// <summary>
        /// Assigns the column and row of the current selection to the
        /// out parameters.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>

        public void GetSelection(out int col, out int row)
        {
            _drawingPanel.GetSelection(out col, out row);
        }


        /// <summary>
        /// When the SpreadsheetPanel is resized, we set the size and locations of the three
        /// components that make it up.
        /// </summary>
        /// <param name="eventargs"></param>

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            var findForm = FindForm();
            if (findForm != null && (findForm.WindowState == FormWindowState.Minimized))
                return;

            _drawingPanel.Size = new Size(Width - ScrollbarWidth, Height - ScrollbarWidth);
            _vScroll.Location = new Point(Width - ScrollbarWidth, 0);
            _vScroll.Size = new Size(ScrollbarWidth, Height - ScrollbarWidth);
            _vScroll.LargeChange = (Height - ScrollbarWidth) / DataRowHeight;
            _hScroll.Location = new Point(0, Height - ScrollbarWidth);
            _hScroll.Size = new Size(Width - ScrollbarWidth, ScrollbarWidth);
            _hScroll.LargeChange = (Width - ScrollbarWidth) / DataColWidth;
        }


        /// <summary>
        /// The event used to send notifications of a selection change
        /// </summary>

        public event SelectionChangedHandler SelectionChanged;


        /// <summary>
        /// Used internally to keep track of cell addresses
        /// </summary>

        private class Address
        {

            public int Col { get; set; }
            public int Row { get; set; }

            public Address(int c, int r)
            {
                Col = c;
                Row = r;
            }

            public override int GetHashCode()
            {
                return Col.GetHashCode() ^ Row.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((obj == null) || !(obj is Address))
                {
                    return false;
                }
                Address a = (Address)obj;
                return Col == a.Col && Row == a.Row;
            }

        }


        /// <summary>
        /// The panel where the spreadsheet grid is drawn.  It keeps track of the
        /// current selection as well as what is supposed to be drawn in each cell.
        /// </summary>

        private sealed class DrawingPanel : Panel
        {
            // Columns and rows are numbered beginning with 0.  This is the coordinate
            // of the selected cell.
            private int _selectedCol;
            private int _selectedRow;

            // Coordinate of cell in upper-left corner of display
            private int _firstColumn;
            private int _firstRow;

            // The strings contained by the spreadsheet
            private readonly Dictionary<Address, String> _values;

            // The containing panel
            private readonly SpreadsheetPanel _ssp;


            public DrawingPanel(SpreadsheetPanel ss)
            {
                DoubleBuffered = true;
                _values = new Dictionary<Address, String>();
                _ssp = ss;
            }


            private bool InvalidAddress(int col, int row)
            {
                return col < 0 || row < 0 || col >= ColCount || row >= RowCount;
            }


            public void Clear()
            {
                _values.Clear();
                Invalidate();
            }


            public bool SetValue(int col, int row, string c)
            {
                if (InvalidAddress(col, row))
                {
                    return false;
                }

                Address a = new Address(col, row);
                if (string.IsNullOrEmpty(c))
                {
                    _values.Remove(a);
                }
                else
                {
                    _values[a] = c;
                }
                Invalidate();
                return true;
            }


            public bool GetValue(int col, int row, out string c)
            {
                if (InvalidAddress(col, row))
                {
                    c = null;
                    return false;
                }
                if (!_values.TryGetValue(new Address(col, row), out c))
                {
                    c = string.Empty;
                }
                return true;
            }


            public bool SetSelection(int col, int row)
            {
                if (InvalidAddress(col, row))
                {
                    return false;
                }
                _selectedCol = col;
                _selectedRow = row;
                Invalidate();
                return true;
            }


            public void GetSelection(out int col, out int row)
            {
                col = _selectedCol;
                row = _selectedRow;
            }


            public void HandleHScroll(Object sender, ScrollEventArgs args)
            {
                _firstColumn = args.NewValue;
                Invalidate();
            }

            public void HandleVScroll(Object sender, ScrollEventArgs args)
            {
                _firstRow = args.NewValue;
                Invalidate();
            }


            protected override void OnPaint(PaintEventArgs e)
            {

                // Clip based on what needs to be refreshed.
                Region clip = new Region(e.ClipRectangle);
                e.Graphics.Clip = clip;

                // Color the background of the data area white
                e.Graphics.FillRectangle(
                    new SolidBrush(Color.White),
                    LabelColWidth,
                    LabelRowHeight,
                    (ColCount - _firstColumn) * DataColWidth,
                    (RowCount - _firstRow) * DataRowHeight);

                // Pen, brush, and fonts to use
                Brush brush = new SolidBrush(Color.Black);
                Pen pen = new Pen(brush);
                Font regularFont = Font;
                Font boldFont = new Font(regularFont, FontStyle.Bold);

                // Draw the column lines
                int bottom = LabelRowHeight + (RowCount - _firstRow) * DataRowHeight;
                e.Graphics.DrawLine(pen, new Point(0, 0), new Point(0, bottom));
                for (int x = 0; x <= (ColCount - _firstColumn); x++)
                {
                    e.Graphics.DrawLine(
                        pen,
                        new Point(LabelColWidth + x * DataColWidth, 0),
                        new Point(LabelColWidth + x * DataColWidth, bottom));
                }

                // Draw the column labels
                for (int x = 0; x < ColCount - _firstColumn; x++)
                {
                    Font f = (_selectedCol - _firstColumn == x) ? boldFont : Font;
                    DrawColumnLabel(e.Graphics, x, f);
                }

                // Draw the row lines
                int right = LabelColWidth + (ColCount - _firstColumn) * DataColWidth;
                e.Graphics.DrawLine(pen, new Point(0, 0), new Point(right, 0));
                for (int y = 0; y <= RowCount - _firstRow; y++)
                {
                    e.Graphics.DrawLine(
                        pen,
                        new Point(0, LabelRowHeight + y * DataRowHeight),
                        new Point(right, LabelRowHeight + y * DataRowHeight));
                }

                // Draw the row labels
                for (int y = 0; y < (RowCount - _firstRow); y++)
                {
                    Font f = (_selectedRow - _firstRow == y) ? boldFont : Font;
                    DrawRowLabel(e.Graphics, y, f);
                }

                // Highlight the selection, if it is visible
                if ((_selectedCol - _firstColumn >= 0) && (_selectedRow - _firstRow >= 0))
                {
                    e.Graphics.DrawRectangle(
                        pen,
                        new Rectangle(LabelColWidth + (_selectedCol - _firstColumn) * DataColWidth + 1,
                                      LabelRowHeight + (_selectedRow - _firstRow) * DataRowHeight + 1,
                                      DataColWidth - 2,
                                      DataRowHeight - 2));
                }

                // Draw the text
                foreach (KeyValuePair<Address, String> address in _values)
                {
                    String text = address.Value;
                    int x = address.Key.Col - _firstColumn;
                    int y = address.Key.Row - _firstRow;
                    float height = e.Graphics.MeasureString(text, regularFont).Height;
                    //float width = e.Graphics.MeasureString(text, regularFont).Width;
                    if (x < 0 || y < 0)
                        continue;

                    Region cellClip = new Region(new Rectangle(LabelColWidth + x * DataColWidth + PADDING,
                                                               LabelRowHeight + y * DataRowHeight,
                                                               DataColWidth - 2 * PADDING,
                                                               DataRowHeight));
                    cellClip.Intersect(clip);
                    e.Graphics.Clip = cellClip;
                    e.Graphics.DrawString(
                        text,
                        regularFont,
                        brush,
                        LabelColWidth + x * DataColWidth + PADDING,
                        LabelRowHeight + y * DataRowHeight + (DataRowHeight - height) / 2);
                }


            }


            /// <summary>
            /// Draws a column label.  The columns are indexed beginning with zero.
            /// </summary>
            /// <param name="g"></param>
            /// <param name="x"></param>
            /// <param name="f"></param>
            private void DrawColumnLabel(Graphics g, int x, Font f)
            {
                String label = ((char)('A' + x + _firstColumn)).ToString(CultureInfo.InvariantCulture);
                float height = g.MeasureString(label, f).Height;
                float width = g.MeasureString(label, f).Width;
                g.DrawString(
                      label,
                      f,
                      new SolidBrush(Color.Black),
                      LabelColWidth + x * DataColWidth + (DataColWidth - width) / 2,
                      (LabelRowHeight - height) / 2);
            }


            /// <summary>
            /// Draws a row label.  The rows are indexed beginning with zero.
            /// </summary>
            /// <param name="g"></param>
            /// <param name="y"></param>
            /// <param name="f"></param>
            private void DrawRowLabel(Graphics g, int y, Font f)
            {
                String label = (y + 1 + _firstRow).ToString(CultureInfo.InvariantCulture);
                float height = g.MeasureString(label, f).Height;
                float width = g.MeasureString(label, f).Width;
                g.DrawString(
                    label,
                    f,
                    new SolidBrush(Color.Black),
                    LabelColWidth - width - PADDING,
                    LabelRowHeight + y * DataRowHeight + (DataRowHeight - height) / 2);
            }


            /// <summary>
            /// Determines which cell, if any, was clicked.  Generates a SelectionChanged event.  All of
            /// the indexes are zero based.
            /// </summary>
            /// <param name="e"></param>

            protected override void OnMouseClick(MouseEventArgs e)
            {
                OnClick(e);
                int x = (e.X - LabelColWidth) / DataColWidth;
                int y = (e.Y - LabelRowHeight) / DataRowHeight;
                if (e.X > LabelColWidth && e.Y > LabelRowHeight && (x + _firstColumn < ColCount) && (y + _firstRow < RowCount))
                {
                    _selectedCol = x + _firstColumn;
                    _selectedRow = y + _firstRow;
                    if (_ssp.SelectionChanged != null)
                    {
                        _ssp.SelectionChanged(_ssp);
                    }
                }
                Invalidate();
            }

        }

    }
}

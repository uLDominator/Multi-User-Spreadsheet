using System;
using System.Windows.Forms;

namespace SpreadsheetView
{
    // Note: The code for the application context below was created by Joe Zachary 
    // with slight modifications by John Chase.

    /// <summary>
    /// Runs the Spreadsheet Form Application
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SpreadSheetApplicationContext applicationContext = SpreadSheetApplicationContext.GetSpreadsheetAppContext();
            applicationContext.RunForm(new SpreadsheetForm());
            Application.Run(applicationContext);
        }
    }

    /// <summary>
    /// Keeps track of how many top-level spreadsheet forms are running.
    /// </summary>
    public class SpreadSheetApplicationContext : ApplicationContext
    {
        // Number of open spreadsheet forms
        private int _formCount = 0;

        // Singleton ApplicationContext
        // (Need to have a class that has only one instance when building applications) 
        private static SpreadSheetApplicationContext _appContext;

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private SpreadSheetApplicationContext() { }

        /// <summary>
        /// Returns the currnent SpreadSheetApplicationContext.
        /// </summary>
        public static SpreadSheetApplicationContext GetSpreadsheetAppContext()
        {
            return _appContext ?? (_appContext = new SpreadSheetApplicationContext());
        }

        /// <summary>
        /// Runs the spreadsheet form.
        /// </summary>
        public void RunForm(Form form)
        {
            try
            {
                // Increase the number of forms running
                _formCount++;

                // When the current form closes, let the application know and exit
                // if it is the last form
                form.FormClosed += (o, e) => { if (--_formCount <= 0) ExitThread(); };

                // Run the current spreadsheet form
                form.Show();
            }
            catch(ObjectDisposedException)
            {
                //Catch in case the user closes the Connect Box using the red X
                Application.Exit();
            }

        }

    }
}

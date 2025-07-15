using System;
using System.Threading;
using System.Windows.Forms;
using ViewerPhotoDan.MVP;
using ViewerPhotoDan.Services;

namespace ViewerPhotoDan
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Add the event handler for handling UI thread exceptions.
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            // Add the event handler for handling non-UI thread exceptions.
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var view = new FormMain();
            var imageService = new ImageService();
            var presenter = new MainPresenter(view, imageService);

            Application.Run(view);
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled UI thread exception occurred:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            MessageBox.Show($"An unhandled non-UI thread exception occurred:\n{ex.Message}\n\n{ex.StackTrace}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

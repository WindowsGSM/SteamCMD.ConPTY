using SteamCMD.ConPTY;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SteamCMD.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public WindowsPseudoConsole pseudoConsole;

        public MainWindow()
        {
            InitializeComponent();

            pseudoConsole = new WindowsPseudoConsole
            {
                // steamcmd.exe directory
                WorkingDirectory = Directory.GetCurrentDirectory(),

                // You can pass argments like "+login anonymous +app_update 232250 +quit"
                Arguments = string.Empty,

                // We need to filter the control sequences in WPF
                FilterControlSequences = true,
            };

            // Set WPF title when title data received
            pseudoConsole.TitleReceived += (sender, data) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Title = data;
                });
            };

            // Append the TextBoxOutput when output data receieved
            pseudoConsole.OutputDataReceived += (sender, data) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TextBoxOutput.Text += data;
                    TextBoxOutput.ScrollToEnd();
                });
            };

            // Close the WPF when steamcmd.exe exited
            pseudoConsole.Exited += (sender, exitCode) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown(exitCode);
                });
            };

            // Start steamcmd conpty
            pseudoConsole.Start("steamcmd.exe");

            // Set up enter listener, and send the data to steamcmd conpty
            TextBoxInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Return)
                {
                    pseudoConsole.WriteLine(TextBoxInput.Text);
                    TextBoxInput.Text = string.Empty;
                }
            };

            TextBoxInput.Focus();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Dispose the steamCMDConPTY when WPF close
            pseudoConsole.Dispose();
        }
    }
}

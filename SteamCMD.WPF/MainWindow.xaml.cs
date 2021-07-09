using SteamCMD.ConPTY;
using System.ComponentModel;
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
        public SteamCMDConPTY steamCMDConPTY;

        public MainWindow()
        {
            InitializeComponent();

            steamCMDConPTY = new SteamCMDConPTY
            {
                // steamcmd.exe directory
                WorkingDirectory = Directory.GetCurrentDirectory(),

                // You can pass argments like "+login anonymous +app_update 232250 +quit"
                Arguments = string.Empty,

                // We need to filter the control sequences in WPF
                FilterControlSequences = true,
            };

            // Set WPF title when title data received
            steamCMDConPTY.TitleReceived += (sender, data) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Title = data;
                });
            };

            // Append the TextBoxOutput when output data receieved
            steamCMDConPTY.OutputDataReceived += (sender, data) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TextBoxOutput.Text += data;
                    TextBoxOutput.ScrollToEnd();
                });
            };
            
            // Close the WPF when steamcmd.exe exited
            steamCMDConPTY.Exited += (sender, exitCode) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            };

            // Start steamcmd conpty
            steamCMDConPTY.Start();

            // Set up enter listener, and send the data to steamcmd conpty
            TextBoxInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Return)
                {
                    steamCMDConPTY.WriteLine(TextBoxInput.Text);
                    TextBoxInput.Text = string.Empty;
                }
            };

            TextBoxInput.Focus();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Dispose the steamCMDConPTY when WPF close
            steamCMDConPTY.Dispose();
        }
    }
}

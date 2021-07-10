using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteamCMD.ConPTY
{
    /// <summary>
    /// Pseudo Console (ConPTY)
    /// </summary>
    public class WindowsPseudoConsole
    {
        /// <summary>
        /// Occurs when console title received
        /// </summary>
        public event EventHandler<string> TitleReceived;

        /// <summary>
        /// Occurs each time console writes a line.
        /// </summary>
        public event EventHandler<string> OutputDataReceived;

        /// <summary>
        /// Occurs when the console exits.
        /// </summary>
        public event EventHandler<int> Exited;

        /// <summary>
        /// Working directory
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Arguments that pass to the console
        /// </summary>
        public string Arguments { get; set; } = string.Empty;

        /// <summary>
        /// Filtering out ANSI escape sequences on <see cref="OutputDataReceived"/> (Default: <see langword="false"/>) 
        /// </summary>
        public bool FilterControlSequences { get; set; } = false;

        private Terminal terminal;
        private Stream inputStream;
        private bool disposed;

        /// <summary>
        /// Pseudo Console (ConPTY), required files will be created on the working directory.
        /// </summary>
        public WindowsPseudoConsole() { }

        /// <summary>
        /// Pseudo Console (ConPTY), required files will be created on the working directory.
        /// </summary>
        /// <param name="workingDirectory">Working directory</param>
        public WindowsPseudoConsole(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        /// <summary>
        /// Pseudo Console (ConPTY), required files will be created on the working directory.
        /// </summary>
        /// <param name="workingDirectory">Working directory</param>
        /// <param name="arguments">Arguments that pass to the console</param>
        public WindowsPseudoConsole(string workingDirectory, string arguments)
        {
            (WorkingDirectory, Arguments) = (workingDirectory, arguments);
        }

        /// <summary>
        /// Start pseudo console
        /// </summary>
        public void Start(string fileName, short width = 120, short height = 30)
        {
            if (WorkingDirectory == null)
            {
                throw new Exception("WorkingDirectory is not set");
            }

            string filePath = Path.Combine(WorkingDirectory, fileName);

            if (!File.Exists(filePath))
            {
                throw new Exception($"File does not exist ({filePath})");
            }

            // Start pseudo console
            terminal = new Terminal();
            terminal.Start($"{filePath}{(string.IsNullOrEmpty(Arguments) ? string.Empty : $" {Arguments}")}", width, height);

            // Save the inputStream
            inputStream = terminal.Input;

            // Read pseudo console output in the background
            Task.Run(() => ReadConPtyOutput(terminal.Output));

            // Wait the pseudo console exit in the background
            Task.Run(() =>
            {
                terminal.WaitToExit();

                // Call Exited event with exit code
                Exited?.Invoke(this, terminal.GetExitCode(out uint exitCode) ? (int)exitCode : -1);
            });
        }

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(char data) => Write(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(char[] data) => Write(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            inputStream.Write(bytes, 0, bytes.Length);
            inputStream.Flush();
        }

        /// <summary>
        /// Write data to the console, followed by a break line character.
        /// </summary>
        /// <param name="data"></param>
        public void WriteLine(string data) => Write($"{data}\x0D");

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public async Task WriteAsync(char data) => await WriteAsync(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public async Task WriteAsync(char[] data) => await WriteAsync(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public async Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            await inputStream.WriteAsync(bytes, 0, bytes.Length);
            await inputStream.FlushAsync();
        }

        /// <summary>
        /// Write data to the console, followed by a break line character.
        /// </summary>
        /// <param name="data"></param>
        public async Task WriteLineAsync(string data) => await WriteAsync($"{data}\x0D");

        private async Task ReadConPtyOutput(Stream output)
        {
            bool titleInvoked = false;
            string title = string.Empty;
            const string cursorLow = "\x1B[?25l", cursorHigh = "\x1B[?25h";

            var regex = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])");

            try
            {
                using (var reader = new StreamReader(output))
                {
                    char[] buffer = new char[1024];

                    while (true)
                    {
                        int readed = reader.Read(buffer, 0, buffer.Length);

                        if (readed > 0)
                        {
                            var outputData = new string(buffer.Take(readed).ToArray());

                            if (!titleInvoked)
                            {
                                title += outputData;

                                string[] subs = title.Split(new string[] { cursorLow, cursorHigh }, 2, StringSplitOptions.None);

                                if (subs.Length <= 1)
                                {
                                    continue;
                                }

                                titleInvoked = true;

                                title = regex.Replace(subs[0], string.Empty).TrimEnd('\x7');
                                title = title.StartsWith("0;") ? title.Substring(2) : title;

                                TitleReceived?.Invoke(this, title);

                                outputData = cursorLow + subs[1];
                            }

                            OutputDataReceived?.Invoke(this, FilterControlSequences ? regex.Replace(outputData, string.Empty) : outputData);
                        }

                        await Task.Delay(1).ConfigureAwait(false);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Disposed
            }
        }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    
                }

                terminal.Dispose();

                disposed = true;
            }
        }

        ~WindowsPseudoConsole()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

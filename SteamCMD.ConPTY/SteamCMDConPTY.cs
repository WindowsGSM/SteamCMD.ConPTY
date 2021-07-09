using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteamCMD.ConPTY
{
    /// <summary>
    /// SteamCMD - Pseudo Console (ConPTY)
    /// </summary>
    public class SteamCMDConPTY : Process
    {
        /// <summary>
        /// Occurs when steamcmd title received
        /// </summary>
        public event EventHandler<string> TitleReceived;

        /// <summary>
        /// Occurs each time steamcmd writes a line.
        /// </summary>
        public new event EventHandler<string> OutputDataReceived;

        /// <summary>
        /// Occurs when the steamcmd exits.
        /// </summary>
        public new event EventHandler<int> Exited;

        /// <summary>
        /// steamcmd.exe working directory
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Arguments that pass to steamcmd.exe. Example: "+login anonymous +app_update 232250 validate +quit"
        /// </summary>
        public string Arguments { get; set; } = string.Empty;

        /// <summary>
        /// Filtering out ANSI escape sequences on <see cref="OutputDataReceived"/> (Default: <see langword="false"/>) 
        /// </summary>
        public bool FilterControlSequences { get; set; } = false;

        private static readonly string conptyExeFileName = "steamcmd.conpty.exe";
        private string inputFilePath;
        private string outputFilePath;
        private FileStream outputFileStream;
        private bool disposedValue;

        /// <summary>
        /// SteamCMD - Pseudo Console (ConPTY), required files will be created on the working directory.
        /// </summary>
        public SteamCMDConPTY() { }

        /// <summary>
        /// SteamCMD - Pseudo Console (ConPTY), required files will be created on the working directory.
        /// </summary>
        /// <param name="workingDirectory">steamcmd.conpty.exe working directory</param>
        public SteamCMDConPTY(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        /// <summary>
        /// SteamCMD - Pseudo Console (ConPTY), required files will be created on the working directory.
        /// </summary>
        /// <param name="workingDirectory">steamcmd.conpty.exe working directory</param>
        /// <param name="arguments">Arguments that pass to steamcmd.exe. Example: "+login anonymous +app_update 232250 validate +quit"</param>
        public SteamCMDConPTY(string workingDirectory, string arguments)
        {
            (WorkingDirectory, Arguments) = (workingDirectory, arguments);
        }

        /// <summary>
        /// Start steamcmd pseudo console
        /// </summary>
        public new void Start()
        {
            if (WorkingDirectory == null)
            {
                throw new Exception("WorkingDirectory is not set");
            }

            string fileName = Path.Combine(WorkingDirectory, conptyExeFileName);

            if (!File.Exists(fileName))
            {
                Stream stream = GetType().Assembly.GetManifestResourceStream($"{typeof(SteamCMDConPTY).Namespace}.Resources.{conptyExeFileName}");
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                File.WriteAllBytes(fileName, bytes);
            }

            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = WorkingDirectory,
                FileName = fileName,
                Arguments = Arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            EnableRaisingEvents = true;

            base.Exited += (s, e) =>
            {
                Exited?.Invoke(this, ExitCode);
            };
            
            base.Start();

            var conptyDirectory = Path.Combine(StartInfo.WorkingDirectory, "conpty");
            Directory.CreateDirectory(conptyDirectory);

            inputFilePath = Path.Combine(conptyDirectory, $"{Id}.input");
            outputFilePath = Path.Combine(conptyDirectory, $"{Id}.output");

            Task.Run(() => ReadConPtyOutput());
        }

        /// <summary>
        /// Write data to the steamcmd.
        /// </summary>
        /// <param name="data"></param>
        public void Write(char data) => Write(data.ToString());

        /// <summary>
        /// Write data to the steamcmd.
        /// </summary>
        /// <param name="data"></param>
        public void Write(char[] data) => Write(data.ToString());

        /// <summary>
        /// Write data to the steamcmd.
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data) => File.AppendAllText(inputFilePath, data);

        /// <summary>
        /// Write data to the steamcmd, followed by a break line character.
        /// </summary>
        /// <param name="data"></param>
        public void WriteLine(string data) => Write($"{data}\x0D");

        private async Task ReadConPtyOutput()
        {
            bool titleInvoked = false;
            string title = string.Empty;
            const string cursorLow = "\x1B[?25l", cursorHigh = "\x1B[?25h";

            var regex = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])");

            try
            {
                outputFileStream = File.Open(outputFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);

                using (var reader = new StreamReader(outputFileStream))
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

        protected new void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    
                }

                // Send ctrl-c kill the terminal
                Write("\x3");

                disposedValue = true;
                outputFileStream?.Dispose();
            }
        }

        ~SteamCMDConPTY()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        public new void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamCMD.ConPTY.Executable
{
    class Program
    {
        private static string inputFilePath;
        private static string outputFilePath;
        private static FileStream inputFileStream;
        private static FileStream outputFileStream;

        static int Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string exePath = Path.Combine(currentDirectory, "steamcmd.exe");

            // Download steamcmd.exe if not exists
            if (!File.Exists(exePath))
            {
                string zipPath = Path.Combine(currentDirectory, "steamcmd.zip");

                // Download steamcmd.zip
                using var webClient = new WebClient();
                webClient.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", zipPath);

                // Extract steamcmd.zip
                ZipFile.ExtractToDirectory(zipPath, currentDirectory);

                // Delete steamcmd.zip
                File.Delete(zipPath);
            }

            // Create directory "conpty"
            var conptyDirectory = Path.Combine(currentDirectory, "conpty");
            Directory.CreateDirectory(conptyDirectory);

            // Set input and output to UTF8
            Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

            try
            {
                // Start the pseudo console
                using var terminal = new Terminal();
                terminal.Start($"{exePath} {string.Join(' ', args)}", (short)Console.BufferWidth, (short)Console.BufferHeight);

                // Save the path of input and out file with current process id to keep unique
                int processId = Process.GetCurrentProcess().Id;
                inputFilePath = Path.Combine(conptyDirectory, $"{processId}.input");
                outputFilePath = Path.Combine(conptyDirectory, $"{processId}.output");

                // Tasks
                Task.Run(() => CopyPipeToOutput(terminal.Output));
                Task.Run(() => CopyInputFileTextToPipe(terminal.Input));
                Task.Run(() => CopyInputToPipe(terminal.Input));
                
                // Wait to exit
                terminal.WaitToExit();

                // Return the pseudo console exit code
                return terminal.GetExitCode();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);

                return -1;
            }
        }

        /// <summary>
        /// Reads PseudoConsole output and copies it to the terminal's standard out and copies it to the output file
        /// </summary>
        /// <param name="output"></param>
        private static async Task CopyPipeToOutput(Stream output)
        {
            try
            {
                char[] buffer = new char[1024];

                using StreamReader reader = new StreamReader(output);
                using StreamWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
                using StreamWriter fileWriter = new StreamWriter(outputFileStream) { AutoFlush = true };

                while (true)
                {
                    int readed = await reader.ReadAsync(buffer, 0, buffer.Length);

                    if (readed > 0)
                    {
                        await writer.WriteAsync(buffer, 0, readed);
                        await fileWriter.WriteAsync(buffer, 0, readed);
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Pseudo Console has been terminated.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Reads terminal input and copies it to the pipe
        /// </summary>
        /// <param name="input"></param>
        private static async Task CopyInputFileTextToPipe(Stream input)
        {
            try
            {
                char[] buffer = new char[1024];

                using StreamWriter writer = new StreamWriter(input) { AutoFlush = true };
                inputFileStream = File.Open(inputFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new StreamReader(inputFileStream);

                while (true)
                {
                    int readed = await reader.ReadAsync(buffer, 0, buffer.Length);

                    if (readed > 0)
                    {
                        string data = new string(buffer.Take(readed).ToArray());

                        if (data.Contains('\x3'))
                        {
                            inputFileStream.Dispose();
                            outputFileStream.Dispose();

                            File.Delete(inputFilePath);
                            File.Delete(outputFilePath);
                        }

                        await writer.WriteAsync(data);
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Pseudo Console has been terminated.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Reads terminal input and copies it to the PseudoConsole
        /// </summary>
        /// <param name="input"></param>
        private static async Task CopyInputToPipe(Stream input)
        {
            using var writer = new StreamWriter(input) { AutoFlush = true };

            ForwardCtrlC(writer);

            while (true)
            {
                var key = Console.ReadKey(intercept: true).KeyChar;

                // Change "Backspace" to "Ctrl + Backspace"
                key = key == '\x08' ? '\x7F' : key;

                // Send input character-by-character to the pipe
                await writer.WriteAsync(key);
            }
        }

        /// <summary>
        /// Don't let ctrl-c kill the terminal, it should be sent to the process in the terminal.
        /// </summary>
        private static void ForwardCtrlC(StreamWriter writer)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                writer.Write("\x3");
            };
        }
    }
}

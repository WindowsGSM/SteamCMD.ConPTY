using SteamCMD.ConPTY.Interop.Definitions;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SteamCMD.ConPTY
{
    /// <summary>
    /// SteamCMD ConPTY
    /// </summary>
    public class SteamCMDConPTY : WindowsPseudoConsole
    {
        /// <summary>
        /// Start steamcmd.exe
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public new ProcessInfo Start(short width = 120, short height = 30)
        {
            FileName = "steamcmd.exe";

            // Download steamcmd.exe if not exists
            if (!File.Exists(Path.Combine(base.WorkingDirectory, FileName)))
            {
                string zipPath = Path.Combine(base.WorkingDirectory, "steamcmd.zip");

                // Download steamcmd.zip
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", zipPath);
                }

                // Extract steamcmd.zip
                ZipFile.ExtractToDirectory(zipPath, base.WorkingDirectory);

                // Delete steamcmd.zip
                File.Delete(zipPath);
            }

            return base.Start(width, height);
        }
    }
}

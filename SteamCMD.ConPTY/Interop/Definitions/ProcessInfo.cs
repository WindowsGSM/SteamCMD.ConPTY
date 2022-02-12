using System;
using System.Runtime.InteropServices;

namespace SteamCMD.ConPTY.Interop.Definitions
{
    /// <summary>
    /// Process Information
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessInfo
    {
        /// <summary>
        /// Process handle
        /// </summary>
        public IntPtr hProcess;

        /// <summary>
        /// Thread handle
        /// </summary>
        public IntPtr hThread;

        /// <summary>
        /// Process Id
        /// </summary>
        public int dwProcessId;

        /// <summary>
        /// Thread Id
        /// </summary>
        public int dwThreadId;
    }
}

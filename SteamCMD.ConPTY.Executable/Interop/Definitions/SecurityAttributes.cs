using System;
using System.Runtime.InteropServices;

namespace SteamCMD.ConPTY.Executable.Interop.Definitions
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bInheritHandle;

        public static readonly SecurityAttributes Zero = new SecurityAttributes
        {
            nLength = Marshal.SizeOf(typeof(SecurityAttributes)),
            bInheritHandle = true,
            lpSecurityDescriptor = IntPtr.Zero
        };
    }
}

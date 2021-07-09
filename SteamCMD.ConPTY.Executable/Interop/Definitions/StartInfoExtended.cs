using System;
using System.Runtime.InteropServices;

namespace SteamCMD.ConPTY.Executable.Interop.Definitions
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct StartInfoExtended
    {
        public StartInfo StartupInfo;
        public IntPtr lpAttributeList;
    }
}

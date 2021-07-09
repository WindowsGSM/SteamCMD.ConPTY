using System.Runtime.InteropServices;

namespace SteamCMD.ConPTY.Executable.Interop.Definitions
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Coordinates
    {
        public short X;
        public short Y;
    }
}

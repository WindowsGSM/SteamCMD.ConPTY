using System;
using SteamCMD.ConPTY.Executable.Interop.Definitions;
using Microsoft.Win32.SafeHandles;

namespace SteamCMD.ConPTY.Executable.Interop
{
    internal class PseudoConsole : IDisposable
    {
        public static readonly IntPtr PseudoConsoleThreadAttribute
            = (IntPtr)Constants.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

        private PseudoConsole(IntPtr handle)
        {
            Handle = handle;
        }

        ~PseudoConsole()
        {
            Dispose(false);
        }

        public IntPtr Handle { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            ConPtyApi.ClosePseudoConsole(Handle);
        }

        public static PseudoConsole Create(SafeFileHandle inputReadSide, SafeFileHandle outputWriteSide, short width, short height)
        {
            int createResult = ConPtyApi.CreatePseudoConsole(
                new Coordinates { X = width, Y = height },
                inputReadSide, outputWriteSide,
                0, out IntPtr hPC);

            if (createResult != 0)
            {
                throw InteropException.CreateWithInnerHResultException($"Could not create pseudo console. Error Code: {createResult}");
            }

            return new PseudoConsole(hPC);
        }
    }
}

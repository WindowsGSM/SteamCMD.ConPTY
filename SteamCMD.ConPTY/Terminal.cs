using System;
using System.IO;
using System.Threading;
using SteamCMD.ConPTY.Interop;
using Microsoft.Win32.SafeHandles;

namespace SteamCMD.ConPTY
{
    public class Terminal : IDisposable
    {
        private Pipe input;
        private Pipe output;
        private PseudoConsole console;
        private Process process;
        private bool disposed;

        public Terminal()
        {
            ConPtyFeature.ThrowIfVirtualTerminalIsNotEnabled();

            if (ConsoleApi.GetConsoleWindow() != IntPtr.Zero)
            {
                ConPtyFeature.TryEnableVirtualTerminalConsoleSequenceProcessing();
            }
        }

        ~Terminal()
        {
            Dispose(false);
        }

        public FileStream Input { get; private set; }

        public FileStream Output { get; private set; }

        public void Start(string shellCommand, short consoleWidth, short consoleHeight)
        {
            input = new Pipe();
            output = new Pipe();

            console = PseudoConsole.Create(input.Read, output.Write, consoleWidth, consoleHeight);
            process = ProcessFactory.Start(shellCommand, PseudoConsole.PseudoConsoleThreadAttribute, console.Handle);

            Input = new FileStream(input.Write, FileAccess.Write);
            Output = new FileStream(output.Read, FileAccess.Read);
        }

        public void KillConsole()
        {
            console?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public WaitHandle BuildWaitHandler()
        {
            return new AutoResetEvent(false)
            {
                SafeWaitHandle = new SafeWaitHandle(process.ProcessInfo.hProcess, ownsHandle: false)
            };
        }

        public void WaitToExit()
        {
            BuildWaitHandler().WaitOne(Timeout.Infinite);
        }

        public bool GetExitCode(out uint exitCode)
        {
            return ProcessApi.GetExitCodeProcess(process.ProcessInfo.hProcess, out exitCode);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            process?.Dispose();
            console?.Dispose();

            if (disposing)
            {
                Input?.Dispose();
                Output?.Dispose();
            }

            input?.Dispose();
            output?.Dispose();
        }
    }
}

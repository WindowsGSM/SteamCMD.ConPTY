using System;
using System.IO;
using SteamCMD.ConPTY.Interop;
using SteamCMD.ConPTY.Interop.Definitions;

namespace SteamCMD.ConPTY
{
    public class NativeConsole : IDisposable
    {
        private IntPtr handle;
        private bool isDisposed;
        private Pipe stdOut, stdErr, stdIn;

        public NativeConsole(bool hidden = true)
        {
            Initialise(hidden);
        }

        ~NativeConsole()
        {
            Dispose(false);
        }

        public FileStream Output { get; private set; }

        public FileStream Error { get; private set; }

        public FileStream Input { get; private set; }

        public static void SendCtrlEvent(CtrlEvent ctrlEvent)
        {
            ConsoleApi.GenerateConsoleCtrlEvent(ctrlEvent, 0);
        }

        public static void RegisterOnCloseAction(Action action)
        {
            RegisterCtrlEventFunction((ctrlEvent) =>
            {
                if (ctrlEvent == CtrlEvent.CtrlClose)
                {
                    action();
                }

                return false;
            });
        }

        public static void RegisterCtrlEventFunction(CtrlEventDelegate function)
        {
            ConsoleApi.SetConsoleCtrlHandler(function, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            if (disposing)
            {
                Input.Dispose();
                Output.Dispose();
                Error.Dispose();
            }

            ConsoleApi.FreeConsole();
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            stdIn.Dispose();
            stdOut.Dispose();
            stdErr.Dispose();
        }

        private void Initialise(bool hidden)
        {
            if (!ConsoleApi.AllocConsole())
            {
                throw InteropException.CreateWithInnerHResultException("Could not allocate console. You may need to FreeConsole first.");
            }

            handle = ConsoleApi.GetConsoleWindow();

            if (handle != IntPtr.Zero)
            {
                ConsoleApi.ShowWindow(handle, hidden ? ShowState.SwHide : ShowState.SwShowDefault);
            }

            RegisterOnCloseAction(ReleaseUnmanagedResources);

            CreateStdOutPipe();
            CreateStdErrPipe();
            CreateStdInPipe();
        }

        private void CreateStdOutPipe()
        {
            stdOut = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.OutputHandle, stdOut.Write.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDOUT.");
            }
            Output = new FileStream(stdOut.Read, FileAccess.Read);
        }

        private void CreateStdErrPipe()
        {
            stdErr = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.ErrorHandle, stdErr.Write.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDERR.");
            }
            Error = new FileStream(stdErr.Read, FileAccess.Read);
        }

        private void CreateStdInPipe()
        {
            stdIn = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.InputHandle, stdIn.Read.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDIN.");
            }
            Input = new FileStream(stdIn.Write, FileAccess.Write);
        }
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Hexalith.Conversations.TestSupport;

/// <summary>Keeps Windows descendants addressable after their Git parent exits.</summary>
internal sealed class GitProcessJob : IDisposable
{
    private readonly nint _handle;

    /// <summary>Creates a job and enrolls the new process before its pipes are drained.</summary>
    internal GitProcessJob(Process process)
    {
        _handle = CreateJobObject(nint.Zero, null);
        if (_handle == nint.Zero)
        {
            process.Kill(entireProcessTree: true);
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        if (!AssignProcessToJobObject(_handle, process.Handle))
        {
            int error = Marshal.GetLastWin32Error();
            process.Kill(entireProcessTree: true);
            _ = CloseHandle(_handle);
            throw new Win32Exception(error);
        }
    }

    /// <summary>Terminates all enrolled descendants, including after the parent exits.</summary>
    internal void Terminate() => _ = TerminateJobObject(_handle, 1);

    /// <inheritdoc />
    public void Dispose() => _ = CloseHandle(_handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateJobObject(nint securityAttributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(nint job, nint process);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateJobObject(nint job, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
}

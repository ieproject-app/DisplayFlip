using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DisplayFlip.UI.Services;

/// <summary>
/// Layanan Single-Instance Guard berbasis Mutex sistem dan EventWaitHandle IPC
/// untuk mencegah instans ganda dan mengaktifkan instans pertama ke foreground.
/// </summary>
public sealed class SingleInstanceService : IDisposable
{
    public const string MutexName = "DisplayFlip.Wpf.SingleInstanceMutex";
    public const string WakeupEventName = "DisplayFlip.Wpf.WakeupEvent";

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static readonly IntPtr HWND_BROADCAST = new(0xffff);
    private static readonly uint WM_DISPLAYFLIP_WAKE = RegisterWindowMessage("DisplayFlip.Wpf.WakeupMessage");

    private readonly Mutex _mutex;
    private readonly bool _isFirstInstance;
    private readonly string _eventName;
    private EventWaitHandle? _wakeEvent;
    private RegisteredWaitHandle? _waitHandle;
    private bool _disposed;

    public bool IsFirstInstance => _isFirstInstance;
    public Action? OnWakeupRequested { get; set; }

    public SingleInstanceService(string? customMutexName = null, string? customWakeupEventName = null)
    {
        string mutexName = customMutexName ?? MutexName;
        _eventName = customWakeupEventName ?? WakeupEventName;

        _mutex = new Mutex(true, mutexName, out _isFirstInstance);

        if (_isFirstInstance)
        {
            try
            {
                _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, _eventName);
                _waitHandle = ThreadPool.RegisterWaitForSingleObject(
                    _wakeEvent,
                    (state, timedOut) =>
                    {
                        if (!timedOut)
                        {
                            OnWakeupRequested?.Invoke();
                        }
                    },
                    null,
                    -1,
                    false);
            }
            catch
            {
                // Fallback jika EventWaitHandle terkendala
            }
        }
    }

    /// <summary>
    /// Mengirimkan sinyal IPC ke instans DisplayFlip yang sedang aktif untuk memunculkan antarmuka.
    /// </summary>
    public static bool SignalRunningInstance(string? customWakeupEventName = null)
    {
        bool signaled = false;
        string eventName = customWakeupEventName ?? WakeupEventName;

        try
        {
            if (EventWaitHandle.TryOpenExisting(eventName, out var existingEvent))
            {
                using (existingEvent)
                {
                    signaled = existingEvent.Set();
                }
            }
        }
        catch
        {
            // Abaikan error open handle
        }

        try
        {
            if (WM_DISPLAYFLIP_WAKE != 0)
            {
                PostMessage(HWND_BROADCAST, WM_DISPLAYFLIP_WAKE, IntPtr.Zero, IntPtr.Zero);
            }
        }
        catch
        {
            // Abaikan error post message
        }

        return signaled;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _waitHandle?.Unregister(null);
        _wakeEvent?.Dispose();

        if (_isFirstInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // Abaikan jika mutex telah di-release
            }
        }

        _mutex.Dispose();
    }
}

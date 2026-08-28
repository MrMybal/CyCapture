using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace CyCapture.Platform.Windows;

internal sealed class PrintScreenHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const uint VkSnapshot = 0x2C;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private readonly NativeMethods.LowLevelKeyboardProc _callback;
    private nint _hook;
    private bool _isDown;

    internal PrintScreenHook()
    {
        _callback = HookCallback;
    }

    internal event EventHandler? Pressed;
    internal event EventHandler? Released;

    internal void Start()
    {
        if (_hook != 0) return;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = NativeMethods.GetModuleHandle(module?.ModuleName);
        _hook = NativeMethods.SetWindowsHookEx(WhKeyboardLl, _callback, moduleHandle, 0);
        if (_hook == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Impossible d’intercepter la touche Impr écran.");
    }

    private nint HookCallback(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            var input = Marshal.PtrToStructure<NativeMethods.KeyboardHookData>(data);
            if (input.VirtualKeyCode == VkSnapshot)
            {
                var messageId = message.ToInt32();
                if (messageId is WmKeyDown or WmSysKeyDown)
                {
                    if (!_isDown)
                    {
                        _isDown = true;
                        Dispatcher.UIThread.Post(() => Pressed?.Invoke(this, EventArgs.Empty));
                    }
                    return 1;
                }

                if (messageId is WmKeyUp or WmSysKeyUp)
                {
                    if (_isDown)
                    {
                        _isDown = false;
                        Dispatcher.UIThread.Post(() => Released?.Invoke(this, EventArgs.Empty));
                    }
                    return 1;
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hook, code, message, data);
    }

    public void Dispose()
    {
        if (_hook == 0) return;
        NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = 0;
        _isDown = false;
        GC.SuppressFinalize(this);
    }
}

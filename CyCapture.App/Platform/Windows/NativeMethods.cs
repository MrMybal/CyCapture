using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CyCapture.Models;

namespace CyCapture.Platform.Windows;

internal static class NativeMethods
{
    internal const string ExistingInstanceEventName = "Local\\CyCapture.Avalonia.ShowExisting";
    internal const uint WdaExcludeFromCapture = 0x00000011;

    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080L;
    private const int DwmwaExtendedFrameBounds = 9;
    private const int DwmwaCloaked = 14;
    private const uint CwpSkipInvisible = 0x0001;
    private const uint CwpSkipTransparent = 0x0004;
    private const uint MonitorinfofPrimary = 1;
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly nint DpiAwarenessContextPerMonitorAwareV2 = new(-4);
    private static readonly nint HwndTopmost = new(-1);

    internal delegate bool EnumWindowsProc(nint window, nint state);
    private delegate bool MonitorEnumProc(nint monitor, nint hdc, nint rect, nint data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly PixelBounds ToBounds() => new(Left, Top, Right - Left, Bottom - Top);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardHookData
    {
        public uint VirtualKeyCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    internal delegate nint LowLevelKeyboardProc(int code, nint message, nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDpiAwarenessContext(nint value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc callback, nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfoEx info);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromPoint(Point point, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint state);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(nint parent, EnumWindowsProc callback, nint state);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint window, StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint window, StringBuilder text, int count);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(nint window, out Rect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint window, out Rect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(nint window, ref Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(nint window, ref Point point);

    [DllImport("user32.dll")]
    private static extern nint ChildWindowFromPointEx(nint parent, Point point, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out Rect value, int size);

    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    private static extern int DwmGetWindowAttributeInt(nint window, int attribute, out int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmFlush();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowDisplayAffinity(nint window, uint affinity);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint SetWindowsHookEx(int hookId, LowLevelKeyboardProc callback, nint module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    internal static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern nint GetModuleHandle(string? moduleName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint icon);

    internal static void EnablePerMonitorDpiAwareness()
    {
        try { SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2); }
        catch { /* Le manifeste reste le repli sur les anciennes versions de Windows. */ }
    }

    internal static void SetWindowBounds(nint window, PixelBounds bounds)
    {
        if (window == 0 || bounds.Width <= 0 || bounds.Height <= 0) return;
        SetWindowPos(
            window,
            HwndTopmost,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            SwpNoActivate | SwpShowWindow);
    }

    internal static void SignalExistingInstance()
    {
        try
        {
            using var signal = EventWaitHandle.OpenExisting(ExistingInstanceEventName);
            signal.Set();
        }
        catch { }
    }

    internal static IReadOnlyList<MonitorDescriptor> GetMonitors()
    {
        var result = new List<MonitorDescriptor>();
        var number = 0;
        EnumDisplayMonitors(0, 0, (monitor, _, _, _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>(), DeviceName = string.Empty };
            if (!GetMonitorInfo(monitor, ref info)) return true;
            number++;
            var scale = 1d;
            try
            {
                if (GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0 && dpiX > 0)
                    scale = dpiX / 96d;
            }
            catch { }

            result.Add(new MonitorDescriptor(
                monitor,
                info.DeviceName,
                $"Écran {number}",
                info.Monitor.ToBounds(),
                info.WorkArea.ToBounds(),
                scale,
                (info.Flags & MonitorinfofPrimary) != 0));
            return true;
        }, 0);
        return result;
    }

    internal static MonitorDescriptor MonitorAt(int x, int y)
    {
        var monitor = MonitorFromPoint(new Point { X = x, Y = y }, MonitorDefaultToNearest);
        return GetMonitors().First(item => item.Handle == monitor);
    }

    internal static (IReadOnlyList<SelectableRegion> Regions, IReadOnlyList<SelectableWindowLayer> WindowLayers)
        EnumerateSelectableRegions()
    {
        var regions = new List<SelectableRegion>();
        var windowLayers = new List<SelectableWindowLayer>();
        var ownProcess = (uint)Environment.ProcessId;
        var zOrder = 0;

        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window) || IsIconic(window)) return true;
            if (IsCloaked(window)) return true;
            if (!TryGetExtendedBounds(window, out var windowRect) || !IsValid(windowRect, 1, 1)) return true;

            var windowBounds = windowRect.ToBounds();
            windowLayers.Add(new SelectableWindowLayer(window, windowBounds, zOrder++));

            GetWindowThreadProcessId(window, out var processId);
            if (processId == ownProcess) return true;
            if ((GetWindowLongPtr(window, GwlExStyle).ToInt64() & WsExToolWindow) != 0) return true;

            var title = WindowText(window);
            if (string.IsNullOrWhiteSpace(title)) return true;
            if (!IsValid(windowRect, 80, 50)) return true;

            regions.Add(new SelectableRegion(window, title, SelectionKind.Window, windowBounds, 10, window));

            if (TryGetClientBounds(window, out var clientRect) && IsValid(clientRect, 60, 40))
            {
                var inset = Math.Abs(clientRect.Left - windowRect.Left) + Math.Abs(clientRect.Top - windowRect.Top)
                    + Math.Abs(clientRect.Right - windowRect.Right) + Math.Abs(clientRect.Bottom - windowRect.Bottom);
                if (inset > 3)
                    regions.Add(new SelectableRegion(
                        window,
                        $"Contenu — {title}",
                        SelectionKind.Client,
                        clientRect.ToBounds().Intersect(windowBounds),
                        20,
                        window));
            }

            EnumChildWindows(window, (child, _) =>
            {
                if (!IsWindowVisible(child) || !GetWindowRect(child, out var rect) || !IsValid(rect, 30, 20)) return true;
                var childTitle = WindowText(child);
                if (string.IsNullOrWhiteSpace(childTitle)) childTitle = WindowClass(child);
                if (string.IsNullOrWhiteSpace(childTitle)) childTitle = "Contrôle";
                var childBounds = rect.ToBounds().Intersect(windowBounds);
                if (childBounds.Width > 0 && childBounds.Height > 0)
                    regions.Add(new SelectableRegion(child, childTitle, SelectionKind.Control, childBounds, 30, window));
                return true;
            }, 0);

            return true;
        }, 0);

        return (regions, windowLayers);
    }

    internal static IReadOnlyList<nint> ChildWindowPathAt(nint rootWindow, int screenX, int screenY)
    {
        var path = new List<nint>();
        var current = rootWindow;
        for (var depth = 0; depth < 32; depth++)
        {
            var point = new Point { X = screenX, Y = screenY };
            if (!ScreenToClient(current, ref point)) break;
            var child = ChildWindowFromPointEx(current, point, CwpSkipInvisible | CwpSkipTransparent);
            if (child == 0 || child == current || path.Contains(child)) break;
            path.Add(child);
            current = child;
        }
        return path;
    }

    internal static nint CreateIconHandle(bool recording)
    {
        using var bitmap = new System.Drawing.Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.Clear(System.Drawing.Color.Transparent);
        using var dark = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 6, 25, 43));
        using var white = new System.Drawing.SolidBrush(System.Drawing.Color.White);
        using var accent = new System.Drawing.Pen(recording
            ? System.Drawing.Color.FromArgb(255, 239, 58, 72)
            : System.Drawing.Color.FromArgb(255, 120, 255, 50), 2.2f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        using var monogram = new System.Drawing.Pen(System.Drawing.Color.White, 2.1f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Flat,
            EndCap = System.Drawing.Drawing2D.LineCap.Flat
        };
        graphics.FillEllipse(dark, 1, 1, 30, 30);
        graphics.DrawArc(accent, 4, 4, 24, 24, 222, 96);
        graphics.DrawArc(accent, 4, 4, 24, 24, 330, 113);
        graphics.DrawArc(accent, 4, 4, 24, 24, 97, 113);
        graphics.DrawArc(monogram, 9, 9.3f, 14, 14, 43, 274);
        graphics.FillPolygon(white,
        [
            new System.Drawing.PointF(13.1f, 13.1f),
            new System.Drawing.PointF(15.1f, 13.1f),
            new System.Drawing.PointF(16, 14.9f),
            new System.Drawing.PointF(16.9f, 13.1f),
            new System.Drawing.PointF(18.9f, 13.1f),
            new System.Drawing.PointF(17, 16.5f),
            new System.Drawing.PointF(17, 21),
            new System.Drawing.PointF(15, 21),
            new System.Drawing.PointF(15, 16.5f)
        ]);
        return bitmap.GetHicon();
    }

    internal static void ReleaseIconHandle(nint handle)
    {
        if (handle != 0) DestroyIcon(handle);
    }

    internal static void ApplyDarkWindowChrome(nint window)
    {
        if (window == 0) return;
        try
        {
            var enabled = 1;
            if (DwmSetWindowAttribute(window, 20, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(window, 19, ref enabled, sizeof(int));

            var caption = ColorRef(10, 15, 15);
            var border = ColorRef(38, 51, 49);
            var text = ColorRef(244, 248, 244);
            DwmSetWindowAttribute(window, 35, ref caption, sizeof(int));
            DwmSetWindowAttribute(window, 34, ref border, sizeof(int));
            DwmSetWindowAttribute(window, 36, ref text, sizeof(int));
        }
        catch
        {
            // Les versions de Windows antérieures ignorent simplement la personnalisation DWM.
        }
    }

    private static int ColorRef(byte red, byte green, byte blue) => red | (green << 8) | (blue << 16);

    private static bool TryGetExtendedBounds(nint window, out Rect rect)
    {
        if (DwmGetWindowAttribute(window, DwmwaExtendedFrameBounds, out rect, Marshal.SizeOf<Rect>()) == 0)
            return true;
        return GetWindowRect(window, out rect);
    }

    private static bool TryGetClientBounds(nint window, out Rect rect)
    {
        rect = default;
        if (!GetClientRect(window, out var client)) return false;
        var topLeft = new Point { X = client.Left, Y = client.Top };
        var bottomRight = new Point { X = client.Right, Y = client.Bottom };
        if (!ClientToScreen(window, ref topLeft) || !ClientToScreen(window, ref bottomRight)) return false;
        rect = new Rect { Left = topLeft.X, Top = topLeft.Y, Right = bottomRight.X, Bottom = bottomRight.Y };
        return true;
    }

    private static bool IsCloaked(nint window)
    {
        try
        {
            return DwmGetWindowAttributeInt(window, DwmwaCloaked, out var value, sizeof(int)) == 0 && value != 0;
        }
        catch { return false; }
    }

    private static bool IsValid(Rect rect, int minimumWidth, int minimumHeight) =>
        rect.Right - rect.Left >= minimumWidth && rect.Bottom - rect.Top >= minimumHeight
        && rect.Left > -30000 && rect.Top > -30000;

    private static string WindowText(nint window)
    {
        var length = GetWindowTextLength(window);
        if (length <= 0) return string.Empty;
        var text = new StringBuilder(length + 1);
        GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }

    private static string WindowClass(nint window)
    {
        var text = new StringBuilder(256);
        GetClassName(window, text, text.Capacity);
        return text.ToString();
    }
}

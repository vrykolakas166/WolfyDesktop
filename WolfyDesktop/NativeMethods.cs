using System.Runtime.InteropServices;
using Windows.Graphics;

namespace WolfyDesktop;

/// <summary>Win32 calls with no Windows App SDK equivalent.</summary>
internal static partial class NativeMethods
{
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int SM_SWAPBUTTON = 23;

    /// <summary>Cursor position in screen pixels.</summary>
    public static PointInt32 GetCursorPosition()
    {
        GetCursorPos(out var point);
        return new PointInt32(point.X, point.Y);
    }

    /// <summary>
    /// Whether the primary mouse button is physically held down right now, independent of which
    /// window received the press or release.
    /// </summary>
    public static bool IsPrimaryButtonDown()
    {
        var button = GetSystemMetrics(SM_SWAPBUTTON) != 0 ? VK_RBUTTON : VK_LBUTTON;
        return (GetAsyncKeyState(button) & 0x8000) != 0;
    }

    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out NativePoint point);

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int key);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);
}

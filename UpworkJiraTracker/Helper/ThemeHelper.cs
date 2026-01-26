using System.Runtime.InteropServices;

using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace UpworkJiraTracker.Helper;

public static class ThemeHelper
{
    public static WpfColor GetTaskbarColor()
    {
        try
        {
            // Try to get the accent color used by Windows
            var result = NativeMethods.DwmGetColorizationColor(out uint color, out bool opaqueBlend);

            if (result == 0)
            {
                // Color is in AARRGGBB format
                byte a = (byte)(color >> 24 & 0xFF);
                byte r = (byte)(color >> 16 & 0xFF);
                byte g = (byte)(color >> 8 & 0xFF);
                byte b = (byte)(color & 0xFF);

                // Make it slightly transparent to match taskbar
                return WpfColor.FromArgb((byte)(opaqueBlend ? 255 : 200), r, g, b);
            }
        }
        catch
        {
            // Fallback
        }

        // Try to get immersive color (Windows 10/11 style)
        try
        {
            uint colorSet = NativeMethods.GetImmersiveUserColorSetPreference(false, false);
            nint colorName = Marshal.StringToHGlobalUni("ImmersiveStartBackground");

            try
            {
                uint colorType = NativeMethods.GetImmersiveColorTypeFromName(colorName);
                uint color = NativeMethods.GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);

                if (color != 0)
                {
                    // Color is in AABBGGRR format for immersive colors
                    byte a = (byte)(color >> 24 & 0xFF);
                    byte b = (byte)(color >> 16 & 0xFF);
                    byte g = (byte)(color >> 8 & 0xFF);
                    byte r = (byte)(color & 0xFF);

                    return WpfColor.FromArgb(a == 0 ? (byte)200 : a, r, g, b);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(colorName);
            }
        }
        catch
        {
            // Fallback
        }

        // Default to a semi-transparent dark color similar to Windows 11 taskbar
        return WpfColor.FromArgb(200, 32, 32, 32);
    }

    public static bool IsLightTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            var value = key?.GetValue("SystemUsesLightTheme");

            if (value is int intValue)
            {
                return intValue != 0;
            }
        }
        catch
        {
            // Fallback
        }

        return false;
    }

    public static WpfColor GetForegroundColor()
    {
        return IsLightTheme() ? WpfColors.Black : WpfColors.White;
    }
}

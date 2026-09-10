using System;

namespace Announcement.Configuration
{
    /// <summary>
    /// Manages application-wide settings and constants for the Announcement utility.
    /// </summary>
    internal static class AppSettingsManager
    {
        // Unique AppUserModelID used to register custom notification toasts with Windows Shell
        public static string AppUserModelId => "EnergySavingAlertApp";

        // Expiration time in minutes before standard sticky notification toasts are cleared from the action center
        public static double ClearAfterMinutes => 2.0;
    }
}

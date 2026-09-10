using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Globalization;
using Announcement.Configuration;
using Announcement.Repositories;
using Announcement.Services;
using Announcement.UI;

namespace Announcement
{
    static class Program
    {
        // Registers explicit process AppUserModelID (required to attach desktop toasts correctly)
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

        private static readonly string _logDir = @"C:\Downloadpath\Notification";

        /// <summary>
        /// Writes message traces into local debug logs.
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(_logDir, "Announcement_debug.log"),
                    $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} | {message}{Environment.NewLine}");
            }
            catch { }
        }

        /// <summary>
        /// Main entry point for the Announcement application.
        /// 
        /// Flow:
        ///   1. --startup flag  →  Show farewell popup ONCE (tracked by LastPopupDate.txt existence)
        ///   2. No flag         →  Show peekaboo toast notification
        ///   3. Campaign lasts ~3 days from StartDate.txt, then auto-stops.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Log($"Application started. Args: [{(args != null ? string.Join(", ", args) : "")}] | .NET: {Environment.Version} | User: {Environment.UserName} | Machine: {Environment.MachineName}");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string appId = AppSettingsManager.AppUserModelId;
                SetCurrentProcessExplicitAppUserModelID(appId);

                // Ensure a valid Shell shortcut with APPID metadata is present in Start Menu (essential for toasts)
                ShortcutService.EnsureStartMenuShortcut(appId);

                DateTime nowRaw = DateTime.Now;
                DateTime now = DateTime.Parse(nowRaw.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                Log($"Current DateTime (Forced AD): {now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}, DayOfWeek: {now.DayOfWeek}");

                // --- Parse Start Date ---
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dateFile = Path.Combine(baseDir, "StartDate.txt");

                DateTime installDate;
                if (File.Exists(dateFile))
                {
                    string dateRawText = File.ReadAllText(dateFile).Trim();
                    string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" };

                    if (DateTime.TryParseExact(dateRawText, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateAD))
                    {
                        // Check if the year looks like Buddhist Era (BE/พ.ศ.) - year >= 2400 is considered BE
                        if (parsedDateAD.Year >= 2400)
                        {
                            installDate = parsedDateAD.AddYears(-543).Date;
                            Log($"StartDate parsed as BE (year={parsedDateAD.Year}), converted to AD: {installDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                        }
                        else
                        {
                            installDate = parsedDateAD.Date;
                            Log($"StartDate parsed (AD): {installDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                        }
                    }
                    else if (DateTime.TryParseExact(dateRawText, formats, new CultureInfo("th-TH"), DateTimeStyles.None, out DateTime parsedDateBE))
                    {
                        installDate = parsedDateBE.Date;
                        Log($"StartDate parsed (BE converted to AD): {installDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                    }
                    else
                    {
                        installDate = now.Date;
                        Log($"StartDate format mismatch in file, fallback to today (AD): {installDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                    }
                }
                else
                {
                    installDate = now.Date;
                    Log($"StartDate not found, using today (AD): {installDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                }

                // Guard: Not yet started
                if (now.Date < installDate)
                {
                    Log($"EXIT: now.Date ({now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}) < installDate ({installDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)})");
                    return;
                }

                // Campaign duration: active for ~3 days from install date
                double daysSinceInstall = (now.Date - installDate).TotalDays;
                bool isCampaignActive = daysSinceInstall <= 3;
                Log($"daysSinceInstall: {daysSinceInstall}, isCampaignActive: {isCampaignActive}");

                if (!isCampaignActive)
                {
                    Log("EXIT: Campaign ended (past 3 days)");
                    return;
                }

                // Parse startup popup parameter
                bool isStartupPopup = false;
                if (args != null)
                {
                    foreach (string arg in args)
                    {
                        if (arg.Equals("--startup", StringComparison.OrdinalIgnoreCase))
                        {
                            isStartupPopup = true;
                            break;
                        }
                    }
                }

                Log($"isStartupPopup: {isStartupPopup}");

                // === Farewell Popup Flow (--startup) ===
                if (isStartupPopup)
                {
                    // Show farewell popup ONCE ever (not once per day — just once)
                    string popupTrackFile = Path.Combine(baseDir, "LastPopupDate.txt");

                    if (!File.Exists(popupTrackFile))
                    {
                        Log("First time — showing farewell popup");
                        try
                        {
                            File.WriteAllText(popupTrackFile, now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                        }
                        catch (Exception ex)
                        {
                            Log($"Error writing track file: {ex.Message}");
                        }

                        Log("Launching MainPopupForm...");
                        Application.Run(new MainPopupForm());
                        Log("MainPopupForm closed");
                    }
                    else
                    {
                        Log("EXIT: Farewell popup already shown");
                    }
                    return;
                }

                // === Peekaboo Toast Flow (17:50 scheduled) ===
                Log($"Toast: title={MessageRepository.ToastTitle}, body={MessageRepository.ToastBody}");

                double clearMin = AppSettingsManager.ClearAfterMinutes;
                Log($"ClearAfterMinutes: {clearMin}");

                var svc = new ToastService(appId);
                svc.ShowStickyToast(MessageRepository.ToastTitle, MessageRepository.ToastBody, clearMin);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UNHANDLED EXCEPTION: {ex}");
                Log($"UNHANDLED EXCEPTION: {ex}");
                try
                {
                    string crashLog = Path.Combine(_logDir, "Announcement_crash.log");
                    File.WriteAllText(crashLog, ex.ToString());
                }
                catch { }
            }
        }
    }
}
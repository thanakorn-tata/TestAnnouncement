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

                // --- (First Date Configuration) ---
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

                if (now.Date < installDate)
                {
                    Log($"EXIT: now.Date ({now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}) < installDate ({installDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)})");
                    return;
                }

                // Guard: Bypass alerts on Weekends
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                {
                    Log($"EXIT: Weekend ({now.DayOfWeek})");
                    return;
                }

                // Parse startup popup parameters
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

                // Campaign duration config: active until 2026-10-31
                double daysSinceInstall = (now.Date - installDate).TotalDays;
                bool isCampaignActive = now.Date <= new DateTime(2026, 10, 31);
                bool isFirstWeek = daysSinceInstall < 7;
                Log($"isStartupPopup: {isStartupPopup}, isCampaignActive: {isCampaignActive}, isFirstWeek: {isFirstWeek}, daysSinceInstall: {daysSinceInstall}");

                // Guard: Exit if the campaign duration has ended
                if (!isCampaignActive)
                {
                    Log("EXIT: Campaign ended (past 2026-10-31), no action taken");
                    return;
                }

                // Fullscreen Popup Flow
                if (isStartupPopup)
                {
                    if (now.Hour < 6)
                    {
                        Log($"EXIT: Hour ({now.Hour}) < 6");
                        return;
                    }

                    // Check if popup alert was already displayed today
                    string popupTrackFile = Path.Combine(baseDir, "LastPopupDate.txt");
                    string todayStr = now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    
                    bool alreadyShown = false;
                    if (File.Exists(popupTrackFile))
                    {
                        try
                        {
                            string lastDate = File.ReadAllText(popupTrackFile).Trim();
                            if (lastDate == todayStr)
                            {
                                alreadyShown = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error reading LastPopupDate: {ex.Message}");
                        }
                    }

                    if (!alreadyShown)
                    {
                        Log("Popup not yet shown today, writing track file and launching popup");
                        try
                        {
                            File.WriteAllText(popupTrackFile, todayStr);
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
                        Log("EXIT: Popup already shown today");
                    }
                    return;
                }

                // Toast Notification Flow (Noon & Evening schedules)
                int idx = (int)now.DayOfWeek - 1;
                if (idx < 0)
                {
                    idx = 0;
                }
                int maxIdx = MessageRepository.CasualNoonMessages.Length - 1;
                int dayIndex = Math.Min(idx, maxIdx);

                // Run noon templates before 3:00 PM, otherwise default to evening templates
                bool isNoon = now.Hour < 15;
                Log($"Toast branch: isNoon={isNoon}, dayIndex={dayIndex}, isFirstWeek={isFirstWeek}");

                string body;
                if (isFirstWeek)
                {
                    body = isNoon ? MessageRepository.FormalNoonMessage : MessageRepository.FormalEveningMessage;
                }
                else
                {
                    body = isNoon ? MessageRepository.CasualNoonMessages[dayIndex] : MessageRepository.CasualEveningMessages[dayIndex];
                }
                Log($"Toast body: {body.Substring(0, Math.Min(body.Length, 80))}...");

                double clearMin = AppSettingsManager.ClearAfterMinutes;
                Log($"ClearAfterMinutes: {clearMin}");

                var svc = new ToastService(appId);
                svc.ShowStickyToast(body, clearMin);
            }
            catch (Exception ex)
            {
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
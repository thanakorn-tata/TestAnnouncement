using System;
using System.Timers;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Announcement.Services
{
    /// <summary>
    /// Serves UWP Toast notifications in a sticky (IncomingCall) context
    /// and handles automatic dismissal schedule timers.
    /// </summary>
    public class ToastService
    {
        private readonly string _appId;

        public ToastService(string appId)
        {
            _appId = appId;
        }

        /// <summary>
        /// Shows a sticky UWP Toast Notification (using IncomingCall template)
        /// that remains on the user's screen and auto-expires after a set duration.
        /// </summary>
        /// <param name="body">Thai alert message string details.</param>
        /// <param name="clearAfterMinutes">Time span in minutes before auto-expiry.</param>
        public void ShowStickyToast(string body, double clearAfterMinutes = 2.0)
        {
            try
            {
                var builder = new ToastContentBuilder()
                    .AddArgument("action", "announcement")
                    .AddText("One Switch , Big Impact")
                    .AddText(body);

                // Configure ToastScenario.IncomingCall to make it stick to the screen,
                // and configure the ExpirationTime so Windows cleans it up automatically.
                builder.SetToastScenario(ToastScenario.IncomingCall)
                       .Show(toast =>
                       {
                           toast.ExpirationTime = DateTime.Now.AddMinutes(clearAfterMinutes);
                       });

                Program.Log("Toast notification shown successfully");
            }
            catch (Exception ex)
            {
                Program.Log($"Toast exception: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Instantly clears all displayed notifications of this application from the Windows Action Center.
        /// </summary>
        public void ClearAll()
        {
            try 
            { 
                ToastNotificationManagerCompat.History.Clear(); 
            }
            catch { }
        }

        /// <summary>
        /// Sets a backup background timer to clear notifications.
        /// </summary>
        private void ScheduleAutoClear(double minutes)
        {
            var timer = new Timer(minutes * 60 * 1000) { AutoReset = false };
            timer.Elapsed += (s, e) =>
            {
                ClearAll();
                timer.Dispose();
            };
            timer.Start();
        }
    }
}

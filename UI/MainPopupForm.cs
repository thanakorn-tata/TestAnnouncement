using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Announcement.UI
{
    /// <summary>
    /// Displays a fullscreen borderless window containing energy saving campaign alerts.
    /// Supports dynamic artwork loading or placeholders fallback.
    /// </summary>
    public class MainPopupForm : Form
    {
        public MainPopupForm()
        {
            // Set standard visual form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "Announcement";
            this.ShowInTaskbar = true;
            this.TopMost = true;

            // Fit form to screen dimensions
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            this.Bounds = screenBounds;
            this.WindowState = FormWindowState.Maximized;

            // Attempt to load background artwork image
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imagePath = Path.Combine(baseDir, "MainPopup.png");

            if (File.Exists(imagePath))
            {
                try
                {
                    this.BackgroundImage = Image.FromFile(imagePath);
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                }
                catch (Exception ex)
                {
                    ShowPlaceholder($"Error loading Artwork: {ex.Message}");
                }
            }
            else
            {
                ShowPlaceholder("Waiting for Artwork (1920x1080)");
            }

            // Close Button (X) placed at top-right corner
            Button closeBtn = new Button();
            closeBtn.Text = "✕";
            closeBtn.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            closeBtn.ForeColor = Color.White;
            closeBtn.BackColor = Color.FromArgb(80, 0, 0, 0);
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Size = new Size(45, 45);
            closeBtn.Location = new Point(screenBounds.Width - 55, 10);
            closeBtn.Cursor = Cursors.Hand;
            closeBtn.Click += (s, e) => this.Close();
            this.Controls.Add(closeBtn);

            // Close Form using ESC key
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };

            // Force topmost window status and focus on screen show
            this.Shown += (s, e) =>
            {
                this.TopMost = true;
                this.Activate();
                this.BringToFront();
                this.Focus();
            };
        }

        /// <summary>
        /// Displays placeholder label if image asset is not found.
        /// </summary>
        private void ShowPlaceholder(string message)
        {
            this.BackColor = Color.FromArgb(24, 24, 24); // Dark theme layout

            Label lbl = new Label();
            lbl.Text = message;
            lbl.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(120, 120, 120);
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Dock = DockStyle.Fill;
            this.Controls.Add(lbl);
        }
    }
}

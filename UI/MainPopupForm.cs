using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Announcement.Repositories;

namespace Announcement.UI
{
    /// <summary>
    /// Displays a fullscreen borderless farewell message with a gradient background.
    /// All text is rendered in the Paint event for proper transparency over the gradient.
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
            this.Text = "ลาก่อนนะครับ";
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.DoubleBuffered = true;

            // Fit form to screen dimensions
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            this.Bounds = screenBounds;
            this.WindowState = FormWindowState.Maximized;

            // Close Button (✕) placed at top-right corner
            Button closeBtn = new Button();
            closeBtn.Text = "✕";
            closeBtn.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            closeBtn.ForeColor = Color.FromArgb(200, 255, 255, 255);
            closeBtn.BackColor = Color.FromArgb(50, 255, 255, 255);
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(100, 255, 255, 255);
            closeBtn.Size = new Size(50, 50);
            closeBtn.Location = new Point(screenBounds.Width - 65, 15);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            // --- Gradient Background (dark blue-purple) ---
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(12, 15, 40),
                Color.FromArgb(35, 20, 55),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }

            // Subtle decorative circle glow (center)
            int glowSize = Math.Min(w, h) / 2;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(w / 2 - glowSize / 2, h / 2 - glowSize / 2, glowSize, glowSize);
                using (PathGradientBrush glowBrush = new PathGradientBrush(path))
                {
                    glowBrush.CenterColor = Color.FromArgb(18, 80, 100, 180);
                    glowBrush.SurroundColors = new Color[] { Color.FromArgb(0, 30, 30, 60) };
                    g.FillPath(glowBrush, path);
                }
            }

            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // --- Wave Emoji ---
            int emojiY = (int)(h * 0.18);
            using (Font emojiFont = new Font("Segoe UI Emoji", 52))
            using (SolidBrush emojiBrush = new SolidBrush(Color.White))
            {
                g.DrawString("\U0001f44b", emojiFont, emojiBrush,
                    new RectangleF(0, emojiY, w, 80), sf);
            }

            // --- Main Farewell Message ---
            int msgTop = (int)(h * 0.30);
            int msgHeight = (int)(h * 0.40);
            using (Font msgFont = new Font("Segoe UI", 24, FontStyle.Regular))
            using (SolidBrush msgBrush = new SolidBrush(Color.FromArgb(235, 238, 245)))
            {
                RectangleF textRect = new RectangleF(80, msgTop, w - 160, msgHeight);
                g.DrawString(MessageRepository.FarewellMessage, msgFont, msgBrush, textRect, sf);
            }

            // --- Signature ---
            int sigY = (int)(h * 0.76);
            using (Font sigFont = new Font("Segoe UI", 20, FontStyle.Italic))
            using (SolidBrush sigBrush = new SolidBrush(Color.FromArgb(140, 150, 175)))
            {
                g.DrawString("— ตาต้า", sigFont, sigBrush,
                    new RectangleF(0, sigY, w, 50), sf);
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Announcement.Repositories;

namespace Announcement.UI
{
    /// <summary>
    /// Ultra-modern, aesthetic mini popup card at the bottom-right corner.
    /// All interactive elements (close ✕, action button, icons) are rendered directly
    /// onto the canvas in OnPaint, eliminating WinForms child control transparency bugs
    /// (black boxes / overlapping text).
    /// </summary>
    public class MainPopupForm : Form
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse
        );

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private bool _isDragging = false;
        private Point _dragCursorPoint;
        private Point _dragFormPoint;

        private bool _isCloseHovered = false;
        private bool _isActionHovered = false;
        private bool _isActionPressed = false;

        private readonly Font _titleFont;
        private readonly Font _subtitleFont;
        private readonly Font _bodyFont;
        private readonly Font _footerFont;
        private readonly Font _btnFont;
        private readonly Font _emojiFont;

        private Rectangle CloseButtonRect => new Rectangle(this.ClientSize.Width - 36, 14, 24, 24);
        private Rectangle ActionButtonRect => new Rectangle(this.ClientSize.Width - 18 - 130, this.ClientSize.Height - 16 - 34, 130, 34);

        public MainPopupForm()
        {
            const int formWidth = 430;
            const int formHeight = 315;
            const int margin = 24;

            // Form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "ข้อความจากตาต้า";
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.ClientSize = new Size(formWidth, formHeight);

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );

            // Position at bottom-right of primary screen working area
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(
                workingArea.Right - formWidth - margin,
                workingArea.Bottom - formHeight - margin
            );

            // Optimized typography
            _titleFont = GetBestFont(11.5f, FontStyle.Bold);
            _subtitleFont = GetBestFont(8.5f, FontStyle.Regular);
            _bodyFont = GetBestFont(9.75f, FontStyle.Regular);
            _footerFont = GetBestFont(9f, FontStyle.Regular);
            _btnFont = GetBestFont(9.5f, FontStyle.Bold);
            _emojiFont = new Font("Segoe UI Emoji", 14f, FontStyle.Regular);

            // Close on ESC key
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };

            this.Shown += (s, e) =>
            {
                this.TopMost = true;
                this.Activate();
                this.BringToFront();
                this.Focus();
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Windows 11 DWM rounded corners and dark mode
            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

                int darkMode = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            }
            catch { }

            // Fallback region for Windows 10
            try
            {
                IntPtr rgn = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 20, 20);
                this.Region = Region.FromHrgn(rgn);
            }
            catch { }
        }

        // --- Mouse and Interaction Handling ---
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(_dragCursorPoint));
                this.Location = Point.Add(_dragFormPoint, new Size(diff));
                return;
            }

            bool closeHover = CloseButtonRect.Contains(e.Location);
            bool actionHover = ActionButtonRect.Contains(e.Location);

            if (closeHover != _isCloseHovered || actionHover != _isActionHovered)
            {
                _isCloseHovered = closeHover;
                _isActionHovered = actionHover;
                this.Cursor = (_isCloseHovered || _isActionHovered) ? Cursors.Hand : Cursors.Default;
                this.Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                if (CloseButtonRect.Contains(e.Location))
                {
                    this.Close();
                    return;
                }

                if (ActionButtonRect.Contains(e.Location))
                {
                    _isActionPressed = true;
                    this.Invalidate();
                    return;
                }

                // Drag window
                _isDragging = true;
                _dragCursorPoint = Cursor.Position;
                _dragFormPoint = this.Location;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isActionPressed)
            {
                _isActionPressed = false;
                this.Invalidate();

                if (ActionButtonRect.Contains(e.Location))
                {
                    this.Close();
                    return;
                }
            }

            _isDragging = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_isCloseHovered || _isActionHovered)
            {
                _isCloseHovered = false;
                _isActionHovered = false;
                this.Cursor = Cursors.Default;
                this.Invalidate();
            }
        }

        // --- Canvas Rendering ---
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            // 1. Deep Obsidian Slate Background
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(15, 19, 34),
                Color.FromArgb(24, 30, 52),
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }

            // 2. Ambient Gradient Glow at Top-Left
            using (GraphicsPath glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(-60, -60, 260, 240);
                using (PathGradientBrush glowBrush = new PathGradientBrush(glowPath))
                {
                    glowBrush.CenterColor = Color.FromArgb(50, 99, 102, 241);
                    glowBrush.SurroundColors = new Color[] { Color.FromArgb(0, 15, 19, 34) };
                    g.FillPath(glowBrush, glowPath);
                }
            }

            // 3. Top Glowing Accent Line
            using (LinearGradientBrush topBarBrush = new LinearGradientBrush(
                new Point(0, 0), new Point(w, 0),
                Color.FromArgb(56, 189, 248),
                Color.FromArgb(168, 85, 247)))
            {
                ColorBlend blend = new ColorBlend(3)
                {
                    Colors = new Color[]
                    {
                        Color.FromArgb(56, 189, 248),
                        Color.FromArgb(99, 102, 241),
                        Color.FromArgb(168, 85, 247)
                    },
                    Positions = new float[] { 0f, 0.5f, 1f }
                };
                topBarBrush.InterpolationColors = blend;
                using (Pen topPen = new Pen(topBarBrush, 2.5f))
                {
                    g.DrawLine(topPen, 16, 1, w - 16, 1);
                }
            }

            // 4. Subtle Outer Border
            using (Pen borderPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1f))
            {
                using (GraphicsPath path = GetRoundedRectanglePath(new Rectangle(0, 0, w - 1, h - 1), 20))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 5. Header: Avatar Ring with Emoji
            int avatarX = 18;
            int avatarY = 14;
            int avatarSize = 38;

            Rectangle avatarRect = new Rectangle(avatarX, avatarY, avatarSize, avatarSize);
            using (LinearGradientBrush avatarBrush = new LinearGradientBrush(
                avatarRect,
                Color.FromArgb(79, 70, 229),
                Color.FromArgb(147, 51, 234),
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillEllipse(avatarBrush, avatarRect);
            }

            using (Pen ringPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f))
            {
                g.DrawEllipse(ringPen, avatarRect);
            }

            StringFormat sfCenter = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("\U0001f44b", _emojiFont, Brushes.White,
                new RectangleF(avatarX, avatarY + 1, avatarSize, avatarSize), sfCenter);

            // 6. Header Titles
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                g.DrawString("ตาต้า (Thanakorn)", _titleFont, titleBrush, new PointF(64, 14));
            }

            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString("ข้อความส่งท้าย • Farewell message", _subtitleFont, subBrush, new PointF(65, 34));
            }

            // 7. Close Button (✕) rendered directly onto canvas
            DrawCloseButton(g);

            // 8. Frosted Glass Quote Card for Message Body
            int cardX = 16;
            int cardY = 60;
            int cardW = w - 32;
            int cardH = 188;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardW, cardH);
            using (GraphicsPath cardPath = GetRoundedRectanglePath(cardRect, 14))
            {
                using (SolidBrush cardBg = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
                {
                    g.FillPath(cardBg, cardPath);
                }

                using (Pen cardBorder = new Pen(Color.FromArgb(30, 255, 255, 255), 1f))
                {
                    g.DrawPath(cardBorder, cardPath);
                }
            }

            // Left decorative accent bar (Gradient cyan-indigo)
            RectangleF accentBarRect = new RectangleF(cardX + 8, cardY + 12, 3.5f, cardH - 24);
            using (LinearGradientBrush accentBrush = new LinearGradientBrush(
                accentBarRect,
                Color.FromArgb(56, 189, 248),
                Color.FromArgb(129, 140, 248),
                LinearGradientMode.Vertical))
            {
                using (GraphicsPath barPath = GetRoundedRectanglePath(Rectangle.Round(accentBarRect), 2))
                {
                    g.FillPath(accentBrush, barPath);
                }
            }

            // Message text inside card
            using (SolidBrush msgBrush = new SolidBrush(Color.FromArgb(226, 232, 240)))
            {
                StringFormat sfMessage = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoClip
                };
                RectangleF textRect = new RectangleF(cardX + 22, cardY + 10, cardW - 32, cardH - 20);
                g.DrawString(MessageRepository.FarewellMessage, _bodyFont, msgBrush, textRect, sfMessage);
            }

            // 9. Footer Note (left side)
            using (SolidBrush footerBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString("ขอให้ทุกคนโชคดี มีความสุขครับ", _footerFont, footerBrush, new PointF(18, h - 16 - 26));
            }
            DrawSparkle(g, 185, h - 16 - 17, 5.5f, Color.FromArgb(250, 204, 21)); // Gold sparkle

            // 10. Primary Action Button rendered directly onto canvas (zero black edges!)
            DrawActionButton(g);
        }

        private void DrawCloseButton(Graphics g)
        {
            Rectangle rect = CloseButtonRect;

            Color bg = _isCloseHovered
                ? Color.FromArgb(60, 255, 255, 255)
                : Color.FromArgb(20, 255, 255, 255);

            using (SolidBrush brush = new SolidBrush(bg))
            {
                g.FillEllipse(brush, rect);
            }

            if (_isCloseHovered)
            {
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                {
                    g.DrawEllipse(pen, rect);
                }
            }

            // ✕ Cross icon with clean round-capped lines
            Color fg = _isCloseHovered ? Color.White : Color.FromArgb(170, 185, 215);
            using (Pen xPen = new Pen(fg, 1.8f))
            {
                xPen.StartCap = LineCap.Round;
                xPen.EndCap = LineCap.Round;
                float pad = 7f;
                g.DrawLine(xPen, rect.Left + pad, rect.Top + pad, rect.Right - pad, rect.Bottom - pad);
                g.DrawLine(xPen, rect.Right - pad, rect.Top + pad, rect.Left + pad, rect.Bottom - pad);
            }
        }

        private void DrawActionButton(Graphics g)
        {
            Rectangle rect = ActionButtonRect;
            int radius = rect.Height / 2;

            Color c1, c2;
            if (_isActionPressed)
            {
                c1 = Color.FromArgb(55, 48, 163);
                c2 = Color.FromArgb(67, 56, 202);
            }
            else if (_isActionHovered)
            {
                c1 = Color.FromArgb(99, 102, 241);
                c2 = Color.FromArgb(129, 140, 248);
            }
            else
            {
                c1 = Color.FromArgb(79, 70, 229);
                c2 = Color.FromArgb(99, 102, 241);
            }

            using (GraphicsPath path = GetPillPath(rect, radius))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Horizontal))
                {
                    g.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw text "รับทราบค้าบ" + Vector Heart
            string text = "รับทราบค้าบ";
            SizeF textSize = g.MeasureString(text, _btnFont);
            float totalW = textSize.Width + 16f; // text + gap + heart
            float startX = rect.X + (rect.Width - totalW) / 2f;
            float startY = rect.Y + (rect.Height - textSize.Height) / 2f;

            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(text, _btnFont, textBrush, startX, startY);
            }

            // Red/Pink vector heart (no broken emoji boxes!)
            float heartCenterX = startX + textSize.Width + 8f;
            float heartCenterY = rect.Y + rect.Height / 2f;
            DrawHeart(g, heartCenterX, heartCenterY, 13f, Color.FromArgb(254, 205, 211));
        }

        private static void DrawHeart(Graphics g, float cx, float cy, float size, Color color)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                float w = size;
                float h = size;
                float x = cx - w / 2f;
                float y = cy - h / 2f;

                path.AddBezier(x + w * 0.5f, y + h * 0.25f, x + w * 0.45f, y + h * 0.05f, x + w * 0.15f, y + h * 0.05f, x + w * 0.05f, y + h * 0.25f);
                path.AddBezier(x + w * 0.05f, y + h * 0.25f, x, y + h * 0.45f, x + w * 0.2f, y + h * 0.65f, x + w * 0.5f, y + h * 0.95f);
                path.AddBezier(x + w * 0.5f, y + h * 0.95f, x + w * 0.8f, y + h * 0.65f, x + w, y + h * 0.45f, x + w * 0.95f, y + h * 0.25f);
                path.AddBezier(x + w * 0.95f, y + h * 0.25f, x + w * 0.85f, y + h * 0.05f, x + w * 0.55f, y + h * 0.05f, x + w * 0.5f, y + h * 0.25f);
                path.CloseFigure();

                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        private static void DrawSparkle(Graphics g, float cx, float cy, float r, Color color)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddLine(cx, cy - r, cx + r * 0.25f, cy - r * 0.25f);
                path.AddLine(cx + r * 0.25f, cy - r * 0.25f, cx + r, cy);
                path.AddLine(cx + r, cy, cx + r * 0.25f, cy + r * 0.25f);
                path.AddLine(cx + r * 0.25f, cy + r * 0.25f, cx, cy + r);
                path.AddLine(cx, cy + r, cx - r * 0.25f, cy + r * 0.25f);
                path.AddLine(cx - r * 0.25f, cy + r * 0.25f, cx - r, cy);
                path.AddLine(cx - r, cy, cx - r * 0.25f, cy - r * 0.25f);
                path.CloseFigure();

                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        private static Font GetBestFont(float size, FontStyle style)
        {
            string[] fontPreferences = { "Leelawadee UI", "Segoe UI Variable Display", "Segoe UI", "Tahoma" };
            foreach (var fontName in fontPreferences)
            {
                try
                {
                    using (var test = new Font(fontName, size, style))
                    {
                        if (test.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase))
                        {
                            return new Font(fontName, size, style);
                        }
                    }
                }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, style);
        }

        private static GraphicsPath GetRoundedRectanglePath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath GetPillPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 90, 180);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 180);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _titleFont?.Dispose();
                _subtitleFont?.Dispose();
                _bodyFont?.Dispose();
                _footerFont?.Dispose();
                _btnFont?.Dispose();
                _emojiFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

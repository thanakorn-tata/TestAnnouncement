namespace Announcement.Repositories
{
    /// <summary>
    /// Static collection of notification messages.
    /// Farewell message for the first popup, and a playful peekaboo for subsequent toasts.
    /// </summary>
    public static class MessageRepository
    {
        // Farewell message (shown once as fullscreen popup)
        public static readonly string FarewellMessage =
            "ผมไปแล้วนะครับ ขอบคุณทุกๆคนมาก\n" +
            "ขอบคุณที่สอนอะไรหลายๆอย่าง\n\n" +
            "และก็ถ้าผมทำอะไรผิดไปผมขอโทษด้วยนะครับ\n" +
            "ผมอาจจะยังไม่เก่งเรื่องการเข้าหา\n" +
            "หรือการวางตัวไปบ้างก็ขอโทษนะครับ";

        // Peekaboo toast (shown at 17:50 for 3 days)
        public static readonly string ToastTitle = "จ๊ะเอ๋! 👀";
        public static readonly string ToastBody = "ตาต้ามาแล้วค้าบ เผื่อทุกคนคิดถึง";
    }
}

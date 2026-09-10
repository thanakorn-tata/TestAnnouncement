namespace Announcement.Repositories
{
    /// <summary>
    /// Static collection of notification alert messages in Thai.
    /// Includes formal alerts for initial setup and casual prompts for daily rotates.
    /// </summary>
    public static class MessageRepository
    {
        // First Week (Formal/Polite Notification Text)
        public static readonly string FormalMainPopUp = "One Switch , Big Impact";
        public static readonly string FormalNoonMessage = "ประหยัดพลังงานง่าย ๆ พักเที่ยงนี้ อย่าลืมปิดไฟ ปิดแอร์";
        public static readonly string FormalEveningMessage = "ร่วมกันเซฟพลังงาน ก่อนกลับบ้าน อย่าลืมปิดคอม ปิดไฟ ปิดแอร์ ถอดปลั๊ก";

        // Weeks 2 to 4 (Casual Notification Text) - Scheduled for 12:00 PM (Lunch Break)
        public static readonly string[] CasualNoonMessages = new[]
        {
            "ผีเจ้าที่ฝากบอก \"ออกไปพักเที่ยงทั้งที ช่วยปิดไฟ ปิดแอร์ให้พี่...ไม่งั้นงอน\"",
            "ปิดไฟ ปิดแอร์ก่อนไปพัก คนน่าฮักเขาทำกัน",
            "เฮ้ โบร๊! พักเที่ยงอย่าลืมทานข้าว ปิดไฟ..ก่อนก้าวออกจากออฟฟิศ !!",
            "ปิดไฟให้ประหยัด...แล้วไปสะบัดตะเกียบที่ร้านข้าว",
            "มงคล...ปิดไฟ ให้เปี๊ยกหน่อย!!!"
        };

        // Weeks 2 to 4 (Casual Notification Text) - Scheduled for 06:00 PM (End of Work Day)
        public static readonly string[] CasualEveningMessages = new[]
        {
            "กลับบ้านสบายใจ...คอม ไฟ ปลั๊ก แอร์ ปิดครบ จบทุกดีล",
            "พักคอม พักไฟ แล้วไปพักใจที่บ้าน",
            "เลิกงานแล้ว คอมก็พัก คนก็พัก อย่าดื้อ!",
            "ก่อนกลับบ้าน อย่าลืมปิดคอม ปิดไฟ ปิดแอร์ ถอดปลั๊กนะ !!",
            "ภารกิจลับก่อนกลับ: ถอดปลั๊ก ปิดไฟ ปิดแอร์...อย่าให้โลกจับได้ว่าเราเปลือง!"
        };
    }
}

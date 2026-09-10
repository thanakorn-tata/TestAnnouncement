import os
import sys
import datetime
import tkinter as tk
import subprocess

LOG_DIR = "/Users/Shared/Notification"
LOG_FILE = os.path.join(LOG_DIR, "Announcement_debug.log")

def log(message):
    try:
        os.makedirs(LOG_DIR, exist_ok=True)
        now_str = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        with open(LOG_FILE, "a", encoding="utf-8") as f:
            f.write(f"{now_str} | {message}\n")
    except:
        pass

# Message Repository
FORMAL_NOON = "ประหยัดพลังงานง่าย ๆ พักเที่ยงนี้ อย่าลืมปิดไฟ ปิดแอร์"
FORMAL_EVENING = "ร่วมกันเซฟพลังงาน ก่อนกลับบ้าน อย่าลืมปิดคอม ปิดไฟ ปิดแอร์ ถอดปลั๊ก"

CASUAL_NOON = [
    "ผีเจ้าที่ฝากบอก \"ออกไปพักเที่ยงทั้งที ช่วยปิดไฟ ปิดแอร์ให้พี่...ไม่งั้นงอน\"",
    "ปิดไฟ ปิดแอร์ก่อนไปพัก คนน่าฮักเขาทำกัน",
    "เฮ้ โบร๊! พักเที่ยงอย่าลืมทานข้าว ปิดไฟ..ก่อนก้าวออกจากออฟฟิศ !!",
    "ปิดไฟให้ประหยัด...แล้วไปสะบัดตะเกียบที่ร้านข้าว",
    "มงคล...ปิดไฟ ให้เปี๊ยกหน่อย!!!"
]

CASUAL_EVENING = [
    "กลับบ้านสบายใจ...คอม ไฟ ปลั๊ก แอร์ ปิดครบ จบทุกดีล",
    "พักคอม พักไฟ แล้วไปพักใจที่บ้าน",
    "เลิกงานแล้ว คอมก็พัก คนก็พัก อย่าดื้อ!",
    "ก่อนกลับบ้าน อย่าลืมปิดคอม ปิดไฟ ปิดแอร์ ถอดปลั๊กนะ !!",
    "ภารกิจลับก่อนกลับ: ถอดปลั๊ก ปิดไฟ ปิดแอร์...อย่าให้โลกจับได้ว่าเราเปลือง!"
]

def show_mac_notification(title, body):
    # Escape quotes for AppleScript
    escaped_body = body.replace('"', '\\"')
    escaped_title = title.replace('"', '\\"')
    script = f'display notification "{escaped_body}" with title "{escaped_title}"'
    subprocess.run(["osascript", "-e", script])
    log("Toast notification shown successfully")

class MainPopupApp:
    def __init__(self, root, img_path):
        self.root = root
        self.root.title("Announcement")
        
        self.root.overrideredirect(True)
        self.root.attributes("-fullscreen", True)
        self.root.attributes("-topmost", True)
        
        screen_width = self.root.winfo_screenwidth()
        screen_height = self.root.winfo_screenheight()
        
        self.root.configure(bg="black")
        
        if os.path.exists(img_path):
            try:
                self.img = tk.PhotoImage(file=img_path)
                # Display image centered
                lbl = tk.Label(self.root, image=self.img, bg="black")
                lbl.pack(fill="both", expand=True)
            except Exception as e:
                self.show_placeholder(f"Error loading image: {str(e)}")
        else:
            self.show_placeholder("Waiting for Artwork (1920x1080)")
            
        close_btn = tk.Button(self.root, text="✕", font=("Segoe UI", 20, "bold"), 
                              fg="white", bg="#333333", activebackground="#666666",
                              bd=0, cursor="hand2", command=self.close_window)
        close_btn.place(x=screen_width - 70, y=20, width=50, height=50)
        
        self.root.bind("<Escape>", lambda e: self.close_window())
        
        self.root.lift()
        self.root.attributes("-topmost", True)
        self.root.focus_force()

    def show_placeholder(self, message):
        lbl = tk.Label(self.root, text=message, font=("Segoe UI", 24, "bold"), fg="#787878", bg="black")
        lbl.pack(fill="both", expand=True)

    def close_window(self):
        log("MainPopupForm closed")
        self.root.destroy()

def main():
    args = sys.argv[1:]
    
    # Get current username
    try:
        username = os.getlogin()
    except:
        username = os.environ.get("USER", "unknown")
        
    log(f"Application started. Args: {args} | Python: {sys.version.split()[0]} | User: {username}")
    
    now = datetime.datetime.now()
    
    # Parse StartDate.txt (stored in /Users/Shared/Notification)
    date_file = os.path.join(LOG_DIR, "StartDate.txt")
    install_date = now.date()
    
    if os.path.exists(date_file):
        try:
            with open(date_file, "r", encoding="utf-8") as f:
                date_str = f.read().strip()
                
            parsed = None
            for fmt in ["%Y-%m-%d", "%Y/%m/%d", "%d/%m/%Y", "%Y%m%d"]:
                try:
                    parsed = datetime.datetime.strptime(date_str, fmt).date()
                    # Handle B.E. (พ.ศ.) year
                    if parsed.year > 2500:
                        parsed = parsed.replace(year=parsed.year - 543)
                    break
                except:
                    pass
            if parsed:
                install_date = parsed
                log(f"StartDate parsed (AD): {install_date.strftime('%Y-%m-%d')}")
            else:
                log(f"StartDate format mismatch in file, fallback to today: {install_date.strftime('%Y-%m-%d')}")
        except Exception as e:
            log(f"Error reading StartDate.txt: {str(e)}")
    else:
        log(f"StartDate.txt not found, using today: {install_date.strftime('%Y-%m-%d')}")
        
    # Date validation
    if now.date() < install_date:
        log(f"EXIT: now.date ({now.date().strftime('%Y-%m-%d')}) < install_date ({install_date.strftime('%Y-%m-%d')})")
        return
        
    # Check weekend (5 = Saturday, 6 = Sunday)
    if now.weekday() in [5, 6]:
        log(f"EXIT: Weekend ({now.strftime('%A')})")
        return
        
    is_startup_popup = "--startup" in args
    is_formal = (now.date() - install_date).days < 7
    log(f"is_startup_popup: {is_startup_popup}, is_formal: {is_formal}, daysSinceInstall: {(now.date() - install_date).days}")
    
    if is_startup_popup:
        if now.hour < 6:
            log(f"EXIT: Hour ({now.hour}) < 6")
            return
            
        if is_formal:
            popup_track_file = os.path.join(LOG_DIR, "LastPopupDate.txt")
            today_str = now.strftime("%Y-%m-%d")
            
            already_shown = False
            if os.path.exists(popup_track_file):
                try:
                    with open(popup_track_file, "r", encoding="utf-8") as f:
                        last_date = f.read().strip()
                    if last_date == today_str:
                        already_shown = True
                except Exception as e:
                    log(f"Error reading LastPopupDate: {str(e)}")
                    
            if not already_shown:
                try:
                    with open(popup_track_file, "w", encoding="utf-8") as f:
                        f.write(today_str)
                except Exception as e:
                    log(f"Error writing track file: {str(e)}")
                    
                log("Launching MainPopupForm...")
                try:
                    root = tk.Tk()
                    img_path = os.path.join(LOG_DIR, "MainPopup.png")
                    app = MainPopupApp(root, img_path)
                    root.mainloop()
                except Exception as e:
                    log(f"UNHANDLED EXCEPTION in GUI: {str(e)}")
            else:
                log("EXIT: Popup already shown today")
        else:
            log("EXIT: Not in formal period, skipping popup")
        return

    # Toast branch
    day_index = min(now.weekday(), len(CASUAL_NOON) - 1)
    is_noon = now.hour < 15
    log(f"Toast branch: is_noon={is_noon}, day_index={day_index}")
    
    if is_formal:
        body = FORMAL_NOON if is_noon else FORMAL_EVENING
    else:
        body = CASUAL_NOON[day_index] if is_noon else CASUAL_EVENING[day_index]
        
    show_mac_notification("One Switch , Big Impact", body)

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        log(f"UNHANDLED EXCEPTION: {str(e)}")

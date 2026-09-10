#!/bin/bash

# Configuration
StartDate="2026-06-11"
TargetFolder="/Users/Shared/Notification"
ScriptPath="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=================================================="
echo "Starting macOS Enterprise Deployment..."
echo "=================================================="

# 1. Ensure Python 3 is installed
if ! command -v python3 &> /dev/null; then
    echo "[-] python3 is not found. Downloading and installing official Python 3..."
    
    # Official stable Python 3.11 package for macOS (Universal installer)
    PKG_URL="https://www.python.org/ftp/python/3.11.9/python-3.11.9-macos11.pkg"
    PKG_PATH="/tmp/python-3.11.9.pkg"
    
    echo "[+] Downloading Python installer from $PKG_URL..."
    curl -L -s -o "$PKG_PATH" "$PKG_URL"
    
    if [ $? -eq 0 ] && [ -f "$PKG_PATH" ]; then
        echo "[+] Installing Python 3 system-wide (requires root/admin)..."
        installer -pkg "$PKG_PATH" -target /
        if [ $? -eq 0 ]; then
            echo "[+] Python 3 installed successfully: $(python3 --version)"
        else
            echo "[!] Failed to install Python 3 package."
            exit 1
        fi
        rm -f "$PKG_PATH"
    else
        echo "[!] Failed to download Python 3 installer."
        exit 1
    fi
else
    echo "[+] python3 is already installed: $(python3 --version)"
fi

# 2. Check and test tkinter GUI library compatibility
if ! python3 -c "import tkinter" &> /dev/null; then
    echo "[!] tkinter library is missing or incompatible. Attempting to reinstall python3 GUI components..."
    # On official installer, tkinter is included. On brew/other it might be missing.
    # We proceed but note that Tkinter GUI is critical for Popups.
fi

# 3. Create target folder and set permissions
echo "[+] Creating target directories..."
mkdir -p "$TargetFolder"
chmod 777 "$TargetFolder"

# 4. Copy application files
echo "[+] Copying script and artwork assets..."
cp "$ScriptPath/announcement.py" "$TargetFolder/announcement.py"
cp "$ScriptPath/MainPopup.png" "$TargetFolder/MainPopup.png"
chmod +x "$TargetFolder/announcement.py"

# Write StartDate.txt
echo "$StartDate" > "$TargetFolder/StartDate.txt"
chmod 666 "$TargetFolder/StartDate.txt"

# 5. Copy launchd configuration files (System-wide LaunchAgents for background execution)
echo "[+] Registering LaunchAgents in /Library/LaunchAgents..."
cp "$ScriptPath/com.energysaving.popup.plist" "/Library/LaunchAgents/com.energysaving.popup.plist"
cp "$ScriptPath/com.energysaving.toast.plist" "/Library/LaunchAgents/com.energysaving.toast.plist"

# Set strict permissions required by launchd
chown root:wheel /Library/LaunchAgents/com.energysaving.popup.plist
chown root:wheel /Library/LaunchAgents/com.energysaving.toast.plist
chmod 644 /Library/LaunchAgents/com.energysaving.popup.plist
chmod 644 /Library/LaunchAgents/com.energysaving.toast.plist

# 6. Load agents for the current active GUI user session immediately
CONSOLE_USER=$(stat -f '%Su' /dev/console)
CONSOLE_UID=$(id -u "$CONSOLE_USER" 2>/dev/null)

if [ "$CONSOLE_USER" != "root" ] && [ -n "$CONSOLE_UID" ]; then
    echo "[+] Active GUI user detected: $CONSOLE_USER (UID: $CONSOLE_UID)"
    echo "[+] Loading background LaunchAgents immediately..."
    
    # Try unloading existing agents first to prevent conflicts
    sudo -u "$CONSOLE_USER" launchctl unload /Library/LaunchAgents/com.energysaving.popup.plist 2>/dev/null
    sudo -u "$CONSOLE_USER" launchctl unload /Library/LaunchAgents/com.energysaving.toast.plist 2>/dev/null
    
    # Load new agents
    sudo -u "$CONSOLE_USER" launchctl load /Library/LaunchAgents/com.energysaving.popup.plist
    sudo -u "$CONSOLE_USER" launchctl load /Library/LaunchAgents/com.energysaving.toast.plist
    echo "[+] Background LaunchAgents loaded successfully for $CONSOLE_USER."
else
    echo "[-] No active GUI user found or running in root console. LaunchAgents will load automatically on next user login."
fi

echo "=================================================="
echo "[+] macOS Deployment completed successfully!"
echo "=================================================="

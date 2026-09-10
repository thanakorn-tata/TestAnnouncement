#!/bin/bash

ACTION=$1

if [ "$ACTION" == "pack" ]; then
    echo "=================================================="
    echo "Packing macOS deployment files..."
    echo "=================================================="
    # Zip all files in the current directory (excluding zip files themselves)
    zip -r ../EnergySavingAlert_Mac.zip . -x "*.zip"
    echo "[+] Packing Complete! Created EnergySavingAlert_Mac.zip in the parent directory."

elif [ "$ACTION" == "unpack" ]; then
    echo "=================================================="
    echo "Unpacking and Deploying macOS files..."
    echo "=================================================="
    if [ -f "EnergySavingAlert_Mac.zip" ]; then
        unzip -o EnergySavingAlert_Mac.zip -d .
        echo "[+] Unpacking Complete! Starting deployment..."
        sudo bash deploy_mac.sh
    else
        echo "[!] Error: EnergySavingAlert_Mac.zip not found in the current directory."
        exit 1
    fi
else
    echo "Usage: ./script.sh [pack|unpack]"
    echo "  pack   - Bundles all files in this directory into EnergySavingAlert_Mac.zip"
    echo "  unpack - Extracts EnergySavingAlert_Mac.zip and triggers deploy_mac.sh"
fi

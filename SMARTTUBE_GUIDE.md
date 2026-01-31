# 📺 SmartTube & Android Installation Guide

This guide explains how to install [SmartTube](https://github.com/yuliskov/SmartTube) on your Android device (TV, Phone, or Tablet) and integrate it with your DNS Agent for ad-blocking and watch history synchronization.

## 🚀 Why SmartTube?
Official YouTube apps on Android TVs and mobile devices often have "hardcoded" ad delivery that standard DNS blocking cannot touch. **SmartTube** is an open-source, third-party client that:
- Blocks all YouTube ads.
- Features built-in [SponsorBlock](https://sponsorblock.org/) support.
- Supports high-resolution playback (4K/8K).
- **DNS Agent Integration**: Allows your DNS Agent to capture watch data and sync it with your local dashboard.

---

## 🛠️ Step 1: Installation (Bypassing Google Blockers)

Google Play Protect often flags third-party YouTube clients as "Harmful" because they circumvent ad revenue. To install, you must explicitly allow "Unknown Sources".

1. **Download the APK**: Download the latest stable APK directly from the [SmartTube GitHub](https://github.com/yuliskov/SmartTube/releases).
2. **Transfer to Device**: Use a USB drive or a "Send Files to TV" app to get the APK onto your device.
3. **Handle Play Protect**:
   - When you open the APK, you may see a "Blocked by Play Protect" warning.
   - Click **Install Anyway** (or click "More Details" -> "Install Anyway").
4. **Allow Unknown Sources**:
   - If prompted, go to **Settings > Security > Unknown Sources** (or "Install unknown apps").
   - Find your file manager or browser in the list and toggle it to **Allowed**.

> [!IMPORTANT]
> **Security Context**: SmartTube is a widely trusted community project with over 40k stars on GitHub. However, you should always [decide for yourself](https://github.com/yuliskov/SmartTube/issues) if you are comfortable with third-party software.

---

## 🔗 Step 2: DNS Agent Integration (The "Proxy Trick")

Capture your TV's watch history and analytics directly in your DNS Agent dashboard without an extension.

1. **Open SmartTube Settings**: Go to **Settings > SponsorBlock**.
2. **Enable SponsorBlock**: Ensure the toggle is ON.
3. **Configure API URL**:
   - Find the **SponsorBlock Settings** or **API URL** field.
   - Change the default URL to your DNS Agent's API endpoint:
     `http://<YOUR_SERVER_IP>:5123/api/sponsorblock/`
4. **Map the Device**:
   - Go to your DNS Agent Dashboard -> **YouTube Insights**.
   - Navigate to **Profile Management**.
   - You should see your TV's IP address listed. Assign your YouTube handle to it to attribute watch data to your profile.

---

## 🏛️ More Context
- **GitHub Project**: [SmartTube on GitHub](https://github.com/yuliskov/SmartTube)
- **Community Discussion**: [Reddit r/SmartTube](https://www.reddit.com/r/SmartTube/)
- **News/Reviews**: [HowToGeek: Why SmartTube is the Best TV App](https://www.howtogeek.com/793134/what-is-smarttube-and-why-is-it-the-best-youtube-app-for-tv/)

---
*Note: This guide is for educational purposes. Always respect copyright and terms of service.*

# 📺 NVIDIA Shield Integration Strategy

This document outlines the architecture for the sideloadable Android APK that will bridge your NVIDIA Shield experience with the DNS Agent ecosystem.

## 🎯 The Technical Objective
To enable **SponsorBlock** and **DeArrow** functionality on the **Official YouTube app** on Android TV, which is normally impossible to modify.

## 🏗️ Architecture: The "Side-Car" Pattern
Instead of replacing the YouTube app, we will build a "Side-Car" application that runs in the background.

### 1. The Accessibility Bridge 🛠️
By enabling the **Accessibility Service** in the APK, the DNS Agent Companion can:
- "See" the on-screen text and metadata of the YouTube app.
- Detect the video title and automatically match it to a Video ID.
- Detect when an ad-overlay or a sponsor segment (via timestamp matching) is currently on screen.
- **Auto-Click**: Programmatically "press" the Skip Ad button or fast-forward through sponsors.

### 2. The Metadata Proxy 🎭
The APK will communicate with your **Windows DNS Agent Server**:
- **Fetch**: It will ask the server for "Clean Titles" from DeArrow using the same privacy-preserving hashing we use in the browser.
- **Push**: It will send "Time Saved" and "Sponsors Skipped" data back to your local SQL database.

### 3. The Leanback Dashboard 📟
A dedicated UI designed for 10-foot interaction (remote control) that shows:
- **Network Queries**: See which domains your Shield is looking up in real-time.
- **Privacy Toggles**: Enable/Disable DNS blocking for specific apps (e.g., allow Netflix but block YouTube ads).

---

## 🚀 Implementation Phases

### Phase 7.1: The Listener (MVP)
- A background service that detects the current YouTube video title via Accessibility events.
- Reports the "Watch Session" to the Windows dashboard.

### Phase 7.2: The Skipper
- Integration with the SponsorBlock API proxy on the Windows server.
- Automatic skipping logic using Android's `AccessibilityNodeInfo` system.

### Phase 7.3: The Remote
- Turn the Shield into a "Management Node."
- Control the Windows DNS Service from the couch.

---

## 🔒 Privacy & Performance
- **Zero Cloud**: The APK will talk *only* to your local IP address.
- **Efficiency**: Designed to use < 50MB of RAM, ensuring it doesn't slow down 4K playback.

> [!IMPORTANT]
> This requires **Sideloading** and enabling **Accessibility Permissions** on the Shield. It is a powerful tool for power users who want the "Extension Experience" on their TV.

# DNS Agent - YouTube Ad Blocker Extension

## 🎯 Features

- **4-Layer YouTube Ad Blocking**
  - CSS injection (instant hiding)
  - Auto-skip buttons
  - Dynamic ad detection
  - Video player manipulation

- **Network-Wide Protection**
  - Syncs with local DNS Agent
  - Block domains across all devices
  - Real-time filter updates

- **Privacy-First**
  - Local network only (no cloud)
  - No data collection
  - Open source

## 📦 Installation

### From Source (Developer Mode)

1. Open Chrome/Edge and go to `chrome://extensions/`
2. Enable **Developer mode** (toggle in top right)
3. Click **"Load unpacked"**
4. Select the `extension` folder
5. The extension icon should appear in your toolbar

### Requirements

- DNS Agent v1.4+ running on your local network
- Chrome, Edge, or Brave browser

## 🚀 Usage

1. **Install DNS Agent** on your local network
2. **Install the extension** in your browser
3. **Visit YouTube** - ads will be blocked automatically!
4. **Click the extension icon** to see statistics

### Context Menu

Right-click any link → **"Block this domain with DNS Agent"** to add it to your network-wide blocklist.

## 🔧 Configuration

The extension auto-discovers DNS Agent on your local network. It checks:
- `http://localhost:5123`
- `http://127.0.0.1:5123`
- `http://192.168.x.x:5123`
- `http://10.x.x.x:5123`

If DNS Agent is running on a different port, the extension will still find it.

## 📊 How It Works

### 1. CSS Injection
Hides ad containers instantly using CSS selectors from DNS Agent API.

### 2. Auto-Skip
Automatically clicks "Skip Ad" buttons when they appear.

### 3. Mutation Observer
Detects dynamically loaded ads and hides them in real-time.

### 4. Video Player Manipulation
Forces video player to skip unskippable ads.

### 5. API Sync
- Fetches latest filters from DNS Agent every 6 hours
- Reports blocking statistics
- Syncs blocked domains network-wide

## 🛡️ Effectiveness

**Target**: 95%+ ad blocking success rate

**Tested on**:
- Pre-roll ads ✅
- Mid-roll ads ✅
- Overlay ads ✅
- Sidebar ads ✅
- Sponsored content ✅

## 🔒 Security

- **Local network only** - API restricted to private IP ranges
- **No external requests** - All data stays on your network
- **Open source** - Audit the code yourself

## 🐛 Troubleshooting

### Extension shows "Disconnected"
- Ensure DNS Agent is running
- Check that it's accessible at `http://localhost:5123`
- Try opening the dashboard manually

### Ads still showing
- Click "Refresh Filters" in the extension popup
- Check DNS Agent is running with protection enabled
- Clear browser cache and reload YouTube

### Can't block domains
- Requires authentication in DNS Agent
- Log in to DNS Agent dashboard first
- Then try blocking again

## 📝 Development

### File Structure
```
extension/
├── manifest.json          # Extension configuration
├── background.js          # Service worker (API communication)
├── youtube-blocker.js     # Content script (ad blocking)
├── popup.html            # Extension popup UI
├── popup.js              # Popup logic
├── styles.css            # Popup styling
└── icons/                # Extension icons
    ├── icon16.png
    ├── icon48.png
    └── icon128.png
```

### API Endpoints Used
- `GET /api/status` - Check DNS Agent connection
- `GET /api/youtube-filters` - Fetch ad blocking filters
- `POST /api/youtube-stats` - Report statistics
- `POST /api/block` - Block domain network-wide

## 🚀 Future Features

- [ ] SponsorBlock integration
- [ ] Custom filter rules
- [ ] Whitelist management
- [ ] Advanced statistics
- [ ] Firefox support

## 📄 License

Open source - same license as DNS Agent

## 🤝 Contributing

Found a YouTube ad that isn't blocked? Submit the selector to the community filter repository!

---

**Made with ❤️ by the DNS Agent team**

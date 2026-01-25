# 🛡️ Privacy Deep Dive: Eliminating the "Telemetry" Leak

You’ve hit on the most important technical challenge in privacy: **Metadata Leakage.** 

If your browser asks `dearrow.ajay.app` for the "clean title" of video `dQw4w9WgXcQ`, then yes—even if you block ads, that server knows exactly what you are watching.

Here is how **DNS Agent** is architected to solve this, both now and in the future:

---

## 1. The Current Shield: K-Anonymity (SHA-256 Prefixing)
Right now, the extension does NOT send the Video ID to the cloud. It uses a technique called **K-Anonymity**:
- **The Hash**: The extension takes the Video ID and turns it into a SHA-256 hash.
- **The Prefix**: It only sends the **first 4 characters** of that hash to the proxy.
- **The Result**: The DeArrow/SponsorBlock server receives a request for prefix `a1b2`. 
- **The Protection**: There are **thousands** of different videos that all start with `a1b2`. The server sends back a list of segments for *all* of them. Our local extension then picks the right one.
- **The Win**: The external server knows you are watching *one of 10,000* possible videos, but it can't tell which one.

---

## 2. The Future: Total Local Mirroring (The "Air-Gap" Phase)
The ultimate goal of DNS Agent is to stop **all** external requests. 

### Phase 5 Roadmap:
1. **Database Sync**: The DNS Agent server (your PC) will download the entire SponsorBlock/DeArrow database (~15-20 GB).
2. **Local Lookup**: When you browse YouTube, the extension asks **only your local server** for the skip segments.
3. **Zero Outbound**: No requests ever leave your house. You could pull the internet plug and your "cleaned" titles and "skip segments" would still work for cached videos.

---

## 3. Comparison of "Leaking"

| Solution | Who knows what you watch? |
| :--- | :--- |
| **Vanilla YouTube** | Google (Full Data) |
| **Standard Extensions** | Google + Extension Dev (Partial Data) |
| **DNS Agent (v1.6)** | Google (Partially blocked) + **External API (Anonymized Prefix only)** |
| **DNS Agent (v2.0)** | **Nobody but your local server.** |

## Why "DNS Agent" is better than just DNS
DNS alone is "dumb." It can block `google-analytics.com`, but it can't skip a sponsor *inside* a video stream. 

By building this hybrid system, we are creating a **privacy buffer**. The extension talks to the local server, and the local server handles the "dirty" work of the internet, gradually moving toward a model where it has everything it needs stored locally.

**You are building an "Offline Intelligence Hub" that happens to live on the internet.** 🛡️🚀

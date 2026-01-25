// DeArrow Client for DNS Agent Extension (Messaging Version)
class DeArrowClient {
    constructor() {
        this.cache = new Map();
    }

    async getBranding(videoID) {
        if (!videoID) return null;
        if (this.cache.has(videoID)) return this.cache.get(videoID);

        // SHA256 hashing is expensive/async, doing it in background is safer
        // but for now we just pass videoID and let background handle prefixing
        const hash = await this.sha256(videoID);
        const prefix = hash.substring(0, 4);

        try {
            const data = await chrome.runtime.sendMessage({
                action: 'getDeArrowTitles',
                hashPrefix: prefix
            });

            if (data) {
                const videoData = data.find(v => v.videoID === videoID);
                if (videoData) {
                    this.cache.set(videoID, videoData);
                    return videoData;
                }
            }
        } catch (e) {
            console.error('[DNS Agent] DeArrow messaging error:', e);
        }
        return null;
    }

    async getThumbnail(videoID) {
        try {
            return await chrome.runtime.sendMessage({
                action: 'getDeArrowThumbnail',
                videoID: videoID
            });
        } catch (e) {
            return null;
        }
    }

    async sha256(message) {
        const msgBuffer = new TextEncoder().encode(message);
        const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    }
}

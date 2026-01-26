// SponsorBlock API Client
// Queries the public SponsorBlock API to get sponsor segment timestamps

class SponsorBlockClient {
    constructor() {
        this.apiUrl = 'https://sponsor.ajay.app/api';
        this.cache = new Map(); // Cache segments per video
    }

    /**
     * Get sponsor segments for a video using privacy-preserving hash
     * @param {string} videoId - YouTube video ID
     * @returns {Promise<Array>} Array of segment objects
     */
    async getSegments(videoId) {
        // Check cache first
        if (this.cache.has(videoId)) {
            console.log('[SponsorBlock] Using cached segments for', videoId);
            return this.cache.get(videoId);
        }

        try {
            // Generate SHA256 hash prefix (first 4 chars for privacy)
            const hashPrefix = await this.sha256Prefix(videoId, 4);

            // Query API with categories we want to skip
            const categories = ['sponsor', 'selfpromo', 'interaction', 'intro', 'outro'];
            const categoriesParam = JSON.stringify(categories);

            const url = `${this.apiUrl}/skipSegments/${hashPrefix}?categories=${encodeURIComponent(categoriesParam)}`;

            console.log('[SponsorBlock] Fetching segments for video:', videoId);
            const response = await fetch(url);

            if (!response.ok) {
                console.log('[SponsorBlock] No segments found (404 is normal)');
                return [];
            }

            const data = await response.json();

            // Filter to exact video ID (hash prefix returns multiple videos)
            const segments = data.filter(s => s.videoID === videoId);

            console.log(`[SponsorBlock] Found ${segments.length} segments`);

            // Cache for 1 hour
            this.cache.set(videoId, segments);
            setTimeout(() => this.cache.delete(videoId), 3600000);

            return segments;
        } catch (error) {
            console.error('[SponsorBlock] Error fetching segments:', error);
            return [];
        }
    }

    /**
     * Generate SHA256 hash prefix for privacy
     * @param {string} str - String to hash
     * @param {number} length - Prefix length (4-32 chars)
     * @returns {Promise<string>} Hash prefix
     */
    async sha256Prefix(str, length) {
        const buffer = new TextEncoder().encode(str);
        const hash = await crypto.subtle.digest('SHA-256', buffer);
        const hex = Array.from(new Uint8Array(hash))
            .map(b => b.toString(16).padStart(2, '0'))
            .join('');
        return hex.substring(0, length);
    }

    /**
     * Get category color for visual indicators
     * @param {string} category - Segment category
     * @returns {string} Hex color code
     */
    getCategoryColor(category) {
        const colors = {
            'sponsor': '#00d400',      // Green
            'selfpromo': '#ffff00',    // Yellow
            'interaction': '#cc00ff',  // Purple
            'intro': '#00ffff',        // Cyan
            'outro': '#0202ed',        // Blue
            'preview': '#008fd6',      // Light blue
            'music_offtopic': '#ff9900' // Orange
        };
        return colors[category] || '#ffffff';
    }

    /**
     * Get category display name
     * @param {string} category - Segment category
     * @returns {string} Human-readable name
     */
    getCategoryName(category) {
        const names = {
            'sponsor': 'Sponsor',
            'selfpromo': 'Self Promotion',
            'interaction': 'Interaction Reminder',
            'intro': 'Intro',
            'outro': 'Outro',
            'preview': 'Preview',
            'music_offtopic': 'Non-Music'
        };
        return names[category] || category;
    }
}

// Export for use in youtube-blocker.js
window.SponsorBlockClient = SponsorBlockClient;

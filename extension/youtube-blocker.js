// YouTube Ad Blocker - Content Script
// Runs on all YouTube pages to block ads and skip sponsor segments

console.log('[DNS Agent] YouTube ad blocker loaded');

// Configuration
let filters = {
    cssSelectors: [],
    skipButtonSelectors: [],
    urlPatterns: []
};

let stats = {
    adsBlocked: 0,
    adsFailed: 0,
    sponsorsSkipped: 0,
    filterVersion: 'unknown'
};

// SponsorBlock integration
let sponsorBlockClient = null;
let currentVideoId = null;
let sponsorSegments = [];
let skippedSegments = new Set();

// Initialize SponsorBlock client
if (typeof SponsorBlockClient !== 'undefined') {
    sponsorBlockClient = new SponsorBlockClient();
    console.log('[SponsorBlock] Client initialized');
}

// Load filters from storage
chrome.storage.local.get(['youtubeFilters'], (result) => {
    if (result.youtubeFilters) {
        filters = result.youtubeFilters;
        console.log('[DNS Agent] Loaded filters version:', filters.version);
        applyAdBlocking();
    } else {
        // Request filters from background script
        chrome.runtime.sendMessage({ action: 'getFilters' });
    }
});

// Listen for filter updates
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === 'filtersUpdated') {
        filters = message.filters;
        console.log('[DNS Agent] Filters updated to version:', filters.version);
        applyAdBlocking();
    }
});

// Main ad blocking function
function applyAdBlocking() {
    // Layer 1: CSS Injection (hide ad containers)
    injectAdBlockCSS();

    // Layer 2: Auto-skip ads
    setInterval(autoSkipAds, 500);

    // Layer 3: Mutation observer (detect dynamic ads)
    observeDOMChanges();

    // Layer 4: Video player manipulation
    monitorVideoPlayer();

    // Layer 5: SponsorBlock integration
    if (sponsorBlockClient) {
        monitorVideoChanges();
        monitorSponsorSegments();
    }
}

// Layer 1: CSS Injection
function injectAdBlockCSS() {
    if (!filters.cssSelectors || filters.cssSelectors.length === 0) {
        // Fallback selectors
        filters.cssSelectors = [
            '.video-ads',
            '.ytp-ad-module',
            '.ytp-ad-overlay-container',
            '#player-ads',
            '.ytd-display-ad-renderer'
        ];
    }

    const style = document.createElement('style');
    style.id = 'dns-agent-ad-blocker';
    style.textContent = filters.cssSelectors.map(s => `${s} { display: none !important; }`).join('\n');

    // Remove existing style if present
    const existing = document.getElementById('dns-agent-ad-blocker');
    if (existing) existing.remove();

    document.head.appendChild(style);
    console.log('[DNS Agent] CSS ad blocking applied');
}

// Layer 2: Auto-skip ads
function autoSkipAds() {
    const skipSelectors = filters.skipButtonSelectors || [
        '.ytp-ad-skip-button',
        '.ytp-skip-ad-button',
        '[class*="skip"][class*="button"]',
        '.ytp-ad-skip-button-modern'
    ];

    // Try to click skip button
    for (const selector of skipSelectors) {
        const skipButton = document.querySelector(selector);
        if (skipButton && skipButton.offsetParent !== null) {
            skipButton.click();
            stats.adsBlocked++;
            console.log('[DNS Agent] Auto-skipped ad via button');
            updateStats();
            return true;
        }
    }

    // If no skip button, try to force skip by manipulating video
    const video = document.querySelector('video');
    const player = document.querySelector('.html5-video-player');

    if (video && player && player.classList.contains('ad-showing')) {
        // Method 1: Jump to end of ad
        if (video.duration && !isNaN(video.duration) && video.duration < 120) {
            video.currentTime = video.duration;
            console.log('[DNS Agent] Forced ad to end');
            stats.adsBlocked++;
            updateStats();
            return true;
        }

        // Method 2: Speed through ad at 16x
        if (video.playbackRate < 16) {
            video.playbackRate = 16;
            video.muted = true;
            console.log('[DNS Agent] Speeding through ad at 16x');
            return true;
        }
    }

    return false;
}

// Layer 3: Mutation Observer
function observeDOMChanges() {
    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node.nodeType === 1) { // Element node
                    // Check if it's an ad container
                    if (isAdElement(node)) {
                        node.style.display = 'none';
                        stats.adsBlocked++;
                        console.log('[DNS Agent] Blocked dynamic ad');
                        updateStats();
                    }
                }
            }
        }
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
}

function isAdElement(element) {
    const adClasses = ['ad-showing', 'video-ads', 'ytp-ad'];
    const className = element.className || '';
    return adClasses.some(adClass => className.includes(adClass));
}

// Layer 4: Video Player Manipulation
function monitorVideoPlayer() {
    const video = document.querySelector('video');
    if (!video) {
        setTimeout(monitorVideoPlayer, 1000);
        return;
    }

    video.addEventListener('timeupdate', () => {
        const player = document.querySelector('.html5-video-player');
        if (player && player.classList.contains('ad-showing')) {
            console.log('[DNS Agent] Ad detected in player, forcing skip');

            // Try to skip to end of ad
            if (video.duration && video.duration < 60) {
                video.currentTime = video.duration;
            }

            // Also try clicking skip button
            autoSkipAds();

            stats.adsBlocked++;
            updateStats();
        }
    });
}

// Update statistics
function updateStats() {
    chrome.storage.local.set({ youtubeStats: stats });

    // Report to background script every 10 ads
    if (stats.adsBlocked % 10 === 0) {
        chrome.runtime.sendMessage({
            action: 'reportStats',
            stats: stats
        });
    }
}

// Context menu: Block domain
document.addEventListener('contextmenu', (e) => {
    const link = e.target.closest('a');
    if (link && link.href) {
        try {
            const url = new URL(link.href);
            chrome.runtime.sendMessage({
                action: 'setContextDomain',
                domain: url.hostname
            });
        } catch (e) {
            // Invalid URL
        }
    }
});

// ===== SponsorBlock Integration =====

/**
 * Monitor for video changes (YouTube is a SPA)
 */
function monitorVideoChanges() {
    let lastUrl = location.href;

    setInterval(() => {
        const url = location.href;
        if (url !== lastUrl) {
            lastUrl = url;
            onVideoChange();
        }
    }, 1000);

    // Also check on initial load
    onVideoChange();
}

/**
 * Handle video change - extract video ID and fetch segments
 */
async function onVideoChange() {
    const videoId = getYouTubeVideoId();

    if (videoId && videoId !== currentVideoId) {
        currentVideoId = videoId;
        skippedSegments.clear();

        console.log('[SponsorBlock] New video detected:', videoId);

        // Fetch sponsor segments
        sponsorSegments = await sponsorBlockClient.getSegments(videoId);

        if (sponsorSegments.length > 0) {
            console.log(`[SponsorBlock] Loaded ${sponsorSegments.length} segments to skip`);
            addTimelineMarkers();
        }
    }
}

/**
 * Extract YouTube video ID from URL
 */
function getYouTubeVideoId() {
    const url = new URL(window.location.href);
    return url.searchParams.get('v');
}

/**
 * Monitor video playback and skip sponsor segments
 */
function monitorSponsorSegments() {
    const video = document.querySelector('video');
    if (!video) {
        setTimeout(monitorSponsorSegments, 1000);
        return;
    }

    video.addEventListener('timeupdate', () => {
        const currentTime = video.currentTime;

        // Check if we're in a sponsor segment
        for (const segment of sponsorSegments) {
            const [start, end] = segment.segment;
            const segmentId = segment.UUID;

            // If we're in a segment and haven't skipped it yet
            if (currentTime >= start && currentTime < end) {
                if (!skippedSegments.has(segmentId)) {
                    // Skip to end of segment
                    video.currentTime = end;
                    skippedSegments.add(segmentId);

                    // Update stats
                    stats.sponsorsSkipped++;
                    updateStats();

                    // Show notification
                    const duration = end - start;
                    const category = sponsorBlockClient.getCategoryName(segment.category);
                    showSkipNotification(category, duration);

                    console.log(`[SponsorBlock] Skipped ${segment.category}: ${start.toFixed(1)}s - ${end.toFixed(1)}s (${duration.toFixed(1)}s saved)`);
                }
            }
        }
    });
}

/**
 * Add visual timeline markers for sponsor segments
 */
function addTimelineMarkers() {
    // Remove existing markers
    document.querySelectorAll('.sponsorblock-marker').forEach(m => m.remove());

    const video = document.querySelector('video');
    const progressBar = document.querySelector('.ytp-progress-bar-container');

    if (!video || !progressBar || !video.duration) {
        setTimeout(addTimelineMarkers, 1000);
        return;
    }

    const duration = video.duration;

    for (const segment of sponsorSegments) {
        const [start, end] = segment.segment;

        const marker = document.createElement('div');
        marker.className = 'sponsorblock-marker';
        marker.style.position = 'absolute';
        marker.style.left = `${(start / duration) * 100}%`;
        marker.style.width = `${((end - start) / duration) * 100}%`;
        marker.style.height = '100%';
        marker.style.backgroundColor = sponsorBlockClient.getCategoryColor(segment.category);
        marker.style.opacity = '0.6';
        marker.style.pointerEvents = 'none';
        marker.style.zIndex = '30';
        marker.title = `${sponsorBlockClient.getCategoryName(segment.category)}: ${start.toFixed(1)}s - ${end.toFixed(1)}s`;

        progressBar.appendChild(marker);
    }
}

/**
 * Show skip notification toast
 */
function showSkipNotification(category, duration) {
    const toast = document.createElement('div');
    toast.className = 'sponsorblock-toast';
    toast.innerHTML = `
        <div style="
            position: fixed;
            bottom: 80px;
            right: 20px;
            background: rgba(0, 212, 0, 0.95);
            color: white;
            padding: 12px 20px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: bold;
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
            z-index: 9999;
            animation: slideIn 0.3s ease-out;
        ">
            ⏭️ Skipped ${category} (${duration.toFixed(1)}s)
        </div>
    `;

    document.body.appendChild(toast);

    setTimeout(() => toast.remove(), 3000);
}

// Initialize
applyAdBlocking();

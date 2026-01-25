// YouTube Ad Blocker - Content Script
// Runs on all YouTube pages to block ads

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
    filterVersion: 'unknown'
};

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

// Initialize
applyAdBlocking();

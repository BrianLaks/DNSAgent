// YouTube Search Augmentation & Algorithm Circumvention - Content Script

console.log('[DNS Agent] Algorithm Circumvention Layer loaded');

let localHistory = [];
let trustedChannels = new Map();
let config = {
    hideWatched: false,
    boostTrusted: true,
    showLocalBadges: true
};

// Initialize
function initSearchAugmentation() {
    if (!window.location.pathname.startsWith('/results')) return;

    chrome.storage.local.get(['youtubeTrackingEnabled'], (result) => {
        if (result.youtubeTrackingEnabled === false) return;

        fetchLocalHistory().then(() => {
            applyAugmentation();
            createControlPanel();
            observeSearchChanges();
        });
    });
}

// Fetch history and analyze for "Trusted Channels"
async function fetchLocalHistory() {
    return new Promise((resolve) => {
        const user = detectYouTubeUser();
        chrome.runtime.sendMessage({
            action: 'getYoutubeHistory',
            limit: 2000,
            user: user
        }, (history) => {
            localHistory = history || [];

            // Build trusted channels (channels watched more than once)
            trustedChannels.clear();
            localHistory.forEach(h => {
                if (h.channel && h.channel !== '[Proxy Captured]') {
                    const count = trustedChannels.get(h.channel) || 0;
                    trustedChannels.set(h.channel, count + 1);
                }
            });

            console.log(`[DNS Agent] Loaded ${localHistory.length} history items. Found ${trustedChannels.size} familiar channels.`);
            resolve();
        });
    });
}

// Apply visual changes and circumvention logic
function applyAugmentation() {
    const searchResults = document.querySelectorAll('ytd-video-renderer, ytd-compact-video-renderer, ytd-grid-video-renderer');
    const container = document.querySelector('#contents.ytd-item-section-renderer');

    let itemsToReorder = [];

    for (const item of searchResults) {
        const link = item.querySelector('a#video-title, a#thumbnail');
        if (!link) continue;

        try {
            const url = new URL(link.href);
            const videoId = url.searchParams.get('v');
            if (!videoId) continue;

            const historyItem = localHistory.find(h => h.videoId === videoId);
            const channelName = item.querySelector('#channel-name a')?.textContent?.trim();
            const isFamiliar = trustedChannels.has(channelName);

            // 1. Filtering (Algorithm Circumvention)
            if (historyItem && config.hideWatched) {
                item.style.display = 'none';
                continue;
            } else {
                item.style.display = '';
            }

            // 2. Highlighting & Badging
            if (!item.dataset.augmented) {
                if (historyItem) {
                    augmentWatchedResult(item, historyItem);
                } else if (isFamiliar) {
                    augmentFamiliarResult(item, channelName);
                }
                item.dataset.augmented = 'true';
            }

            // 3. Collect for Re-ranking
            if (config.boostTrusted && (historyItem || isFamiliar)) {
                itemsToReorder.push(item);
            }
        } catch (e) { }
    }

    // 4. Re-ranking (Prioritize your data over YouTube's)
    if (config.boostTrusted && itemsToReorder.length > 0 && container) {
        itemsToReorder.sort((a, b) => {
            // Prioritize already watched, then familiar channels
            const aWatched = localHistory.some(h => h.videoId === new URL(a.querySelector('a#thumbnail').href).searchParams.get('v'));
            const bWatched = localHistory.some(h => h.videoId === new URL(b.querySelector('a#thumbnail').href).searchParams.get('v'));
            if (aWatched && !bWatched) return -1;
            if (!aWatched && bWatched) return 1;
            return 0;
        });

        // Insert at the top of the search results
        itemsToReorder.forEach(item => {
            if (container.firstChild !== item) {
                container.prepend(item);
            }
        });
    }
}

function augmentWatchedResult(element, history) {
    if (!config.showLocalBadges) return;

    const thumbnail = element.querySelector('#thumbnail');
    if (thumbnail && !thumbnail.querySelector('.dns-agent-badge')) {
        const badge = document.createElement('div');
        badge.className = 'dns-agent-badge dns-agent-history-badge';
        badge.textContent = 'WATCHED';
        badge.style.cssText = `position: absolute; top: 5px; left: 5px; background: #0078d4; color: white; padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: bold; z-index: 10;`;
        thumbnail.querySelector('#overlays')?.appendChild(badge);
    }

    const title = element.querySelector('#video-title');
    if (title) title.style.color = '#3ea6ff';
}

function augmentFamiliarResult(element, channelName) {
    if (!config.showLocalBadges) return;

    const count = trustedChannels.get(channelName);
    const thumbnail = element.querySelector('#thumbnail');
    if (thumbnail && !thumbnail.querySelector('.dns-agent-badge')) {
        const badge = document.createElement('div');
        badge.className = 'dns-agent-badge dns-agent-familiar-badge';
        badge.textContent = `TRUSTED (${count} views)`;
        badge.style.cssText = `position: absolute; top: 5px; left: 5px; background: #22c55e; color: white; padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: bold; z-index: 10;`;
        thumbnail.querySelector('#overlays')?.appendChild(badge);
    }
}

// Create a UI control panel for the user
function createControlPanel() {
    if (document.getElementById('dns-agent-controls')) return;

    const filterMenu = document.querySelector('ytd-search-sub-menu-renderer #container');
    if (!filterMenu) return;

    const panel = document.createElement('div');
    panel.id = 'dns-agent-controls';
    panel.style.cssText = `padding: 10px; margin-bottom: 20px; border: 1px solid #333; border-radius: 8px; background: #1a1a1a; color: #fff; font-size: 12px; display: flex; gap: 20px; align-items: center;`;

    panel.innerHTML = `
        <div style="font-weight: bold; color: #0078d4;">🛡️ DNS Agent Algorithm Controls:</div>
        <label style="cursor: pointer;"><input type="checkbox" id="dns-hide-watched" ${config.hideWatched ? 'checked' : ''}> Hide Watched</label>
        <label style="cursor: pointer;"><input type="checkbox" id="dns-boost-trusted" ${config.boostTrusted ? 'checked' : ''}> Boost Trusted Channels</label>
    `;

    filterMenu.parentNode.insertBefore(panel, filterMenu.nextSibling);

    document.getElementById('dns-hide-watched').addEventListener('change', (e) => {
        config.hideWatched = e.target.checked;
        applyAugmentation();
    });

    document.getElementById('dns-boost-trusted').addEventListener('change', (e) => {
        config.boostTrusted = e.target.checked;
        window.location.reload(); // Re-rank is cleaner on reload
    });
}

function observeSearchChanges() {
    if (window._dnsSearchObserver) window._dnsSearchObserver.disconnect();
    const observer = new MutationObserver(() => {
        if (window.location.pathname.startsWith('/results')) applyAugmentation();
    });
    const content = document.querySelector('ytd-app') || document.body;
    observer.observe(content, { childList: true, subtree: true });
    window._dnsSearchObserver = observer;
}

// Initial run & SPA handling
setInterval(() => {
    if (window.location.pathname.startsWith('/results')) {
        initSearchAugmentation();
    }
}, 3000);

/**
 * Detect the currently logged-in YouTube handle/account
 */
function detectYouTubeUser() {
    try {
        if (window.ytInitialData?.responseContext?.serviceTrackingParams) {
            const params = window.ytInitialData.responseContext.serviceTrackingParams;
            for (const p of params) {
                const val = p.params?.find(x => x.key === 'logged_in_as')?.value;
                if (val && val.length > 0) return val;
            }
        }

        const handleEl = document.querySelector('#handle');
        if (handleEl && handleEl.textContent.trim()) return handleEl.textContent.trim();

        const avatarImg = document.querySelector('button#avatar-btn img');
        if (avatarImg && avatarImg.alt) {
            const altText = avatarImg.alt.trim();
            const genericStrings = ["Avatar image", "Account profile photo"];
            if (!genericStrings.some(gs => altText.toLowerCase().includes(gs.toLowerCase())) && altText.length > 0) {
                return altText;
            }
        }
    } catch (e) { }
    return null;
}

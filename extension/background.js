// Background Service Worker
// Handles API communication with DNS Agent

console.log('[DNS Agent] Background service worker started');

// Configuration
let dnsAgentUrl = 'http://localhost:5123';
let connected = false;
let contextDomain = null;
let clientId = null;
let machineName = 'Unknown machine';
let userName = 'Unknown user';
let dnsProxyActive = false;

// Generate or retrieve Client ID
async function getClientId() {
    const result = await chrome.storage.local.get(['clientId']);
    if (result.clientId) {
        clientId = result.clientId;
    } else {
        clientId = self.crypto.randomUUID();
        await chrome.storage.local.set({ clientId });
    }
    return clientId;
}

async function getSystemInfo() {
    // Use browser info to make machine name more descriptive
    const browserInfo = navigator.userAgent.split(' ').pop();
    machineName = `Machine-${clientId.substring(0, 8)} (${browserInfo})`;
    userName = 'Extension User';
}

// Initialize state from storage
chrome.storage.local.get(['dnsAgentUrl', 'connected'], (result) => {
    if (result.dnsAgentUrl) dnsAgentUrl = result.dnsAgentUrl;
    if (result.connected) connected = result.connected;
    console.log('[DNS Agent] Initialized state from storage:', { dnsAgentUrl, connected });

    // Proactive check on startup
    getClientId().then(() => {
        getSystemInfo();
        checkConnection().then(sendHeartbeat);
    });
});

// Check if current connection is still valid
async function checkConnection() {
    const state = await chrome.storage.local.get(['dnsAgentUrl', 'manualOverride']);
    if (state.dnsAgentUrl) dnsAgentUrl = state.dnsAgentUrl;

    try {
        const response = await fetch(`${dnsAgentUrl}/api/status`, {
            method: 'GET',
            signal: AbortSignal.timeout(connected ? 5000 : 2000)
        });
        if (response.ok) {
            const data = await response.json();
            dnsProxyActive = data.clientDnsActive || false;
            connected = true;
            chrome.storage.local.set({ connected: true, dnsProxyActive });
            return true;
        }
    } catch (e) {
        // Fallback to discovery only if NOT in manual override mode
        if (state.manualOverride) {
            connected = false;
            chrome.storage.local.set({ connected: false });
            return false;
        }
    }
    return await discoverDnsAgent();
}

// Auto-discover DNS Agent on local network
async function discoverDnsAgent() {
    const state = await chrome.storage.local.get(['manualOverride']);
    if (state.manualOverride) return false;

    // Determine possible hosts
    const possibleHosts = [
        dnsAgentUrl,
        'http://localhost:5123',
        'http://127.0.0.1:5123',
        'http://192.168.1.1:5123',
        'http://192.168.1.168:5123', // Common server path
        'http://10.0.0.1:5123'
    ];

    // Remove duplicates
    const uniqueHosts = [...new Set(possibleHosts)];

    for (const host of uniqueHosts) {
        if (!host) continue;
        try {
            console.log('[DNS Agent] Probing host:', host);
            const response = await fetch(`${host}/api/status`, {
                method: 'GET',
                signal: AbortSignal.timeout(2000)
            });

            if (response.ok) {
                const data = await response.json();
                if (data.version) {
                    dnsAgentUrl = host;
                    connected = true;
                    dnsProxyActive = data.clientDnsActive || false;
                    console.log('[DNS Agent] Connected to:', host, 'Version:', data.version, 'Proxy:', dnsProxyActive);

                    // Save to storage
                    chrome.storage.local.set({ dnsAgentUrl: host, connected: true, dnsProxyActive });

                    // Fetch filters
                    await fetchFilters();
                    return true;
                }
            }
        } catch (e) {
            // Host not reachable
        }
    }

    connected = false;
    chrome.storage.local.set({ connected: false });
    console.log('[DNS Agent] Could not connect to DNS Agent');
    return false;
}

// Fetch YouTube filters from API
async function fetchFilters() {
    try {
        const response = await fetch(`${dnsAgentUrl}/api/youtube-filters`);
        if (response.ok) {
            const filters = await response.json();
            chrome.storage.local.set({ youtubeFilters: filters });
            console.log('[DNS Agent] Fetched filters version:', filters.version);

            // Notify all YouTube tabs
            const tabs = await chrome.tabs.query({ url: '*://*.youtube.com/*' });
            for (const tab of tabs) {
                chrome.tabs.sendMessage(tab.id, {
                    action: 'filtersUpdated',
                    filters: filters
                }).catch(() => { });
            }

            return filters;
        }
    } catch (e) {
        console.error('[DNS Agent] Failed to fetch filters:', e);
    }
    return null;
}

// Report statistics to API
async function reportStats(stats) {
    if (!connected) return;

    try {
        const payload = {
            adsBlocked: stats.adsBlocked || 0,
            adsFailed: stats.adsFailed || 0,
            sponsorsSkipped: stats.sponsorsSkipped || 0,
            timeSavedSeconds: stats.timeSavedSeconds || 0,
            filterVersion: stats.filterVersion || 'unknown',
            titlesCleaned: stats.titlesCleaned || 0,
            thumbnailsReplaced: stats.thumbnailsReplaced || 0
        };

        await fetch(`${dnsAgentUrl}/api/youtube-stats`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                ...payload,
                machineName: machineName
            })
        });
        console.log('[DNS Agent] Reported stats:', payload);
    } catch (e) {
        console.error('[DNS Agent] Failed to report stats:', e);
    }
}

// Send heartbeat to DNS Agent
async function sendHeartbeat() {
    if (!connected || !clientId) return;

    try {
        const manifest = chrome.runtime.getManifest();
        await fetch(`${dnsAgentUrl}/api/heartbeat`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                clientId: clientId,
                machineName: machineName,
                userName: userName,
                version: manifest.version
            })
        });
        console.log('[DNS Agent] Heartbeat sent');
    } catch (e) {
        console.error('[DNS Agent] Heartbeat failed:', e);
    }
}

// DeArrow Proxy: Fetch crowdsourced titles
async function getDeArrowTitles(hashPrefix) {
    if (!connected) return null;
    try {
        const response = await fetch(`${dnsAgentUrl}/api/dearrow/branding/${hashPrefix}`);
        if (response.ok) return await response.json();
    } catch (e) {
        console.error('[DNS Agent] DeArrow Proxy Error (Branding):', e);
    }
    return null;
}

// DeArrow Proxy: Fetch crowdsourced thumbnails
async function getDeArrowThumbnail(videoID) {
    if (!connected) return null;
    try {
        const response = await fetch(`${dnsAgentUrl}/api/dearrow/v1/getThumbnail?videoID=${videoID}`);
        if (response.ok) return await response.json();
    } catch (e) {
        console.error('[DNS Agent] DeArrow Proxy Error (Thumbnail):', e);
    }
    return null;
}

// Block domain via API
async function blockDomain(domain, reason = 'Blocked from extension') {
    if (!connected) {
        console.error('[DNS Agent] Not connected to DNS Agent');
        return false;
    }

    try {
        const response = await fetch(`${dnsAgentUrl}/api/block`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ domain, reason })
        });

        if (response.ok) {
            console.log('[DNS Agent] Blocked domain:', domain);
            return true;
        } else if (response.status === 401) {
            console.error('[DNS Agent] Authentication required to block domains');
            return false;
        }
    } catch (e) {
        console.error('[DNS Agent] Failed to block domain:', e);
    }
    return false;
}

// Message handler
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === 'getFilters') {
        fetchFilters().then(sendResponse);
        return true; // Async response
    }

    if (message.action === 'reportStats') {
        reportStats(message.stats);
    }

    if (message.action === 'getDeArrowTitles') {
        getDeArrowTitles(message.hashPrefix).then(sendResponse);
        return true;
    }

    if (message.action === 'getDeArrowThumbnail') {
        getDeArrowThumbnail(message.videoID).then(sendResponse);
        return true;
    }

    if (message.action === 'setContextDomain') {
        contextDomain = message.domain;
    }

    if (message.action === 'blockDomain') {
        blockDomain(message.domain, message.reason).then(sendResponse);
        return true; // Async response
    }

    if (message.action === 'checkConnection') {
        checkConnection().then(sendResponse);
        return true;
    }

    if (message.action === 'getConnectionStatus') {
        const lastStatus = { connected, dnsAgentUrl, dnsProxyActive };
        // Trigger a check in the background to refresh status
        checkConnection();
        sendResponse(lastStatus);
    }
});

// Context menu: Block this domain
function createContextMenu() {
    chrome.contextMenus.removeAll(() => {
        chrome.contextMenus.create({
            id: 'block-domain',
            title: 'Block this domain with DNS Agent',
            contexts: ['link']
        });
    });
}

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
    if (info.menuItemId === 'block-domain' && contextDomain) {
        const success = await blockDomain(contextDomain);
        if (success) {
            chrome.notifications.create({
                type: 'basic',
                iconUrl: 'icons/icon48.png',
                title: 'DNS Agent',
                message: `Blocked ${contextDomain} network-wide!`
            });
        } else {
            chrome.notifications.create({
                type: 'basic',
                iconUrl: 'icons/icon48.png',
                title: 'DNS Agent',
                message: `Failed to block ${contextDomain}. Check connection.`
            });
        }
    }
});

// Auto-update filters every 6 hours
setInterval(fetchFilters, 6 * 60 * 60 * 1000);

// Heartbeat every 2 minutes
setInterval(sendHeartbeat, 2 * 60 * 1000);

// Initialize
chrome.runtime.onInstalled.addListener(() => {
    console.log('[DNS Agent] Extension installed');
    createContextMenu();
    checkConnection();
});

// Try to connect on startup
createContextMenu();
checkConnection();

// Periodic connection check (every 5 minutes)
setInterval(async () => {
    if (!connected) {
        await checkConnection();
    }
}, 5 * 60 * 1000);

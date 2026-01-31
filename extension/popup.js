// Popup Script
document.addEventListener('DOMContentLoaded', async () => {
    // Load connection status
    const status = await chrome.runtime.sendMessage({ action: 'getConnectionStatus' });
    updateConnectionStatus(status);

    // Load statistics
    const result = await chrome.storage.local.get(['youtubeStats', 'youtubeFilters']);
    if (result.youtubeStats) {
        document.getElementById('ads-blocked').textContent = result.youtubeStats.adsBlocked;
    }
    if (result.youtubeFilters) {
        document.getElementById('filter-version').textContent = result.youtubeFilters.version || '-';
    }

    // Refresh filters button
    document.getElementById('refresh-filters').addEventListener('click', async () => {
        const btn = document.getElementById('refresh-filters');
        btn.disabled = true;
        btn.textContent = '⏳ Refreshing...';

        await chrome.runtime.sendMessage({ action: 'getFilters' });

        setTimeout(() => {
            btn.disabled = false;
            btn.textContent = '✅ Refreshed!';
            setTimeout(() => {
                btn.textContent = '🔄 Refresh Filters';
            }, 2000);
        }, 1000);
    });

    // Open dashboard button
    document.getElementById('open-dashboard').addEventListener('click', () => {
        if (status.connected && status.dnsAgentUrl) {
            chrome.tabs.create({ url: status.dnsAgentUrl });
        } else {
            alert('DNS Agent is not connected. Please ensure it is running on your local network.');
        }
    });

    // Test API button
    document.getElementById('test-api').addEventListener('click', async () => {
        const btn = document.getElementById('test-api');
        btn.disabled = true;
        btn.textContent = '🧪 Sending Test...';

        // Send synthetic stats report
        const testStats = {
            adsBlocked: 1,
            sponsorsSkipped: 1,
            titlesCleaned: 1,
            timeSavedSeconds: 10,
            filterVersion: 'v2.3.13-TEST'
        };

        await chrome.runtime.sendMessage({ action: 'reportStats', stats: testStats });

        setTimeout(() => {
            btn.disabled = false;
            btn.textContent = '✅ Success! check dashboard.';
            btn.style.background = '#22c55e';
            setTimeout(() => {
                btn.textContent = '🧪 Test API Connection';
                btn.style.background = '#a855f7';
            }, 3000);
        }, 1500);
    });

    // Settings navigation
    const mainView = document.querySelector('.main-view');
    const settingsView = document.getElementById('settings-view');
    const statsSection = document.querySelector('.stats-section');

    document.getElementById('settings-link').addEventListener('click', (e) => {
        e.preventDefault();
        mainView.classList.add('hidden');
        statsSection.classList.add('hidden');
        settingsView.style.display = 'flex';

        // Load current URL into input
        chrome.storage.local.get(['dnsAgentUrl'], (result) => {
            if (result.dnsAgentUrl) {
                document.getElementById('server-url').value = result.dnsAgentUrl;
            }
        });
    });

    document.getElementById('back-to-main').addEventListener('click', () => {
        settingsView.style.display = 'none';
        mainView.classList.remove('hidden');
        statsSection.classList.remove('hidden');
    });

    // Save settings
    document.getElementById('save-settings').addEventListener('click', async () => {
        const url = document.getElementById('server-url').value.trim();
        if (!url) return;

        const saveBtn = document.getElementById('save-settings');
        saveBtn.disabled = true;
        saveBtn.textContent = '...';

        // Save and notify background
        await chrome.storage.local.set({ dnsAgentUrl: url, manualOverride: true });
        await chrome.runtime.sendMessage({ action: 'checkConnection' });

        setTimeout(async () => {
            const newStatus = await chrome.runtime.sendMessage({ action: 'getConnectionStatus' });
            updateConnectionStatus(newStatus);
            saveBtn.disabled = false;
            saveBtn.textContent = 'Save';

            if (newStatus.connected) {
                // Flash success
                saveBtn.style.background = '#4ade80';
                setTimeout(() => { saveBtn.style.background = ''; }, 2000);
            }
        }, 500);
    });

    // YouTube Tracking Toggle
    const trackingToggle = document.getElementById('youtube-tracking-toggle');
    if (trackingToggle) {
        chrome.storage.local.get(['youtubeTrackingEnabled'], (result) => {
            trackingToggle.checked = result.youtubeTrackingEnabled !== false;
        });

        trackingToggle.addEventListener('change', () => {
            chrome.storage.local.set({ youtubeTrackingEnabled: trackingToggle.checked });
        });
    }
});

function updateConnectionStatus(status) {
    const statusDot = document.getElementById('status-dot');
    const statusText = document.getElementById('status-text');
    const statusDetails = document.getElementById('status-details');

    if (status.connected) {
        statusDot.className = 'status-dot connected';
        statusText.textContent = 'Connected';
        statusDetails.textContent = `Connected to ${status.dnsAgentUrl}`;
        // Update service version
        chrome.runtime.sendMessage({ action: 'getServiceStatus' }).then(res => {
            if (res && res.version) {
                document.getElementById('service-version-footer').textContent = `Service: v${res.version}`;
            }
        });
    } else {
        statusDot.className = 'status-dot disconnected';
        statusText.textContent = 'Disconnected';
        statusDetails.textContent = 'Service not found. Check settings.';
        document.getElementById('service-version-footer').textContent = 'Service: OFFLINE';
    }
    // Update Proxy Status
    const proxyStatus = document.getElementById('proxy-status');
    if (proxyStatus) {
        if (status.dnsProxyActive) {
            proxyStatus.textContent = 'ACTIVE';
            proxyStatus.className = 'badge bg-success';
        } else {
            proxyStatus.textContent = 'INACTIVE';
            proxyStatus.className = 'badge bg-secondary';
        }
    }
}

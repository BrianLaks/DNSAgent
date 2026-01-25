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
});

function updateConnectionStatus(status) {
    const statusDot = document.getElementById('status-dot');
    const statusText = document.getElementById('status-text');
    const statusDetails = document.getElementById('status-details');

    if (status.connected) {
        statusDot.className = 'status-dot connected';
        statusText.textContent = 'Connected';
        statusDetails.textContent = `Connected to ${status.dnsAgentUrl}`;
    } else {
        statusDot.className = 'status-dot disconnected';
        statusText.textContent = 'Disconnected';
        statusDetails.textContent = 'DNS Agent not found on local network';
    }
}

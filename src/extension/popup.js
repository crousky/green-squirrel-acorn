document.addEventListener('DOMContentLoaded', async () => {
    const authSection = document.getElementById('auth-section');
    const mainSection = document.getElementById('main-section');
    const loginBtn = document.getElementById('login-btn');
    const localTestBtn = document.getElementById('local-test-btn');
    const sendBtn = document.getElementById('send-btn');
    const statusMsg = document.getElementById('status-msg');
    const openSettings = document.getElementById('open-settings');

    // Check auth state
    const token = await getToken();
    if (token) {
        showMainInterface();
    }

    loginBtn.addEventListener('click', () => {
        // Real OAuth flow - placeholder
        // In real impl, open tab to auth endpoint, handle callback
        alert("OAuth flow not implemented in this local version. Use Local Test Mode.");
    });

    localTestBtn.addEventListener('click', async () => {
        // Set local tester token
        await setToken("LocalTester");
        showMainInterface();
    });

    sendBtn.addEventListener('click', async () => {
        sendBtn.disabled = true;
        statusMsg.textContent = "Capturing page...";
        statusMsg.className = "status";

        try {
            const tab = await getCurrentTab();
            if (!tab) throw new Error("No active tab");

            // Execute script to get HTML
            const result = await chrome.scripting.executeScript({
                target: { tabId: tab.id },
                func: () => {
                    return {
                        html: document.documentElement.outerHTML,
                        title: document.title,
                        url: window.location.href
                    };
                }
            });

            if (!result || !result[0] || !result[0].result) {
                throw new Error("Failed to capture page");
            }

            const pageData = result[0].result;

            statusMsg.textContent = "Processing & Sending...";

            // Send to background
            chrome.runtime.sendMessage({
                action: "processPage",
                payload: {
                    pageHtml: pageData.html,
                    pageTitle: pageData.title,
                    pageUrl: pageData.url,
                    publishDate: new Date().toISOString()
                }
            }, (response) => {
                if (response && response.success) {
                    statusMsg.textContent = "✓ Sent to Kindle!";
                    statusMsg.className = "status success";
                } else {
                    statusMsg.textContent = "Error: " + (response ? response.error : "Unknown error");
                    statusMsg.className = "status error";
                }
                sendBtn.disabled = false;
            });

        } catch (err) {
            statusMsg.textContent = "Error: " + err.message;
            statusMsg.className = "status error";
            sendBtn.disabled = false;
        }
    });

    openSettings.addEventListener('click', (e) => {
        e.preventDefault();
        chrome.tabs.create({ url: 'https://localhost:5001/account' });
    });

    function showMainInterface() {
        authSection.classList.add('hidden');
        mainSection.classList.remove('hidden');
    }

    function getToken() {
        return new Promise((resolve) => {
            chrome.storage.local.get(['authToken'], (result) => {
                resolve(result.authToken);
            });
        });
    }

    function setToken(token) {
        return new Promise((resolve) => {
            chrome.storage.local.set({ authToken: token }, resolve);
        });
    }

    async function getCurrentTab() {
        let queryOptions = { active: true, lastFocusedWindow: true };
        let [tab] = await chrome.tabs.query(queryOptions);
        return tab;
    }
});

// background.js

const API_BASE_URL = "http://localhost:7071/api"; // Localhost for dev
// const API_BASE_URL = "https://functions.greensquirrel.dev/api"; // Prod

// Listen for messages from popup
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === "processPage") {
        processPage(request.payload, sendResponse);
        return true; // Keep channel open for async response
    }
});

async function processPage(payload, sendResponse) {
    try {
        const token = await getToken();
        if (!token) {
            sendResponse({ success: false, error: "Not authenticated" });
            return;
        }

        const response = await fetch(`${API_BASE_URL}/hive-reader/process`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            const data = await response.json();
            sendResponse({ success: true, data: data });
        } else {
            const errText = await response.text();
            sendResponse({ success: false, error: `Server error: ${response.status} - ${errText}` });
        }
    } catch (error) {
        sendResponse({ success: false, error: error.message });
    }
}

function getToken() {
    return new Promise((resolve) => {
        chrome.storage.local.get(['authToken'], (result) => {
            resolve(result.authToken);
        });
    });
}

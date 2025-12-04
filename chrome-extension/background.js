// HiveReader Chrome Extension - Background Service Worker

const API_BASE_URL = 'https://greensquirrel.dev/api';

// Listen for messages from popup or content scripts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === 'AUTH_TOKEN') {
    // Save token from auth flow
    chrome.storage.local.set({
      jwtToken: message.token,
      userEmail: message.email
    }).then(() => {
      sendResponse({ success: true });
    });
    return true; // Indicates async response
  }

  if (message.type === 'CHECK_AUTH') {
    chrome.storage.local.get(['jwtToken']).then((result) => {
      sendResponse({ authenticated: !!result.jwtToken });
    });
    return true;
  }
});

// Listen for tab updates to catch auth callback
chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.url) {
    // Check if this is an auth callback from greensquirrel.dev
    const url = new URL(changeInfo.url);

    // Handle extension auth callback
    if (url.hostname === 'greensquirrel.dev' &&
        url.pathname === '/auth/extension/callback') {
      // The page should contain the token
      // We'll inject a content script to extract it
      try {
        await chrome.scripting.executeScript({
          target: { tabId: tabId },
          func: extractAuthToken
        });
      } catch (error) {
        console.error('Error extracting auth token:', error);
      }
    }
  }
});

// Function to inject into auth callback page
function extractAuthToken() {
  // Look for token in page content or URL parameters
  const urlParams = new URLSearchParams(window.location.search);
  const token = urlParams.get('token');
  const email = urlParams.get('email');

  if (token) {
    // Send token to background script
    chrome.runtime.sendMessage({
      type: 'AUTH_TOKEN',
      token: token,
      email: email
    });

    // Close this tab
    window.close();
  }
}

// Handle extension install
chrome.runtime.onInstalled.addListener((details) => {
  if (details.reason === 'install') {
    // Open welcome page or account settings
    chrome.tabs.create({
      url: 'https://greensquirrel.dev/account?extension=installed'
    });
  }
});

// Context menu for right-click "Send to Kindle"
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: 'send-to-kindle',
    title: 'Send to Kindle via HiveReader',
    contexts: ['page']
  });
});

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === 'send-to-kindle') {
    // Check if authenticated
    const storage = await chrome.storage.local.get(['jwtToken']);
    if (!storage.jwtToken) {
      // Open popup to prompt sign in
      // Note: Can't programmatically open popup, so open account page
      chrome.tabs.create({ url: 'https://greensquirrel.dev/login?redirect=extension' });
      return;
    }

    // Send directly
    await sendPageToKindle(tab, storage.jwtToken);
  }
});

async function sendPageToKindle(tab, jwtToken) {
  try {
    // Show notification that we're processing
    chrome.action.setBadgeText({ text: '...' });
    chrome.action.setBadgeBackgroundColor({ color: '#4CAF50' });

    // Capture page content
    const results = await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      func: () => {
        return {
          html: document.documentElement.outerHTML,
          title: document.title,
          url: window.location.href,
          author: document.querySelector('meta[name="author"]')?.getAttribute('content') || null
        };
      }
    });

    if (!results || !results[0] || !results[0].result) {
      throw new Error('Failed to capture page');
    }

    const pageContent = results[0].result;

    // Send to API
    const response = await fetch(`${API_BASE_URL}/hive-reader/process`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        pageHtml: pageContent.html,
        pageTitle: pageContent.title,
        pageUrl: pageContent.url,
        author: pageContent.author
      })
    });

    const data = await response.json();

    if (response.ok && data.success) {
      // Success
      chrome.action.setBadgeText({ text: '✓' });
      setTimeout(() => chrome.action.setBadgeText({ text: '' }), 3000);

      // Show notification
      chrome.notifications.create({
        type: 'basic',
        iconUrl: 'icons/icon128.png',
        title: 'HiveReader',
        message: 'Article sent to your Kindle!'
      });
    } else {
      throw new Error(data.error || 'Failed to send');
    }
  } catch (error) {
    console.error('Error sending to Kindle:', error);
    chrome.action.setBadgeText({ text: '!' });
    chrome.action.setBadgeBackgroundColor({ color: '#f44336' });
    setTimeout(() => chrome.action.setBadgeText({ text: '' }), 3000);

    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icons/icon128.png',
      title: 'HiveReader Error',
      message: error.message || 'Failed to send article'
    });
  }
}

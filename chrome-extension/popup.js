// HiveReader Chrome Extension - Popup Script

const API_BASE_URL = 'https://greensquirrel.dev/api';
const AUTH_URL = 'https://greensquirrel.dev/login';
const ACCOUNT_URL = 'https://greensquirrel.dev/account';

// DOM Elements
const elements = {
  loading: document.getElementById('loading'),
  authRequired: document.getElementById('auth-required'),
  authenticated: document.getElementById('authenticated'),
  signInBtn: document.getElementById('sign-in-btn'),
  signOutBtn: document.getElementById('sign-out-btn'),
  userEmail: document.getElementById('user-email'),
  kindleNotConfigured: document.getElementById('kindle-not-configured'),
  readyToSend: document.getElementById('ready-to-send'),
  sendBtn: document.getElementById('send-btn'),
  currentPageTitle: document.getElementById('current-page-title'),
  currentPageUrl: document.getElementById('current-page-url'),
  sending: document.getElementById('sending'),
  sendingStatus: document.getElementById('sending-status'),
  success: document.getElementById('success'),
  error: document.getElementById('error'),
  errorMessage: document.getElementById('error-message'),
  retryBtn: document.getElementById('retry-btn')
};

// State
let currentTab = null;
let jwtToken = null;
let userProfile = null;
let kindleEmail = null;

// Initialize popup
document.addEventListener('DOMContentLoaded', initialize);

async function initialize() {
  // Get current tab info
  const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
  currentTab = tabs[0];

  // Load saved token
  const storage = await chrome.storage.local.get(['jwtToken', 'userEmail']);
  jwtToken = storage.jwtToken;

  if (jwtToken) {
    // Verify token and load user data
    await loadUserData();
  } else {
    showView('auth-required');
  }

  // Setup event listeners
  setupEventListeners();
}

function setupEventListeners() {
  elements.signInBtn.addEventListener('click', handleSignIn);
  elements.signOutBtn.addEventListener('click', handleSignOut);
  elements.sendBtn.addEventListener('click', handleSendToKindle);
  elements.retryBtn.addEventListener('click', handleRetry);
}

async function loadUserData() {
  try {
    // Verify token is still valid
    const response = await fetch(`${API_BASE_URL}/auth/verify`, {
      headers: {
        'Authorization': `Bearer ${jwtToken}`
      }
    });

    if (!response.ok) {
      // Token is invalid, clear it
      await chrome.storage.local.remove(['jwtToken', 'userEmail']);
      jwtToken = null;
      showView('auth-required');
      return;
    }

    const data = await response.json();
    if (data.success && data.data) {
      userProfile = data.data;
      elements.userEmail.textContent = userProfile.email;
    }

    // Load Kindle email
    await loadKindleEmail();

    // Show appropriate view
    if (!kindleEmail) {
      showAuthenticated('kindle-not-configured');
    } else {
      updatePageInfo();
      showAuthenticated('ready-to-send');
    }
  } catch (error) {
    console.error('Error loading user data:', error);
    showView('auth-required');
  }
}

async function loadKindleEmail() {
  try {
    const response = await fetch(`${API_BASE_URL}/users/me/kindle-email`, {
      headers: {
        'Authorization': `Bearer ${jwtToken}`
      }
    });

    if (response.ok) {
      const data = await response.json();
      if (data.success && data.data && data.data.kindleEmail) {
        kindleEmail = data.data.kindleEmail;
      }
    }
  } catch (error) {
    console.error('Error loading Kindle email:', error);
  }
}

function updatePageInfo() {
  if (currentTab) {
    elements.currentPageTitle.textContent = currentTab.title || 'Untitled';
    elements.currentPageUrl.textContent = currentTab.url || '';
  }
}

async function handleSignIn() {
  // Open auth page in new tab
  chrome.tabs.create({ url: `${AUTH_URL}?redirect=extension` });

  // The background script will handle the auth callback
  // For now, close the popup - user will need to click again after auth
  window.close();
}

async function handleSignOut() {
  await chrome.storage.local.remove(['jwtToken', 'userEmail']);
  jwtToken = null;
  userProfile = null;
  kindleEmail = null;
  showView('auth-required');
}

async function handleSendToKindle() {
  if (!currentTab || !jwtToken || !kindleEmail) {
    return;
  }

  showAuthenticated('sending');
  elements.sendingStatus.textContent = 'Capturing page content...';

  try {
    // Capture page content
    const pageContent = await capturePageContent();

    elements.sendingStatus.textContent = 'Converting to EPUB...';

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
      showAuthenticated('success');
    } else {
      const errorMsg = data.error || data.data?.message || 'Failed to process article';
      elements.errorMessage.textContent = errorMsg;
      showAuthenticated('error');
    }
  } catch (error) {
    console.error('Error sending to Kindle:', error);
    elements.errorMessage.textContent = 'Network error. Please check your connection.';
    showAuthenticated('error');
  }
}

async function capturePageContent() {
  // Execute script in the current tab to capture content
  const results = await chrome.scripting.executeScript({
    target: { tabId: currentTab.id },
    func: () => {
      // Get the full HTML
      const html = document.documentElement.outerHTML;

      // Get title
      const title = document.title;

      // Try to get author from meta tags
      let author = null;
      const authorMeta = document.querySelector('meta[name="author"]') ||
                         document.querySelector('meta[property="article:author"]') ||
                         document.querySelector('meta[property="og:article:author"]');
      if (authorMeta) {
        author = authorMeta.getAttribute('content');
      }

      // Get URL
      const url = window.location.href;

      return { html, title, author, url };
    }
  });

  if (results && results[0] && results[0].result) {
    return results[0].result;
  }

  throw new Error('Failed to capture page content');
}

function handleRetry() {
  updatePageInfo();
  showAuthenticated('ready-to-send');
}

function showView(viewId) {
  elements.loading.classList.add('hidden');
  elements.authRequired.classList.add('hidden');
  elements.authenticated.classList.add('hidden');

  if (viewId === 'auth-required') {
    elements.authRequired.classList.remove('hidden');
  } else if (viewId === 'authenticated') {
    elements.authenticated.classList.remove('hidden');
  } else if (viewId === 'loading') {
    elements.loading.classList.remove('hidden');
  }
}

function showAuthenticated(subView) {
  showView('authenticated');

  // Hide all sub-views
  elements.kindleNotConfigured.classList.add('hidden');
  elements.readyToSend.classList.add('hidden');
  elements.sending.classList.add('hidden');
  elements.success.classList.add('hidden');
  elements.error.classList.add('hidden');

  // Show requested sub-view
  if (subView === 'kindle-not-configured') {
    elements.kindleNotConfigured.classList.remove('hidden');
  } else if (subView === 'ready-to-send') {
    elements.readyToSend.classList.remove('hidden');
  } else if (subView === 'sending') {
    elements.sending.classList.remove('hidden');
  } else if (subView === 'success') {
    elements.success.classList.remove('hidden');
  } else if (subView === 'error') {
    elements.error.classList.remove('hidden');
  }
}

// Green Squirrel Dev - JavaScript Interop

// Google Sign-In Configuration
const GOOGLE_CLIENT_ID = 'YOUR_GOOGLE_CLIENT_ID_HERE'; // This should be configured in appsettings.json

let googleInitialized = false;

// Initialize Google Sign-In
function initializeGoogle() {
    if (googleInitialized) return;

    window.google.accounts.id.initialize({
        client_id: GOOGLE_CLIENT_ID,
        callback: handleGoogleCallback,
        auto_select: false,
        cancel_on_tap_outside: true,
    });

    googleInitialized = true;
}

// Handle Google Sign-In callback
function handleGoogleCallback(response) {
    if (response.credential) {
        // Store the ID token temporarily
        window.googleIdToken = response.credential;
    }
}

// Trigger Google Sign-In
window.googleSignIn = async function () {
    try {
        if (!googleInitialized) {
            initializeGoogle();
        }

        return new Promise((resolve, reject) => {
            window.google.accounts.id.prompt((notification) => {
                if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
                    // Fallback to button-based sign-in
                    window.google.accounts.id.renderButton(
                        document.getElementById('google-signin-button'),
                        { theme: 'outline', size: 'large', width: 300 }
                    );
                    reject('Google Sign-In prompt was not displayed');
                } else if (notification.isDismissedMoment()) {
                    reject('User dismissed the sign-in prompt');
                }
            });

            // Wait for the callback to set the token
            const checkToken = setInterval(() => {
                if (window.googleIdToken) {
                    clearInterval(checkToken);
                    const token = window.googleIdToken;
                    window.googleIdToken = null;
                    resolve(token);
                }
            }, 100);

            // Timeout after 60 seconds
            setTimeout(() => {
                clearInterval(checkToken);
                reject('Google Sign-In timeout');
            }, 60000);
        });
    } catch (error) {
        console.error('Google Sign-In error:', error);
        throw error;
    }
};

// Send token to Chrome extension
window.sendTokenToExtension = function (extensionId, token) {
    try {
        // Method 1: Try window.postMessage for extension content script
        window.postMessage(
            {
                type: 'GREEN_SQUIRREL_AUTH',
                extensionId: extensionId,
                token: token,
            },
            '*'
        );

        // Method 2: Store in local storage with a unique key
        const storageKey = `gsdev_ext_token_${extensionId}`;
        localStorage.setItem(storageKey, token);

        // Method 3: Try custom event
        const event = new CustomEvent('greensquirrel:auth', {
            detail: { extensionId, token },
        });
        window.dispatchEvent(event);

        console.log('Token sent to extension:', extensionId);
        return true;
    } catch (error) {
        console.error('Error sending token to extension:', error);
        return false;
    }
};

// Smooth scroll to anchor links
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href !== '#' && href !== '#!') {
                e.preventDefault();
                const target = document.querySelector(href);
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start',
                    });
                }
            }
        });
    });
});

// Initialize Google when script loads
if (typeof google !== 'undefined') {
    google.accounts.id.initialize({
        client_id: GOOGLE_CLIENT_ID,
    });
}

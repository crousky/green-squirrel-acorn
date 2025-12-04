# Product Requirements Document
## HiveReader - Web to Kindle Chrome Extension

**Version:** 1.0  
**Date:** December 4, 2025  
**Project Owner:** Green Squirrel Dev  
**Repository:** crousky/green-squirrel-acorn  
**Parent Project:** Green Squirrel Dev Portfolio

---

## Executive Summary

HiveReader is a Chrome extension and web service that enables users to save web articles directly to their Kindle devices for distraction-free reading. The service captures the full content of web pages (not just URLs) to bypass paywalls and access restrictions, converts the content to EPUB format, and emails it to the user's registered Kindle email address using Azure Communication Services.

---

## Project Goals

### Primary Goals
1. Enable one-click saving of web articles from Chrome to Kindle
2. Capture full page content to avoid paywall and access issues
3. Convert web content to properly formatted EPUB files
4. Deliver content to Kindle via email using Azure Communication Service
5. Provide secure, authenticated access requiring user login
6. Allow users to manage their Kindle email address through account settings

### Success Metrics
- Chrome extension published and installable from Chrome Web Store
- <5 second capture-to-send workflow for typical articles
- 95%+ successful EPUB conversions
- 98%+ email delivery success rate
- User authentication integrated with greensquirrel.dev
- User can configure Kindle email address on account page
- 90%+ user satisfaction with reading experience on Kindle

---

## User Personas

### Primary Persona: The Avid Reader
- **Background:** Professionals and students who read articles regularly, owns a Kindle device
- **Goals:** Save interesting articles for later reading in a distraction-free format
- **Pain Points:** Paywalls prevent "send to Kindle" bookmarklets from working, web interfaces are cluttered
- **Technical Comfort:** High; comfortable with browser extensions and cloud services
- **Use Case:** Browses web during the day, saves 3-5 articles, reads them on Kindle in the evening

### Secondary Persona: The Researcher
- **Background:** Academic or professional researcher who needs to save and organize articles
- **Goals:** Build a reading queue of research materials, access full articles without restrictions
- **Pain Points:** Needs reliable access to full article content, wants portable reading format
- **Technical Comfort:** Medium to High

---

## Technical Stack

### Chrome Extension
- **Platform:** Chrome Extension Manifest V3
- **Language:** JavaScript/TypeScript
- **Key APIs:** 
  - chrome.tabs (for page access)
  - chrome.storage (for settings and tokens)
  - chrome.identity (for authentication flow)
  - fetch API (for backend communication)

### Frontend (Web Portal)
- **Framework:** Blazor WebAssembly (integrated with existing greensquirrel.dev)
- **Pages:** Account settings page for Kindle email configuration
- **Authentication:** Shared with main site (Google OAuth + JWT)

### Backend
- **Service:** Azure Functions (C#)
- **Runtime:** .NET 8 or latest LTS
- **Key Functions:**
  - Page content processing
  - EPUB generation
  - Email sending via Azure Communication Service
  - User settings management

### Data Layer
- **Database:** Azure Cosmos DB (shared with greensquirrel.dev)
- **Collections:** 
  - Users (extended schema for Kindle email)
  - ConversionJobs (tracking and history)

### Email Service
- **Provider:** Azure Communication Service
- **Service Name:** green-squirrel-comms (existing resource)
- **Resource Group:** [Your existing resource group]

### Content Processing
- **HTML Parser:** HtmlAgilityPack or AngleSharp
- **EPUB Generation:** EPubSharp or VersOne. Epub (C# libraries)
- **Content Cleaning:** Mozilla Readability algorithm (port to C#) or similar

---

## Functional Requirements

### FR-1: Chrome Extension

#### FR-1.1: Extension Installation & Setup
- User installs extension from Chrome Web Store
- On first launch, extension prompts user to sign in
- Extension opens new tab to greensquirrel.dev/auth/extension
- User authenticates via Google OAuth (existing flow)
- Extension receives and securely stores JWT token
- Extension stores authentication state in chrome.storage. local

#### FR-1.2: Extension UI
**Browser Action (Icon in Toolbar):**
- Icon: Squirrel holding an acorn/book (consistent with Green Squirrel branding)
- Badge: Shows queue count if applicable
- Click behavior: Opens popup

**Popup Interface:**
- Shows current authentication status
- "Send to Kindle" button (primary action)
- Settings link (opens web portal account page)
- Sign out option
- Visual feedback during processing

**Context Menu Integration (Optional):**
- Right-click on page → "Send to Kindle via HiveReader"

#### FR-1.3: Page Capture
When user clicks "Send to Kindle":
1. Extension captures current tab content via chrome.tabs API
2. Extracts full HTML content (document. documentElement.outerHTML)
3. Captures page title, author (if available), URL metadata
4. Shows loading indicator in popup
5. Sends payload to backend API

**Content Capture Requirements:**
- Capture page AFTER JavaScript rendering (if dynamic content)
- Include inline styles and critical CSS
- Capture images (as base64 or URLs)
- Preserve article structure (headings, paragraphs, lists)
- Exclude ads, navigation, comments (cleanup happens server-side)

#### FR-1.4: Authentication Management
- Store JWT token securely in chrome.storage.local
- Include token in Authorization header for all API requests
- Handle token expiration (redirect to login if 401 response)
- Refresh token mechanism if implemented
- Clear token on sign out

#### FR-1.5: Error Handling
- Network errors: Show "Unable to connect" message
- Authentication errors: Prompt to sign in again
- Processing errors: Show "Unable to process page" with option to retry
- Email delivery errors: Show notification with error details
- Success notification: "Article sent to your Kindle!"

### FR-2: Backend API (Azure Functions)

#### FR-2.1: Content Processing Endpoint

**POST /api/hive-reader/process**

**Input:**
```json
{
  "pageHtml": "<full HTML content>",
  "pageTitle": "Article Title",
  "pageUrl": "https://example.com/article",
  "author": "Author Name (optional)",
  "publishDate": "2025-12-04 (optional)"
}
```

**Authorization:** Bearer JWT token (required)

**Process:**
1.  Validate JWT token, extract user ID
2. Retrieve user's Kindle email from Cosmos DB
3. Verify user has configured Kindle email (return 400 if not)
4. Parse HTML content
5. Clean content (remove ads, navigation, scripts, etc.)
6. Extract article body using readability algorithm
7. Preserve images (download and embed in EPUB)
8. Generate EPUB file
9. Send EPUB via Azure Communication Service
10.  Log conversion job in Cosmos DB
11. Return success response

**Output (Success):**
```json
{
  "status": "success",
  "jobId": "conversion-job-guid",
  "message": "Article sent to your Kindle",
  "sentAt": "2025-12-04T14:30:00Z"
}
```

**Output (Error):**
```json
{
  "status": "error",
  "errorCode": "KINDLE_EMAIL_NOT_CONFIGURED | PROCESSING_FAILED | EMAIL_SEND_FAILED",
  "message": "User-friendly error message"
}
```

#### FR-2.2: User Settings Endpoints

**GET /api/users/me/kindle-email**
- Input: JWT token
- Output: User's configured Kindle email address or null

**PUT /api/users/me/kindle-email**
- Input: JWT token, new Kindle email address
- Validation: Email format, must end with @kindle.com
- Output: Updated user profile

**DELETE /api/users/me/kindle-email**
- Input: JWT token
- Process: Remove Kindle email from user profile
- Output: Confirmation

#### FR-2.3: Conversion History Endpoint (Future)

**GET /api/hive-reader/history**
- Input: JWT token, pagination parameters
- Output: List of recent conversions with status
- Include: Page title, URL, sent date, status

### FR-3: EPUB Generation

#### FR-3. 1: Content Cleaning
- Remove navigation elements (nav, header, footer, sidebar)
- Remove advertisements (by class/id patterns)
- Remove social sharing buttons
- Remove comments sections
- Remove scripts and iframes
- Preserve main article content
- Preserve images within article body
- Clean up excessive whitespace

#### FR-3. 2: EPUB Structure
**Metadata:**
- Title: Page title
- Author: Extracted or "Unknown"
- Publisher: "HiveReader by Green Squirrel Dev"
- Publication date: Current date
- Language: Detected or default to "en"
- Identifier: Generated UUID

**Content:**
- Cover page with title and metadata
- Table of contents (if article has sections)
- Main content with proper HTML structure
- Embedded images (downloaded and included)
- Proper CSS styling for readability

**File Requirements:**
- Valid EPUB 3.0 format
- Proper mimetype and container. xml
- Well-formed XHTML content
- Optimized for Kindle rendering

### FR-4: Email Delivery via Azure Communication Service

#### FR-4.1: Azure Communication Service Configuration
- **Service Name:** green-squirrel-comms
- **Sender Address:** Configure verified sender domain (e.g., noreply@greensquirrel.dev)
- **Authentication:** Connection string stored in Azure Key Vault or Function App settings

#### FR-4.2: Email Composition
**From:** noreply@greensquirrel.dev (or configured sender)  
**To:** User's Kindle email address (e.g., username@kindle.com)  
**Subject:** "Article from HiveReader: [Page Title]"  
**Body (Plain Text):**
```
Your article has been sent to your Kindle. 

Title: [Page Title]
Source: [Page URL]
Sent: [Timestamp]

Sent by HiveReader - greensquirrel.dev
```

**Attachment:**
- Filename: [sanitized-title]. epub
- Content-Type: application/epub+zip
- File: Generated EPUB binary

#### FR-4.3: Email Delivery
- Use Azure Communication Service Email SDK
- Send email with EPUB attachment
- Implement retry logic (max 3 attempts) for transient failures
- Log delivery status
- Handle delivery failures gracefully

#### FR-4.4: Kindle Email Requirements
- Must be a valid Kindle email address (@kindle.com domain)
- User must have approved sending email address in Kindle settings
- Provide instructions to users on how to whitelist noreply@greensquirrel.dev

### FR-5: User Account Management

#### FR-5.1: Account Settings Page (Web Portal)
Add new section to existing user profile page at greensquirrel.dev:

**Kindle Settings Section:**
- Header: "Kindle Email Configuration"
- Description: "Set your Kindle email address to receive articles from HiveReader"
- Input field: Email address
- Validation: Must end with @kindle.com
- Save button
- Instructions: Link to help article on finding Kindle email and whitelisting sender

**Display:**
- Show current configured email if set
- Show "Not configured" with prompt if not set
- Edit/Update functionality
- Remove/Clear functionality

#### FR-5.2: Setup Instructions
Provide user guidance:
1. How to find your Kindle email address (Amazon account settings)
2. How to whitelist noreply@greensquirrel.dev in Kindle Personal Document Settings
3. How to install the Chrome extension
4. How to use the extension to send articles

### FR-6: Authentication & Authorization

#### FR-6.1: Authentication Requirements
- All API endpoints require valid JWT token
- Extension must authenticate via existing greensquirrel.dev flow
- Token stored securely in extension storage
- Token included in Authorization header: `Bearer <token>`

#### FR-6.2: Authorization Rules
- Users can only access their own Kindle email settings
- Users can only view their own conversion history
- All processing requests associated with authenticated user

### FR-7: Database Schema Extensions

#### Collection: Users (Extended)
Add to existing user schema:
```json
{
  "id": "user-guid",
  "googleUserId": "google-id",
  "email": "user@example.com",
  "displayName": "John Doe",
  "kindleEmail": "username@kindle.com",
  "kindleEmailUpdatedAt": "2025-12-04T10:00:00Z",
  "hiveReaderSettings": {
    "emailNotifications": true,
    "includeImages": true,
    "fontSize": "medium"
  },
  // ...  existing fields
}
```

#### Collection: ConversionJobs (New)
```json
{
  "id": "job-guid",
  "userId": "user-guid",
  "pageUrl": "https://example.com/article",
  "pageTitle": "Article Title",
  "status": "success | failed | processing",
  "errorMessage": "error details if failed",
  "epubSizeBytes": 245678,
  "processingTimeMs": 1234,
  "sentAt": "2025-12-04T14:30:00Z",
  "createdAt": "2025-12-04T14:29:45Z",
  "partitionKey": "userId"
}
```

---

## Non-Functional Requirements

### NFR-1: Performance
- Page capture: < 1 second
- HTML to EPUB conversion: < 5 seconds for typical article
- Total time from click to email sent: < 10 seconds
- API response time: < 500ms excluding processing
- Extension popup load time: < 200ms

### NFR-2: Scalability
- Support 1,000 conversions per day initially
- Design for 10,000 conversions per day without major refactoring
- Azure Functions auto-scaling for peak loads
- Cosmos DB throughput appropriate for usage patterns

### NFR-3: Reliability
- 98%+ successful EPUB conversions
- 98%+ email delivery success rate
- Retry mechanism for transient failures
- Graceful degradation for partial content extraction
- Clear error messages for user-actionable issues

### NFR-4: Security
- All API calls over HTTPS
- JWT token validation on all protected endpoints
- Secure token storage in extension
- No storage of page content after processing
- CORS configuration restricted to extension and web portal
- Sanitize all user inputs
- Validate email addresses server-side

### NFR-5: Content Quality
- Preserve article structure and formatting
- Maintain image quality (within Kindle constraints)
- Proper typography in EPUB output
- Handle various article layouts and formats
- Readable on all Kindle devices and apps

### NFR-6: Privacy
- No storage of article content beyond processing time
- No tracking of reading behavior
- Minimal data retention (conversion logs for 90 days max)
- User can delete account and all associated data
- Clear privacy policy for extension and service

### NFR-7: Browser Compatibility
- Chrome (latest 2 versions) - primary target
- Edge Chromium (latest 2 versions) - secondary
- Brave, Opera (best effort, should work with Chromium extension)

### NFR-8: Kindle Compatibility
- EPUB format compatible with all Kindle devices
- Tested on Kindle Paperwhite, Kindle Oasis, Kindle app
- Proper formatting on e-ink displays
- Images optimized for Kindle rendering

---

## Architecture Overview

### High-Level Architecture
```
[Chrome Browser]
     |
     | User clicks extension
     |
[HiveReader Extension]
     |
     | 1.  Captures page HTML
     | 2. Sends to API with JWT token
     |
     | HTTPS
     |
[Azure Functions - HiveReader API]
     |
     +--> Validate JWT token
     +--> Retrieve user's Kindle email from Cosmos DB
     +--> Parse and clean HTML content
     +--> Extract article using readability algorithm
     +--> Download and embed images
     +--> Generate EPUB file (in-memory)
     +--> Send email via Azure Communication Service
     +--> Log conversion job
     |
     | Azure SDK
     |
[Azure Communication Service: green-squirrel-comms]
     |
     | Email with EPUB attachment
     |
[User's Kindle Email] → [Amazon Kindle Service] → [User's Kindle Device]


[User Account Settings - Blazor Web App]
     |
     | Configure Kindle email
     |
[Azure Functions - User Settings API]
     |
[Azure Cosmos DB - Users Collection]
```

### Extension Authentication Flow
```
1. User installs extension
2. Extension detects no auth token
3. User clicks "Sign in"
4. Extension opens greensquirrel.dev/auth/extension
5. User completes Google OAuth (existing flow)
6. Token passed back to extension via messaging
7. Extension stores token in chrome.storage.local
8.  Extension ready to use
```

### Article Processing Flow
```
1. User navigates to article in browser
2. User clicks HiveReader extension icon
3. Extension captures page HTML using chrome.tabs API
4. Extension sends POST to /api/hive-reader/process with:
   - Full HTML content
   - Page metadata
   - JWT token in Authorization header
5. Azure Function receives request
6. Function validates JWT, gets user ID
7. Function queries Cosmos DB for user's Kindle email
8. If no Kindle email: Return error "Please configure Kindle email"
9. Function parses HTML with HtmlAgilityPack
10. Function cleans content (remove ads, navigation, etc.)
11. Function extracts article body using readability algorithm
12.  Function downloads images referenced in content
13. Function generates EPUB with EPubSharp
14. Function composes email with EPUB attachment
15. Function sends email via Azure Communication Service
16. Function logs conversion job to Cosmos DB
17. Function returns success response to extension
18. Extension shows success notification to user
19. User receives email on Kindle within minutes
```

---

## User Experience Flows

### Flow 1: First-Time User Setup
1. User installs HiveReader extension from Chrome Web Store
2. Extension icon appears in toolbar
3. User clicks extension icon
4.  Popup shows "Sign in to get started" with Google sign-in button
5. User clicks "Sign in with Google"
6. New tab opens to greensquirrel.dev/auth/extension
7. User authenticates with Google OAuth
8. Success page shows "You're all set! Configure your Kindle email."
9. User clicks link to account settings
10. User enters Kindle email address (e.g., user123@kindle.com)
11.  User clicks "Save"
12. User sees instructions to whitelist noreply@greensquirrel.dev
13.  Setup complete, ready to use

### Flow 2: Sending Article to Kindle
1. User browses to article on any website
2. User clicks HiveReader extension icon
3.  Popup shows "Send to Kindle" button
4. User clicks "Send to Kindle"
5.  Popup shows loading spinner "Processing article..."
6. After 3-5 seconds, popup shows "✓ Sent to your Kindle!"
7. User receives email on Kindle within 5 minutes
8. User opens article on Kindle, reads distraction-free

### Flow 3: Handling Paywall Content
1. User hits paywall on news site
2. User signs in to news site to view article
3. Once article is loaded (behind paywall)
4. User clicks HiveReader extension
5. Extension captures full rendered HTML (including paywall content)
6. Article processed and sent to Kindle
7.  User can read full article on Kindle without paywall

---

## Development Phases

### Phase 1: Backend Foundation (Week 1)
**Deliverables:**
- Azure Functions project for HiveReader API
- User schema extended with kindleEmail field
- ConversionJobs collection created
- Basic /api/users/me/kindle-email endpoints
- Integration with Azure Communication Service green-squirrel-comms

**Tasks:**
- Set up Functions project in solution
- Update Cosmos DB schema
- Implement Kindle email CRUD endpoints
- Configure Azure Communication Service SDK
- Test email sending with sample EPUB

### Phase 2: EPUB Generation (Week 2)
**Deliverables:**
- HTML parsing and cleaning implementation
- Readability algorithm integration
- EPUB generation working end-to-end
- Image downloading and embedding
- Valid EPUB output tested on Kindle

**Tasks:**
- Integrate HtmlAgilityPack or AngleSharp
- Implement content cleaning rules
- Integrate EPubSharp or similar library
- Build EPUB generator service
- Test EPUB files on multiple Kindle devices
- Optimize for Kindle rendering

### Phase 3: Processing API (Week 3)
**Deliverables:**
- /api/hive-reader/process endpoint complete
- Full processing pipeline working
- Email sending with EPUB attachment
- Error handling and retry logic
- Logging and monitoring

**Tasks:**
- Implement process endpoint
- Wire up HTML → EPUB → Email pipeline
- Add comprehensive error handling
- Implement retry logic for email sending
- Add Application Insights logging
- Load testing

### Phase 4: Web Portal Integration (Week 3-4)
**Deliverables:**
- Kindle settings page in Blazor app
- Account settings UI for Kindle email
- Help documentation page
- Setup instructions

**Tasks:**
- Add Kindle settings component to user profile
- Implement save/update/delete UI
- Create help page with setup instructions
- Add validation and error messaging
- Test on mobile and desktop

### Phase 5: Chrome Extension Development (Week 4-5)
**Deliverables:**
- Chrome extension with Manifest V3
- Page capture functionality
- Authentication integration
- API communication
- Error handling and notifications
- Extension tested and working

**Tasks:**
- Initialize extension project structure
- Implement manifest.json with required permissions
- Build popup UI (HTML/CSS/JS)
- Implement authentication flow with greensquirrel.dev
- Implement page capture logic
- Implement API communication with backend
- Add error handling and user feedback
- Test across different websites

### Phase 6: Testing & Polish (Week 6)
**Deliverables:**
- Comprehensive testing complete
- Bug fixes implemented
- Performance optimizations
- Documentation complete
- Chrome Web Store submission materials ready

**Tasks:**
- Test on variety of websites (news, blogs, documentation)
- Test with paywalled content
- Test error scenarios
- Cross-browser testing
- Performance profiling and optimization
- Create Chrome Web Store listing (description, screenshots, icons)
- Prepare privacy policy and terms of service
- User acceptance testing with beta users

### Phase 7: Launch (Week 7)
**Deliverables:**
- Extension published to Chrome Web Store
- Web portal live with Kindle settings
- Documentation published
- Monitoring configured
- Launch announcement

**Tasks:**
- Submit extension to Chrome Web Store
- Update greensquirrel.dev with HiveReader showcase
- Publish help documentation
- Configure alerts and monitoring
- Beta launch to limited users
- Public launch announcement
- Monitor for issues

---

## Chrome Extension Manifest V3 Specification

### manifest.json
```json
{
  "manifest_version": 3,
  "name": "HiveReader - Send to Kindle",
  "version": "1.0. 0",
  "description": "Save articles from the web to your Kindle for distraction-free reading",
  "permissions": [
    "activeTab",
    "storage",
    "scripting"
  ],
  "host_permissions": [
    "https://greensquirrel.dev/*"
  ],
  "action": {
    "default_popup": "popup.html",
    "default_icon": {
      "16": "icons/icon16.png",
      "32": "icons/icon32.png",
      "48": "icons/icon48.png",
      "128": "icons/icon128.png"
    }
  },
  "icons": {
    "16": "icons/icon16.png",
    "32": "icons/icon32.png",
    "48": "icons/icon48.png",
    "128": "icons/icon128.png"
  },
  "background": {
    "service_worker": "background.js"
  },
  "content_security_policy": {
    "extension_pages": "script-src 'self'; object-src 'self'"
  }
}
```

### Key Permissions Explained
- **activeTab:** Access to current tab content when user clicks extension
- **storage:** Store JWT token and settings
- **scripting:** Execute content scripts to capture page HTML
- **host_permissions:** Communicate with greensquirrel.dev API

---

## Azure Communication Service Configuration

### Resource Details
- **Service Name:** green-squirrel-comms
- **Type:** Azure Communication Service
- **Resource Group:** [Existing resource group]
- **Location:** [Same as other resources]

### Configuration Required
1. **Email Domain Setup:**
   - Add custom domain (greensquirrel.dev) to Communication Service
   - Verify domain ownership (DNS records)
   - Configure SPF, DKIM, DMARC for deliverability

2. **Sender Address:**
   - Configure: noreply@greensquirrel.dev
   - Or: kindle@greensquirrel.dev (more semantic)

3. **Connection String:**
   - Store in Azure Key Vault: HiveReader-AzureCommunicationService-ConnectionString
   - Reference in Function App settings

4. **Email Settings:**
   - Configure retry policies
   - Set up delivery reports (optional)
   - Monitor sending quota and limits

### SDK Integration (C#)
```csharp
using Azure.Communication.Email;

var connectionString = Environment.GetEnvironmentVariable("AzureCommunicationService__ConnectionString");
var emailClient = new EmailClient(connectionString);

var emailMessage = new EmailMessage(
    senderAddress: "noreply@greensquirrel.dev",
    recipientAddress: userKindleEmail,
    content: new EmailContent("Article from HiveReader")
    {
        PlainText = emailBody,
        Html = emailHtml
    }
);

// Add EPUB attachment
var attachment = new EmailAttachment(
    name: $"{sanitizedTitle}.epub",
    contentType: "application/epub+zip",
    contentInBase64: Convert.ToBase64String(epubBytes)
);
emailMessage.Attachments.Add(attachment);

// Send email
var emailSendOperation = await emailClient.SendAsync(
    WaitUntil.Started, 
    emailMessage
);
```

---

## Security Considerations

### Extension Security
- **Token Storage:** Store JWT in chrome.storage.local (encrypted by Chrome)
- **No Sensitive Data:** Never store page content locally
- **HTTPS Only:** All API communication over HTTPS
- **Content Script Isolation:** Minimal privileges, no access to extension storage
- **Manifest V3:** Use service worker, no remote code execution

### API Security
- **Authentication Required:** All endpoints require valid JWT
- **Input Validation:** Sanitize all HTML input, validate email addresses
- **Rate Limiting:** Implement per-user rate limits (e.g., 50 conversions/day)
- **Content Size Limits:** Max HTML size 10MB, max EPUB size 50MB
- **Timeout Protection:** Processing timeout after 30 seconds

### Email Security
- **Sender Verification:** Use verified domain with SPF/DKIM
- **No User Data in Email Body:** Minimal information in email text
- **Attachment Scanning:** Virus scan EPUBs before sending (Azure built-in)
- **Whitelist Verification:** Recommend users whitelist sender in Kindle settings

### Privacy & Data Protection
- **Minimal Data Storage:** Only store metadata, not content
- **Temporary Processing:** Delete HTML content after EPUB generation
- **User Control:** User can delete conversion history
- **No Sharing:** Never share page content or user data with third parties
- **GDPR Compliance:** Right to access, delete, and export data

---

## Error Handling & Edge Cases

### Extension Errors
1. **Not Authenticated:**
   - Show: "Please sign in to use HiveReader"
   - Action: Redirect to login flow

2. **Kindle Email Not Configured:**
   - Show: "Please configure your Kindle email in settings"
   - Action: Open settings page

3. **Network Error:**
   - Show: "Unable to connect.  Please check your internet connection."
   - Action: Retry button

4. **API Error:**
   - Show specific error message from API
   - Action: Retry button or contact support link

### Processing Errors
1. **Invalid HTML:**
   - Attempt best-effort extraction
   - If critical failure: Return error "Unable to process this page"

2. **No Article Content Found:**
   - Try fallback extraction (full body)
   - Warn user: "Content extraction may be incomplete"

3. **Large Images:**
   - Resize images to max 800px width
   - Compress images for Kindle
   - Skip images if download fails (log warning)

4. **EPUB Generation Failure:**
   - Retry once with simplified content
   - If still fails: Return error with job ID for support

5. **Email Send Failure:**
   - Retry up to 3 times with exponential backoff
   - If all retries fail: Log error, return failure status
   - User-facing message: "Unable to send email. Please try again."

### Edge Cases
1. **JavaScript-Heavy Pages:**
   - Extension captures rendered DOM (after JS execution)
   - May require delay before capture (future enhancement)

2. **Single Page Apps (SPAs):**
   - Capture works as it gets current DOM state
   - User should wait for content to load before clicking extension

3. **PDF Pages:**
   - Not supported initially
   - Show: "PDF documents are not supported.  Please convert to HTML first."

4. **Very Long Articles:**
   - Process articles up to 50,000 words
   - Split into multiple EPUBs if necessary (future enhancement)

5. **Non-English Content:**
   - Support Unicode/UTF-8 in EPUB
   - Set language metadata based on page language attribute

6. **Paywalled Content:**
   - Primary use case: Works because we capture rendered HTML
   - Requires user to be logged in to source site in their browser

---

## User Documentation Requirements

### Extension Store Listing
- **Title:** HiveReader - Send Web Articles to Kindle
- **Short Description:** Save articles to your Kindle for distraction-free reading.  Bypasses paywalls by capturing full content.
- **Full Description:** Detailed explanation of features, benefits, setup process
- **Screenshots:** 5 screenshots showing extension in action
- **Promotional Images:** Required sizes for Chrome Web Store
- **Privacy Policy:** Detailed privacy policy on greensquirrel.dev
- **Terms of Service:** Terms on greensquirrel.dev

### Help Documentation
**Topics to Cover:**
1. Getting Started
   - Installing the extension
   - Signing in with Google
   - Finding your Kindle email address
   - Configuring Kindle settings

2. Using HiveReader
   - Sending an article to Kindle
   - What to expect after sending
   - How long until article appears on Kindle

3. Kindle Configuration
   - Whitelisting the sender email
   - Managing approved email addresses in Amazon account
   - Troubleshooting email not arriving

4.  Troubleshooting
   - Extension not working
   - Articles not arriving on Kindle
   - Content formatting issues
   - Authentication problems

5. Privacy & Security
   - What data is collected
   - How content is processed
   - Data retention policy

6. FAQ
   - Does this work with paywalled content? 
   - What websites are supported?
   - Can I use this on mobile? 
   - How much does it cost?

---

## Testing Strategy

### Unit Testing
- Azure Functions: Test each endpoint with various inputs
- EPUB Generation: Test with different HTML structures
- Content Cleaning: Test with sample pages from various sites
- Email Service: Mock Azure Communication Service for testing

### Integration Testing
- End-to-end flow: HTML → EPUB → Email
- Authentication flow: Extension → Web Portal → API
- Database operations: CRUD operations on user settings
- Azure Communication Service integration: Real email sending in test environment

### Extension Testing
- Test on variety of websites:
  - News sites (CNN, NYTimes, Medium)
  - Blogs (WordPress, Ghost)
  - Documentation sites (MDN, GitHub docs)
  - E-commerce pages (product descriptions)
  - Paywalled content (with valid subscription)
- Test error scenarios (no internet, invalid page, etc.)
- Test authentication flow
- Test on different Chrome versions

### EPUB Quality Testing
- Validate EPUB structure (use EPUBCheck tool)
- Test on multiple Kindle devices:
  - Kindle Paperwhite
  - Kindle Oasis
  - Kindle app on iOS
  - Kindle app on Android
- Verify formatting, images, typography
- Test with various content types (text-heavy, image-heavy, mixed)

### Performance Testing
- Load test API endpoints (simulate 100 concurrent users)
- Measure processing time for various article sizes
- Monitor Azure Functions performance
- Test email delivery time
- Test extension popup responsiveness

### User Acceptance Testing
- Beta test with 10-20 users
- Collect feedback on:
  - Ease of use
  - Content quality on Kindle
  - Feature requests
  - Bug reports
- Iterate based on feedback

---

## Monitoring & Analytics

### Application Insights Metrics

**Extension Metrics:**
- Install count (from Chrome Web Store)
- Daily/monthly active users
- Button click count
- Success rate
- Error rate by error type
- Average time to completion

**API Metrics:**
- Request count per endpoint
- Response time (p50, p95, p99)
- Success/failure rate
- Processing time by step (parse, clean, EPUB, email)
- EPUB size distribution
- Concurrent processing jobs

**Email Metrics:**
- Emails sent count
- Delivery success rate
- Delivery time
- Bounce rate
- Failed send reasons

### Custom Events to Track
- user_configured_kindle_email
- article_sent_to_kindle_success
- article_sent_to_kindle_failed
- epub_generation_success
- epub_generation_failed
- email_delivery_success
- email_delivery_failed
- user_sign_in_from_extension

### Alerts to Configure
- Error rate exceeds 5% (5-minute window)
- API response time > 10 seconds
- Email send failure rate > 5%
- Azure Function failures
- Cosmos DB throttling
- Azure Communication Service quota approaching limit

### Dashboard Requirements
Create Azure Dashboard with:
- Total articles processed (daily/weekly/monthly)
- Success rate trend
- Average processing time
- Active users count
- Top error types
- Email delivery metrics

---

## Cost Estimation

### Azure Services Cost (Monthly Estimates)

**Azure Functions (Consumption Plan):**
- Assume 1,000 conversions/day = 30,000/month
- Average execution time: 5 seconds
- Memory: 512MB
- Estimated: ~$5-10/month

**Azure Cosmos DB (Serverless):**
- User records: 100-500 users
- Conversion logs: 30,000 writes/month, 10,000 reads/month
- Estimated: ~$10-20/month

**Azure Communication Service:**
- Email sending: 30,000 emails/month
- Pricing: ~$0.0001 per email
- Estimated: ~$3-5/month

**Azure Static Web Apps (Standard):**
- Shared with main site
- No additional cost for HiveReader specifically

**Total Estimated Monthly Cost:**
- Low usage (1,000 conversions/day): ~$18-35/month
- High usage (10,000 conversions/day): ~$80-150/month

### Chrome Web Store
- One-time developer fee: $5
- No ongoing fees

---

## Privacy Policy & Legal Requirements

### Privacy Policy Additions
Add section for HiveReader:
- **Data Collection:** JWT token, Kindle email address, conversion metadata
- **Data Use:** Process web pages, convert to EPUB, send to user's Kindle
- **Data Storage:** Conversion logs retained for 90 days, then deleted
- **Data Sharing:** No sharing with third parties; email sent via Azure Communication Service
- **User Rights:** Access, modify, delete Kindle email; view conversion history; delete account

### Chrome Web Store Requirements
- Privacy policy URL (on greensquirrel.dev)
- Clear disclosure of permissions usage
- Data handling transparency
- Contact information for support

### Terms of Service Additions
- Fair use policy (rate limits, no abuse)
- Content ownership (user owns content, we don't claim rights)
- Service availability (best effort, no SLA for free tier)
- Account termination conditions
- Limitation of liability

---

## Future Enhancements (Post-MVP)

### Phase 2 Features
1. **Reading Queue:**
   - Save articles to queue instead of immediate send
   - Batch send multiple articles at once
   - Manage queue in web portal

2. **Formatting Options:**
   - Font size selection
   - Include/exclude images toggle
   - Article summary generation (AI-powered)

3. **Bookmarklet:**
   - Alternative to Chrome extension for other browsers
   - JavaScript bookmarklet with same functionality

4. **Firefox Extension:**
   - Port Chrome extension to Firefox Add-ons

5. **Mobile Support:**
   - Share sheet integration for iOS/Android
   - PWA for mobile browsers

6. **Enhanced Content Extraction:**
   - Better handling of complex layouts
   - Support for multi-page articles
   - Automatic pagination detection

7. **Reading Statistics:**
   - Track articles sent
   - Reading time estimates
   - Popular sources

8. **Collections:**
   - Organize articles into collections/folders
   - Tags for categorization

9. **Kindle Integration Improvements:**
   - Check Kindle delivery status
   - Option to send to Kindle library vs. email

10. **Premium Features:**
    - Higher rate limits
    - Priority processing
    - Advanced formatting options
    - Cloud storage of articles

---

## Success Criteria

### Launch Success (Month 1)
- ✅ Chrome extension published and approved
- ✅ 50+ extension installs
- ✅ 90%+ successful conversions
- ✅ 95%+ email delivery success
- ✅ No critical bugs reported
- ✅ Average processing time < 10 seconds
- ✅ Positive user feedback (4+ star rating)

### 3-Month Success
- 500+ extension installs
- 100+ active users (sent at least 1 article)
- 5,000+ total articles processed
- <2% error rate
- 10+ user reviews with 4+ average rating
- Feature requests collected and prioritized

### 6-Month Success
- 2,000+ extension installs
- 500+ active users
- 30,000+ total articles processed
- Considering paid tier or premium features
- Integration with at least one other Green Squirrel project
- User testimonials and case studies

---

## Risks & Mitigations

### Risk 1: Chrome Extension Rejection
**Impact:** High  
**Likelihood:** Medium  
**Mitigation:**
- Follow all Chrome Web Store policies carefully
- Clear privacy policy and permission justifications
- Thorough testing before submission
- Be prepared to make changes based on review feedback
- Have alternative distribution methods ready (direct installation)

### Risk 2: Kindle Email Delivery Issues
**Impact:** High  
**Likelihood:** Medium  
**Mitigation:**
- Proper email authentication (SPF, DKIM, DMARC)
- Test with multiple Kindle email addresses
- Provide clear instructions for whitelisting sender
- Monitor bounce rates and delivery failures
- Implement retry logic
- Alternative delivery method research (Kindle Direct Publishing API)

### Risk 3: Content Extraction Failures
**Impact:** Medium  
**Likelihood:** Medium  
**Mitigation:**
- Implement robust parsing with multiple fallbacks
- Test on wide variety of websites
- Allow manual content selection (future enhancement)
- Provide feedback mechanism for problematic sites
- Continuously improve extraction algorithms
- Build library of site-specific extractors for popular sources

### Risk 4: EPUB Formatting Issues on Kindle
**Impact:** Medium  
**Likelihood:** Medium  
**Mitigation:**
- Thorough testing on multiple Kindle devices
- Follow Kindle Publishing Guidelines
- Use conservative CSS styling
- Image optimization for e-ink displays
- Validate EPUB structure with EPUBCheck
- User feedback mechanism for formatting issues

### Risk 5: Azure Communication Service Limitations
**Impact:** High  
**Likelihood:** Low  
**Mitigation:**
- Understand sending quotas and limits
- Monitor usage closely
- Implement queueing system if needed
- Have backup email service ready (SendGrid, Mailgun)
- Request quota increases proactively
- Consider batching for high-volume users

### Risk 6: Paywall Detection and Blocking
**Impact:** Medium  
**Likelihood:** Low  
**Mitigation:**
- Emphasize that user must have legitimate access
- Clear in documentation that this is for personal use
- No circumvention of access controls, just capture rendered content
- Terms of service protecting Green Squirrel Dev from liability
- Monitor for abuse and implement rate limits

### Risk 7: Performance at Scale
**Impact:** Medium  
**Likelihood:** Medium  
**Mitigation:**
- Use Azure Functions consumption plan (auto-scaling)
- Optimize EPUB generation (caching, efficient libraries)
- Implement queueing for heavy processing
- Monitor performance metrics closely
- Load testing before public launch
- Consider async processing for large articles

---

## Appendices

### Appendix A: Sample Article Processing Code

```csharp
public class ArticleProcessor
{
    public async Task<ArticleResult> ProcessArticleAsync(string html, string url, string title)
    {
        // 1. Parse HTML
        var document = new HtmlDocument();
        document.LoadHtml(html);
        
        // 2. Clean content
        var cleaner = new ContentCleaner();
        var cleanedDoc = cleaner.RemoveUnwantedElements(document);
        
        // 3. Extract article
        var extractor = new ReadabilityExtractor();
        var article = extractor.Extract(cleanedDoc, url);
        
        // 4. Download images
        var imageDownloader = new ImageDownloader();
        var imagesWithData = await imageDownloader.DownloadAndEmbedImages(article. Images);
        
        // 5. Generate EPUB
        var epubGenerator = new EpubGenerator();
        var epubBytes = epubGenerator.Generate(
            title: article.Title,
            author: article.Author,
            content: article.Content,
            images: imagesWithData
        );
        
        return new ArticleResult
        {
            EpubBytes = epubBytes,
            Title = article.Title,
            WordCount = article.WordCount
        };
    }
}
```

### Appendix B: Sample Extension Code

```javascript
// popup.js - Send to Kindle button handler

document.getElementById('sendToKindle').addEventListener('click', async () => {
  // Get JWT token from storage
  const { jwtToken } = await chrome.storage.local.get('jwtToken');
  
  if (!jwtToken) {
    showError('Please sign in first');
    return;
  }
  
  // Get current tab
  const [tab] = await chrome.tabs. query({ active: true, currentWindow: true });
  
  // Capture page content
  showLoading('Capturing article...');
  
  const [{ result }] = await chrome.scripting.executeScript({
    target: { tabId: tab.id },
    func: () => {
      return {
        html: document. documentElement.outerHTML,
        title: document.title,
        url: window.location.href
      };
    }
  });
  
  // Send to API
  showLoading('Converting to EPUB...');
  
  try {
    const response = await fetch('https://greensquirrel.dev/api/hive-reader/process', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        pageHtml: result.html,
        pageTitle: result.title,
        pageUrl: result.url
      })
    });
    
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to process article');
    }
    
    const data = await response.json();
    showSuccess('Article sent to your Kindle!');
    
  } catch (error) {
    showError(error.message);
  }
});
```

### Appendix C: Kindle Email Whitelist Instructions

**For Users:**
1. Go to Amazon.com and sign in
2. Navigate to "Manage Your Content and Devices"
3. Click on "Preferences" tab
4.  Scroll to "Personal Document Settings"
5. Under "Approved Personal Document E-mail List", click "Add a new approved e-mail address"
6. Enter: noreply@greensquirrel.dev
7. Click "Add Address"
8. Your Kindle email address is shown under "Send-to-Kindle E-Mail Settings"

### Appendix D: Environment Variables

```
# Azure Functions - HiveReader
CosmosDb__ConnectionString = <cosmos-connection-string>
CosmosDb__DatabaseName = GreenSquirrelDev

AzureCommunicationService__ConnectionString = <acs-connection-string>
AzureCommunicationService__SenderEmail = noreply@greensquirrel.dev

Jwt__Secret = <jwt-secret-key>
Jwt__Issuer = https://greensquirrel.dev
Jwt__Audience = https://greensquirrel.dev

HiveReader__MaxHtmlSizeBytes = 10485760
HiveReader__MaxEpubSizeBytes = 52428800
HiveReader__ProcessingTimeoutSeconds = 30
HiveReader__RateLimitPerUserPerDay = 50

AllowedOrigins = https://greensquirrel.dev,chrome-extension://<extension-id>
```

### Appendix E: Chrome Web Store Listing

**Title:**
HiveReader - Send Web Articles to Kindle

**Short Description:**
Save articles from any website directly to your Kindle.  Bypasses paywalls by capturing full content.  One-click reading. 

**Full Description:**
HiveReader lets you save articles from any website directly to your Kindle device for distraction-free reading. 

KEY FEATURES:
• One-click sending from any webpage
• Bypasses paywalls by capturing full rendered content
• Automatically converts articles to Kindle-friendly EPUB format
• Preserves formatting, images, and article structure
• Secure authentication with Google account
• Configure your Kindle email address once, use everywhere

HOW IT WORKS:
1. Install the extension and sign in with Google
2. Configure your Kindle email address in your account settings
3. Browse to any article you want to read
4. Click the HiveReader icon
5.  Receive the article on your Kindle within minutes

PERFECT FOR:
• News articles from paywalled sites (requires your subscription)
• Long-form content you want to read later
• Technical documentation and blog posts
• Research materials and academic articles

PRIVACY & SECURITY:
• Your content is processed securely and never stored
• We only keep metadata about conversions (not content)
• Open source and transparent
• No tracking or data sharing with third parties

REQUIREMENTS:
• A free account at greensquirrel.dev
• A Kindle device or Kindle app
• Your Kindle email address (found in Amazon account settings)
• Whitelist our sender email in your Kindle settings

SUPPORT:
Visit greensquirrel.dev/help for setup instructions and troubleshooting.

---

## Document Control

**Version History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-04 | Green Squirrel Dev | Initial HiveReader PRD |

**Review Schedule:**
- Weekly during development (Phases 1-7)
- Bi-weekly post-launch for first month
- Monthly thereafter

**Related Documents:**
- greensquirrel-dev-prd.md (parent project)
- Privacy Policy (to be created)
- Terms of Service (to be updated)
- Help Documentation (to be created)

---

This PRD provides a comprehensive blueprint for building HiveReader.  The project leverages the existing Green Squirrel Dev infrastructure (authentication, Cosmos DB) and the existing Azure Communication Service (green-squirrel-comms) to deliver a unique value proposition: capturing full web page content to bypass paywalls and delivering distraction-free reading on Kindle devices.

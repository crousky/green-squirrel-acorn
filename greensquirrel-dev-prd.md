# Product Requirements Document
## Green Squirrel Dev Portfolio Site

**Version:** 1.0  
**Date:** November 13, 2025  
**Project Owner:** Green Squirrel Dev  
**Domain:** greensquirrel.dev

---

## Executive Summary

Green Squirrel Dev is a portfolio and project showcase site for an individual software development company. The platform will serve as a professional landing page highlighting completed and in-progress projects, with integrated authentication to support both web-based and Chrome extension-based user experiences. The site will establish brand identity while providing seamless access to various developer tools and services.

---

## Project Goals

### Primary Goals
1. Create a professional online presence for Green Squirrel Dev
2. Showcase completed and upcoming projects with rich previews
3. Provide centralized authentication for all Green Squirrel Dev projects
4. Enable users to create accounts and access project-specific features
5. Support Chrome extension authentication for cross-platform projects

### Success Metrics
- Site deployed and accessible at greensquirrel.dev within project timeline
- Google authentication successfully integrated with <10 second sign-in flow
- Chrome extension authentication working seamlessly
- All featured projects accessible with working previews
- Professional design that reflects brand identity
- 99.9% uptime on Azure Static Web Apps Standard plan

---

## User Personas

### Primary Persona: The Developer/Tech Enthusiast
- **Background:** Software developers, runners, productivity enthusiasts
- **Goals:** Discover useful tools, create account to access premium features
- **Pain Points:** Needs reliable, fast tools; wants single sign-on across projects
- **Technical Comfort:** High; comfortable with web apps and browser extensions

### Secondary Persona: The Casual Visitor
- **Background:** Found site through search or referral
- **Goals:** Learn about projects, possibly try featured tools
- **Pain Points:** Wants quick access without mandatory sign-in
- **Technical Comfort:** Medium; familiar with basic web navigation

---

## Technical Stack

### Frontend
- **Framework:** Blazor WebAssembly (WASM)
- **Language:** C#
- **Hosting:** Azure Static Web Apps (Standard Plan)
- **Domain:** greensquirrel.dev

### Backend
- **Service:** Azure Functions (C#)
- **Runtime:** .NET (latest LTS version recommended)
- **API Type:** HTTP-triggered functions

### Data Layer
- **Database:** Azure Cosmos DB
- **API:** SQL API (for C# SDK compatibility)
- **Collections:** Users, Projects (metadata), potentially project-specific data

### Authentication
- **Provider:** Google OAuth 2.0
- **Custom Implementation:** Azure Functions-based auth endpoints
- **Token Management:** JWT tokens for session management
- **Extension Support:** Custom token exchange flow for Chrome extension

---

## Functional Requirements

### FR-1: Landing Page

#### FR-1.1: Hero Section
- Display Green Squirrel Dev branding and tagline
- Brief introduction to the individual software development company
- Call-to-action button for sign-in/sign-up
- Professional yet fun visual design (suggest incorporating subtle squirrel-themed elements)

#### FR-1.2: Projects Showcase Section
Display project cards with the following information:
- Project name and logo/icon
- Brief description (2-3 sentences)
- Project status (Live, In Development, Coming Soon)
- Screenshot/preview images (2-3 images per project)
- Link to project (opens in new tab or navigates to subdomain)
- Technology stack badges

**Initial Projects:**

**Project 1: Pace Calculator**
- Status: Live
- URL: pacecalculator.greensquirrel.dev
- Description: A handy tool for runners to calculate running paces, times, and distances. Perfect for training planning and race day strategy.
- Screenshots: 3 screenshots showing main interface, calculations, and mobile view
- Tech: Blazor WASM, responsive design

**Project 2: HiveReader**
- Status: In Development
- URL: TBD (hive-reader.greensquirrel.dev OR greensquirrel.dev/hive-reader)
- Description: Send articles from the web directly to your Kindle for distraction-free reading. Includes Chrome extension for one-click saving.
- Screenshots: Mockups/wireframes showing web interface and extension
- Tech: Blazor WASM, Azure Functions, Chrome Extension, Kindle integration
- Special note: Chrome extension available (once live)

#### FR-1.3: About Section
- Information about Green Squirrel Dev
- Development philosophy
- Contact information or contact form
- Links to GitHub, LinkedIn, or other professional profiles (optional)

#### FR-1.4: Footer
- Copyright information
- Privacy policy link
- Terms of service link
- Links to social media/GitHub

### FR-2: Authentication System

#### FR-2.1: Google OAuth Integration
- "Sign in with Google" button on landing page and in navigation
- OAuth 2.0 flow using Google Identity Platform
- Redirect URI configuration for greensquirrel.dev
- Secure token exchange with Azure Functions backend

#### FR-2.2: User Account Creation
Upon successful Google authentication:
1. Azure Function receives OAuth token
2. Validates token with Google
3. Checks if user exists in Cosmos DB (by Google user ID)
4. If new user:
   - Creates user record in Cosmos DB
   - Stores: Google user ID, email, display name, profile picture URL, account creation date, last login date
5. If existing user:
   - Updates last login date
6. Issues JWT token for session management
7. Returns JWT to client

#### FR-2.3: Chrome Extension Authentication
**Critical Requirement:** Support authentication flow initiated from Chrome extension

**Proposed Flow:**
1. User clicks "Sign in with Google" in Chrome extension
2. Extension opens new tab to greensquirrel.dev/auth/extension
3. User completes Google OAuth flow
4. After successful auth, backend generates extension-specific token
5. Token is passed back to extension via:
   - Option A: Custom URL scheme (chrome-extension://...)
   - Option B: Message passing to extension content script
   - Option C: Local storage bridge with window.postMessage
6. Extension stores token securely
7. Extension can make authenticated requests to Azure Functions API

**Security Considerations:**
- Extension ID verification
- Time-limited token exchange
- CORS configuration for extension origin
- Token refresh mechanism

#### FR-2.4: Session Management
- JWT tokens with 24-hour expiration
- Refresh token mechanism for extended sessions
- Automatic token refresh before expiration
- Sign-out functionality clears all tokens

#### FR-2.5: User Profile
- View profile information
- Display name and email from Google account
- Profile picture from Google account
- Account creation date
- Manage project-specific settings (future enhancement)

### FR-3: Backend API (Azure Functions)

#### FR-3.1: Authentication Endpoints

**POST /api/auth/google**
- Input: Google OAuth code or token
- Process: Validates with Google, creates/updates user in Cosmos DB
- Output: JWT token, user profile

**POST /api/auth/refresh**
- Input: Refresh token
- Process: Validates token, issues new JWT
- Output: New JWT token

**POST /api/auth/extension/initiate**
- Input: Extension ID, callback URL
- Process: Generates auth session token
- Output: Auth URL for extension to open

**POST /api/auth/extension/complete**
- Input: Session token, Google OAuth token
- Process: Validates both tokens, creates user session
- Output: Extension-compatible JWT token

**GET /api/auth/verify**
- Input: JWT token (in Authorization header)
- Process: Validates token
- Output: User information or 401 Unauthorized

#### FR-3.2: User Management Endpoints

**GET /api/users/me**
- Input: JWT token
- Process: Retrieves current user information
- Output: User profile object

**PUT /api/users/me**
- Input: JWT token, profile updates
- Process: Updates user information in Cosmos DB
- Output: Updated user profile

#### FR-3.3: Projects API (Future)
- Endpoints for project-specific functionality
- Will be expanded as projects require backend support

### FR-4: Database Schema (Cosmos DB)

#### Collection: Users
```json
{
  "id": "generated-guid",
  "googleUserId": "google-unique-id",
  "email": "user@example.com",
  "displayName": "John Doe",
  "profilePictureUrl": "https://...",
  "createdAt": "2025-01-15T10:30:00Z",
  "lastLoginAt": "2025-11-13T14:22:00Z",
  "partitionKey": "user",
  "extensionTokens": [
    {
      "extensionId": "chrome-extension-id",
      "tokenHash": "hashed-token",
      "issuedAt": "2025-11-13T14:22:00Z",
      "expiresAt": "2025-11-14T14:22:00Z"
    }
  ]
}
```

#### Collection: Projects (Metadata)
```json
{
  "id": "pace-calculator",
  "name": "Pace Calculator",
  "description": "A handy tool for runners...",
  "status": "live",
  "url": "https://pacecalculator.greensquirrel.dev",
  "thumbnailUrl": "https://...",
  "screenshots": ["url1", "url2", "url3"],
  "technologies": ["Blazor", "Azure"],
  "createdAt": "2024-06-01T00:00:00Z",
  "launchedAt": "2024-07-15T00:00:00Z",
  "partitionKey": "project"
}
```

### FR-5: HiveReader Hosting Decision

**Recommendation: Subdomain Approach (hive-reader.greensquirrel.dev)**

**Rationale:**
1. **Better Auth Integration:** Separate subdomain allows independent CORS configuration
2. **Extension Compatibility:** Chrome extensions work better with consistent origins
3. **Deployment Flexibility:** Can deploy HiveReader independently without affecting main site
4. **Cookie Scope:** Easier to manage authentication cookies at subdomain level
5. **Scalability:** If HiveReader grows, it can be moved to separate infrastructure

**Implementation:**
- Deploy HiveReader as separate Azure Static Web App
- Configure custom domain hive-reader.greensquirrel.dev
- Share Azure Functions backend with main site
- Use same Cosmos DB instance
- JWT tokens valid across both domains (configure allowed origins)

**Alternative (Path-Based: /hive-reader):**
- Simpler initial setup
- Single deployment
- May complicate extension authentication
- All traffic through single Static Web App

**Decision:** Proceed with subdomain unless technical constraints arise during implementation.

---

## Non-Functional Requirements

### NFR-1: Performance
- Landing page load time: < 2 seconds on 3G connection
- Time to Interactive (TTI): < 3 seconds
- Blazor WASM bootstrap: < 5 seconds
- API response time: < 500ms for 95th percentile

### NFR-2: Security
- All traffic over HTTPS (enforced by Azure Static Web Apps)
- JWT tokens with secure signing algorithm (RS256)
- Tokens stored in httpOnly cookies where possible
- CORS configuration restricted to known domains
- Regular security updates for dependencies
- Google OAuth tokens never stored, only validated
- Extension authentication requires additional verification

### NFR-3: Scalability
- Azure Static Web Apps Standard plan supports scale requirements
- Azure Functions consumption plan auto-scales
- Cosmos DB provisioned throughput appropriate for user base
- Initial estimate: Support 1,000 concurrent users
- Design for 10x growth without major refactoring

### NFR-4: Availability
- Target: 99.9% uptime
- Azure Static Web Apps SLA compliance
- Graceful degradation if backend services unavailable
- Health monitoring and alerts configured

### NFR-5: Accessibility
- WCAG 2.1 AA compliance
- Keyboard navigation support
- Screen reader compatible
- Sufficient color contrast
- Responsive design (mobile, tablet, desktop)

### NFR-6: Browser Compatibility
- Chrome (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Edge (latest 2 versions)
- Mobile browsers (iOS Safari, Chrome Mobile)

### NFR-7: Design Standards
- Professional appearance with playful elements
- Consistent color scheme (suggest greens with accent colors)
- Subtle squirrel-themed branding elements
- Modern, clean UI/UX
- Smooth animations and transitions
- Responsive layout with mobile-first approach

---

## Architecture Overview

### High-Level Architecture
```
[User Browser/Extension]
         |
         | HTTPS
         |
    [Azure CDN] (Static content delivery)
         |
         +--> [Azure Static Web Apps]
         |         |
         |         +--> [Blazor WASM App]
         |         |         |
         |         |         +--> [Landing Page]
         |         |         +--> [Auth UI]
         |         |         +--> [Profile Page]
         |         |
         |         +--> [Static Assets]
         |
         | HTTPS API calls
         |
    [Azure Functions] (C# HTTP Functions)
         |
         +--> [Auth Functions]
         |         +--> Google OAuth validation
         |         +--> JWT generation
         |         +--> Extension auth flow
         |
         +--> [User Management Functions]
         |
         | Cosmos DB SDK
         |
    [Azure Cosmos DB] (SQL API)
         +--> Users Collection
         +--> Projects Collection

[External Services]
    - Google OAuth 2.0 API
    - (Future: Kindle Email API for HiveReader)
```

### Authentication Flow Diagram

**Web Authentication:**
```
User → Click "Sign in with Google" 
     → Redirected to Google OAuth consent
     → User approves
     → Redirected back with auth code
     → Blazor app sends code to /api/auth/google
     → Function validates with Google API
     → Function creates/updates user in Cosmos DB
     → Function returns JWT token
     → Blazor app stores token
     → User authenticated
```

**Chrome Extension Authentication:**
```
User → Click "Sign in" in extension
     → Extension opens new tab: greensquirrel.dev/auth/extension?extensionId=xxx
     → User completes Google OAuth
     → Function validates and creates extension session token
     → Token passed to extension via messaging
     → Extension stores token securely
     → Extension can make authenticated API calls
```

---

## Deployment Strategy

### Infrastructure Setup (Azure)

1. **Azure Static Web Apps (Standard Plan)**
   - Main site: greensquirrel.dev
   - Configure custom domain
   - SSL certificate (automatic via Azure)
   - Enable Azure Functions integration

2. **Azure Functions (Consumption Plan)**
   - Function App in same region as Static Web App
   - Linked to Static Web App for authentication
   - Environment variables for secrets
   - Application Insights for monitoring

3. **Azure Cosmos DB (Serverless or Provisioned)**
   - Recommend serverless for initial deployment
   - SQL API
   - Primary region: (select based on user location)
   - Automatic failover if multi-region desired

4. **Azure Key Vault (Optional but Recommended)**
   - Store Google OAuth client secret
   - Store JWT signing key
   - Function App accesses via managed identity

### CI/CD Pipeline

**GitHub Actions Workflow:**
1. Triggered on push to main branch
2. Build Blazor WASM project
3. Build Azure Functions project
4. Run unit tests
5. Deploy to Azure Static Web Apps (automatically handled by SWA)
6. Deploy Functions if changed
7. Run smoke tests

### Environment Configuration

**Development:**
- Local Blazor WASM with dotnet run
- Azure Functions Core Tools for local Functions
- Cosmos DB emulator OR dev instance
- Google OAuth test credentials

**Production:**
- Azure Static Web Apps
- Azure Functions in production Function App
- Production Cosmos DB
- Production Google OAuth credentials
- Custom domain configured

---

## Development Phases

### Phase 1: Foundation (Weeks 1-2)
**Deliverables:**
- Azure infrastructure provisioned
- Basic Blazor WASM site deployed
- Landing page with placeholder content
- Custom domain configured
- Basic project showcase layout

**Tasks:**
- Set up Azure resources
- Initialize Blazor WASM project
- Create landing page components
- Configure domain DNS
- Basic responsive layout

### Phase 2: Authentication (Weeks 3-4)
**Deliverables:**
- Google OAuth integration working
- Azure Functions auth endpoints deployed
- User creation in Cosmos DB
- JWT token management
- Basic user profile page

**Tasks:**
- Implement Google OAuth flow
- Create auth Azure Functions
- Set up Cosmos DB schema
- Implement JWT generation/validation
- Build sign-in UI components
- Test authentication flow end-to-end

### Phase 3: Content & Polish (Week 5)
**Deliverables:**
- Pace Calculator showcase with screenshots
- HiveReader preview content
- Professional design implementation
- About section content
- Footer with links

**Tasks:**
- Capture and optimize screenshots
- Write project descriptions
- Apply design theme and branding
- Implement animations/transitions
- Create About section
- Add contact information

### Phase 4: Chrome Extension Auth (Week 6)
**Deliverables:**
- Extension authentication flow working
- Extension-specific endpoints
- Test Chrome extension for HiveReader
- Documentation for extension integration

**Tasks:**
- Design extension auth flow
- Implement extension auth endpoints
- Build test extension for validation
- Test token exchange mechanism
- Document extension auth process
- Security review of extension flow

### Phase 5: Testing & Launch (Week 7)
**Deliverables:**
- Comprehensive testing completed
- Performance optimization
- Security review
- Monitoring configured
- Site live at greensquirrel.dev

**Tasks:**
- Cross-browser testing
- Mobile responsiveness testing
- Load testing
- Security audit
- Set up Application Insights
- Configure alerts
- Soft launch to limited audience
- Final review and public launch

---

## Security Considerations

### Authentication Security
- **OAuth tokens:** Validated server-side, never stored
- **JWT tokens:** Signed with strong algorithm (RS256), short expiration
- **Refresh tokens:** Stored securely, single-use if possible
- **Extension tokens:** Additional verification layer, extension ID validation

### API Security
- **CORS:** Restricted to known origins (greensquirrel.dev, subdomains, extension ID)
- **Rate limiting:** Implement on Functions (using Function App built-in or custom)
- **Input validation:** All Function inputs validated and sanitized
- **SQL injection:** N/A with Cosmos DB SDK, but validate all queries

### Data Protection
- **PII minimal:** Only store essential user information from Google
- **Encryption:** Data at rest encrypted by Azure (Cosmos DB, Key Vault)
- **Access control:** Cosmos DB accessed only by Functions, not directly from client
- **Secrets management:** Use Azure Key Vault or Function App settings, never in code

### Privacy Considerations
- **Data collection:** Clearly state what data is collected in privacy policy
- **Google data:** Only request necessary OAuth scopes
- **User deletion:** Provide mechanism for users to delete accounts (future enhancement)
- **Third-party sharing:** No sharing of user data with third parties

---

## Monitoring & Analytics

### Application Insights
- **Frontend:** Blazor WASM telemetry
  - Page views
  - Exception tracking
  - Custom events (sign-in, project clicks)
  
- **Backend:** Azure Functions monitoring
  - Function execution time
  - Success/failure rates
  - Exception tracking
  - Dependency tracking (Cosmos DB, Google API)

### Key Metrics to Track
- Daily/monthly active users
- Sign-in success rate
- Average session duration
- Project click-through rates
- API response times
- Error rates
- Chrome extension authentication success rate

### Alerts
- Function failures exceeding threshold
- API response time degradation
- Authentication failures spike
- Cosmos DB throttling
- Certificate expiration warnings

---

## Future Enhancements

### Phase 2 Features (Post-Launch)
- User dashboard with usage statistics
- Project-specific user settings
- Email notifications (opt-in)
- Dark mode toggle
- Blog section for technical articles
- API documentation page

### HiveReader Specific
- Kindle integration
- Article parsing and formatting
- Reading queue management
- Chrome extension full functionality
- Browser bookmarklet as alternative to extension

### Additional Projects
- Framework for easily adding new projects
- Project categories/tags
- Search functionality across projects
- Featured project rotation on home page

---

## Risks & Mitigations

### Risk 1: Chrome Extension Authentication Complexity
**Impact:** High  
**Likelihood:** Medium  
**Mitigation:** 
- Early prototyping of extension auth flow
- Research best practices from established extensions
- Build simple test extension first
- Plan for alternative auth methods (bookmarklet)

### Risk 2: Google OAuth Changes
**Impact:** High  
**Likelihood:** Low  
**Mitigation:**
- Stay updated on Google Identity Platform changes
- Subscribe to Google developer newsletters
- Implement generic OAuth layer for easier provider swapping
- Monitor deprecation notices

### Risk 3: Azure Cost Overruns
**Impact:** Medium  
**Likelihood:** Low  
**Mitigation:**
- Start with Cosmos DB serverless mode
- Monitor usage via Azure Cost Management
- Set up budget alerts
- Optimize Function execution (cold starts, efficiency)

### Risk 4: Performance Issues with Blazor WASM
**Impact:** Medium  
**Likelihood:** Low  
**Mitigation:**
- Lazy loading for components
- AOT compilation for production
- Optimize bundle size
- Use CDN for static assets
- Performance testing before launch

### Risk 5: Security Vulnerabilities
**Impact:** High  
**Likelihood:** Low  
**Mitigation:**
- Regular dependency updates
- Security scanning in CI/CD
- Follow OWASP best practices
- Third-party security audit (if budget allows)
- Bug bounty program (future consideration)

---

## Success Criteria

### Launch Success
- ✅ Site accessible at greensquirrel.dev with HTTPS
- ✅ Google authentication working with <10 second flow
- ✅ Pace Calculator showcase visible with working link
- ✅ HiveReader preview visible
- ✅ Responsive design works on mobile, tablet, desktop
- ✅ No critical bugs or security issues

### 30-Day Success
- 100+ unique visitors
- 10+ user registrations
- <1% error rate
- 99%+ uptime
- Positive feedback from beta users

### 90-Day Success
- 500+ unique visitors
- 50+ user registrations
- Chrome extension auth tested with HiveReader beta
- At least one new project added or planned
- Analytics showing healthy engagement metrics

---

## Appendices

### Appendix A: Design Guidelines

**Color Palette Suggestions:**
- Primary: Forest Green (#228B22 or similar)
- Secondary: Warm Brown (#8B4513)
- Accent: Bright Orange/Amber (#FFA500)
- Background: Off-white (#F8F9FA)
- Text: Dark Gray (#333333)

**Typography:**
- Headings: Modern sans-serif (e.g., Inter, Roboto, Open Sans)
- Body: Readable sans-serif
- Code: Monospace for any code snippets

**Squirrel Theme Integration:**
- Subtle squirrel icon/logo
- Playful hover effects
- Nature-inspired patterns (leaves, acorns as decorative elements)
- Professional layout with fun micro-interactions

### Appendix B: API Endpoints Reference

**Base URL:** https://greensquirrel.dev/api

| Endpoint | Method | Auth Required | Purpose |
|----------|--------|---------------|---------|
| /auth/google | POST | No | Exchange Google OAuth code for JWT |
| /auth/refresh | POST | Yes | Refresh JWT token |
| /auth/extension/initiate | POST | No | Start extension auth flow |
| /auth/extension/complete | POST | No | Complete extension auth |
| /auth/verify | GET | Yes | Verify JWT token validity |
| /users/me | GET | Yes | Get current user profile |
| /users/me | PUT | Yes | Update user profile |

### Appendix C: Environment Variables

**Azure Functions Configuration:**
```
CosmosDb__ConnectionString = <connection-string>
CosmosDb__DatabaseName = GreenSquirrelDev
Google__ClientId = <google-oauth-client-id>
Google__ClientSecret = <key-vault-reference>
Jwt__Secret = <key-vault-reference>
Jwt__Issuer = https://greensquirrel.dev
Jwt__Audience = https://greensquirrel.dev
Jwt__ExpirationMinutes = 1440
AllowedOrigins = https://greensquirrel.dev,https://hive-reader.greensquirrel.dev
```

### Appendix D: Google OAuth Setup Checklist

- [ ] Create Google Cloud Project
- [ ] Enable Google+ API
- [ ] Create OAuth 2.0 credentials
- [ ] Configure authorized redirect URIs:
  - https://greensquirrel.dev/auth/callback
  - https://greensquirrel.dev/auth/extension
  - http://localhost:5000/auth/callback (for development)
- [ ] Configure authorized JavaScript origins:
  - https://greensquirrel.dev
  - http://localhost:5000 (for development)
- [ ] Request only necessary scopes (profile, email)
- [ ] Set up OAuth consent screen
- [ ] Add privacy policy URL
- [ ] Add terms of service URL

---

## Document Control

**Version History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-13 | Green Squirrel Dev | Initial PRD |

**Approvals:**

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Product Owner | [Your Name] | | |
| Technical Lead | [Your Name] | | |

**Review Schedule:**
- Weekly during development (Phases 1-5)
- Monthly post-launch for first quarter
- Quarterly thereafter

---

## Contact & Questions

For questions or clarifications about this PRD:
- **Email:** [your-email]
- **GitHub:** [repository-link if applicable]

This document is a living document and will be updated as requirements evolve and new information becomes available.

# Project Walkthrough: QuanTro (Motel Management System)

QuanTro is a comprehensive property management solution designed for small to medium-sized motel owners. It provides an end-to-end workflow from infrastructure setup and tenant on-boarding to automated billing and real-time support requests.

---

## 1. Detailed Database Schema

The system utilizes a highly NORMALIZED SQL Server design to ensure performance and data integrity.

| Category | Table Name | Description & Key Fields |
| :--- | :--- | :--- |
| **User & Auth** | `Roles` | Permission levels (Superuser, Admin, Tenant). |
| | `Users` | Credentials, BCrypt Hashing, Status, Avatars. |
| **Infrastructure** | `Motels` | Property info, addresses, ownership. |
| | `Floors` | Floor organization within properties. |
| | `Rooms` | Unit details, codes, status (Vacant/Occupied). |
| **Pricing & Services**| `RoomSettings` | Rent, Deposit, Standard Occupancy, Surcharges. |
| | `Services` | Global system utilities (Electricity, Water, Wifi...). |
| | `RoomServiceSettings`| PER-ROOM service config (Toggle, Custom Price). |
| **Tenants & Contracts**| `Tenants` | Detailed profiles (ID Card, Address, DOB, Gender). |
| | `RoomOccupants` | Real-time staying list (Primary vs Member). |
| | `Contracts` | Legal agreements, effective dates, fixed pricing. |
| **Finance** | `MeterReadings` | Utility consumption (Prev, Current, Date). |
| | `Invoices` | Monthly summaries (Rent + Utilities). |
| | `InvoiceDetails` | Breakdown of each line item in an invoice. |
| | `Payments` | Transaction and payment history. |
| **Interaction** | `Requests` | Maintenance, complaints, and tenant requests. |
| | `Notifications` | System alerts and real-time SignalR notifications. |

---

## 2. Development History & Milestone Logs

### Phases 1-13: Core Infrastructure, APIs & Dashboard UX

- Implemented the full Database Schema with EF Core.
- Built automated billing logic and robust RBAC security (JWT, BCrypt, HttpOnly Cookies).
- Developed a Premium Glassmorphism Single Page Application (SPA) dashboard.
- Resolved circular reference issues by utilizing DTO patterns across all services.
- Added Serilog for structured logging and Swagger/OpenAPI for documentation.

### Session 14-16: Core Business Flow & Visual Mapping

- **Floor Map Generation**: Redesigned the motel management module into a visual floor map grid. Dynamic room status syncing based on active Contracts.
- **Contract Lifecycle Management**: Added manual termination workflow and room clearing.
- **Automated Grace Period Expiring**: Implemented `ContractExpirationService` background worker running hourly.

### Session 17-20: UI/UX Optimization & Logic Refinement

- **Dual Meter Reading Modal**: Overhauled the modal into a dual-input form showing both Electricity and Water fields.
- **Infinite Redirect Loop Resolution**: Resolved conflicts between localStorage and JWT Cookies.
- **Soft Termination**: Changed contract termination logic to preserve history (status: Terminated/MovedOut) instead of hard-deleting records.

### Session 21: Runtime Error Resolution

- **Non-floor Properties**: Updated Backend and Frontend to display direct room lists for properties without floors.
- **Defensive JS**: Implemented comprehensive null-safety checks to prevent DOM access crashes.
- **Auth Flow**: Added interceptors to automatically redirect to Login upon 401/403 API responses.

### [2026-05-05] - Session 22: Automated Contract Management

- **Grace Period Mechanism**: Expired contracts transition to "Waiting" status for 7 days before full termination, preserving final invoices.

### [2026-05-05] - Session 23: Advanced Billing & Exports

- **Invoice Calculation**: Advanced formula involving base rent, per-capita utility consumption, mandatory/optional services, and partial payment carry-overs.
- **Automated Notifications**: `IEmailService` sends initial billing notifications. Background `InvoiceReminderWorker` automatically dispatches daily overdue warnings.
- **Role-Based Workflows**: Segregated capabilities ensuring only admins mutate financial models while tenants possess restricted, view-centric proxies.

## 6. Document Generation & Automation

- **Printable Contracts**: `ContractService` integrates with `PrintContract.cshtml` to compile robust lease agreements natively formatted for A4 physical printing. Eliminates need for heavy PDF-generation libraries in favor of native CSS `@media print` paradigms.
- **Excel Exports**: `ClosedXML` utilization in `ExportService` allowing bulk administrative archiving of localized financial data.

### [2026-05-05] - Session 24-25: Refactoring & Occupant Management
- **Modularization**: Decoupled `Index.cshtml` and monolithic JS into maintainable modules.
- **Add Occupant Modal**: Built the logic to easily add roommates and dynamically calculate Extra Occupant Surcharges.

### [2026-05-07] - Session 26: Tenant Portal Re-architecture & Notifications
- **Modular Tenant UI**: Refactored profiles into sections and added pages for Room, Invoices, and Support.
- **Global Toast System**: Implemented `showPremiumToast` for system-wide, real-time feedback.
- **Build Fixes**: Synchronized naming conventions (`DefaultPrice`/`UnitPrice`) and eliminated null reference warnings.

### [2026-05-07] - Session 27: Tenant Dashboard Completion
- **Tenant Dashboard UX**: Built a modern dashboard hub with sidebars and summary widgets for tenants.
- **API Fixes**: Resolved routing errors and 403 permission bugs for invoice viewing.
- **Hardening**: Enhanced the user ID extraction logic from JWT tokens.

### [2026-05-08] - Session 28: Security Audit (JWT)
- **Current State Analysis**: Audited JWT implementation across API, MVC, and SignalR components.
- **Key Findings**: JWT is successfully implemented using HttpOnly Cookies combined with robust Policy-based Authorization.
- **Security Roadmap**: Identified post-release security enhancements: Refresh Token logic, Token Revocation (Blacklisting), and CSRF protection measures.

### [2026-05-09] - Session 29: QR Payment Integration & Stabilization
- **QR Payment Portal**: Integrated VietQR dynamic generation and secure payment proof upload flow.
- **Data Synchronization**: Enforced camelCase API standards and fixed 404/Undefined errors across modules.
- **Personalized Notifications**: Added notification bell and list, secured by JWT identity.
- **UX Refinements**: Implemented automatic state transitions (Pending status) based on invoice lifecycle.

### [2026-05-11] - Session 30: Admin Dashboard UI Standardization & Layout Bug Fixes
- **Contracts Module Layout**: Replaced the legacy `<table>`-based layout with a Bootstrap `row g-4` card grid.
- **Root Cause Bug Fix**: Applied defensive null-safety guards in `contracts.js` to prevent crashes.
- **Refactoring**: Refactored `renderGlobalMotelSelector` to target module-specific list containers.
- **Floor Map Sync**: Automatically read `window._globalSelectedMotelId` to render rooms immediately upon property selection.
- **Cache Pre-loading**: Fetched motels preemptively to prevent blank UI states.

### [2026-05-12] - Session 31: Motel Setup UI Overhaul & Complete CRUD
- **Comprehensive Motel CRUD**: Implemented full Create, Read, Update, and Delete capabilities for Motels, Floors, and Rooms with secure API calls and dynamic, seamless DOM updates.
- **Safe Deletion Protocol**: Enforced strict backend constraints; prevented the deletion of Floors and Rooms if they are currently occupied, safeguarding active contract data integrity.
- **Tabbed Interface Refactor**: Completely overhauled the Motel Setup module (`_MotelSetupModule.cshtml`). Transitioned the bulky Floor and Room declaration forms into a modern, compact Tab layout, maximizing screen real estate and eliminating scrolling fatigue.
- **Pagination & Filtering Engine**: Developed robust client-side Javascript logic for room list pagination (10 items/page), complemented by real-time search functionality and floor-based filtering.
- **Bulk Room Generation**: Introduced a bulk-add utility that automatically generates multiple rooms with sequentially incremented codes (e.g., 101, 102, 103...), drastically cutting down initial data entry overhead for large properties.
- **Workflow & UI Polish**: Removed the isolated "Edit Contract" button from the Floor Map to centralize all contract lifecycle management into the dedicated Contracts module. Customized CSS to ensure inactive Tab buttons stand out against the translucent Glassmorphism theme.

### [2026-05-12] - Session 32: Business Logic Optimization & Conflict Resolution
- **Contract Termination & Archiving**: Updated the contract termination logic. The system now deletes the tenant's login account (User record) to block access while preserving all profiles (Tenant), Contracts, and Invoices in the DB for historical auditing (Archiving).
- **Invoice Cancellation & Correction**: Added a "Cancel Invoice" feature for unpaid bills. This allows Admins to delete erroneous invoices, fix meter readings, and regenerate the correct bill.
- **Reading Correction**: Enabled the deletion of incorrect meter readings if the corresponding monthly invoice has not yet been generated.
- **Partial Payment Verification**: Upgraded the QR Payment verification flow. Admins can now input the "Actual Amount Received" from the payment proof. The system records the partial payment and updates the status to "Partially Paid" if the balance is not zero, instead of defaulting to 100% paid.

---

### [2026-05-15] - Session 33: Profile Synchronization & Premium Header Redesign
- **Phone Persistence Fix**: Resolved a critical bug where phone numbers were erased during Identity Profile updates by ensuring field inclusion in the JS payload.
- **Bi-directional Data Sync**: Enhanced `UserService` and `TenantService` to automatically propagate changes between `Users` and `Tenants` tables.
- **Premium Header Overhaul**: Redesigned the top header for both Admin and Tenant dashboards with glassmorphism, fixed alignment, and interactive profile badges.
- **Interactive Feedback (Toasts & Loading)**: Integrated `showPremiumToast` and button loading states to provide real-time visual feedback on user actions.
- **Backend Robustness**: Implemented null-safe partial updates in the service layer to prevent accidental data erasure during profile saves.

### [2026-05-15] - Session 34: JWT Security System (Refresh Token & Revocation)
- **Refresh Token Rotation**: Implemented a secure 64-byte Refresh Token mechanism stored in the Database.
- **CSRF Protection**: Applied `SameSite=Strict` Cookie for the Refresh Token with an isolated API path.
- **Token Revocation**: Integrated revocation logic (nullifying DB token) upon user Logout.
- **Lifespan Upgrade**: Reduced Access Token lifespan from 6 hours to 15 minutes to minimize theft risks.
- **Real-time Status Validation (Token Delay Fix)**: Updated `OnTokenValidated` in Program.cs to query User status instantly, blocking banned tenants immediately rather than waiting for token expiration.

### [2026-05-15] - Session 35: Revenue Analytics Chart (Dashboard)
- **Backend API**: Created the `/api/Invoice/RevenueChart` endpoint to aggregate financial data for the last 6 months, automatically calculating Total Expected (issued invoices) and Total Collected (paid invoices).
- **Frontend Dashboard**: Integrated `Chart.js` for a Bar Chart with dual parallel columns. Placed the chart directly on the Admin Overview page for dynamic financial trend analysis, styled cohesively with the transparent Glassmorphism theme.

### [2026-05-15] - Session 36: Frontend Architecture Hardening & Asset Localization
- **Modular JavaScript**: Extracted over 500 lines of inline JS from layout files into dedicated modules (`admin-ui.js`, `tenant-notifications.js`, etc.), improving load times and maintainability.
- **Asset Localization**: Eliminated CDN dependencies by self-hosting Bootstrap Icons, Chart.js, and SignalR within `wwwroot/lib` for offline reliability.
- **Dark Mode Optimization**: Conducted a full UI audit and fixed "hidden text" accessibility issues across Floor Map, Overview, and Meter Reading modules.
- **Error Handling Standardization**: Unified the global notification system with premium toasts and standardized catch-block logging.

### [2026-05-16] - Session 36: Theme System Design & Silent Refresh
- **Centralized Theme System**: Introduced `theme-system.css` for global management of design tokens (Colors, Shadows, Gradients). Cleaned up legacy CSS in `glassmorphism.css` and `premium_dashboard.css`.
- **Sidebar UX Fix**: Implemented independent scrolling for the sidebar navigation to ensure accessibility of the logout button.
- **Security Hardening (Silent Refresh)**: Implemented a Global Fetch Interceptor to handle automatic `/RefreshToken` calls, preventing session interruptions.
- **Workflow Cleanup**: Removed redundant "Quick Initialize" button from the Billing module.

## 🚀 Future Roadmap: Smart Metering Ecosystem
*Vision locked, implementation scheduled after current system stabilization:*
1. **Phase 1:** DB & Storage upgrade for proof-of-reading images.
2. **Phase 2:** AI/OCR Integration (Tesseract.js) for scanning meter digits.
3. **Phase 3:** Tenant Self-Service portal for transparent submissions.
4. **Phase 4:** Full IoT Integration for smart electric/water meters.

---

## 3. Current System Status

**Last Updated**: 2026-05-15
**By**: Antigravity (AI)
**Status**: ✅ Stable — Full User Experience (UX) optimization, Security hardening, and Data synchronization complete.

**Completed Features:**
- [x] Authentication & RBAC (JWT Cookie)
- [x] Motel, Floor, and Room Setup
- [x] Visual Floor Map with Global Motel Selector
- [x] Tenant Account Registration
- [x] 3-Step Contract Generation Wizard
- [x] Global Utility Services Configuration
- [x] Dual-Input Meter Reading Form
- [x] Automated Invoice Calculation
- [x] Tenant Portal (Invoices & Requests)
- [x] Background Cleanup for Expired Contracts
- [x] QR Payment Portal (VietQR + Proof Upload)
- [x] SignalR Real-time Notifications
- [x] Unified Card Grid UI across all Admin modules
- [x] UI Optimization for Motel Setup (Tabbed Layout, Pagination, Bulk Add)
- [x] Bi-directional Profile Data Synchronization
- [x] Premium Header Redesign & Interactive Toast Notifications
- [x] JWT Refresh Token & Revocation Mechanism
- [x] Advanced Revenue Analytics & Charts

**Pending / Future Enhancements (Bug Fixing Priority):**
- [ ] Fix "Empty Room" title visibility in Dark Mode (color clash).
- [ ] Comprehensive Modal Audit for Design System consistency.
- [ ] Implement Smart Metering Ecosystem (post-stabilization).
- [ ] Mobile Application (Flutter/React Native).

---

## 4. Refactoring & Bug Fix Logs

*(Detailed refactoring steps regarding DRY Principles, JS Module Separation, DTO extraction, and Database Constraint Audits have been completed and are documented in the version history.)*

*This document is continuously updated to reflect the project's evolution.*

---

## 5. Summary of Antigravity's (AI) Role in the Project

As an Agentic AI Coding Assistant, Antigravity has played a core role in architecting, developing, and optimizing the QuanTro system from the ground up. Below is a detailed summary of the main contributions:

### 🚀 Achievements & Developed Features
- **Comprehensive Management System**: Completed the business logic for managing Motels, Floors, Rooms, Tenants, and Services with strict data integrity constraints.
- **Business Automation**: Built complex billing algorithms (including prorated rent calculation based on actual days stayed), automated contract lifecycles, and room status transitions.
- **Tenant Portal & Admin Dashboard**: Successfully developed dedicated portals for both Managers (Admins) and Renters (Tenants) allowing independent and secure operations.
- **VietQR Payment Integration**: Integrated dynamic QR code generation and payment proof upload workflows, streamlining the reconciliation process.
- **Business Logic Optimization**: Resolved logical conflicts between invoices, readings, and payments, making the system more flexible and resilient to real-world scenarios.

### 🛠 Technologies Utilized
- **Backend**: C# .NET 8 (ASP.NET Core MVC & Web API), Entity Framework Core (EF Core) for ORM, LINQ.
- **Database**: SQL Server with a highly normalized schema architecture.
- **Frontend**: Vanilla Javascript (Modularized), HTML5, CSS3, Bootstrap 5.
- **Security**: JSON Web Tokens (JWT) stored via HttpOnly Cookies, BCrypt (Password Hashing), Role-Based Access Control (RBAC).
- **Utilities**: SignalR (WebSockets for Real-time Notifications), Serilog (Structured Logging), ClosedXML (Excel Export).

### ⚡ Optimization Strategies
- **Codebase Refactoring (Maintainability)**:
  - Decomposed a massive monolithic `Index.cshtml` into over 15 separate Partial Views to improve developer experience.
  - Extracted inline JavaScript functions into 10 distinct module files, strictly adhering to Separation of Concerns (SoC).
  - Split `MotelEntities.cs` into 15 individual entity models and extensively applied DTO patterns to prevent Circular References and protect sensitive data payload.
- **UX/UI Optimization**:
  - Implemented a **Glassmorphism** design philosophy paired with a unified **Card Grid Layout** to deliver a modern, premium feel.
  - Maximized screen real estate by utilizing Tabs, Modals, and seamless DOM manipulations without requiring full page reloads.
- **Performance & System Hardening**:
  - Applied Unique Index constraints at the Database level to prevent data duplication (e.g., forbidding duplicate room codes within the same property).
  - Developed the `ContractExpirationService` Background Worker to automate contract state transitions without blocking the main application thread.
  - Implemented comprehensive Null-safety checks across all JavaScript modules to prevent silent runtime crashes.

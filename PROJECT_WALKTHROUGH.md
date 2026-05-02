# Project Walkthrough: QuanTro (Motel Management System)

QuanTro is a comprehensive property management solution designed for small to medium-sized motel owners. It provides an end-to-end workflow from infrastructure setup and tenant on-boarding to automated billing and real-time support requests.

---

## 1. Detailed Database Schema

The system is built on a robust SQL Server foundation with 16+ interconnected tables ensuring data integrity and business rule enforcement.

### 1.1. User Management & Authorization
- **Roles**: Defines access levels: superuser, admin, tenant.
- **Users**: Authentication and security state.

### 1.2. Infrastructure Group
- **Motels**: Property-level information.
- **Floors**: Floor organization within a property.
- **Rooms**: Individual unit tracking.

### 1.3. Business & Pricing Group
- **RoomSettings**: Core rental rules and pricing per room.
- **Services**: Global catalog of utilities (Electricity, Water, etc.).

### 1.4. Tenants & Contracts Group
- **Tenants**: Comprehensive tenant profiles.
- **RoomOccupants**: Real-time tracking of room occupancy.
- **Contracts**: Legal rental agreements (Start Date, End Date, Rent, Deposit).

### 1.5. Billing & Finance Group
- **MeterReadings**: Utility consumption tracking.
- **Invoices**: Monthly billing statements.
- **Payments**: Transaction history.

### 1.6. Interaction Group
- **Requests**: Tenant support tickets and maintenance requests.
- **Notifications**: System alerts and real-time feedback.

---

## 2. Development History & Milestone Logs

### Phases 1-13: Core Infrastructure, APIs & Dashboard UX
- Implemented the full Database Schema with EF Core.
- Built automated billing logic and robust RBAC security (JWT, BCrypt, HttpOnly Cookies).
- Developed a Premium Glassmorphism Single Page Application (SPA) dashboard.
- Resolved circular reference issues by utilizing DTO patterns across all services.
- Added Serilog for structured logging and Swagger/OpenAPI for documentation.
- Built comprehensive global utility services management (Electricity, Water, Trash, Wifi).

### Session 14-16: Core Business Flow & Visual Mapping
- **Floor Map Generation**: 
  - Redesigned the motel management module into a visual floor map grid. 
  - Dynamic room status syncing based on active Contracts (Vacant vs Occupied).
  - Side panel for quick financial and mandatory utility checks.
- **Contract Lifecycle Management (Hard Delete)**:
  - Added manual termination workflow that cascades and permanently purges all related Invoices, Requests, Occupants, Contracts, and User accounts, resetting the room to Vacant.
- **Automated Grace Period Expiring**:
  - Implemented `ContractExpirationService` background worker running hourly. Expired contracts enter a 7-day grace period before being completely purged from the system.
- **Invoice & Billing Generation**:
  - Upgraded the Admin Invoice module to display all occupied rooms.
  - Implemented the `generateInvoice` backend algorithm to automatically calculate the total sum (Rent + Services) and issue an invoice.
  - Tenant Portal accurately fetches and displays only the authorized user's invoices.

### Session 17: UI/UX Optimization for Utility Readings (Latest)
- **Dual Meter Reading Modal**: 
  - Following the user's request, the "Record Meter" (Ghi chỉ số) modal was overhauled.
  - Replaced the single-service dropdown with a dual-input form showing both Electricity and Water fields side by side.
  - Automatically fetches the previous month's readings for both utilities.
  - Submits both readings simultaneously via `Promise.all` for a seamless and highly efficient user experience.

---

## 3. Current System Status

**Last Updated**: 2026-05-02
**By**: Antigravity (AI)
**Status**: ✅ Stable — Core workflows are fully functional and ready for production staging.

**Completed Features:**
- [x] Authentication & RBAC (JWT Cookie)
- [x] Motel, Floor, and Room Setup
- [x] Visual Floor Map with Real-time Sync
- [x] Tenant Account Registration
- [x] 3-Step Contract Generation Wizard
- [x] Global Utility Services Configuration
- [x] Dual-Input Meter Reading Form
- [x] Automated Invoice Calculation
- [x] Tenant Portal (Invoices & Requests)
- [x] Background Cleanup for Expired Contracts

**Pending / Future Enhancements:**
- [ ] Online Payment Gateway Integration (VNPay, Momo)
- [ ] Advanced Revenue Analytics & Charts
- [ ] Mobile Application (Flutter/React Native)

*This walkthrough is a living document and will be updated as the project evolves.*

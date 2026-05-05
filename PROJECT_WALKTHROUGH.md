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

### Session 17: UI/UX Optimization for Utility Readings
- **Dual Meter Reading Modal**:
  - Overhauled the "Record Meter" modal into a dual-input form showing both Electricity and Water fields side by side.
  - Submits both readings simultaneously for a more efficient admin workflow.

### Session 18: System Stability & Redirect Loop Fix
- **Infinite Redirect Loop Resolution**:
  - Resolved conflicts between `localStorage` and `JWT Cookies` in `auth.js`.
- **Process Management & Startup Optimization**:
  - Fixed "Address already in use" by purging zombie `dotnet` processes.

### Session 20: Contract-First Workflow & Data Preservation (Latest)
- **Invoice Decoupling**: Refactored `InvoiceCalculationService` to calculate bills based on room settings and occupants, making the formal contract object an optional reference.
- **"Contract First" Enforcement**: Restricted adding occupants to rooms without active contracts.
- **Automated Check-in**: Primary tenants are now automatically added to the room occupancy list upon contract signing.
- **Soft Termination**: Changed contract termination logic to preserve history (status: Terminated/MovedOut) instead of hard-deleting records.
- **Build Fixes**: Resolved CS0117 error by adding `ServiceId` to `MeterReadingResponse` DTO.
- **Mid-Month Logic**: Fixed calculation errors for contracts starting mid-month.

---

## 3. Current System Status

**Last Updated**: 2026-05-04
**By**: Antigravity (AI)
**Status**: ✅ Stable — System hang and redirect loop issues resolved.

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

---

## 4. Refactoring & Bug Fix Logs

### [2026-05-04] — DRY Principle Refactoring: Access Control Centralization

**Issue**: Authorization methods like `IsSuperuser`, `CanAccessRoom`, and `CanAccessInvoice` were manually duplicated across multiple service files (`MotelService`, `InvoiceService`, `PaymentService`, `MeterReadingService`, etc.). This violated the DRY (Don't Repeat Yourself) principle and created significant security maintenance risks.

**Solution (Refactoring Step 1)**:
1. **Created Centralized Service**: Introduced `IAccessControlService` and `AccessControlService` as the single source of truth for authorization checks.
2. **Consolidated Logic**: Migrated `IsSuperuserAsync`, `IsAdminOfMotelAsync`, `IsAdminOrSuperAsync`, `CanAccessRoomAsync`, and `CanAccessInvoiceAsync` into the new service.
3. **DI Registration**: Registered `IAccessControlService` in `Program.cs`.
4. **Removed Redundant Code**: Updated 6 different services (`AdminService`, `TenantService`, `MotelService`, `InvoiceService`, `MeterReadingService`, `PaymentService`) by removing their private duplicate methods and injecting the new centralized service.
5. **Verified Stability**: Achieved 0 compilation errors. API routes and business behaviors remained 100% unchanged.

### [2026-05-04] — Refactoring Step 2: Service Synchronization & Invoice Logic Fix

**Issue**:
1. `InvoiceCalculationService` was incorrectly calculating costs for *all* globally active services, disregarding whether the service was explicitly enabled for a specific room via `RoomServiceSettings.IsActive`.
2. When an Admin created a new Global Service, existing rooms were not automatically provisioned with a corresponding `RoomServiceSetting` record, causing errors and missing services when modifying existing contracts.

**Solution**:
1. **Invoice Calculation Fix**: Updated `InvoiceCalculationService` to only calculate services that are both globally active AND active at the room level. The unit price remains tied to the global `Service.DefaultPrice` to ensure centralized pricing control.
2. **Occupancy Service Fix**: Updated `CreateContractAsync` and `UpdateContractAsync` in `OccupancyService.cs`. The system now actively scans all global services when creating or updating a contract. If a room lacks a `RoomServiceSetting` for any global service, it automatically initializes it. This guarantees rooms always have complete service configurations.
3. System compiled with 0 errors. No UI or API route changes were necessary.

### [2026-05-04] — Refactoring Step 3: Financial Data Preservation (Soft-Termination)

**Issue**: The `ContractExpirationService` was configured to perform hard-deletes of `Invoices`, `Payments`, `Tenants`, and `Users` once a contract remained in the `Waiting` state for over 7 days. This destructive behavior violates standard accounting practices by deleting historical financial records.

**Solution**:
1. Changed behavior to **Soft-Termination**: Instead of deletion, contracts are now moved to the `Terminated` status.
2. Kept Financials: `Invoices` and `Payments` remain untouched for revenue reporting. `Tenants` and `Users` are kept to preserve historical occupant records.
3. Released Resources: Only the physical room occupancy is affected. The room status is set back to `Vacant`, and `RoomOccupants` are cleared out to make way for new tenants.

### [2026-05-04] — Refactoring Step 4: Frontend JavaScript Modularization

**Issue**: The `Views/Admin/Index.cshtml` file had become a massive monolithic file (nearly 3,000 lines), containing over 1,700 lines of inline JavaScript intermingling over 60 functions. This heavily violated Separation of Concerns (SoC) and presented a huge maintenance risk.

**Solution**:
1. Extracted JS Modules: Utilized a custom Python script to automatically parse and safely extract the JavaScript logic into **10 separate module files** located at `wwwroot/js/admin/` (e.g., `admin-core.js`, `contracts.js`, `billing.js`).
2. Maintained Compatibility: Exposed all extracted functions to the `window` object (e.g., `window.switchModule = switchModule`) to preserve existing inline HTML `onclick` bindings without breaking the UI.
3. Cleaned Up Razor View: Replaced the massive script block in `Index.cshtml` with concise `<script src="...">` includes, reducing the file size to roughly 1,200 lines.
4. The system compiled with 0 errors and no changes to backend API routes or UI behavior were required.

### [2026-05-04] — Refactoring Step 5: Data Model Modularization

**Objective**: Improve Separation of Concerns (SoC) and code maintainability at the Data Models layer.

**Solution**:
1. Organized Structure: Created a new directory `Models/Entities/` to host domain-specific entities.
2. Split Monolith: Decomposed the large `Models/MotelEntities.cs` (over 400 lines) into **15 individual entity files** (e.g., `Motel.cs`, `Room.cs`, `Invoice.cs`, etc.).
3. **Preserved Namespace**: Maintained the original `UserManagementSystem.Models` namespace in all new files. This ensured full backward compatibility and avoided breaking any references in existing Controllers or Services.
4. Safe Refactor: Ensured no changes were made to the database schema, migrations, or business logic.
5. Cleanup: Deleted the now-obsolete `Models/MotelEntities.cs` file.
6. The system compiled with **0 errors**.

### [2026-05-04] — Refactoring Step 6: DTO & Service Interface Modularization

**Objective**: Enhance Separation of Concerns (SoC) for DTOs and Service Contracts (Interfaces).

**Solution**:
1. **Interface Reorganization**: Created a `Services/Interfaces/` directory and decomposed the consolidated `IBillingAndRequestServices.cs` into individual files: `IMeterReadingService.cs`, `IInvoiceCalculationService.cs`, `IInvoiceService.cs`, `IPaymentService.cs`, and `IRequestService.cs`.
2. **DTO Reorganization**: Created a `Models/DTOs/` directory and redistributed classes from `MotelManagementDTOs.cs` and `BillingAndRequestDTOs.cs` into functional groups: `AdminDTOs.cs`, `TenantDTOs.cs`, `MotelDTOs.cs`, `RoomDTOs.cs`, `ServiceDTOs.cs`, `ContractDTOs.cs`, `BillingDTOs.cs`, and `RequestDTOs.cs`.
3. **Namespace Stability**: Maintained original namespaces (`UserManagementSystem.Services` and `UserManagementSystem.Models`) to ensure absolute compatibility across the codebase.
4. Consistent Logic: Zero changes to business logic, API routes, or database schemas.
5. Cleanup: Deleted all now-obsolete monolithic files.
6. The system compiled with **0 errors**.

### [2026-05-04] — Refactoring Step 7A: Extracting Global Services

**Objective**: Reduce monolithic dependency on `MotelService` and adhere to the Single Responsibility Principle (SRP).

**Solution**:
1. **Logic Separation**: Successfully migrated system-wide service management (Get, Create, Update, Seed) from `MotelService` to the newly created `GlobalServiceService`.
2. **Auth Optimization**: Replaced manual database user/role queries with `IAccessControlService.IsSuperuserAsync`, ensuring cleaner and more consistent permission checks.
3. **Notification Update**: Refined notification logic to target the correct role identifier ("tenant") when global prices change.
4. **Behavior Integrity**: Maintained full API route and frontend compatibility.
5. The system compiled with **0 errors**, and all admin JS files passed syntax validation.

### [2026-05-04] — Refactoring Step 7B: Extracting Room and RoomSetting Logic

**Objective**: Modularize room-specific management logic, separating it from general property (motel) management.

**Solution**:
1. **Logic Migration**: Successfully moved all room-related methods (`CreateRoom`, `UpdateRoom`, `UpdateRoomSetting`, `GetRoomSettings`, `GetRoomServices`, `GetRoomOccupants`) to the new `RoomManagementService`.
2. **Refined GetRoomServices**: Updated `GetRoomServicesAsync` to join the `Services` table (global category & pricing) with `RoomServiceSettings` (per-room activation toggles). This ensures accurate `IsActive` status for each room. Pricing Rule: `UnitPrice` in `RoomServiceSettings` is strictly auxiliary; the system always prioritizes `Services.DefaultPrice` during invoice calculation to maintain global pricing consistency.
3. **MotelService Cleanup**: Removed unused dependencies (`INotificationService`, `IConfiguration`) and fields from `MotelService`, streamlining it to focus solely on Motel and Floor operations.
4. **API Stability**: Maintained all original endpoints in `MotelManagementController` with zero changes to routes or frontend behavior.
5. The project compiled with **0 errors**.

### [2026-05-04] — Step 8: Database Audit and Safety Constraints

**Objective**: Enhance data integrity at the physical layer, preventing application-level logical errors.

**Solution**:
1. **Migration**: Generated and applied the `AddSafeDatabaseConstraints` migration to introduce unique indices.
2. **Rooms**: Added a unique index on `(MotelId, RoomCode)` to prevent duplicate room numbers within the same property.
3. **Services**: Added a unique index on `ServiceCode` to ensure unique service identifiers system-wide.
4. **RoomServiceSettings**: Added a unique index on `(RoomId, ServiceId)` to ensure one configuration record per service per room.
5. **MeterReadings**: Added a unique index on `(RoomId, ServiceId, BillingMonth, BillingYear)` to prevent duplicate readings for the same period.
6. **Contracts**: Implemented a **Filtered Unique Index** on `RoomId` where `ContractStatus = 'Active'`, ensuring a maximum of one active contract per room.
7. The system compiled successfully with **0 errors**.

### [2026-05-04] — Step 9: Adding Test Project for Core Business Logic

**Objective**: Ensure core services (Invoice, Payment, Room, Access Control) behave correctly after multiple refactoring steps.

**Solution**:
1. **Setup**: Created `UserManagementSystem.Tests` project using xUnit and SQLite In-memory to respect the new database constraints.
2. **InvoiceCalculationService**: Verified global pricing rules, room-specific service exclusion, and error handling for missing meter readings.
3. **PaymentService**: Ensured payments cannot exceed the remaining invoice balance.
4. **RoomManagementService**: Validated accurate retrieval of room-specific service activation status.
5. **AccessControlService**: Tested superuser bypass and tenant-level room access restrictions.
6. **Project Isolation**: Configured `UserManagementSystem.csproj` to properly exclude test files from the production build.

### [2026-05-04] - Step 10: Floor Map UI Stability & Runtime Error Resolution

**Issue**: The Motel Floor Map was failing to load or crashing due to API inconsistencies (null handling for non-floor properties) and JavaScript errors (accessing classList on null elements).

**Solution**:
1. **API Refinement**: Updated `MotelService.cs` to properly map `Rooms` for properties without floors.
2. **Defensive Programming**: Implemented comprehensive null-safety checks in `admin-core.js`, `billing.js`, and `motels-floormap.js`.
3. **Session Management**: Added global interceptors for 401/403 errors and updated the logout flow to properly clear server-side sessions.
4. **UI Fixes**: Synchronized `Index.cshtml` with JS selectors to prevent element-not-found errors.

### [2026-05-05] - Step 11: Automated Contract Expiration Management (Background Task)

**Objective**: Automate the lifecycle of rental contracts to ensure rooms are freed up and tenant statuses are updated immediately upon contract expiration.

**Solution**:
1. **Background Service**: Implemented `ContractExpirationService` running hourly.
2. **Expiration Logic**: Automatically transitions "Active" contracts to "Waiting" status when they expire, resetting room status to "Vacant" and occupants to "MovedOut".
3. **Grace Period**: Implemented a 7-day "Waiting" period before final "Termination" to preserve historical financial data and allow for final adjustments.
4. **Monitoring**: Integrated detailed Serilog logging for background task execution.

**Files Created/Modified**:
- `Services/BackgroundTasks/ContractExpirationService.cs`
- `Program.cs` (Service registration)

### [2026-05-05] - Step 12: Advanced Billing & Prorated Rent Logic

**Objective**: Implement professional-grade billing features, including automated meter rollover, prorated rent calculation for new tenants, and Excel export capability.

**Solution**:
1. **Prorated Rent System**:
    - Enhanced `InvoiceCalculationService` with logic based on the contract start date:
        - Stay duration $\le$ 7 days: 0% Rent (Free).
        - Stay duration 8-15 days: 50% Rent.
        - Stay duration > 15 days: 100% Rent.
    - Utilities (Electricity/Water) remain strictly metered based on actual consumption.
2. **Automated Meter Rollover**: Refined `MeterReadingService` to automatically fetch the previous month's final reading as the current month's starting point, reducing manual entry errors.
3. **Excel Integration**:
    - Integrated `ClosedXML` library to generate dynamic Excel workbooks.
    - Added an `ExportExcel` endpoint to provide downloadable .xlsx invoices with professional styling and detailed cost breakdowns.
4. **UI Enhancement**: 
    - Implemented `invoiceDetailsModal` for instant invoice preview.
    - Updated `billing.js` to handle real-time usage calculation and seamless Excel downloads.

**Files Created/Modified**:
- `Services/InvoiceCalculationService.cs`
- `Services/InvoiceService.cs`
- `Controllers/InvoiceController.cs`

### [2026-05-05] - Step 13: Frontend Modularization (Partial Views)

**Objective**: Decouple the monolithic `Index.cshtml` into smaller, reusable partial views to improve maintainability and scalability.

**Solution**:
1. **Implementation of Partial Views**: Extracted all major UI sections and modal groups into standalone `.cshtml` files.
2. **Directory Organization**:
    - Created `Views/Admin/Partials/Modules/` for feature-specific sections (Billing, Floor Map, etc.).
    - Created `Views/Admin/Partials/Modals/` for organized modal management.
3. **Preservation of Logic**: Maintained all existing IDs and class selectors to ensure full compatibility with the existing JavaScript module system.
4. **Result**: Reduced `Index.cshtml` from 1200+ lines to ~100 lines, significantly improving developer experience and reducing the risk of merge conflicts.

**Files Created/Modified**:
- `Views/Admin/Index.cshtml` (Skeleton)
- 14 new partial view files in the `Partials` directory.

### [2026-05-05] - Step 14: Billing Sync, Query Fixes, and Occupancy Enhancements

**Objective**: Ensure perfectly accurate monthly invoices based strictly on contract terms, fix critical database query exceptions, and refine occupant tracking for extra-person surcharges.

**Solution**:
1. **Invoice Calculation Precision**: 
    - Updated `InvoiceCalculationService` to strictly bill only the services explicitly selected in `RoomServiceSettings`. The controversial fallback mechanism that automatically billed unselected global services was fully removed to prevent erroneous charges.
    - Added comprehensive console debugging logs to easily trace why specific services are billed or skipped.
2. **EF Core Translation Bug Fix**:
    - Addressed a critical 500 Internal Server Error when fetching previous meter readings caused by an incompatible `.GroupBy()` LINQ query in `MeterReadingService.cs`. 
    - Re-wrote the query to perform client-side evaluation after `.ToListAsync()` to restore stability when users input new meter indices.
3. **Occupancy Management (Roommates & Surcharges)**:
    - **Contract UI Filter**: Implemented filtering in `contracts.js` to ensure users currently assigned to a room (`Status === 'Staying'`) are hidden from the "Select Primary Tenant" dropdown when creating new contracts.
    - **Add Occupant Modal**: Built and integrated the `addOccupantModal` logic to allow landlords to assign roommates/family members to existing active contracts. 
    - **Occupant Limits**: Refactored `OccupancyService.cs` (`CreateContractAsync` and `UpdateContractAsync`) to properly record `StandardOccupants` and `ExtraOccupantFee` to the `RoomSettings` table. Dynamically adjusted `MaxOccupants` to equal `StandardOccupants + 5` to remove artificial constraints, thereby ensuring the system correctly calculates "Extra Occupant Surcharges" when roommates are added.

**Files Created/Modified**:
- `Services/MeterReadingService.cs`
- `Services/OccupancyService.cs`
- `Services/InvoiceCalculationService.cs`
- `wwwroot/js/admin/contracts.js`
- `wwwroot/js/admin/room-details.js`
- `Views/Admin/Partials/Modals/_TenantModals.cshtml`

*This walkthrough is a living document and will be updated as the project evolves.*

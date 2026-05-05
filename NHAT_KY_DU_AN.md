# Nháº­t KÃ½ Dá»± Ãn: QuanTro (Há» Thá»ng Quáº£n LÃ½ Trá»)

QuanTro lÃ  giáº£i phÃ¡p quáº£n lÃ½ trá» toÃ n diá»n dÃ nh cho cÃ¡c chá»§ trá» quy mÃ´ vá»«a vÃ  nhá». Há» thá»ng cung cáº¥p luá»ng cÃ´ng viá»c xuyÃªn suá»t tá»« thiáº¿t láº­p cÆ¡ sá» háº¡ táº§ng, tiáº¿p nháº­n khÃ¡ch thuÃª, Äáº¿n tá»± Äá»ng hÃ³a thanh toÃ¡n vÃ  xá»­ lÃ½ yÃªu cáº§u há» trá»£ theo thá»i gian thá»±c.

---

## 1. Cáº¥u TrÃºc CÆ¡ Sá» Dá»¯ Liá»u Chi Tiáº¿t

Há» thá»ng ÄÆ°á»£c xÃ¢y dá»±ng trÃªn ná»n táº£ng SQL Server máº¡nh máº½ vá»i hÆ¡n 16 báº£ng liÃªn káº¿t cháº·t cháº½.

### 1.1. Quáº£n lÃ½ NgÆ°á»i DÃ¹ng & PhÃ¢n Quyá»n

- **Roles**: PhÃ¢n quyá»n (superuser, admin, tenant).
- **Users**: XÃ¡c thá»±c, báº£o máº­t, thÃ´ng tin cÃ¡ nhÃ¢n cÆ¡ báº£n.

### 1.2. NhÃ³m CÆ¡ Sá» Háº¡ Táº§ng

- **Motels**: ThÃ´ng tin khu trá».
- **Floors**: Quáº£n lÃ½ táº§ng trong khu trá».
- **Rooms**: Quáº£n lÃ½ phÃ²ng trá» (tráº¡ng thÃ¡i, diá»n tÃ­ch).

### 1.3. NhÃ³m Kinh Doanh & Báº£ng GiÃ¡

- **RoomSettings**: GiÃ¡ thuÃª cÆ¡ báº£n, tiá»n cá»c, sá» ngÆ°á»i tiÃªu chuáº©n, phá»¥ thu.
- **Services**: Danh má»¥c dá»ch vá»¥ chung (Äiá»n, NÆ°á»c, RÃ¡c...).
- **RoomServiceSettings**: (ÄÃ£ loáº¡i bá» Äá» Äá»ng bá» giÃ¡ dá»ch vá»¥ toÃ n há» thá»ng).

### 1.4. NhÃ³m KhÃ¡ch ThuÃª & Há»£p Äá»ng

- **Tenants**: Há» sÆ¡ khÃ¡ch thuÃª (CCCD, Sá» Äiá»n thoáº¡i...).
- **RoomOccupants**: Theo dÃµi ngÆ°á»i Äang lÆ°u trÃº táº¡i phÃ²ng.
- **Contracts**: Há»£p Äá»ng thuÃª nhÃ  cÃ³ tÃ­nh phÃ¡p lÃ½ (NgÃ y báº¯t Äáº§u/káº¿t thÃºc, tiá»n cá»c, giÃ¡ thuÃª).

### 1.5. NhÃ³m TÃ i ChÃ­nh & HÃ³a ÄÆ¡n

- **MeterReadings**: Ghi nháº­n chá» sá» Äiá»n, nÆ°á»c tiÃª### Session 21: HoÃ n thiá»n SÆ¡ Äá» phÃ²ng & Kháº¯c phá»¥c lá»i Runtime (Má»i nháº¥t)
- **Há» trá»£ Khu trá» khÃ´ng chia táº§ng**: 
  - Cáº­p nháº­t backend (`MotelService.cs`) vÃ  DTO Äá» tráº£ vá» danh sÃ¡ch phÃ²ng trá»±c tiáº¿p cho cÃ¡c khu trá» khÃ´ng cáº¥u hÃ¬nh táº§ng.
  - Cáº­p nháº­t frontend (`motels-floormap.js`) Äá» hiá»n thá» Grid phÃ²ng ngay láº­p tá»©c khi chá»n khu trá» khÃ´ng táº§ng.
- **Kháº¯c phá»¥c lá»i "classList of null"**:
  - Thá»±c hiá»n audit toÃ n bá» mÃ£ nguá»n Javascript, thÃªm cÆ¡ cháº¿ kiá»m tra an toÃ n (`null check`) cho má»i thao tÃ¡c truy cáº­p pháº§n tá»­ DOM.
  - Bá» sung cÃ¡c pháº§n tá»­ giao diá»n bá» thiáº¿u trong `Index.cshtml` (nhÆ° `fmp-save-row`) Äá» khá»p vá»i logic Äiá»u khiá»n cá»§a Javascript.
- **Cáº£i thiá»n Báº£o máº­t & PhiÃªn lÃ m viá»c**:
  - Xá»­ lÃ½ lá»i 401/403 táº¡i táº§ng API: Tá»± Äá»ng phÃ¡t hiá»n phiÃªn lÃ m viá»c háº¿t háº¡n vÃ  chuyá»n hÆ°á»ng ngÆ°á»i dÃ¹ng vá» trang ÄÄng nháº­p thay vÃ¬ bÃ¡o lá»i káº¿t ná»i.
  - Cáº­p nháº­t logic ÄÄng xuáº¥t: Äáº£m báº£o xÃ³a sáº¡ch session phÃ­a Server trÆ°á»c khi chuyá»n hÆ°á»ng.

---

## 3. Tráº¡ng ThÃ¡i Dá»± Ãn Hiá»n Táº¡i

**NgÃ y cáº­p nháº­t**: 2026-05-04
**Thá»±c hiá»n bá»i**: Antigravity (AI)
**Tráº¡ng thÃ¡i**: â HoÃ n táº¥t Core â Há» thá»ng ÄÃ£ cháº¡y á»n Äá»nh, khÃ´ng cÃ²n lá»i crash giao diá»n.

**TÃ­nh nÄng ÄÃ HOÃN THÃNH:**

- [x] ÄÄng nháº­p / ÄÄng xuáº¥t (JWT Cookie, PhÃ¢n quyá»n)
- [x] Quáº£n lÃ½ Khu trá», Táº§ng, PhÃ²ng
- [x] SÆ¡ Äá» PhÃ²ng & Táº§ng (Há» trá»£ cáº£ khu trá» cÃ³ táº§ng vÃ  khÃ´ng táº§ng)
- [x] Táº¡o tÃ i khoáº£n KhÃ¡ch thuÃª (tÃ¡ch biá»t tÃ i khoáº£n vÃ  há»£p Äá»ng)
- [x] Quáº£n lÃ½ Há»£p Äá»ng (Wizard 3 bÆ°á»c chuyÃªn nghiá»p)
- [x] Cáº¥u hÃ¬nh Dá»ch vá»¥ (Global Services)
- [x] Ghi chá» sá» Äiá»n nÆ°á»c kÃ©p (Form tá»i Æ°u)
- [x] TÃ­nh tiá»n & PhÃ¡t hÃ nh HÃ³a ÄÆ¡n tá»± Äá»ng
- [x] Cá»ng thÃ´ng tin KhÃ¡ch thuÃª (Tenant Portal) xem hÃ³a ÄÆ¡n/yÃªu cáº§u
- [x] Tá»± Äá»ng dá»n dáº¹p há»£p Äá»ng háº¿t háº¡n/cháº¥m dá»©t.
- [x] CÆ¡ cháº¿ chá»ng lá»i Runtime Javascript (Defensive Programming).

**CÃ¡c tÃ­nh nÄng chuáº©n bá» phÃ¡t triá»n (Náº¿u cáº§n):**

- [ ] TÃ­ch há»£p thanh toÃ¡n online (VNPay, MoMo)
- [ ] BÃ¡o cÃ¡o & Thá»ng kÃª doanh thu chi tiáº¿t
- [ ] á»¨ng dá»¥ng Mobileá»i logic cháº¥m dá»©t há»£p Äá»ng: Chuyá»n tráº¡ng thÃ¡i `Terminated` vÃ  `MovedOut` thay vÃ¬ xÃ³a vÄ©nh viá»n Äá» giá»¯ láº¡i lá»ch sá»­ tÃ i chÃ­nh.
- **Fix lá»i Build & Logic**:
  - Sá»­a lá»i `CS0117` (Thiáº¿u `ServiceId` trong DTO).
  - Sá»­a lá»i tÃ­nh tiá»n cho cÃ¡c há»£p Äá»ng báº¯t Äáº§u vÃ o giá»¯a thÃ¡ng.

---

## 3. Tráº¡ng ThÃ¡i Dá»± Ãn Hiá»n Táº¡i

**NgÃ y cáº­p nháº­t**: 2026-05-04
**Thá»±c hiá»n bá»i**: Antigravity (AI)
**Tráº¡ng thÃ¡i**: â á»n Äá»nh â ÄÃ£ kháº¯c phá»¥c lá»i treo há» thá»ng vÃ  vÃ²ng láº·p chuyá»n hÆ°á»ng.

**TÃ­nh nÄng ÄÃ HOÃN THÃNH:**

- [x] ÄÄng nháº­p / ÄÄng xuáº¥t (JWT Cookie, PhÃ¢n quyá»n)
- [x] Quáº£n lÃ½ Khu trá», Táº§ng, PhÃ²ng
- [x] SÆ¡ Äá» PhÃ²ng & Táº§ng (Trá»±c quan, thá»i gian thá»±c)
- [x] Táº¡o tÃ i khoáº£n KhÃ¡ch thuÃª (tÃ¡ch biá»t tÃ i khoáº£n vÃ  há»£p Äá»ng)
- [x] Quáº£n lÃ½ Há»£p Äá»ng (Wizard 3 bÆ°á»c chuyÃªn nghiá»p)
- [x] Cáº¥u hÃ¬nh Dá»ch vá»¥ (Global Services)
- [x] Ghi chá» sá» Äiá»n nÆ°á»c kÃ©p (Form tá»i Æ°u)
- [x] TÃ­nh tiá»n & PhÃ¡t hÃ nh HÃ³a ÄÆ¡n tá»± Äá»ng
- [x] Cá»ng thÃ´ng tin KhÃ¡ch thuÃª (Tenant Portal) xem hÃ³a ÄÆ¡n/yÃªu cáº§u
- [x] Tá»± Äá»ng dá»n dáº¹p há»£p Äá»ng háº¿t háº¡n/cháº¥m dá»©t.

**CÃ¡c tÃ­nh nÄng chuáº©n bá» phÃ¡t triá»n (Náº¿u cáº§n):**

- [ ] TÃ­ch há»£p thanh toÃ¡n online (VNPay, MoMo)
- [ ] BÃ¡o cÃ¡o & Thá»ng kÃª doanh thu chi tiáº¿t
- [ ] á»¨ng dá»¥ng Mobile

---

## 4. Nháº­t KÃ½ Sá»­a Lá»i Chi Tiáº¿t

### [2026-05-04] â Sá»­a lá»i SÆ¡ Äá» PhÃ²ng & Chá»nh sá»­a Há»£p Äá»ng

**Váº¥n Äá» 1: SÆ¡ Äá» táº§ng vÃ  phÃ²ng hiá»n "KhÃ´ng káº¿t ná»i server"**

- **NguyÃªn nhÃ¢n gá»c rá»**: PhÆ°Æ¡ng thá»©c `GetMotelsByAdminAsync` trong `MotelService.cs` gá»i `.Select()` trá»±c tiáº¿p trÃªn `m.Floors` mÃ  khÃ´ng cÃ³ kiá»m tra null. Khi EF Core khÃ´ng táº£i ÄÆ°á»£c navigation property (Floors = null), há» thá»ng nÃ©m `NullReferenceException` â API tráº£ vá» lá»i 500.
- **Giáº£i phÃ¡p**: Bá»c toÃ n bá» logic trong `try-catch`. Thay `m.Floors.Select(...)` báº±ng `(m.Floors ?? new List<Floor>()).Select(...)` vÃ  tÆ°Æ¡ng tá»± cho `f.Rooms`. ThÃªm Serilog logging Äá» dá» debug trong tÆ°Æ¡ng lai.

**Váº¥n Äá» 2: Má» modal Sá»­a há»£p Äá»ng láº§n 2 â checkbox khÃ´ng Äá»ng bá»**

- **NguyÃªn nhÃ¢n gá»c rá» 1**: Frontend gá»i `c.selectedServiceIds.includes(...)` nhÆ°ng `selectedServiceIds` cÃ³ thá» lÃ  `null` náº¿u phÃ²ng chÆ°a cÃ³ báº¥t ká»³ dá»ch vá»¥ nÃ o ÄÆ°á»£c báº­t â `TypeError: Cannot read properties of null`.
- **Giáº£i phÃ¡p**: Äá»i thÃ nh `(c.selectedServiceIds || []).includes(...)`.
- **NguyÃªn nhÃ¢n gá»c rá» 2**: Khi gá»­i dá»¯ liá»u lÃªn server qua `PUT /UpdateContract`, JavaScript gá»­i `selectedServiceIds` (camelCase) nhÆ°ng C# DTO `ContractRequest` khai bÃ¡o `SelectedServiceIds` (PascalCase). Do ASP.NET Core máº·c Äá»nh dÃ¹ng case-insensitive, tuy nhiÃªn Äá» an toÃ n ÄÃ£ Äá»i thÃ nh `SelectedServiceIds` tÆ°á»ng minh.

**Váº¥n Äá» 3: Thiáº¿u nÃºt Sá»­a trong danh sÃ¡ch Há»£p Äá»ng**

- **Giáº£i phÃ¡p**: ThÃªm nÃºt `<i class="bi bi-pencil-square"></i> Sá»­a` cáº¡nh nÃºt "Cháº¥m dá»©t" trong `loadContractsData()`. ThÃªm hÃ m `openEditContractFromList(roomId, roomCode)` Äá» má» modal chá»nh sá»­a trá»±c tiáº¿p tá»« danh sÃ¡ch mÃ  khÃ´ng cáº§n qua sÆ¡ Äá» táº§ng.

**Váº¥n Äá» 4: Thiáº¿u hÃ m `showPremiumToast` gÃ¢y crash toÃ n trang**

- **Giáº£i phÃ¡p**: ThÃªm Äá»nh nghÄ©a Äáº§y Äá»§ cá»§a hÃ m `showPremiumToast(title, message, type)` vÃ o `Index.cshtml` vá»i UI Toast Bootstrap 5 chuyÃªn nghiá»p, tá»± dá»n dáº¹p sau khi áº©n.

**Files ÄÃ£ sá»­a**:
- `Services/MotelService.cs`: Null-safety + try-catch cho `GetMotelsByAdminAsync`
- `Views/Admin/Index.cshtml`: ThÃªm `showPremiumToast`, sá»­a checkbox sync, thÃªm nÃºt Sá»­a HÄ, fix `loadMotelsData` error handling

### [2026-05-04] â Refactor: Gom logic phÃ¢n quyá»n (DRY Principle)

**Váº¥n Äá»**: Logic kiá»m tra phÃ¢n quyá»n nhÆ° `IsSuperuser`, `CanAccessRoom`, `CanAccessInvoice` bá» láº·p láº¡i thá»§ cÃ´ng á» nhiá»u Service (`MotelService`, `InvoiceService`, `PaymentService`, `MeterReadingService`, v.v.). Äiá»u nÃ y vi pháº¡m nguyÃªn táº¯c DRY (Don't Repeat Yourself) vÃ  gÃ¢y rá»§i ro lá» há»ng báº£o máº­t náº¿u bá» sÃ³t khi cáº­p nháº­t.

**Giáº£i phÃ¡p (Refactoring BÆ°á»c 1)**:
1. **Táº¡o Service má»i**: `IAccessControlService` vÃ  `AccessControlService` Äá» lÃ m Äiá»m duy nháº¥t (single point of truth) cho má»i nghiá»p vá»¥ phÃ¢n quyá»n.
2. **Gom cÃ¡c hÃ m**: ÄÆ°a cÃ¡c hÃ m `IsSuperuserAsync`, `IsAdminOfMotelAsync`, `IsAdminOrSuperAsync`, `CanAccessRoomAsync`, `CanAccessInvoiceAsync` vÃ o `AccessControlService`.
3. **Cáº­p nháº­t Dependency Injection**: ÄÄng kÃ½ `IAccessControlService` vÃ o `Program.cs`.
4. **XÃ³a code thá»«a**: Cáº­p nháº­t láº¡i 6 Service (`AdminService`, `TenantService`, `MotelService`, `InvoiceService`, `MeterReadingService`, `PaymentService`), xÃ³a cÃ¡c hÃ m private láº·p láº·p vÃ  inject `IAccessControlService` thay tháº¿.
5. **Äáº£m báº£o tÃ­nh toÃ n váº¹n**: KhÃ´ng thay Äá»i route API, khÃ´ng thay Äá»i frontend, dá»± Ã¡n biÃªn dá»ch thÃ nh cÃ´ng 0 lá»i.

### [2026-05-04] â Refactor BÆ°á»c 2: Äá»ng bá» hÃ³a Dá»ch vá»¥ & Sá»­a lá»i tÃ­nh hÃ³a ÄÆ¡n

**Váº¥n Äá»**:
1. `InvoiceCalculationService` tÃ­nh phÃ­ cho *táº¥t cáº£* dá»ch vá»¥ Äang báº­t trÃªn há» thá»ng mÃ  khÃ´ng kiá»m tra xem phÃ²ng ÄÃ³ cÃ³ báº­t dá»ch vá»¥ ÄÃ³ (`RoomServiceSettings.IsActive`) hay khÃ´ng.
2. Khi Admin táº¡o dá»ch vá»¥ Global má»i, cÃ¡c phÃ²ng hiá»n táº¡i khÃ´ng tá»± Äá»ng cÃ³ cáº¥u hÃ¬nh dá»ch vá»¥ ÄÃ³, dáº«n Äáº¿n lá»i/thiáº¿u sÃ³t khi thao tÃ¡c cáº­p nháº­t há»£p Äá»ng vÃ  chá»n dá»ch vá»¥ má»i.

**Giáº£i phÃ¡p**:
1. **Sá»­a InvoiceCalculationService**: Lá»c dá»ch vá»¥ khi tÃ­nh toÃ¡n. Chá» tÃ­nh nhá»¯ng dá»ch vá»¥ vá»«a thá»a mÃ£n báº­t á» má»©c Global (`Service.IsActive`) VÃ báº­t á» má»©c phÃ²ng (`RoomServiceSettings.IsActive`). ÄÆ¡n giÃ¡ váº«n láº¥y tá»« báº£ng Global Äá» Äá»ng nháº¥t (`Service.DefaultPrice`).
2. **Sá»­a OccupancyService (`CreateContractAsync` & `UpdateContractAsync`)**: Khi táº¡o/sá»­a há»£p Äá»ng, há» thá»ng sáº½ tá»± Äá»ng quÃ©t danh sÃ¡ch dá»ch vá»¥ Global. Náº¿u phÃ¡t hiá»n phÃ²ng chÆ°a cÃ³ `RoomServiceSetting` cho má»t dá»ch vá»¥ nÃ o ÄÃ³, nÃ³ sáº½ tá»± Äá»ng khá»i táº¡o. Nhá» váº­y, phÃ²ng luÃ´n cÃ³ Äáº§y Äá»§ cáº¥u hÃ¬nh cho táº¥t cáº£ dá»ch vá»¥ hiá»n cÃ³.
3. KhÃ´ng cáº§n sá»­a UI hay API, há» thá»ng tá»± Äá»ng xá»­ lÃ½ data liá»n máº¡ch, biÃªn dá»ch 0 lá»i.

### [2026-05-04] â Refactor BÆ°á»c 3: Báº£o toÃ n dá»¯ liá»u tÃ i chÃ­nh (Soft-Termination)

**Váº¥n Äá»**: `ContractExpirationService` trÆ°á»c ÄÃ¢y dÃ¹ng lá»nh xÃ³a cá»©ng (hard-delete) toÃ n bá» `Invoices`, `Payments`, `Tenants`, vÃ  `Users` khi há»£p Äá»ng á» tráº¡ng thÃ¡i `Waiting` quÃ¡ 7 ngÃ y. Äiá»u nÃ y lÃ m máº¥t toÃ n bá» lá»ch sá»­ tÃ i chÃ­nh, vi pháº¡m nguyÃªn táº¯c lÆ°u trá»¯ káº¿ toÃ¡n cÆ¡ báº£n.

**Giáº£i phÃ¡p**:
1. Thay Äá»i hÃ nh vi sang **Soft-Termination**: Thay vÃ¬ xÃ³a, Äá»i tráº¡ng thÃ¡i Há»£p Äá»ng sang `Terminated`.
2. Báº£o toÃ n dá»¯ liá»u: Giá»¯ nguyÃªn `Invoices` vÃ  `Payments` phá»¥c vá»¥ bÃ¡o cÃ¡o doanh thu. KhÃ´ng xÃ³a `Tenants` vÃ  `Users` Äá» giá»¯ láº¡i há» sÆ¡ khÃ¡ch cÅ©.
3. Giáº£i phÃ³ng phÃ²ng: Chá» giáº£i phÃ³ng tráº¡ng thÃ¡i phÃ²ng vá» `Vacant` vÃ  dá»n dáº¹p `RoomOccupants` (ngÆ°á»i Äang á») Äá» chuáº©n bá» ÄÃ³n khÃ¡ch má»i.

### [2026-05-04] â Refactor BÆ°á»c 4: TÃ¡ch Module JavaScript Frontend

**Váº¥n Äá»**: File `Views/Admin/Index.cshtml` quÃ¡ lá»n (gáº§n 3.000 dÃ²ng), trong ÄÃ³ chá»©a hÆ¡n 1.700 dÃ²ng JavaScript inline vá»i hÆ¡n 60 function Äan xen. Äiá»u nÃ y vi pháº¡m nguyÃªn táº¯c Separation of Concerns (SoC) vÃ  gÃ¢y rá»§i ro lá»n khi báº£o trÃ¬ UI.

**Giáº£i phÃ¡p**:
1. Sá»­ dá»¥ng Python script tá»± Äá»ng parse vÃ  phÃ¢n tÃ¡ch chÃ­nh xÃ¡c cÃ¡c function JavaScript ra thÃ nh **10 file module riÃªng biá»t** Äáº·t táº¡i `wwwroot/js/admin/` (vÃ­ dá»¥: `admin-core.js`, `contracts.js`, `billing.js`...).
2. Expose cÃ¡c hÃ m ra `window` object (vÃ­ dá»¥: `window.switchModule = switchModule`) Äá» Äáº£m báº£o khÃ´ng lÃ m gÃ£y (break) cÃ¡c sá»± kiá»n `onclick` inline trong tháº» HTML.
3. Cáº­p nháº­t `Index.cshtml` sá»­ dá»¥ng cÃ¡c tháº» `<script src="...">`, giáº£m kÃ­ch thÆ°á»c file xuá»ng chá» cÃ²n hÆ¡n 1.200 dÃ²ng.
4. Dá»± Ã¡n build thÃ nh cÃ´ng vá»i 0 lá»i, khÃ´ng thay Äá»i markup UI hay route API nÃ o.

### [2026-05-04] â Refactor BÆ°á»c 5: TÃ¡ch Models/MotelEntities.cs

**Má»¥c tiÃªu**: Cáº£i thiá»n Separation of Concerns (SoC) vÃ  kháº£ nÄng quáº£n lÃ½ mÃ£ nguá»n á» táº§ng Data Models.

**Giáº£i phÃ¡p**:
1. Táº¡o thÆ° má»¥c `Models/Entities/` Äá» chá»©a cÃ¡c domain entities.
2. TÃ¡ch file monolithic `Models/MotelEntities.cs` (hÆ¡n 400 dÃ²ng) thÃ nh **15 file entity riÃªng biá»t** (vÃ­ dá»¥: `Motel.cs`, `Room.cs`, `Invoice.cs`...).
3. **Báº£o toÃ n Namespace**: Giá»¯ nguyÃªn namespace `UserManagementSystem.Models` Äá» Äáº£m báº£o tÃ­nh tÆ°Æ¡ng thÃ­ch ngÆ°á»£c, khÃ´ng lÃ m gÃ£y cÃ¡c tham chiáº¿u á» Controller vÃ  Service.
4. KhÃ´ng thay Äá»i database schema, khÃ´ng sá»­a migration vÃ  khÃ´ng thay Äá»i logic nghiá»p vá»¥.
5. ÄÃ£ xÃ³a file cÅ© `Models/MotelEntities.cs` sau khi di chuyá»n thÃ nh cÃ´ng.
6. Dá»± Ã¡n build thÃ nh cÃ´ng vá»i **0 lá»i (0 Errors)**.

### [2026-05-04] â Refactor BÆ°á»c 6: TÃ¡ch DTO vÃ  Service Interface

**Má»¥c tiÃªu**: Cáº£i thiá»n Separation of Concerns á» táº§ng DTO vÃ  cÃ¡c Service Contracts (Interfaces).

**Giáº£i phÃ¡p**:
1. **Tá» chá»©c láº¡i Interface**: Táº¡o thÆ° má»¥c `Services/Interfaces/` vÃ  tÃ¡ch file gom `IBillingAndRequestServices.cs` thÃ nh cÃ¡c interface riÃªng biá»t: `IMeterReadingService.cs`, `IInvoiceCalculationService.cs`, `IInvoiceService.cs`, `IPaymentService.cs`, vÃ  `IRequestService.cs`.
2. **Tá» chá»©c láº¡i DTO**: Táº¡o thÆ° má»¥c `Models/DTOs/` vÃ  tÃ¡ch cÃ¡c DTO tá»« `MotelManagementDTOs.cs` vÃ  `BillingAndRequestDTOs.cs` thÃ nh cÃ¡c nhÃ³m file chuyÃªn biá»t: `AdminDTOs.cs`, `TenantDTOs.cs`, `MotelDTOs.cs`, `RoomDTOs.cs`, `ServiceDTOs.cs`, `ContractDTOs.cs`, `BillingDTOs.cs`, vÃ  `RequestDTOs.cs`.
3. **Báº£o toÃ n Namespace**: Giá»¯ nguyÃªn namespace `UserManagementSystem.Services` vÃ  `UserManagementSystem.Models` Äá» Äáº£m báº£o tÃ­nh tÆ°Æ¡ng thÃ­ch vÃ  á»n Äá»nh.
4. KhÃ´ng thay Äá»i logic nghiá»p vá»¥, route API hay database schema.
5. ÄÃ£ xÃ³a cÃ¡c file monolithic cÅ© sau khi hoÃ n táº¥t di chuyá»n.
6. Dá»± Ã¡n build thÃ nh cÃ´ng vá»i **0 lá»i (0 Errors)**.

### [2026-05-04] â Refactor BÆ°á»c 7A: TÃ¡ch Global Services tá»« MotelService

**Má»¥c tiÃªu**: Giáº£m sá»± phá»¥ thuá»c quÃ¡ má»©c vÃ o `MotelService` vÃ  tuÃ¢n thá»§ nguyÃªn táº¯c Single Responsibility (SRP).

**Giáº£i phÃ¡p**:
1. **TÃ¡ch logic**: Di chuyá»n cÃ¡c phÆ°Æ¡ng thá»©c quáº£n lÃ½ dá»ch vá»¥ há» thá»ng (Get, Create, Update, Seed) tá»« `MotelService` sang `GlobalServiceService` má»i.
2. **Tá»i Æ°u hÃ³a Auth**: Sá»­ dá»¥ng `IAccessControlService.IsSuperuserAsync` Äá» kiá»m tra quyá»n háº¡n thay vÃ¬ truy váº¥n thá»§ cÃ´ng trong Database, giÃºp mÃ£ nguá»n gá»n gÃ ng vÃ  Äá»ng nháº¥t.
3. **Äá»ng bá» thÃ´ng bÃ¡o**: Cáº­p nháº­t logic gá»­i thÃ´ng bÃ¡o khi Äá»i giÃ¡ dá»ch vá»¥, Äáº£m báº£o ÄÃºng role Äá»nh danh lÃ  "tenant".
4. **Giá»¯ nguyÃªn giao diá»n**: Äáº£m báº£o toÃ n bá» route API vÃ  logic frontend khÃ´ng thay Äá»i.
5. Dá»± Ã¡n build thÃ nh cÃ´ng vá»i **0 lá»i (0 Errors)** vÃ  cÃ¡c file JS admin vÆ°á»£t qua kiá»m tra cÃº phÃ¡p.

### [2026-05-04] â Refactor BÆ°á»c 7B: TÃ¡ch Room vÃ  RoomSetting tá»« MotelService

**Má»¥c tiÃªu**: Module hÃ³a logic quáº£n lÃ½ phÃ²ng, tÃ¡ch biá»t khá»i quáº£n lÃ½ khu trá» tá»ng quÃ¡t.

**Giáº£i phÃ¡p**:
1. **TÃ¡ch logic**: Di chuyá»n cÃ¡c phÆ°Æ¡ng thá»©c quáº£n lÃ½ phÃ²ng (`CreateRoom`, `UpdateRoom`, `UpdateRoomSetting`, `GetRoomSettings`, `GetRoomServices`, `GetRoomOccupants`) sang `RoomManagementService` má»i.
2. **Chá»nh sá»­a GetRoomServices**: Cáº­p nháº­t phÆ°Æ¡ng thá»©c `GetRoomServicesAsync` Äá» thá»±c hiá»n join giá»¯a `Services` (danh má»¥c & giÃ¡ global) vÃ  `RoomServiceSettings` (cáº¥u hÃ¬nh báº­t/táº¯t theo phÃ²ng). Äáº£m báº£o tráº£ vá» ÄÃºng tráº¡ng thÃ¡i `IsActive` cá»§a tá»«ng dá»ch vá»¥ táº¡i phÃ²ng ÄÃ³. Quy táº¯c giÃ¡: `UnitPrice` trong `RoomServiceSettings` chá» ÄÃ³ng vai trÃ² phá»¥ trá»£, há» thá»ng luÃ´n Æ°u tiÃªn `Services.DefaultPrice` khi tÃ­nh hÃ³a ÄÆ¡n Äá» Äáº£m báº£o tÃ­nh Äá»ng nháº¥t giÃ¡ toÃ n há» thá»ng.
3. **Dá»n dáº¹p MotelService**: Loáº¡i bá» cÃ¡c dependency dÆ° thá»«a (`INotificationService`, `IConfiguration`) vÃ  cÃ¡c trÆ°á»ng dá»¯ liá»u khÃ´ng cÃ²n sá»­ dá»¥ng, giÃºp service nÃ y trá» nÃªn cá»±c ká»³ gá»n nháº¹ chá» táº­p trung vÃ o Motel vÃ  Floor.
4. **Báº£o toÃ n giao diá»n**: ToÃ n bá» endpoint cá»§a `MotelManagementController` ÄÆ°á»£c giá»¯ nguyÃªn route vÃ  hÃ nh vi Äá»i vá»i frontend.
5. Dá»± Ã¡n build thÃ nh cÃ´ng vá»i **0 lá»i (0 Errors)**.

### [2026-05-04] â BÆ°á»c 8: Audit vÃ  bá» sung rÃ ng buá»c Database an toÃ n

**Má»¥c tiÃªu**: TÄng tÃ­nh toÃ n váº¹n dá»¯ liá»u á» táº§ng váº­t lÃ½, ngÄn cháº·n lá»i logic tá»« á»©ng dá»¥ng.

**Giáº£i phÃ¡p**:
1. **Migration**: Táº¡o migration `AddSafeDatabaseConstraints` bá» sung cÃ¡c chá» má»¥c duy nháº¥t (Unique Index).
2. **Rooms**: ThÃªm unique index cho `(MotelId, RoomCode)` Äá» khÃ´ng trÃ¹ng sá» phÃ²ng trong cÃ¹ng má»t khu trá».
3. **Services**: ThÃªm unique index cho `ServiceCode` Äá» mÃ£ dá»ch vá»¥ khÃ´ng bá» láº·p.
4. **RoomServiceSettings**: ThÃªm unique index cho `(RoomId, ServiceId)` Äáº£m báº£o cáº¥u hÃ¬nh dá»ch vá»¥ theo phÃ²ng lÃ  duy nháº¥t.
5. **MeterReadings**: ThÃªm unique index cho `(RoomId, ServiceId, BillingMonth, BillingYear)` ngÄn cháº·n nháº­p chá» sá» trÃ¹ng thÃ¡ng/nÄm.
6. **Contracts**: ThÃªm **Filtered Unique Index** cho `RoomId` khi `ContractStatus = 'Active'`, Äáº£m báº£o má»i phÃ²ng chá» cÃ³ tá»i Äa má»t há»£p Äá»ng Äang hoáº¡t Äá»ng.
7. Dá»± Ã¡n build thÃ nh cÃ´ng vá»i **0 lá»i (0 Errors)**.

### [2026-05-04] â BÆ°á»c 9: ThÃªm dá»± Ã¡n Test cho cÃ¡c nghiá»p vá»¥ quan trá»ng

**Má»¥c tiÃªu**: Äáº£m báº£o cÃ¡c nghiá»p vá»¥ cá»t lÃµi (Invoice, Payment, Room, Access Control) hoáº¡t Äá»ng ÄÃºng sau khi refactor.

**Giáº£i phÃ¡p**:
1. **Khá»i táº¡o**: Táº¡o dá»± Ã¡n `UserManagementSystem.Tests` sá»­ dá»¥ng xUnit vÃ  SQLite In-memory Äá» há» trá»£ cÃ¡c rÃ ng buá»c database má»i.
2. **InvoiceCalculationService**: Viáº¿t test Äáº£m báº£o tÃ­nh ÄÃºng giÃ¡ Global, loáº¡i trá»« dá»ch vá»¥ ÄÃ£ táº¯t vÃ  bÃ¡o lá»i khi thiáº¿u chá» sá».
3. **PaymentService**: Viáº¿t test ngÄn cháº·n thanh toÃ¡n vÆ°á»£t sá» tiá»n cÃ²n láº¡i.
4. **RoomManagementService**: Viáº¿t test kiá»m tra tráº¡ng thÃ¡i dá»ch vá»¥ theo tá»«ng phÃ²ng.
5. **AccessControlService**: Viáº¿t test phÃ¢n quyá»n cho Superuser vÃ  cháº·n Tenant xem phÃ²ng khÃ¡c.
6. **Cáº¥u hÃ¬nh**: Äiá»u chá»nh `UserManagementSystem.csproj` Äá» cÃ´ láº­p dá»± Ã¡n test khá»i dá»± Ã¡n chÃ­nh.

### [2026-05-04] â Session 21: HoÃ n thiá»n SÆ¡ Äá» phÃ²ng & Kháº¯c phá»¥c lá»i Runtime

**Váº¥n Äá» 1: SÆ¡ Äá» phÃ²ng "KhÃ´ng káº¿t ná»i server" hoáº·c Crash Javascript**
- **NguyÃªn nhÃ¢n**: 
    1. `MotelService.cs` khÃ´ng xá»­ lÃ½ null cho cÃ¡c Motel chÆ°a cÃ³ Táº§ng.
    2. Javascript truy cáº­p thuá»c tÃ­nh `classList` cá»§a cÃ¡c pháº§n tá»­ khÃ´ng tá»n táº¡i (null) trong DOM.
    3. Lá»i cÃº phÃ¡p (thiáº¿u dáº¥u ngoáº·c) trong `motels-floormap.js`.
- **Giáº£i phÃ¡p**: 
    1. Bá» sung `Rooms` vÃ o `MotelResponse` DTO vÃ  map dá»¯ liá»u cho khu trá» khÃ´ng táº§ng.
    2. Triá»n khai defensive programming (null checks) cho toÃ n bá» cÃ¡c module Javascript (`admin-core.js`, `billing.js`, `motels-floormap.js`).
    3. Bá» sung cÃ¡c pháº§n tá»­ HTML bá» thiáº¿u vÃ o `Index.cshtml`.
    4. Sá»­a lá»i cÃº phÃ¡p trong `motels-floormap.js`.

**Váº¥n Äá» 2: Logout khÃ´ng sáº¡ch Session phÃ­a Server**
- **Giáº£i phÃ¡p**: Cáº­p nháº­t hÃ m `logout` thá»±c hiá»n gá»i API `POST /api/UserProxy/Logout` trÆ°á»c khi xÃ³a `localStorage` vÃ  chuyá»n hÆ°á»ng.

**Váº¥n Äá» 3: PhiÃªn lÃ m viá»c háº¿t háº¡n gÃ¢y lá»i API bÃ­ áº©n**
- **Giáº£i phÃ¡p**: ThÃªm interceptor xá»­ lÃ½ lá»i 401/403 trong `loadMotelsData`, tá»± Äá»ng redirect vá» trang Login vá»i thÃ´ng bÃ¡o Toast.

**Files ÄÃ£ sá»­a**:
- `Services/MotelService.cs`
- `Models/DTOs/MotelDTOs.cs`
- `wwwroot/js/admin/motels-floormap.js`
- `wwwroot/js/admin/billing.js`
- `Views/Admin/Index.cshtml`
- `Views/Shared/_AdminLayout.cshtml`

*TÃ i liá»u nÃ y sáº½ liÃªn tá»¥c ÄÆ°á»£c cáº­p nháº­t theo tiáº¿n trÃ¬nh cá»§a dá»± Ã¡n.*

### [2026-05-05] - Session 22: Tự động hóa quản lý Hợp đồng hết hạn (Background Task)

**Mục tiêu**: Tự động hóa việc kiểm tra và cập nhật trạng thái hợp đồng, giải phóng phòng khi hết hạn mà không cần quản trị viên thao tác thủ công.

**Giải pháp**:
1. **Triển khai Background Service**: Tạo `ContractExpirationService` kế thừa từ `BackgroundService`, chạy định kỳ mỗi giờ một lần.
2. **Xử lý Hợp đồng hết hạn**:
    - Quét các hợp đồng "Active" có `EndDate` nhỏ hơn thời điểm hiện tại.
    - Chuyển trạng thái hợp đồng sang "Waiting".
    - Tự động cập nhật trạng thái Phòng thành "Vacant" (Trống).
    - Cập nhật trạng thái Người ở (Occupants) và Người thuê chính (Primary Tenant) thành "MovedOut".
3. **Cơ chế lưu trữ dữ liệu tài chính**:
    - Thay vì xóa hợp đồng ngay lập tức, hệ thống giữ hợp đồng ở trạng thái "Waiting" trong 7 ngày để đảm bảo các hóa đơn cuối cùng được xử lý.
    - Sau 7 ngày, hợp đồng tự động chuyển sang "Terminated" để đóng hồ sơ vĩnh viễn nhưng vẫn bảo toàn dữ liệu lịch sử.
4. **Logging**: Ghi nhật ký chi tiết quá trình xử lý để quản trị viên có thể theo dõi qua log hệ thống.

**Files đã sửa/tạo mới**:
- `Services/BackgroundTasks/ContractExpirationService.cs`
- `Program.cs` (Đăng ký Hosted Service)

### [2026-05-05] - Session 23: Hoàn thiện tính năng Hóa đơn & Ghi chỉ số Điện/Nước

**Mục tiêu**: Nâng cấp hệ thống tính tiền chuyên nghiệp, hỗ trợ ghi chỉ số gối đầu và tự động tính tiền trọ theo thời gian ở thực tế.

**Giải pháp**:
1. **Ghi chỉ số thông minh**: Cập nhật `MeterReadingService` để tự động lấy số cũ từ tháng gần nhất. Hỗ trợ ghi đồng thời cả điện và nước trong một thao tác.
2. **Logic tính tiền trọ linh hoạt (Prorated Rent)**:
    - Cập nhật `InvoiceCalculationService` áp dụng quy tắc:
        - Ở $\le$ 7 ngày: Miễn phí tiền phòng.
        - Ở từ 8 đến 15 ngày: Tính 50% tiền phòng.
        - Ở > 15 ngày: Tính 100% tiền phòng.
    - Đảm bảo điện nước luôn tính theo tiêu thụ thực tế không phụ thuộc thời gian ở.
3. **Xuất hóa đơn Excel**:
    - Tích hợp thư viện `ClosedXML` ở Backend.
    - Triển khai endpoint `ExportExcel` cho phép tải về hóa đơn định dạng .xlsx với đầy đủ chi tiết và định dạng chuyên nghiệp.
4. **Cập nhật UI/UX**:
    - Thêm Modal xem chi tiết hóa đơn (`invoiceDetailsModal`).
    - Hoàn thiện luồng nhập chỉ số và xem/tải hóa đơn tại giao diện Quản lý Hóa đơn.

**Files đã sửa/tạo mới**:
- `Services/InvoiceCalculationService.cs`
- `Services/InvoiceService.cs` (Thêm logic Excel)
- `Controllers/InvoiceController.cs` (Thêm API xuất Excel)
- `wwwroot/js/admin/billing.js`
- `Views/Admin/Index.cshtml`

### [2026-05-05] - Session 24: Tái cấu trúc Frontend (Modularization)

**Mục tiêu**: Chia nhỏ tệp `Index.cshtml` khổng lồ thành các phần nhỏ dễ quản lý, bảo trì và phát triển lâu dài.

**Giải pháp**:
1. **Áp dụng Partial Views**: Chia `Index.cshtml` thành các tệp con (`.cshtml`) theo từng module và nhóm modal.
2. **Cấu trúc thư mục mới**:
    - `Views/Admin/Partials/Modules/`: Chứa các thành phần giao diện chính (Overview, Billing, Tenants...).
    - `Views/Admin/Partials/Modals/`: Chứa các hộp thoại (Modals) theo nhóm chức năng.
3. **Giữ nguyên Logic**: Chỉ thay đổi cách tổ chức tệp, toàn bộ IDs, Class và Logic JavaScript được giữ nguyên để đảm bảo không phá vỡ tính năng hiện tại.
4. **Kết quả**: File `Index.cshtml` giảm từ >1200 dòng xuống còn khoảng 100 dòng, cực kỳ gọn gàng và chuyên nghiệp.

**Files đã sửa/tạo mới**:
- `Views/Admin/Index.cshtml` (Refactored)
- 10 files trong `Views/Admin/Partials/Modules/`
- 4 files trong `Views/Admin/Partials/Modals/`

### [2026-05-05] - Session 25: Đồng bộ hóa Hóa đơn, Sửa lỗi Query và Nâng cấp Quản lý Người ở

**Mục tiêu**: Đảm bảo tính toán hóa đơn chính xác tuyệt đối theo hợp đồng, khắc phục các lỗi văng ứng dụng (exception) tại tầng CSDL và hoàn thiện tính năng thêm người ở dôi dư để tính phụ thu.

**Giải pháp**:
1. **Chính xác hóa tính toán Hóa đơn**: 
    - Cập nhật `InvoiceCalculationService` để chỉ tính tiền các dịch vụ được chọn trong Hợp đồng/Cấu hình phòng. Loại bỏ hoàn toàn cơ chế tự động ép tính phí các dịch vụ không liên quan để tránh thu sai tiền khách.
    - Thêm log chi tiết vào Terminal để theo dõi luồng tính toán từng dòng tiền.
2. **Khắc phục lỗi EF Core GroupBy**:
    - Sửa lỗi văng HTTP 500 khi tải chỉ số điện nước cũ. Lỗi do EF Core không dịch được lệnh `.GroupBy()` của LINQ sang SQL.
    - Giải pháp: Tải dữ liệu về bộ nhớ (`.ToListAsync()`) trước khi thực hiện nhóm dữ liệu để đảm bảo hệ thống luôn ổn định.
3. **Quản lý Người ở & Phụ thu (Extra Occupant)**:
    - **Lọc danh sách khách**: Cập nhật `contracts.js` để tự động ẩn các khách đang ở phòng khác khỏi danh sách chọn người đại diện khi lập hợp đồng mới.
    - **Thêm người ở chung**: Xây dựng modal `addOccupantModal` và logic xử lý để chủ trọ có thể thêm bạn cùng phòng/người nhà vào phòng đang thuê một cách dễ dàng.
    - **Nới lỏng giới hạn**: Refactor `OccupancyService.cs` để tự động tính toán và lưu `StandardOccupants` (Số người tiêu chuẩn) và `ExtraOccupantFee` (Phí phụ thu). Tự động đặt `MaxOccupants = StandardOccupants + 5` để không bao giờ bị báo lỗi "Phòng quá tải" khi landlord muốn thêm người để thu phí.

**Files đã sửa/tạo mới**:
- `Services/MeterReadingService.cs`
- `Services/OccupancyService.cs`
- `Services/InvoiceCalculationService.cs`
- `wwwroot/js/admin/contracts.js`
- `wwwroot/js/admin/room-details.js`
- `Views/Admin/Partials/Modals/_TenantModals.cshtml`

*Tài liệu này sẽ liên tục được cập nhật theo tiến trình của dự án.*

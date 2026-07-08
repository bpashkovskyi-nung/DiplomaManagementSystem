# Feature 4 — мультитенансі (факультет → кафедра)

## Мета

Перехід від **single-tenant** (одна кафедра в `appsettings`) до **multi-department** в одній інстанції ВНЗ.

- **Tenant** = **кафедра** (`Department`)
- **Факультет/інститут** — обов’язковий батько, зв’язок **1:N**
- **Super Admin (SA)** керує структурою ВНЗ; **адмін кафедри** — операційна робота (як зараз `/Admin`)
- Для **Student, Secretary, Employee** UI **не змінюється**, якщо користувач прив’язаний до однієї кафедри
- Один користувач може мати **обидві ролі**: `SuperAdmin` + `Admin` (кафедра)

## Затверджені бізнес-правила

| # | Правило |
|---|---------|
| R1 | Межа даних — **кафедра**: сесії захисту, групи, студенти, дипломи, annual roles, workload limits |
| R2 | **Факультет 1:N кафедра**; кафедра без факультету — заборонено |
| R3 | **Студент** не може існувати на двох кафедрах (один email = один студентський обліковий запис у ВНЗ) |
| R4 | **Викладач**: один Google-логін (`ApplicationUser`), **кілька профілів на кафедру** (`DepartmentEmployee`); при >1 кафедрі — перемикач контексту |
| R5 | **SA** призначається лише вручну (seed / інший SA); може бути одночасно `Employee` на кафедрі |
| R6 | `AllowedEmailDomains` — **глобально** для ВНЗ |
| R7 | Орг. реквізити: **ВНЗ/міністерство/місто/ректор** — `appsettings:Organization`; **спеціальність, форма навчання, назва кафедри** — сутність `Department`; **назва факультету** — сутність `Faculty` |
| R8 | Існуючі дані мігруються в **default Faculty + Department** з поточного `appsettings.json` |
| R9 | SA: окрема зона `/SuperAdmin` + **«Увійти в кафедру»** → контекст кафедри + звичайний `/Admin` |
| R10 | Імпорт структури: **2-рівневий JSON** (факультети → кафедри) + CRUD для SA |

## Не в scope (ітерація 1)

- Окремі інстанції / subdomain per факультет
- Per-department `AllowedEmailDomains`
- Per-department Google Drive root (залишаємо спільний root + підпапка per кафедра — **опційно phase 1.5**)
- Зміни UI Student / Secretary (окрім умовного перемикача кафедри для Employee з >1 membership)
- Делегування SA на факультет (faculty-level admin)

---

## Модель даних

### Нові сутності

```text
Faculty
  Id              Guid PK
  Name            string (required, max 256)   // «факультет інформаційних технологій»
  IsActive        bool
  CreatedAt       DateTimeOffset

Department
  Id              Guid PK
  FacultyId       Guid FK → Faculty (required)
  Name            string (required)            // «кафедра комп'ютерних систем і мереж»
  SpecialtyCode   string
  SpecialtyName   string
  StudyForm       string                       // «очної форми навчання»
  IsActive        bool
  CreatedAt       DateTimeOffset
  UNIQUE (FacultyId, Name)

DepartmentAdminAssignment
  Id              Guid PK
  DepartmentId    Guid FK
  UserId          Guid FK → ApplicationUser
  AssignedAt      DateTimeOffset
  UNIQUE (DepartmentId, UserId)

DepartmentEmployee          // профіль викладача в межах кафедри
  Id              Guid PK
  DepartmentId    Guid FK
  UserId          Guid FK → ApplicationUser
  FullName        string
  AcademicRank    EmployeeAcademicRank?
  ShortDisplayName string?
  IsActive        bool
  CreatedAt       DateTimeOffset
  UNIQUE (DepartmentId, UserId)
```

### Зміни існуючих сутностей

| Сутність | Зміна |
|----------|-------|
| `DefenceSession` | `+ DepartmentId` (FK, required після міграції) |
| `ApplicationUser` | `AcademicRank`, `ShortDisplayName` для Employee — **deprecated**, читати з `DepartmentEmployee` (поля залишити nullable для backward compat 1 реліз, потім прибрати) |
| `EmployeeSessionWorkloadLimit` | без зміни схеми (employee = `DepartmentEmployee.Id` **або** лишаємо `EmployeeId` = `UserId` + фільтр по department сесії — **рекомендація**: `EmployeeId` → `DepartmentEmployeeId` у phase 1.5; у MVP фільтрувати через session.DepartmentId) |

### Ролі ASP.NET Identity

| Роль | Призначення |
|------|-------------|
| `SuperAdmin` | **нова** — управління факультетами/кафедрами, призначення адмінів |
| `Admin` | адмін **кафедри** (потребує `DepartmentAdminAssignment`) |
| `Employee` | без змін |
| `Student` | без змін |

Користувач може мати `{SuperAdmin}`, `{Admin}`, `{Employee}` одночасно.

### OrganizationOptions (appsettings) — скорочення

Залишаються **лише університетські** поля:

- `MinistryName`, `UniversityName`, `City`, `RectorName`

Видаляються з config (переїжджають у БД):

- `SpecialtyCode`, `SpecialtyName`, `FacultyName`, `StudyForm`, `DepartmentName`

`TopicOrderDocxGenerator` збирає: `OrganizationOptions` + `Faculty.Name` + `Department.*` через `DefenceSession.DepartmentId`.

---

## Контекст кафедри (runtime)

### `IDepartmentContext` (scoped)

```csharp
Guid? CurrentDepartmentId { get; }
bool IsSuperAdminImpersonating { get; }  // SA увійшов в контекст кафедри
```

### Cookie / middleware

| Cookie | Хто | Призначення |
|--------|-----|-------------|
| `dms.dept` | Admin, Employee (multi-dept) | `DepartmentId` поточного контексту |
| `dms.sec.session` | Secretary | без змін (вже є) |

**Правила:**

- `Admin` без cookie → redirect на вибір кафедри (якщо 1 assignment — auto-set)
- `SuperAdmin` у `/SuperAdmin` — cookie не обов’язковий
- SA натискає «Увійти в кафедру» → set `dms.dept` → `/Admin`
- `Employee` з 1 `DepartmentEmployee` → auto-set, UI без змін
- `Employee` з >1 → компактний перемикач (аналог secretary session picker), **єдине виняткове UI**

### Authorization

Новий сервіс `IDepartmentAuthorizationService`:

- `EnsureDepartmentAccess(userId, departmentId)` — admin assignment або SA
- `EnsureSessionInDepartment(sessionId, departmentId)`
- Усі Admin-сервіси отримують `departmentId` з контексту і фільтрують запити

`[Authorize(Roles = Admin)]` лишається; додаткова перевірка в `AdminControllerBase.OnActionExecutionAsync`.

`SuperAdminControllerBase`: `[Authorize(Roles = SuperAdmin)]`.

---

## Super Admin UI (`/SuperAdmin`)

### Навігація

1. **Головна** — огляд факультетів/кафедр
2. **Факультети** — CRUD
3. **Кафедри** — CRUD (фільтр по факультету)
4. **Адміни кафедр** — призначити/зняти (`email` → user lookup)
5. **Імпорт структури** — upload JSON
6. **Увійти в кафедру** — на картці кафедри → set context → `/Admin/Home`

### JSON імпорт (2 рівні)

```json
[
  {
    "name": "Факультет інформаційних технологій",
    "departments": [
      {
        "name": "Кафедра комп'ютерних систем і мереж",
        "specialtyCode": "123",
        "specialtyName": "Комп'ютерна інженерія",
        "studyForm": "очної форми навчання"
      }
    ]
  }
]
```

| Правило імпорту |
|-----------------|
| `name` факультету — required |
| `departments` — required, ≥1 |
| Дублікат `(facultyName, departmentName)` — skip або update (режим на формі: **CreateOnly** / **Upsert**) |
| Неактивні записи не видаляються автоматично |

---

## Admin UI (`/Admin`) — без візуальних змін

- Ті самі сторінки, таблиці, навігація
- Усі запити неявно scoped до `dms.dept`
- `DefenceSessions`, `Students`, `Employees`, `Import`, `AnnualRoles`, `WorkloadLimits` — лише дані поточної кафедри
- Якщо користувач `Admin` кількох кафедр — **dropdown перемикач** у шапці Admin (мінімальний, як secretary session); при 1 кафедрі — прихований

---

## Employee / Student / Secretary

| Роль | Зміна |
|------|-------|
| **Student** | 0 UI; сесія вже в одній кафедрі |
| **Secretary** | 0 UI; annual role прив’язаний до session → department |
| **Employee** | Workflow queries фільтруються по `dms.dept`; дані з `DepartmentEmployee`; при >1 dept — перемикач |

### Рефакторинг Employee

- `IEmployeeHomeService`, `ISupervisorDiplomaListService`, … — приймають `departmentEmployeeId` або `departmentId` з контексту
- `EmployeeHomeQueries.HasAnySupervisorDiplomasAsync` — filter `Diploma.DefenceSession.DepartmentId`
- Імпорт викладачів (Admin) створює `DepartmentEmployee`, не дублює `ApplicationUser` якщо email існує

---

## Міграція даних

### Крок 1 — schema migration

1. Таблиці `faculties`, `departments`, `department_admin_assignments`, `department_employees`
2. `defence_sessions.department_id` nullable

### Крок 2 — data migration (SQL / hosted seeder)

З `OrganizationOptions` (поточний `appsettings.json`):

```
Faculty.Name          ← Organization.FacultyName
Department.Name       ← Organization.DepartmentName
Department.Specialty* ← Organization.Specialty*
Department.StudyForm  ← Organization.StudyForm
```

1 default faculty + 1 default department

### Крок 3 — backfill

| Дані | Дія |
|------|-----|
| Усі `DefenceSession` | `DepartmentId` = default department |
| Усі `ApplicationUser` з `UserKind.Employee` | `DepartmentEmployee` у default department (копія FullName, Rank, ShortName) |
| Користувачі з роллю `Admin` | `DepartmentAdminAssignment` → default department |
| Bootstrap email | + роль `SuperAdmin` |

### Крок 4 — enforce

- `defence_sessions.DepartmentId` NOT NULL — міграція `MakeDefenceSessionDepartmentRequired`
  (backfill NULL → default department, потім `ALTER ... SET NOT NULL`)
- Модель домену: `DefenceSession.DepartmentId` = `Guid` (не nullable); config `.IsRequired()`
- Унікальний індекс студента: email вже global — достатньо для R3

> **Порядок деплою на існуючу БД (expand-contract):**
> 1. `dotnet ef database update AddMultiTenancyOrganization` — додає nullable колонку
> 2. Запустити застосунок → hosted seeder створює default faculty/department і backfill сесій
> 3. `dotnet ef database update` — застосовує `MakeDefenceSessionDepartmentRequired` (NOT NULL)
>
> На чистій БД (без legacy сесій) обидві міграції можна застосувати одразу — backfill зачіпає 0 рядків.

---

## План імплементації (фази)

### Фаза 1 — Domain + Infrastructure

1. Сутності `Faculty`, `Department`, `DepartmentAdminAssignment`, `DepartmentEmployee`
2. EF configurations, `DbSet`, міграція schema
3. `IDepartmentContext` + middleware + cookie helpers
4. Data migration seeder `DefaultOrganizationMigrator`

### Фаза 2 — Application core

5. `RoleNames.SuperAdmin`
6. `IDepartmentAuthorizationService`
7. `IFacultyAdminService`, `IDepartmentAdminService` (SA CRUD)
8. `IOrganizationStructureImportService` (JSON)
9. `IDepartmentAdminAssignmentService`
10. Оновити `DefenceSessionService` — filter/create by department
11. Оновити `EmployeeAdminService`, `EmployeeImportService` → `DepartmentEmployee`
12. Оновити `StudentAdminService` — ensure session in department
13. Оновити `TopicOrderDocxGenerator` — department + faculty з БД
14. Оновити `EmployeeHomeService`, `DiplomaAuthorizationService`, queries — department filter
15. `BootstrapAdminSeeder` — SuperAdmin role

### Фаза 3 — Web SuperAdmin

16. Area `SuperAdmin` (controllers, views, navigation)
17. Faculties / Departments CRUD
18. Department admins assignment
19. JSON import page
20. «Увійти в кафедру» action

### Фаза 4 — Web Admin / Employee context

21. `AdminControllerBase` — inject `IDepartmentContext`, enforce access
22. Department switcher (Admin, conditional Employee)
23. Employee middleware — resolve `DepartmentEmployee` from context
24. Оновити всі Admin controllers (мінімально — base class)

### Фаза 5 — Тести + docs

25. Повний набір тестів (див. нижче)
26. `appsettings` example оновити
27. `User-Manual` — розділ SA (коротко)

---

## Повний перелік тестів

### Domain.Tests

| ID | Тест |
|----|------|
| D1 | `Faculty` / `Department` — валідація імен (якщо є domain rules) |

### Application.Tests

| ID | Тест |
|----|------|
| A1 | `FacultyAdminService` — Create, Update, Deactivate, list |
| A2 | `DepartmentAdminService` — Create under faculty, unique name per faculty |
| A3 | `OrganizationStructureImportService` — valid 2-level JSON creates faculties + departments |
| A4 | `OrganizationStructureImportService` — invalid JSON (empty departments, missing name) |
| A5 | `OrganizationStructureImportService` — Upsert mode updates existing |
| A6 | `DepartmentAdminAssignmentService` — assign, remove, list by department |
| A7 | `DepartmentAuthorizationService` — admin can access own department |
| A8 | `DepartmentAuthorizationService` — admin cannot access other department |
| A9 | `DepartmentAuthorizationService` — SA can access any when impersonating |
| A10 | `DefenceSessionService.GetAllAsync` — returns only current department sessions |
| A11 | `DefenceSessionService.CreateAsync` — sets DepartmentId from context |
| A12 | `EmployeeAdminService.GetAllAsync` — only `DepartmentEmployee` in department |
| A13 | `EmployeeImportService` — existing email adds second `DepartmentEmployee` on another dept |
| A14 | `EmployeeImportService` — same dept duplicate email fails |
| A15 | `StudentAdminService` — cannot attach student to session from other department |
| A16 | `TopicOrderDocxGenerator` — uses Faculty.Name + Department fields (not appsettings specialty) |
| A17 | `DefaultOrganizationMigrator` — maps OrganizationOptions to default entities |
| A18 | `EmployeeHomeService` — cards only for current department diplomas |
| A19 | `DiplomaAuthorizationService` — supervisor in dept A cannot act on dept B diploma |

### Infrastructure.Tests (новий або в Integration)

| ID | Тест |
|----|------|
| I1 | Migration backfill — all sessions have DepartmentId |
| I2 | Migration backfill — all employees have DepartmentEmployee |
| I3 | Unique `(DepartmentId, UserId)` on DepartmentEmployee |

### Web.Tests

| ID | Тест |
|----|------|
| W1 | `SuperAdmin` area — 403 without SuperAdmin role |
| W2 | `Admin` area — 403 without department context (multi-admin user) |
| W3 | `Admin` area — auto-set context when single assignment |
| W4 | `DepartmentContextCookie` — serialize/deserialize |
| W5 | `EmployeeRoleNavigationBuilder` — без змін (regression) |
| W6 | `SuperAdminNavigation` — links order |
| W7 | `OrganizationStructureImportViewModel` validator |

### Integration.Tests (PostgreSQL)

| ID | Тест |
|----|------|
| INT1 | `SeedDefaultOrganization` — після migrate є 1 faculty + 1 department |
| INT2 | **Isolation** — dept A admin не бачить sessions dept B (HTTP 404/403) |
| INT3 | **Isolation** — dept A student diploma не видно dept B admin |
| INT4 | SA створює 2 кафедри, 2 sessions — кожен admin бачить лише свою |
| INT5 | SA JSON import — 2 faculties, 3 departments |
| INT6 | SA призначає admin на кафедру — user отримує доступ до `/Admin` |
| INT7 | SA «увійти в кафедру» — cookie + `/Admin/DefenceSessions` 200 |
| INT8 | User з `{SuperAdmin, Admin}` — обидві зони доступні |
| INT9 | Employee з 2 `DepartmentEmployee` — workflow лише в обраній кафедрі |
| INT10 | Employee switch department — бачить інших студентів |
| INT11 | Студент email існує в dept A — імпорт того ж email в dept B fails |
| INT12 | `TopicOrder` endpoint — preamble містить specialty з Department |
| INT13 | Regression: повний `IntegrationScenario` на default department — workflow green |
| INT14 | Bootstrap seeder — SuperAdmin role на `Bootstrap:AdminEmail` |
| INT15 | `EmployeeHomeEndpoint` — regression після department filter |
| INT16 | `UnifiedUiEndpoint` — regression Admin/Employee nav |

### Регресія

Усі існуючі тести (561+ unit, 142 integration) мають залишитись зеленими після оновлення fixture:

- `IntegrationScenarioBuilder` — прив’язка до default department
- `PostgreSqlFixture` — seed SuperAdmin role
- `IntegrationTestWebClient` — set `dms.dept` cookie для Admin clients

---

## Критерії приймання

- [ ] SA CRUD факультетів і кафедр
- [ ] SA імпорт 2-рівневого JSON
- [ ] SA призначає адмінів кафедр
- [ ] SA може увійти в контекст кафедри і працювати в `/Admin`
- [ ] Один user: `SuperAdmin` + `Admin` одночасно
- [ ] Адмін кафедри бачить лише свої дані; UI ідентичний поточному
- [ ] Викладач: один логін, профілі per кафедра, перемикач при >1
- [ ] Студент не дублюється між кафедрами
- [ ] Міграція: default faculty/department з `appsettings`
- [ ] Накази (TopicOrder) — specialty/department/faculty з БД
- [ ] Усі тести з переліку вище + існуючі — зелені

---

## Ризики та мітігація

| Ризик | Мітігація |
|-------|-----------|
| Великий refactor Employee queries | `IDepartmentContext` + єдиний query extension `WhereDepartment` |
| Regression у 142 integration tests | Default department у fixture; фаза 5 окремим PR-кроком |
| `ApplicationUser.AcademicRank` duplication | Читати з `DepartmentEmployee`; deprecate поля на user |
| SA бачить PII при impersonate | Audit log на «enter department context» |

---

## Оцінка обсягу

| Фаза | Орієнтовно |
|------|------------|
| 1 Domain + Infra | 2–3 дні |
| 2 Application | 3–4 дні |
| 3 SuperAdmin UI | 2 дні |
| 4 Admin/Employee context | 2–3 дні |
| 5 Тести + стабілізація | 2–3 дні |
| **Разом** | **~11–15 днів** |

---

## Наступний крок

Після **затвердження плану** — імплементація **фаза 1 → 5** послідовно, кожна фаза з окремим комітом і зеленими тестами.

---

## Керівництво SuperAdmin (операційне)

### Перший запуск

1. Переконатись, що в `appsettings` заповнені секції `Organization` та `Bootstrap:AdminEmail`.
2. **Чиста БД:** застосувати всі міграції одразу (`dotnet ef database update`).
   **Існуюча БД з даними:** застосувати expand-contract у 3 кроки (див. «Міграція даних → Крок 4»).
3. Після migrate + перший старт застосунку автоматично створюються default faculty + department
   з `Organization:FacultyName` / `Organization:DepartmentName`, і backfill legacy сесій.
4. Користувач з `Bootstrap:AdminEmail` отримує роль `SuperAdmin`.
5. `DefenceSession.DepartmentId` — обов'язкове (NOT NULL): сесія завжди належить кафедрі.

### Зони доступу

| Зона | Роль | Контекст кафедри |
|------|------|------------------|
| `/SuperAdmin` | `SuperAdmin` | не потрібен |
| `/Admin` | `Admin` або SA з impersonation | cookie `dms.dept` |
| `/Employee` | `Employee` | cookie `dms.dept` |

### Типові дії SA

1. **Факультети / кафедри** — CRUD у `/SuperAdmin/Faculties`, `/SuperAdmin/Departments`.
2. **Призначення адмінів** — `/SuperAdmin/DepartmentAdmins`.
3. **Імпорт структури** — `/SuperAdmin/OrganizationImport` (JSON: faculties → departments).
4. **Увійти в кафедру** — кнопка на списку кафедр → cookie `dms.dept` + `dms.dept.sa` → redirect `/Admin/Home`.

### Конфігурація (`appsettings`)

```json
"Organization": {
  "UniversityName": "...",
  "FacultyName": "...",
  "DepartmentName": "...",
  "SpecialtyCode": "123",
  "SpecialtyName": "..."
},
"Department": {
  "SelectedDepartmentCookieName": "dms.dept",
  "ImpersonationCookieName": "dms.dept.sa"
}
```

Університетські поля — у `Organization`; спеціальність/кафедра/факультет для документів — у БД (`Department` / `Faculty`).

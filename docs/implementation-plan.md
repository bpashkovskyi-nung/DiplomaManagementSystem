# Implementation Plan — v1

ASP.NET Core MVC + PostgreSQL + .NET 10.  
Бізнес-вимоги: [wiki](https://github.com/bpashkovskyi/diploma-management-system/wiki).  
Модель: [domain-model.md](domain-model.md).  
Імена: [naming-glossary.md](naming-glossary.md).

---

## 0. Стек і архітектура

### 0.1 Рішення

| Компонент | Вибір |
|-----------|--------|
| Web | ASP.NET Core **MVC** + **Areas** |
| DB | PostgreSQL 16+ |
| ORM | EF Core 10 + Npgsql |
| Auth | Google OAuth 2.0 |
| Files | Google Drive API (v2, реалізовано) |
| Deploy | Docker на Linux |

MVC + Areas: `Admin`, `Secretary`, `Student`, `Employee` — достатньо для v1 без SPA.

### 0.2 Solution structure

```
src/
  DiplomaManagementSystem.Domain
  DiplomaManagementSystem.Application      # use cases, DTO, validators
    Admin/{Feature}/                       # *Service + validators
    Admin/{Feature}/Dtos/                  # …Application.Admin.{Feature}.Dtos
    Admin/{Feature}/Contracts/             # …Application.Admin.{Feature}.Contracts
    {Area}/Dtos/                           # …Application.{Area}.Dtos
    {Area}/Contracts/                      # …Application.{Area}.Contracts
    Persistence/Contracts/                 # …Application.Persistence.Contracts
  DiplomaManagementSystem.Infrastructure   # EF, migrations, query impl, Drive
  DiplomaManagementSystem.Web
tests/
  DiplomaManagementSystem.Domain.Tests
  DiplomaManagementSystem.Application.Tests
  DiplomaManagementSystem.Web.Tests
```

### 0.3 Шари

- **Domain** — сутності, enums, domain services, інваріанти.
- **Application** — use cases (`*Service`), DTO, FluentValidation; **інтерфейси** — у `{Area}/Contracts/`; read-моделі запитів — у `Persistence/`.
- **Infrastructure** — `ApplicationDbContext`, EF configurations, **міграції**, **query-класи**, Google Drive, Identity seed, `AddPersistence()`.
- **Web** — Controllers, ViewModels, Views, localization `uk-UA`.

#### 0.3.1 Contracts, Persistence і queries

| Що | Де | Namespace |
|----|-----|-----------|
| `I*Service`, cross-cutting interfaces | `Application/{Area}/Contracts/` | `…Application.{Area}.Contracts` |
| Admin `I*Service` | `Application/Admin/{Feature}/Contracts/` | `…Application.Admin.{Feature}.Contracts` |
| `IApplicationDbContext`, `I*Queries` | `Application/Persistence/Contracts/` | `…Application.Persistence.Contracts` |
| `QueryModels`, `DiplomaWritableCriteria` | `Application/Persistence/` | `…Application.Persistence` |
| `*Service` (реалізації) | `Application/{Area}/` або `Application/Admin/{Feature}/` | `…Application.{Area}.{Feature}` |
| DTO | `Application/{Area}/Dtos/` або `Application/Admin/{Feature}/Dtos/` | `…Application.{Area}.{Feature}.Dtos` |
| `ApplicationDbContext`, migrations, `*Queries.cs` | `Infrastructure/Persistence/` | — |

**Приклад Admin (feature folder):**

```
Application/Admin/AnnualRoles/
  AnnualRoleService.cs
  AssignAnnualRoleValidator.cs
  Dtos/AnnualRoleDtos.cs           → Application.Admin.AnnualRoles.Dtos
  Contracts/IAnnualRoleService.cs  → Application.Admin.AnnualRoles.Contracts
```

**Правило:** read-only доступ у workflow-сервісах — через `I*Queries`; запис і транзакції — через `IApplicationDbContext` (tracked entities). Admin CRUD поки що може звертатися до `IApplicationDbContext` напряму.

**DI:**

```csharp
// Web Program.cs
services.AddApplication();      // use cases
services.AddInfrastructure();   // → AddPersistence() + file storage + …
```

**EF migrations:**

```bash
dotnet ef migrations add <Name> \
  --project src/DiplomaManagementSystem.Infrastructure \
  --startup-project src/DiplomaManagementSystem.Web
```

#### 0.3.2 Статус міграції на queries (фаза 2)

| Область | Статус |
|---------|--------|
| Secretary list / dashboard / report / details (read) | ✓ queries |
| Employee home, admission review, supervisor / head workflow | ✓ queries |
| Student diploma (read + lifecycle reads) | ✓ queries |
| `DiplomaLifecycleHelper` | ✓ `IAdmissionStepQueries` |
| Admin CRUD, import | `IApplicationDbContext` (навмисно) |

#### 0.3.3 Application layering trade-off (F-06)

Класичний Clean Architecture передбачає, що **Application** залежить лише від Domain і власних abstractions, а EF / Identity / ASP.NET живуть у Infrastructure і Web. У цьому репозиторії свідомо обрано **прагматичний варіант v1**:

| Що в Application | Чому так | Майбутнє (не v1) |
|------------------|----------|------------------|
| `FrameworkReference` на `Microsoft.AspNetCore.App` | `UserManager`, Identity types у import і workflow | Винести identity-операції за `IUserAccountService` |
| `IApplicationDbContext` + tracked writes у `*Service` | Менше mapping-шару для Admin CRUD і import | Repository / unit-of-work у Infrastructure |
| FluentValidation + use cases в одному проєкті | Швидша доставка feature-ів | Розділити read/write ports за потреби |

**Правило для нового коду:** read-only orchestration — через `I*Queries` у `Persistence/Contracts`; cross-cutting Web concerns (session, claims, ViewComponents) — у Web або нові Application contracts, **не** прямий `DbContext` у Razor. Повна міграція DbContext з Application — **XL / high risk**, не планується до стабілізації workflow.

---

## 1. Domain layer

### 1.1 Enums

```csharp
enum DefenceSessionType { Bachelor, Master }
enum DefenceSessionStatus { Active, Archived }
enum UserKind { Student, Employee }
enum AnnualRoleType {
    DepartmentHead,
    ExamCommissionSecretary,   // UI: Секретар ДЕК
    AntiPlagiarismOfficer,
    FormattingReviewer         // UI: Нормоконтролер
}
enum SupervisorAssignmentStatus { Pending, Confirmed, Rejected }
enum ReviewAssignmentStatus { NotAssigned, Assigned, Completed }
enum DiplomaLifecycleStatus {
    AwaitingSupervisor, SupervisorConfirmed, TopicInReview,
    TopicApproved, WorkInProgressByStudent, DocumentsInProgress,
    ReadyForAdmission, Admitted
}
enum DiplomaAdmissionStatus { NotAdmitted, Admitted }
enum TopicVersionStatus { PendingSupervisor, PendingHead, Approved, Rejected }
enum AdmissionStep {
    SupervisorFeedback, ExternalReview, AntiPlagiarismClearance, FormattingReview
}
enum CheckpointOutcome { Approved, NotApproved }
```

### 1.2 Domain services

| Service | Відповідальність |
|---------|------------------|
| `DiplomaCreationService` | Створення дипломів при прив'язці групи до сесії; 1 диплом / студент / рік |
| `DiplomaTopicService` | Версії теми; блок після `Approved` |
| `AdmissionWorkflowService` | Кроки допуску (`AdmissionStep`), спроби, outcomes |
| `DiplomaLifecycleService` | Дозволені переходи `DiplomaLifecycleStatus` |
| `ReviewerAssignmentService` | Призначення рецензента |
| `CheckpointCompletionService` | Завершення кроків допуску |

### 1.3 Audit

`AuditLog` для override секретаря та критичних змін.

---

## 2. Database (PostgreSQL + EF Core)

### 2.1 Таблиці

| Таблиця | Примітки |
|---------|----------|
| `academic_years` | `label` unique |
| `study_groups` | `defence_session_id` FK NOT NULL, unique `(defence_session_id, name)` |
| `defence_sessions` | |
| `users` | Identity + `user_kind`, `study_group_id` |
| `annual_role_assignments` | unique `(defence_session_id, role_type)` |
| `diplomas` | unique `(student_id, academic_year_id)` через join до session year |
| `diploma_topic_versions` | |
| `diploma_admission_step_attempts` | історія кроків допуску |
| `diploma_documents` | Google Drive metadata |
| `diploma_comments` | |
| `audit_logs` | |

### 2.2 Індекси

- `diplomas(defence_session_id)`, `diplomas(student_id)`
- `users(email)` unique
- `study_groups(defence_session_id)`

### 2.3 Міграції

- Проєкт: **`DiplomaManagementSystem.Infrastructure`**
- Seed admin / dev data — `BootstrapAdminSeeder` (Infrastructure).

---

## 3. Автентифікація

1. Google OIDC.
2. Match user by email (після імпорту).
3. `Bootstrap:AdminEmail` у конфігу.
4. `AllowedEmailDomains` — whitelist.
5. Identity roles: `Admin`, `Student`, `Employee` + контекстні перевірки через `AnnualRoleAssignment` та `Diploma`.

---

## 4. Функціональні модулі

### 4.1 Admin

| Feature | Опис |
|---------|------|
| Import students | CSV/XLSX: ПІБ, email, group |
| Import employees | ПІБ, email |
| Academic years | CRUD |
| Defence sessions | Create: year, type, semester; перегляд груп сесії |
| Study groups | CRUD у межах обраної сесії (`DefenceSessionId` обов'язковий) |
| Annual roles | 4 ролі на рік |
| Archive session | `DefenceSessionStatus.Archived` |
| Admin preview | Перегляд UI від імені ролей (dev/support) |

**Правило групи:** група завжди належить одній сесії; назва унікальна в межах сесії.

### 4.2 Secretary (`ExamCommissionSecretary`)

| Feature | Опис |
|---------|------|
| Session selector | Поточна активна сесія в header |
| Dashboard | Лічильники за lifecycle / admission steps |
| Diploma list | Таблиця + фільтри + checklist partial |
| Diploma details | Hub: керівник override, рецензент, admission steps, admit, comments |
| Assign reviewer | `ReviewAssignmentStatus` |
| Admit | `AdmissionStatus`, `DefenceDate`, `LifecycleStatus.Admitted` |
| Admitted report | Список допущених (HTML + CSV опційно) |

> **Примітка:** окремий «supervisor pool» прибрано — студент обирає керівника зі списку викладачів.

### 4.3 Student

| Feature | Опис |
|---------|------|
| My diploma | Одна картка + workflow progress |
| Select supervisor | Зі списку викладачів |
| Submit topic | Після підтвердження керівником |
| Upload work | PDF/DOCX/ODT → Google Drive |
| View status | Checklist, тема, коментарі, документи |

### 4.4 Employee

| Роль | Feature |
|------|---------|
| Supervisor | Confirm/reject student; approve/reject topic; `SupervisorFeedback` step + файл |
| DepartmentHead | Approve/reject topic `PendingHead` |
| Reviewer | Complete `ExternalReview` step + файл |
| AntiPlagiarismOfficer | Complete `AntiPlagiarismClearance` |
| FormattingReviewer | `FormattingReview` outcome + comment |

### 4.5 Admission steps (з файлами)

UI: завантаження документа + фіксація кроку; нормоконтроль — форма з outcome.  
Секретар може override крок → `AuditLog`.

Після кожної зміни → `DiplomaLifecycleService` + `DiplomaLifecycleHelper.RecalculateAsync`.

---

## 5. Authorization

`IDiplomaAuthorizationService` + `DiplomaAction` enum — **єдиний шлюз** для перевірки «чи може користувач виконати дію над дипломом у сесії» (supervisor, head, secretary, admission reviewers).

Матриця прав — як у [wiki v1-Roles](https://github.com/bpashkovskyi/diploma-management-system/wiki/v1-Roles-and-Permissions), з заміною імен ролей.

| Рівень | Статус v1 |
|--------|-----------|
| MVC `[Authorize(Roles = …)]` на areas | ✓ coarse gate (Admin / Employee / Student) |
| `DiplomaAction` у workflow services | ✓ supervisor, head, secretary, admission |
| ASP.NET policies на sub-roles (`DepartmentHead`, `FormattingReviewer`, …) | backlog (F-58) — поки sub-role визначається в сервісі + annual role queries |

`IArchiveGuard` — block writes на archived session.

---

## 6. UI / Localization

- Bootstrap 5, адаптивна таблиця дипломів.
- Partials: admission checklist, lifecycle badge, topic history.
- Усі підписи ролей через ресурси / `UkrainianDisplay` → [naming-glossary.md](naming-glossary.md).

---

## 7. Тестування

| Рівень | Фокус |
|--------|-------|
| Domain | Lifecycle, topic immutability, group-session constraint |
| Application | Workflow guidance, authorization, query-backed services |
| Integration | Testcontainers PG, full admit flow |

**Acceptance scenarios:**
1. Import → session + groups → diplomas created.
2. Supervisor flow → topic → head approve.
3. 4 admission steps → ready → secretary admit.
4. Group cannot join two sessions.
5. Archived session read-only.
6. Student work upload → Drive folder hierarchy.

---

## 8. DevOps (Linux)

```yaml
services:
  app:       # .NET 10
  postgres:  # volume
  nginx:     # reverse proxy
```

Health: `/health` (DB).  
Backups: `pg_dump` + Drive metadata у БД.

---

## 9. Sprint backlog

| Sprint | Deliverable |
|--------|-------------|
| S1 | Solution, Domain, EF, Docker PG |
| S2 | Google auth, import, bootstrap admin |
| S3 | AcademicYear, DefenceSession, group assign, diplomas, annual roles |
| S4 | Secretary: list, dashboard |
| S5 | Student: supervisor, topic |
| S6 | Supervisor + department head topic flow |
| S7 | Admission steps + lifecycle recalc |
| S8 | Secretary: reviewer, admit, report |
| S9 | Comments, audit, archive, auth hardening |
| S10 | Tests, uk-UA polish, UAT |
| S11+ | Drive documents, query layer, Infrastructure persistence split |

---

## 10. v2 (частково в репо)

| Feature | Статус |
|---------|--------|
| Файли документів (Google Drive) | ✓ |
| Завантаження дипломної роботи студентом | ✓ |
| Декларація про використання ШІ (Gaidet) | planned |
| Excel export | planned |

Деталі: [wiki v2-roadmap](https://github.com/bpashkovskyi/diploma-management-system/wiki/v2-Roadmap).

---

## 11. Деталізація (Part 2)

| Артефакт | Файл | Статус |
|----------|------|--------|
| Routing table (Controller/Action) | [routing-table.md](routing-table.md) | ✓ |
| DDL draft (PostgreSQL) | [ddl-draft.sql](ddl-draft.sql) | частково застарілий |
| ViewModels per screen | [viewmodels.md](viewmodels.md) | ✓ |
| `appsettings` template | [appsettings.template.json](appsettings.template.json) | ✓ |
| Code style / tech debt | [code-style-audit.md](code-style-audit.md) | ✓ |

### 11.1 Routing — короткий огляд

4 Areas + shared: **Admin** (CRUD, import), **Secretary** (dashboard, diplomas, admit), **Student** (my diploma), **Employee** (supervisor, head, reviewer, anti-plagiarism, formatting).  
Повна таблиця: 50+ маршрутів з HTTP, policy, sprint — [routing-table.md](routing-table.md).

### 11.2 DDL — короткий огляд

11+ доменних таблиць + Identity (`users` розширює AspNetUsers).  
Enum-и → `smallint`. Актуальна схема — EF migrations у `Infrastructure/Persistence/Migrations/`.  
Чернетка: [ddl-draft.sql](ddl-draft.sql).

### 11.3 ViewModels — короткий огляд

~35 ViewModels / screen models, згруповані за Area.  
Спільні partials: admission checklist, topic version, comment items.  
Деталі + валідація: [viewmodels.md](viewmodels.md).

### 11.4 Configuration

Секції: `ConnectionStrings`, `Authentication:Google`, `Bootstrap:AdminEmail`, `Security:AllowedEmailDomains`, `Localization`, `Secretary` (cookie сесії), `Import`, `FileStorage` (Drive).  
Шаблон: [appsettings.template.json](appsettings.template.json).  
У prod — User Secrets / env vars (`ConnectionStrings__DefaultConnection`, `Authentication__Google__ClientSecret`, тощо).

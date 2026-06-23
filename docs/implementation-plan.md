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
| Deploy | Docker на Linux |

MVC + Areas: `Admin`, `Secretary`, `Student`, `Employee` — достатньо для v1 без SPA.

### 0.2 Solution structure

```
src/
  DiplomaManagementSystem.Domain
  DiplomaManagementSystem.Application
  DiplomaManagementSystem.Infrastructure
  DiplomaManagementSystem.Web
tests/
  DiplomaManagementSystem.Domain.Tests
  DiplomaManagementSystem.Application.Tests
  DiplomaManagementSystem.Web.Tests
```

### 0.3 Шари

- **Domain** — сутності, enums, domain services, інваріанти.
- **Application** — use cases (handlers), DTO, FluentValidation, `IDiplomaAuthorizationService`.
- **Infrastructure** — EF Core, Google auth, CSV/Excel import, (v2: Google Drive).
- **Web** — Controllers, ViewModels, Views, localization `uk-UA`.

**v1 без `IFileStorage`** — checkpoint-и без файлів.

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
    TopicApproved, DocumentsInProgress, ReadyForAdmission, Admitted
}
enum DiplomaAdmissionStatus { NotAdmitted, Admitted }
enum TopicVersionStatus { PendingSupervisor, PendingHead, Approved, Rejected }
enum AdmissionCheckpointType {
    SupervisorFeedback, ExternalReview, AntiPlagiarismClearance, FormattingReview
}
enum FormattingReviewOutcome { Approved, ApprovedWithRemarks, NotApproved }
```

### 1.2 Domain services

| Service | Відповідальність |
|---------|------------------|
| `DiplomaCreationService` | Створення дипломів при прив'язці групи до сесії; 1 диплом / студент / рік |
| `DiplomaTopicService` | Версії теми; блок після `Approved` |
| `AdmissionReadinessService` | `ReadyForAdmission` за формулою з domain-model |
| `DiplomaLifecycleService` | Дозволені переходи `DiplomaLifecycleStatus` |
| `StudyGroupAssignmentService` | Перевірка: група ще не в іншій сесії |

### 1.3 Audit

`AuditLog` для override секретаря та критичних змін.

---

## 2. Database (PostgreSQL + EF Core)

### 2.1 Таблиці

| Таблиця | Примітки |
|---------|----------|
| `academic_years` | `label` unique |
| `study_groups` | `name` unique, `defence_session_id` FK nullable, **unique** на `defence_session_id` якщо 1:1 на групу — насправді багато груп → одна сесія: unique на `(id)` де session_id set; constraint: одна група не в двох сесіях = `defence_session_id` просто FK без duplicate group ids |
| `defence_sessions` | |
| `users` | Identity + `user_kind`, `study_group_id` |
| `annual_role_assignments` | unique `(academic_year_id, role_type)` |
| `supervisor_pool_entries` | unique `(defence_session_id, employee_id)` |
| `diplomas` | unique `(student_id, academic_year_id)` через join до session year |
| `diploma_topic_versions` | |
| `diploma_admission_checkpoints` | unique `(diploma_id, type)` |
| `diploma_comments` | |
| `audit_logs` | |

### 2.2 Індекси

- `diplomas(defence_session_id)`, `diplomas(student_id)`
- `users(email)` unique
- `study_groups(defence_session_id)`

### 2.3 Міграції

- `InitialCreate` → seed `AcademicYear` опційно в dev.

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
| Defence sessions | Create: year, type, semester; assign **groups** |
| Group → session | При assign: `StudyGroup.DefenceSessionId = session`, create `Diploma` per student |
| Annual roles | 4 ролі на рік |
| Archive session | `DefenceSessionStatus.Archived` |

**Правило групи:** якщо `DefenceSessionId != null` — не можна прив'язати до іншої сесії без відв'язки адміном.

### 4.2 Secretary (`ExamCommissionSecretary`)

| Feature | Опис |
|---------|------|
| Session selector | Поточна активна сесія в header |
| Supervisor pool | CRUD викладачів у пулі |
| Dashboard | Лічильники: без керівника, без теми, без checkpoint-ів, ready, admitted |
| Diploma list | Таблиця + фільтри + checklist partial |
| Diploma details | Hub: керівник override, рецензент, checkpoints, admit, comments |
| Assign reviewer | `ReviewAssignmentStatus` |
| Admit | `AdmissionStatus`, `DefenceDate`, `LifecycleStatus.Admitted` |
| Admitted report | Список допущених (HTML + CSV опційно) |

### 4.3 Student

| Feature | Опис |
|---------|------|
| My diploma | Одна картка |
| Select supervisor | З пулу сесії |
| Submit topic | Після `SupervisorConfirmed` |
| View status | Checklist, тема, коментарі |

### 4.4 Employee

| Роль | Feature |
|------|---------|
| Supervisor | Confirm/reject student; approve/reject topic; complete `SupervisorFeedback` checkpoint |
| DepartmentHead | Approve/reject topic `PendingHead` |
| Reviewer | Complete `ExternalReview` checkpoint |
| AntiPlagiarismOfficer | Complete `AntiPlagiarismClearance` |
| FormattingReviewer | `FormattingReview` outcome + comment |

### 4.5 Checkpoints (v1, без файлів)

UI: кнопка «Зафіксувати» / форма для нормоконтролю.  
Секретар може override → `AuditLog`.

Після кожної зміни → `DiplomaLifecycleService.RecalculateAsync`.

---

## 5. Authorization

`IDiplomaAuthorizationService` + `DiplomaAction` enum.

Матриця прав — як у [wiki v1-Roles](https://github.com/bpashkovskyi/diploma-management-system/wiki/v1-Roles-and-Permissions), з заміною імен ролей.

`IArchiveGuard` — block writes на archived session.

---

## 6. UI / Localization

- Bootstrap 5, адаптивна таблиця дипломів.
- Partials: `_CheckpointChecklist`, `_LifecycleBadge`, `_TopicHistory`.
- Усі підписи ролей через `IStringLocalizer` → [naming-glossary.md](naming-glossary.md).

---

## 7. Тестування

| Рівень | Фокус |
|--------|-------|
| Domain | Readiness, topic immutability, group-session constraint |
| Application | Import, authorization |
| Integration | Testcontainers PG, full admit flow |

**Acceptance scenarios:**
1. Import → session + groups → diplomas created.
2. Supervisor flow → topic → head approve.
3. 4 checkpoints → ready → secretary admit.
4. Group cannot join two sessions.
5. Archived session read-only.

---

## 8. DevOps (Linux)

```yaml
services:
  app:       # .NET 10
  postgres:  # volume
  nginx:     # reverse proxy
```

Health: `/health` (DB).  
Backups: `pg_dump` + (v2) Drive metadata.

---

## 9. Sprint backlog

| Sprint | Deliverable |
|--------|-------------|
| S1 | Solution, Domain, EF, Docker PG |
| S2 | Google auth, import, bootstrap admin |
| S3 | AcademicYear, DefenceSession, group assign, diplomas, annual roles |
| S4 | Secretary: pool, list, dashboard |
| S5 | Student: supervisor, topic |
| S6 | Supervisor + department head topic flow |
| S7 | Admission checkpoints + lifecycle recalc |
| S8 | Secretary: reviewer, admit, report |
| S9 | Comments, audit, archive, auth hardening |
| S10 | Tests, uk-UA polish, UAT |

---

## 10. v2 (out of scope v1, в репо для контексту)

| Feature | Технологія |
|---------|------------|
| Файли документів | **Google Drive API** |
| Декларація про використання ШІ | **Gaidet** інтеграція |
| Файл дипломної роботи | Drive |
| Excel export | ClosedXML |

Деталі: [wiki v2-roadmap](https://github.com/bpashkovskyi/diploma-management-system/wiki/v2-Roadmap).

---

## 11. Наступні кроки docs (Part 2)

- [ ] Повний routing table (Controller/Action)
- [ ] DDL draft
- [ ] ViewModels per screen
- [ ] `appsettings` template

Напишіть **continue** для Part 2.

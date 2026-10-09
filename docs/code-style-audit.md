# Code Style & Maintainability Audit — Consolidated

> **Архів.** Актуальні правила стилю та якості: **[coding-style.md](coding-style.md)**.  
> Цей документ зберігає історичні findings (F-01–F-76); не використовуйте його як джерело enforcement.

**Дата:** 2026-06-30  
**Джерела:** [Pass 1](code-style-audit-pass1.md) · [Pass 2](code-style-audit-pass2.md) · [Pass 3](code-style-audit-pass3.md)  
**Scope:** Solution-wide maintainability (не security/performance)  
**Оцінка:** Adequate → Needs Improvement  
**Findings:** F-01 — F-76 (76 шт., після дедуплікації — 72 унікальних теми)

---

## Executive Summary

| Сильні сторони | Слабкі місця |
|----------------|--------------|
| Domain services фокусовані, з тестами | Монолітні read-orchestration методи (Secretary/Student details) |
| Areas за ролями, `sealed`/`internal`, primary constructors | Дубль POST/mapping/`GetUserId` у Employee controllers |
| `AdmissionReviewService` — єдиний checkpoint orchestrator | `DiplomaAction` частково dead code; auth розірвана |
| `SecretaryControllerBase`, `CheckpointCompletionHelper` | Import без транзакції; checkpoint SaveChanges до upload |
| Naming + `naming-glossary.md`, FluentValidation, records | Немає `.editorconfig`/CI; `viewmodels.md` відстає |

**Найвищий ROI:** `EmployeeControllerBase` → `_TempDataAlerts` → import transaction → mapper extraction → `DiplomaAction` wiring → checkpoint+file transaction.

---

## Статистика за пріоритетом

| Priority | Кількість | Effort (орієнтир) |
|----------|-----------|-------------------|
| **P1** | 12 | 1×XS, 3×S, 6×M, 1×L, 1×XL* |
| **P2** | 44 | XS–M |
| **P3** | 20 | XS–S |

\* F-06 — архітектурний trade-off, **не мігрувати зараз**.

---

## Дедуплікація (пов'язані findings)

| Головний ID | Дублікати / продовження | Суть |
|-------------|-------------------------|------|
| F-37 / F-43 | F-05 | `EmployeeControllerBase` + `GetUserId()` |
| F-04 / F-44 | — | Checkpoint POST pipeline ×4 |
| F-01 / F-74 | F-54 | Моноліт `SecretaryDiplomaDetailsService` + `MapDetails` |
| F-09 / F-69 | F-54, F-68, F-71 | Mapping у controllers vs `Web/Mapping` |
| F-32 / F-62 | — | TempData alert block ×14+ |
| F-38 / F-55 | — | Flash: TempData vs ViewData |
| F-35 / F-66 | — | Hardcoded role names у ViewComponents/Layout |
| F-11 / F-75 | F-28 | Web-layer services (`ISecretarySessionService`, AdminPreview) |
| F-46 / F-57 | F-72 | `DiplomaAction` / auth duplication |

---

# P1 — Критично / високий вплив

> Виправляти першими. Сортування: effort ↑ (швидкі перемоги зверху).

### F-04 · F-44 — Checkpoint POST pipeline ×4
- **Pass:** 1, 3 | **Effort:** S | **Risk:** Low
- **Локація:** `SupervisorController`, `ReviewerController`, `AntiPlagiarismController`, `FormattingReviewController`
- **Проблема:** validate → file → try/catch → TempData → redirect — ідентичний у 4 controllers. `CheckpointCompletionHelper` покриває лише file part.
- **Рекомендація:** `EmployeeControllerBase.CompleteCheckpointAsync(...)` або shared filter.

### F-37 · F-43 · F-05 — Немає `EmployeeControllerBase`
- **Pass:** 1, 2, 3 | **Effort:** S | **Risk:** Low
- **Локація:** 6 Employee controllers (`GetUserId()` ×6); Secretary вже має `SecretaryControllerBase`
- **Рекомендація:** base class за зразком Secretary: `GetUserId`, optional `HandleDomainAction`.

### F-23 — Student import — частковий commit
- **Pass:** 2 | **Effort:** M | **Confidence:** Confirmed
- **Локація:** `UserProvisioningService.CreateWithRoleAsync` + `StudentImportService`
- **Проблема:** Identity `CreateAsync` на кожному рядку; diplomas/groups — один `SaveChanges` в кінці batch.
- **Рекомендація:** транзакція на batch або unit-of-work.

### F-20 — Дубльований import row loop
- **Pass:** 2 | **Effort:** M
- **Локація:** `StudentImportService` vs `EmployeeImportService` (~80% однакового коду)
- **Рекомендація:** `ImportRowProcessor<T>` з hooks.

### F-03 — `DiplomaWorkflowGuidance` bloat
- **Pass:** 1 | **Effort:** M | **Risk:** Low–Medium
- **Локація:** `DiplomaWorkflowGuidance.cs` (~770 рядків, ~320 LOC)
- **Проблема:** static utility, UI-тексти в Application, ~50% порожніх рядків.
- **Рекомендація:** compact format, messages в `.resx`, таблиця правил.

### F-01 · F-74 — Монолітний `SecretaryDiplomaDetailsService`
- **Pass:** 1, 3 | **Effort:** M | **Risk:** Medium
- **Локація:** `SecretaryDiplomaDetailsService.GetDetailsAsync` (~600 LOC)
- **Рекомендація:** `DiplomaDetailsAssembler` — LoadContext, BuildPermissions, BuildHistory.

### F-54 · F-69 · F-09 — Mapping моноліти в controllers
- **Pass:** 1, 3 | **Effort:** M
- **Локація:** `DiplomasController.MapDetails` (~110 LOC), `DiplomaController.MapToViewModel` (~75 LOC); `Web/Mapping` — лише 2 файли
- **Рекомендація:** `SecretaryDiplomaDetailsMapper`, `StudentDiplomaViewModelMapper` у `Web/Mapping`.

### F-02 — Дублювання student/secretary workflow flags
- **Pass:** 1 | **Effort:** L | **Risk:** Medium
- **Локація:** `StudentDiplomaService.cs` + `SecretaryDiplomaDetailsService.cs`
- **Проблема:** `Can*` / `Show*` прапорці дубльовані.
- **Рекомендація:** `DiplomaWorkflowState.From(Diploma, audience)`.

### F-06 — Application містить persistence + ASP.NET ⚠️ *не мігрувати*
- **Pass:** 1 | **Effort:** XL | **Risk:** High
- **Локація:** `ApplicationDbContext`, migrations, `FrameworkReference Microsoft.AspNetCore.App`
- **Рекомендація:** свідомий trade-off з `implementation-plan.md` — **документувати**, не рефакторити зараз.

---

# P2 — Середній вплив

> Сортування: за областю (Infrastructure → Web → Application → Tests).

## Інфраструктура та конвенції

### F-07 — Відсутній `.editorconfig`
- **Pass:** 1 | **Effort:** XS
- **Рекомендація:** `max_blank_lines_in_row = 1`, `dotnet_sort_system_directives_first`, file-scoped namespaces (поступово).

### F-08 — `viewmodels.md` stale
- **Pass:** 1 | **Effort:** S
- **Проблема:** немає `WorkflowProgress`, `Documents`, file upload fields.

### F-12 — Вертикальне форматування в нових файлах
- **Pass:** 1 | **Effort:** S
- **Локація:** `DiplomaWorkflowGuidance`, `SecretaryDiplomaDetailsService`, `Application.csproj` vs компактні старі файли.
- **Рекомендація:** `dotnet format` PR після `.editorconfig`.

### F-10 — Механічні I*Service 1:1
- **Pass:** 1 | **Effort:** — | **Needs context**
- **Локація:** 23 інтерфейси, більшість 1:1 з implementation.
- **Рекомендація:** залишити; розглянути merge лише для trivial CRUD.

## Import pipeline

### F-21 — Import errors англійською
- **Pass:** 2 | **Effort:** S | **Локація:** `StudentImportService`, `UserProvisioningService`

### F-22 — Broad `catch (Exception)` на рядок import
- **Pass:** 2 | **Effort:** S

### F-24 — CSV silent row drop
- **Pass:** 2 | **Effort:** S | **Локація:** `ImportFileParser`

### F-25 — Naive CSV parser
- **Pass:** 2 | **Effort:** M | **Локація:** `ImportFileParser.SplitCsvLine`

### F-27 — Немає ImportService tests
- **Pass:** 2 | **Effort:** M | **Примітка:** лише `StudentImportRowValidatorTests`

## Admin area

### F-13 — `ValidateFormAsync` ×5
- **Pass:** 2 | **Effort:** S
- **Локація:** `StudentsController`, `EmployeesController`, `DefenceSessionsController`, `StudyGroupsController`, …
- **Рекомендація:** `ControllerValidationExtensions` або `AdminControllerBase`.

### F-14 — `GetSessionLabelAsync` ×3
- **Pass:** 2 | **Effort:** XS
- **Локація:** `StudentsController`, `StudyGroupsController`, `ImportController`

### F-15 — Непослідовна flash/error стратегія
- **Pass:** 2 | **Effort:** S
- **Проблема:** ModelState vs TempData (`AnnualRoles`, `Archive`).

### F-17 — StudyGroup delete over-fetch
- **Pass:** 2 | **Effort:** XS | **Локація:** `StudyGroupsController.BuildDeleteViewModelAsync` — `GetAllAsync`

## AdminPreview

### F-28 · F-75 — DbContext у Web layer
- **Pass:** 2, 3 | **Effort:** M
- **Локація:** `AdminPreviewController`, `AdminPreviewViewComponent`, `AdminPreviewClaimsTransformation`
- **Рекомендація:** `IAdminPreviewUserLookup` в Application.

### F-30 — Secretary/Employee mode alias scatter
- **Pass:** 2 | **Effort:** S
- **Локація:** `AdminPreviewService`, Controller, Filter

## Razor / UI

### F-32 · F-62 — TempData alert block ×14+
- **Pass:** 2, 3 | **Effort:** S
- **Рекомендація:** `_TempDataAlerts.cshtml` partial.

### F-33 — `site.js` порожній
- **Pass:** 2 | **Effort:** XS

### F-34 — Inline `confirm()` у views
- **Pass:** 2 | **Effort:** XS

### F-36 — Admin forms без client validation scripts
- **Pass:** 2 | **Effort:** XS

### F-63 — Topic review views ≈ копія
- **Pass:** 3 | **Effort:** S
- **Локація:** `Supervisor/TopicReviews` ↔ `DepartmentHead/PendingTopics`
- **Рекомендація:** `_TopicReviewTable.cshtml`.

### F-64 — Topic history не перевикористовує `_TopicHistory`
- **Pass:** 3 | **Effort:** S | **Локація:** `Areas/Secretary/Views/Diplomas/Details.cshtml`

## Employee workflow

### F-45 — Topic approve/reject POST ×2
- **Pass:** 3 | **Effort:** S
- **Локація:** `SupervisorController`, `DepartmentHeadController`

### F-46 · F-57 — `SupervisorWorkflowService` обходить `IDiplomaAuthorizationService`
- **Pass:** 3 | **Effort:** S
- **Проблема:** ручні `supervisorId` checks; перші 4 `DiplomaAction` enum values без callers.
- **Рекомендація:** `EnsureCanPerformAsync` для Confirm/Reject/Approve/Reject topic.

### F-47 — Дубль lifecycle recalculation
- **Pass:** 3 | **Effort:** XS
- **Локація:** `SupervisorWorkflowService.RecalculateLifecycleAsync` vs `DiplomaLifecycleHelper`

### F-48 — Дубль `TopicReviewItemDto` mapping
- **Pass:** 3 | **Effort:** S
- **Локація:** `SupervisorWorkflowService`, `DepartmentHeadWorkflowService`

### F-50 — `EmployeeHomeService` pending counts неточні
- **Pass:** 3 | **Effort:** S
- **Ризик:** badge показує більше, ніж реальний pending list (без `IsStepActionable` / active session).

### F-51 — Override supervisor rules дубльовані
- **Pass:** 3 | **Effort:** S
- **Локація:** `SecretarySupervisorOverrideService` + `SecretaryDiplomaDetailsService`

### F-52 — Secretary actions без resource authorization abstraction
- **Pass:** 3 | **Effort:** M

### F-58 — Employee sub-roles без fine-grained policies
- **Pass:** 3 | **Effort:** M
- **Проблема:** лише coarse `[Authorize(Roles = Employee)]`.

### F-60 — Secretary actions trust sessionId parameter
- **Pass:** 3 | **Effort:** M

### F-61 — Authorization error messages англійською
- **Pass:** 3 | **Effort:** S
- **Локація:** `DiplomaAuthorizationService`, `SupervisorWorkflowService`

### F-68 — `Web/Mapping` покриває 2 з ~8 flows
- **Pass:** 3 | **Effort:** M

### F-72 — Auth checks дубль Application ↔ Authorization service
- **Pass:** 3 | **Effort:** M

### F-73 — Application list/query mapping boilerplate
- **Pass:** 3 | **Effort:** M
- **Рекомендація:** `DiplomaListProjection` shared queries.

### F-75 · F-11 — Web-layer services поза Application
- **Pass:** 1, 3 | **Effort:** M
- **Локація:** `ISecretarySessionService`, `IAdminPreviewService`

### F-76 — Checkpoint: `SaveChangesAsync` перед document upload
- **Pass:** 3 | **Effort:** S | **Confidence:** Likely
- **Локація:** `AdmissionReviewService.Complete*Async`
- **Ризик:** attempt без файлу при storage failure.
- **Рекомендація:** транзакція або upload-before-commit.

### F-38 · F-55 — POST feedback inconsistency
- **Pass:** 2, 3 | **Effort:** XS
- **Проблема:** Employee TempData vs Secretary ViewData copy на Details GET.

## Integration tests

### F-39 — Монолітний `AcceptanceScenarioTests`
- **Pass:** 2 | **Effort:** M

### F-40 — Немає `WebApplicationFactory`
- **Pass:** 2 | **Effort:** M

### F-41 — Немає Import/Admin integration tests
- **Pass:** 2 | **Effort:** M

### F-42 — Прямі DbContext queries у тестах
- **Pass:** 2 | **Effort:** S

---

# P3 — Низький вплив

> Косметика, дрібні оптимізації, consistency.

### F-16 — `AnnualRoles.Assign` re-fetch через Index
- **Pass:** 2 | **Effort:** XS

### F-18 — Students Edit GET — подвійний service call
- **Pass:** 2 | **Effort:** XS

### F-19 — Inline mapping DefenceSessions Details
- **Pass:** 2 | **Effort:** XS

### F-26 — Подвійна перевірка duplicate email
- **Pass:** 2 | **Effort:** XS

### F-29 — `BuildReturnUrl` дубль
- **Pass:** 2 | **Effort:** XS | **Локація:** AdminPreview ViewComponent + Filter

### F-35 · F-66 — Hardcoded role names
- **Pass:** 2, 3 | **Effort:** XS
- **Локація:** `_Layout` (`"Admin"`), `EmployeeNavViewComponent` (`"Employee"`)
- **Рекомендація:** `RoleNames.*`.

### F-49 — Inconsistent DTO→VM mapping у Employee controllers
- **Pass:** 3 | **Effort:** XS

### F-53 — `SecretaryDiplomaListService` formatting + redundant queries
- **Pass:** 3 | **Effort:** XS

### F-56 — `AddCommentAsync` без audit log
- **Pass:** 3 | **Effort:** XS | **Локація:** `SecretaryDiplomaActionService`

### F-59 — Подвійна DI-реєстрація `SecretaryAccessService`
- **Pass:** 3 | **Effort:** XS
- **Локація:** `Program.cs:39`, `DependencyInjection.cs:108`

### F-65 — Checkpoint views — table vs cards
- **Pass:** 3 | **Effort:** S
- **Рекомендація:** `_PendingCheckpointList.cshtml` з layout mode.

### F-67 — `_DiplomaDocuments` hardcoded normcontrol text
- **Pass:** 3 | **Effort:** XS

### F-70 — `DiplomaDocumentMapper.FormatKind` поза `UkrainianDisplay`
- **Pass:** 3 | **Effort:** XS

### F-71 — Secretary Index list mapping inline
- **Pass:** 3 | **Effort:** S

---

# Рекомендований порядок виправлень

## Фаза 1 — Quick wins (1–2 дні)

| # | Finding | Дія |
|---|---------|-----|
| 1 | F-37/F-43 | `EmployeeControllerBase` + `GetUserId` |
| 2 | F-04/F-44 | `CompleteCheckpointAsync` у base |
| 3 | F-32/F-62 | `_TempDataAlerts.cshtml` |
| 4 | F-07 | `.editorconfig` |
| 5 | F-47 | `DiplomaLifecycleHelper` всюди |
| 6 | F-76 | транзакція checkpoint + file upload |

## Фаза 2 — DRY та data integrity (1–2 тижні)

| # | Finding | Дія |
|---|---------|-----|
| 7 | F-23 | import batch transaction |
| 8 | F-20 | `ImportRowProcessor<T>` |
| 9 | F-46/F-57 | wire `DiplomaAction` у SupervisorWorkflow |
| 10 | F-45/F-63 | topic POST helper + `_TopicReviewTable` |
| 11 | F-50 | вирівняти `EmployeeHomeService` counts |
| 12 | F-13/F-14 | Admin validation/session label helpers |

## Фаза 3 — Структурні покращення (2–4 тижні)

| # | Finding | Дія |
|---|---------|-----|
| 13 | F-54/F-69 | mappers у `Web/Mapping` |
| 14 | F-01/F-74 | split `SecretaryDiplomaDetailsService` |
| 15 | F-03 | refactor `DiplomaWorkflowGuidance` |
| 16 | F-28/F-75 | `IAdminPreviewUserLookup` |
| 17 | F-40/F-41 | WebApplicationFactory + import tests |
| 18 | F-08 | оновити `viewmodels.md` |

## Фаза 4 — Великі рефакторинги (backlog)

| # | Finding | Дія |
|---|---------|-----|
| 19 | F-02 | `DiplomaWorkflowState` |
| 20 | F-06 | документувати Application layering trade-off |
| 21 | F-52/F-58/F-60 | fine-grained authorization matrix |

---

## 30 / 60 / 90 Day Plan

| Період | Цілі |
|--------|------|
| **0–30 днів** | Фаза 1 + F-23 + F-08 + document service tests |
| **31–60 днів** | Фаза 2 + F-03 compact + CI build+test |
| **61–90 днів** | Фаза 3; F-02 якщо details split стабільний |

---

## Scope по проходах (довідка)

| Pass | Findings | Фокус |
|------|----------|-------|
| 1 | F-01–F-12 | Solution-wide, Application, Domain, largest files |
| 2 | F-13–F-42 | Admin, Import, AdminPreview, Razor/JS, Integration tests |
| 3 | F-43–F-76 | Employee, Secretary, Auth matrix, Mapping, boundaries |

---

## Що вже зроблено добре (не чіпати без потреби)

- Domain services: `TopicReviewService`, `ReviewerAssignmentService`, `SecretarySupervisorOverrideService`
- `AdmissionReviewService` + `CheckpointCompletionHelper` + `_CheckpointOutcomeFields`
- `SecretaryControllerBase` + `ExecuteActionAsync` + secretary policy handler
- `StudentWorkflowProgressBuilder` з тестами
- `WorkflowProgressMapper`, `DiplomaDocumentMapper` (розширити, не переписувати)
- Import: `ImportFileParser`, FluentValidation row validators
- AdminPreview: claims transformation, redirect rules, impersonation filter + tests

---

*Консолідовано з Pass 1–3. Детальні приклади коду — у відповідних pass-файлах.*

# Test plan — Diploma Management System

План тестування по модулях: що покривати **unit**-тестами, що **integration**-тестами.

**Останнє оновлення:** 2026-07-01 (хвиля 18). Детальний прогрес — [test-progress.md](./test-progress.md), каталог кейсів — [test-cases.md](./test-cases.md).

## Легенда

| Позначка | Значення |
|----------|----------|
| ✅ | Вже є тести |
| 🔲 | Потрібно додати |
| — | Не пріоритет / покривається іншим рівнем |

**Unit** — швидко, без PostgreSQL: domain rules, чисті функції, валідатори, маппери, in-memory EF для простого CRUD.

**Integration** — PostgreSQL (Docker/Testcontainers): EF-запити, кілька сервісів, транзакції, audit, файли, обмежений HTTP smoke.

## CI

| Job | Scope | Коли |
|-----|-------|------|
| `unit` | Domain + Application + Web, filter `FullyQualifiedName!~Integration` | кожен PR |
| `integration` | Integration.Tests + PostgreSQL service | кожен PR |

Локально integration — Docker Desktop (Testcontainers) або `DIPLOMA_INTEGRATION_PG`.

**Поточний стан:** 504 unit + 111 integration — усі проходять.

---

## 1. Domain (`DiplomaManagementSystem.Domain`)

### Unit ✅ (основний рівень)

| Компонент | Що тестувати |
|-----------|--------------|
| `AdmissionWorkflowService` ✅ | старт admission review, override кроку, заборонені переходи |
| `AdmissionReadinessService` ✅ | готовність до admit, блокування за невиконаними кроками |
| `AdmissionStepSequence` ✅ | порядок кроків, next step |
| `WorkReadinessService` ✅ | declare work ready |
| `DiplomaTopicService` ✅ | submit topic, версії |
| `TopicReviewService` ✅ | approve/reject теми (supervisor, head) |
| `SupervisorSelectionService` ✅ | вибір керівника |
| `SupervisorConfirmationService` ✅ | confirm/reject студента |
| `SupervisorAssignmentRules` ✅ | правила призначення |
| `ReviewerAssignmentService` ✅ | assign reviewer, без теми, до/після anti-plagiarism |
| `DiplomaAdmissionService` ✅ | admit, defence date |
| `DefenceSessionArchiveService` ✅ | archive session |
| `SecretarySupervisorOverrideService` ✅ | override керівника (domain rules) |
| `DiplomaLifecycleService` ✅ | переходи lifecycle |
| `DiplomaCreationService` ✅ | створення диплома при enrollment |
| `SupervisorOverridePolicy` ✅ | політика override |
| `AdmissionStepStatusResolver` ✅ | resolved status для кроку |
| `CheckpointOutcomeRules` ✅ | правила outcome (approved/rejected/revision) |

### Integration —

Domain не тестується integration-ом окремо; перевіряється через application-сценарії.

---

## 2. Application — спільна логіка UI/workflow

### Unit ✅

| Компонент | Що тестувати |
|-----------|--------------|
| `DiplomaWorkflowGuidance` ✅ | `Build*BlockedReason` для кожної дії (secretary + student) |
| `StudentWorkflowProgressBuilder` ✅ | кроки, hints, completed/current/upcoming |
| `WorkflowUkrainianLabels` ✅ | лейбли кроків, override comment prefix |
| `TopicVersionApprovalFormatter` ✅ | approved display, step detail |
| `DefenceWorkLabel` ✅ | форматування типу роботи |
| `PersonNameSort` ✅ | сортування ПІБ |
| `DiplomaStoragePathBuilder` ✅ | сегменти шляху для документів |
| `SecretarySessionLabel` ✅ | підпис сесії |
| `ArchiveGuard` ✅ | archived → exception (з mock session) |

### Integration ✅

| Сценарій | Статус |
|----------|--------|
| Guidance ↔ реальний стан | ✅ student + secretary (admit, override, assign) `GuidanceAlignmentScenarioTests` |

---

## 3. Application — Authorization

### Unit ✅ (P0)

| Компонент | Що тестувати |
|-----------|--------------|
| `DiplomaAuthorizationService` ✅ | матриця `DiplomaAction` × роль |
| | `expectedSessionId` mismatch → `SessionMismatch` |
| | archived session → deny |
| | diploma not found, topic version not found |
| | `EnsureCanPerformOnTopicVersionAsync` |

### Integration ✅ (P0)

| Сценарій | Статус |
|----------|--------|
| Annual role secretary | ✅ wrong role / assign без topic |
| Wrong employee role | ✅ `AuthorizationScenarioTests` |
| Student scope | ✅ area auth HTTP |
| Archived session | ✅ `ArchivedSessionScenarioTests` |

---

## 4. Application — Student

### Unit ✅

| Компонент | Що тестувати |
|-----------|--------------|
| `SelectSupervisorValidator` ✅ | FluentValidation rules |
| `SubmitTopicValidator` ✅ | FluentValidation rules |
| `StudentWorkflowProgressBuilder` ✅ | (див. §2) |

### Integration ✅ / 🔲

| Сценарій | Статус |
|----------|--------|
| `SelectSupervisorAsync` | ✅ topic flow + HTTP POST |
| `SubmitTopicAsync` | ✅ |
| `DeclareWorkReadyAsync` | ✅ |
| Re-select supervisor після reject | ✅ `SupervisorRejectStudentScenarioTests` |
| Submit topic без confirmed supervisor | ✅ `SubmitTopicWithoutSupervisorScenarioTests` |
| `GetMyDiplomaAsync` composite DTO | ✅ `MyDiplomaReadScenarioTests` |
| Empty student (немає диплома) | ✅ `GetMyDiploma_WithoutDiploma_ReturnsEmptyComposite` |

---

## 5. Application — Employee

### Unit ✅

| Компонент | Що тестувати |
|-----------|--------------|
| `CompleteCheckpointValidator` ✅ | required fields, outcome |
| `ReviewTopicRejectValidator` ✅ | |
| `SupervisorRejectValidator` ✅ | |

### Integration ✅ / 🔲

#### `SupervisorWorkflowService`

| Сценарій | Статус |
|----------|--------|
| Confirm student | ✅ |
| Approve topic | ✅ |
| Reject topic | ✅ `TopicRejectionScenarioTests` |
| Reject student (supervisor) | ✅ `SupervisorRejectStudentScenarioTests` |

#### `DepartmentHeadWorkflowService`

| Сценарій | Статус |
|----------|--------|
| Approve topic | ✅ |
| Reject topic | ✅ `DepartmentHeadTopicRejectionScenarioTests` |

#### `AdmissionReviewService`

| Сценарій | Статус |
|----------|--------|
| Complete supervisor feedback | ✅ |
| Complete formatting review | ✅ |
| Complete anti-plagiarism | ✅ |
| Complete external review | ✅ |
| Rejected checkpoint → retry | ✅ `CheckpointRejectionScenarioTests` |
| Wrong employee for step | ✅ |
| Checkpoint без документа (де required) | ✅ `AdmissionCheckpointEdgeScenarioTests` |
| Out-of-order step | ✅ `AdmissionCheckpointEdgeScenarioTests` |

#### `EmployeeHomeService`

| Сценарій | Статус |
|----------|--------|
| Pending tasks для supervisor/reviewer/formatting | ✅ `EmployeeHomeScenarioTests` |

---

## 6. Application — Secretary

### Unit ✅

| Компонент | Що тестувати |
|-----------|--------------|
| `AssignReviewerValidator` ✅ | |
| `AdmitDiplomaValidator` ✅ | defence date |
| `OverrideSupervisorValidator` ✅ | |
| `OverrideAdmissionStepValidator` ✅ | |
| `AddCommentValidator` ✅ | |
| `DiplomaDetailsAssembler.BuildHistory` | ✅ `DiplomaDetailsAssemblerTests` |
| `DiplomaDetailsAssembler.BuildScreenParts` | ✅ `DiplomaDetailsAssemblerTests` |

### Integration ✅ / 🔲

#### `SecretaryDiplomaActionService`

| Сценарій | Статус |
|----------|--------|
| `AssignReviewerAsync` | ✅ |
| `AdmitAsync` | ✅ service + HTTP POST |
| `AddCommentAsync` | ✅ archived blocks |
| `OverrideSupervisorAsync` + audit | ✅ `SecretaryOverrideAuditScenarioTests` |
| `OverrideAdmissionStepAsync` | ✅ `SecretaryOverrideAdmissionStepScenarioTests` |
| Audit log після override дій | ✅ supervisor + admission step |

#### Read paths

| Компонент | Статус |
|-----------|--------|
| `SecretaryDiplomaDetailsService` + assembler | ✅ |
| `SecretaryDiplomaListService` / projection | ✅ filters + search |
| `SecretaryDashboardService` | ✅ service-level (HTTP render — 500 у test host) |
| `AdmittedReportService` | ✅ `AdmittedReportScenarioTests` |
| `SecretaryAccessService` | ✅ `SecretaryAccessScenarioTests` |

---

## 7. Application — Admin

### Unit ✅

| Компонент | Unit | Що тестувати |
|-----------|------|--------------|
| `EmployeeAdminService` ✅ | CRUD, soft constraints |
| `StudentAdminService` ✅ | create, session binding |
| `StudyGroupAdminService` ✅ | create, list |
| `EmployeeFormValidator` ✅ | |
| `StudentFormValidator` ✅ | |
| `StudyGroupFormValidator` ✅ | |
| `DefenceSessionFormValidator` ✅ | |
| `AssignAnnualRoleValidator` ✅ | |
| `AnnualRoleService` ✅ | assign/remove role, duplicate |

### Integration ✅ / 🔲

| Сценарій | Статус |
|----------|--------|
| Duplicate study group name | ✅ |
| `DefenceSessionService.Create/Archive/Update/GetDetails` | ✅ `DefenceSessionArchiveScenarioTests`, `DefenceSessionAdminScenarioTests` |
| Create student → diploma exists | ✅ enrollment scenario |
| Student admin list/details/update | ✅ `StudentAdminScenarioTests` |
| Annual role assignment | ✅ (у seed) |

---

## 8. Application — Import

### Unit ✅

| Компонент | Що тестувати |
|-----------|--------------|
| `ImportFileParser` / `CsvLineTokenizer` ✅ | |
| `StudentImportRowValidator` ✅ | |
| `EmployeeImportRowValidator` ✅ | |
| `ImportRowProcessor` ✅ | |
| `EmailDomainValidator` ✅ | |
| `ImportResultComposer` ✅ | |

### Integration ✅

| Сценарій | Статус |
|----------|--------|
| Student import → students + diplomas | ✅ |
| Employee import | ✅ |
| Partial failure (invalid row) | ✅ |
| Duplicate email | ✅ |

---

## 9. Application — Documents

### Unit ✅

| Компонент | Що тестувати |
|-----------|--------------|
| `DiplomaStoragePathBuilder` ✅ | path segments |

### Integration ✅ / 🔲

| Сценарій | Статус |
|----------|--------|
| Upload PDF на checkpoint | ✅ admission flow |
| Wrong content type | ✅ `DocumentUploadScenarioTests` |
| Document linked to attempt | ✅ `DocumentAttemptLinkScenarioTests` |
| Download / storage path | ✅ `DocumentDownloadScenarioTests`, `DocumentDownloadEndpointTests` |

---

## 10. Application — Identity & Audit

### Integration ✅ / 🔲

| Сценарій | Статус |
|----------|--------|
| `UserProvisioningService` | ✅ import + seed |
| `AuditLogWriter` | ✅ override supervisor |
| `BootstrapAdminSeeder` | — (вимкнено в test host; admin через `IntegrationAdminHelper`) |

---

## 11. Application — Admin Preview

### Unit ✅

| Компонент | Що тестувати |
|-----------|--------------|
| `AdminPreviewService` ✅ | impersonation targets |
| `AdminPreviewRedirectRules` ✅ | redirect по ролі |

### Integration ✅

| Сценарій | Статус |
|----------|--------|
| POST `AdminPreview/Set` → SelectUser | ✅ `AdminPreviewEndpointTests` |
| Preview impersonation end-to-end | ✅ `AdminPreviewEndpointTests.PostSetUser_RedirectsToStudentDiploma` |

---

## 12. Infrastructure

Покриття через integration-сценарії (queries, FK, indexes). Окремі unit не потрібні.

---

## 13. Web (`DiplomaManagementSystem.Web`)

### Unit ✅

| Компонент | Статус |
|-----------|--------|
| `UkrainianDisplay` ✅ | |
| `SecretaryDiplomaDetailsMapper` ✅ | |
| `StudentDiplomaViewModelMapper` ✅ | |
| `SecretaryListViewModelMapper` ✅ | |
| `SecretaryDashboardViewModelMapper` ✅ | |
| `SecretaryReportsViewModelMapper` ✅ | |
| `WorkflowProgressMapper` ✅ | |
| `TopicHistoryMapper` ✅ | |
| `DiplomaDocumentMapper` ✅ | |
| `EmployeeViewModelMapper` ✅ | |
| `AdminDefenceSessionViewModelMapper` ✅ | |
| `UploadFileMapper` ✅ | |
| `CheckpointCompletionHelper` ✅ | |
| `AdminFlashMessages` ✅ | |

### Integration (HTTP smoke) ✅ / 🔲

| Сценарій | Статус |
|----------|--------|
| `GET /healthchecks` | ✅ |
| Auth + role area access | ✅ |
| POST student select supervisor | ✅ |
| POST secretary admit | ✅ (antiforgery з `/Employee/Home`; cookie сесії вручну) |
| Secretary list HTTP | ✅ `SecretaryDiplomaListEndpointTests` |
| Secretary Dashboard/Details render | ✅ `SecretaryDashboardEndpointTests` |
| Document download `/local-files` | ✅ `DocumentDownloadEndpointTests` |
| Student upload / declare work ready HTTP | ✅ `StudentDiplomaEndpointTests` |
| Formatting / reviewer checkpoint HTTP | ✅ `FormattingCheckpointEndpointTests`, `ReviewerCheckpointEndpointTests` |
| Admin import HTTP | ✅ `ImportEndpointTests` |
| Secretary inaccessible session cookie | ✅ `SecretaryAccessEndpointTests` |

---

## 14. Integration — зведена матриця

### Реалізовано ✅ (77 тестів)

| # | Сценарій | Тест |
|---|----------|------|
| 1 | Student enrollment | `StudentEnrollmentScenarioTests` |
| 2 | Topic approved | `TopicApprovalScenarioTests` |
| 3 | Full admission | `FullAdmissionScenarioTests` |
| 4 | Archived session | `ArchivedSessionScenarioTests` |
| 5 | Import student/employee | `ImportScenarioTests` |
| 6 | Study group duplicate | `StudyGroupAdminScenarioTests` |
| 7 | Health | `HealthEndpointTests` |
| 8 | Topic rejected | `TopicRejectionScenarioTests` |
| 9 | Checkpoint rejected | `CheckpointRejectionScenarioTests` |
| 10 | Authorization matrix | `AuthorizationScenarioTests` |
| 11 | Secretary override + audit | `SecretaryOverrideAuditScenarioTests` |
| 12 | GetMyDiploma composite | `MyDiplomaReadScenarioTests` |
| 13 | Import partial failure | `ImportScenarioTests` |
| 14 | Import duplicate email | `ImportScenarioTests` |
| 15 | Invalid upload MIME | `DocumentUploadScenarioTests` |
| 16 | HTTP area auth | `AreaAuthorizationEndpointTests` |
| 17 | POST select supervisor | `StudentSelectSupervisorEndpointTests` |
| 18 | Secretary list filters | `SecretaryDiplomaListScenarioTests` |
| 19 | Secretary dashboard | `SecretaryDashboardScenarioTests` |
| 20 | POST secretary admit | `SecretaryAdmitEndpointTests` |
| 21 | Admin preview Set | `AdminPreviewEndpointTests` |
| 22 | Empty student read model | `MyDiplomaReadScenarioTests` |
| 23 | Document ↔ attempt link | `DocumentAttemptLinkScenarioTests` |
| 24 | Override admission step + audit | `SecretaryOverrideAdmissionStepScenarioTests` |
| 25 | Admitted report + CSV | `AdmittedReportScenarioTests` |
| 26 | Head reject topic | `DepartmentHeadTopicRejectionScenarioTests` |
| 27 | Supervisor reject student | `SupervisorRejectStudentScenarioTests` |
| 28 | Employee home pending tasks | `EmployeeHomeScenarioTests` |
| 29 | Guidance ↔ blocked reason | `GuidanceAlignmentScenarioTests` |
| 30 | Admin preview SetUser | `AdminPreviewEndpointTests` |
| 31 | Secretary Dashboard HTTP | `SecretaryDashboardEndpointTests` |
| 32 | Secretary Details HTTP | `SecretaryDashboardEndpointTests` |
| 33 | Checkpoint empty document | `AdmissionCheckpointEdgeScenarioTests` |
| 34 | Out-of-order checkpoint | `AdmissionCheckpointEdgeScenarioTests` |
| 35 | Submit topic без supervisor | `SubmitTopicWithoutSupervisorScenarioTests` |
| 36 | Archive session + audit | `DefenceSessionArchiveScenarioTests` |
| 37 | Secretary guidance assign reviewer | `GuidanceAlignmentScenarioTests` |
| 38 | Employee home formatting | `EmployeeHomeScenarioTests` |
| 39 | Employee home reviewer | `EmployeeHomeScenarioTests` |
| 40 | Employee home anti-plagiarism | `EmployeeHomeScenarioTests` |
| 41 | Secretary admit guidance | `GuidanceAlignmentScenarioTests` |
| 42 | Secretary override guidance | `GuidanceAlignmentScenarioTests` |
| 43 | Archive blocks student upload | `ArchivedSessionScenarioTests` |
| 44 | Secretary list HTTP | `SecretaryDiplomaListEndpointTests` |
| 45 | Secretary list HTTP search | `SecretaryDiplomaListEndpointTests` |
| 46 | Document download service | `DocumentDownloadScenarioTests` |
| 47 | Document download HTTP | `DocumentDownloadEndpointTests` |
| 48 | DefenceSession admin CRUD | `DefenceSessionAdminScenarioTests` |
| 49 | Secretary access service | `SecretaryAccessScenarioTests` |
| 50 | Student admin read/update | `StudentAdminScenarioTests` |
| 51 | Secretary wrong session HTTP | `SecretaryAccessEndpointTests` |
| 52 | Formatting checkpoint HTTP | `FormattingCheckpointEndpointTests` |
| 53 | Reviewer checkpoint HTTP | `ReviewerCheckpointEndpointTests` |
| 54 | Student upload / declare HTTP | `StudentDiplomaEndpointTests` |
| 55 | Admin import HTTP | `ImportEndpointTests` |

### Наступні кандидати 🔲

*(backlog закрито; нові тести — лише за зміною продукту або E2E)*

---

## 15. Хвилі імплементації

| Хвиля | Фокус | Тестів (+) | Статус |
|-------|-------|------------|--------|
| 1 | Domain gaps, Authorization, Guidance, validators | unit +93 | ✅ |
| 2 | Workflow progress, AnnualRole, Import composer | unit +31 | ✅ |
| 3 | Web mappers smoke | unit → 324 | ✅ |
| 4 | Integration негативи + CI PostgreSQL | int +6 → 14 | ✅ |
| 5 | Import partial / duplicate, invalid MIME | int +3 → 17 | ✅ |
| 6 | HTTP auth, secretary read models | int +7 → 24 | ✅ |
| 7 | HTTP admit, admin preview, empty student, doc↔attempt | int +4 → 28 | ✅ |
| 8 | Backlog §14 (override step, reports, employee home) | int +8 → 36 | ✅ |
| 9 | Checkpoint edges, archive, secretary Razor fix | int +9 → 45 | ✅ |
| 10 | Anti-plagiarism home, HTTP list, guidance admit | int +6 → 51 | ✅ |
| 11 | Archive declare ready, reports HTTP, checkpoint HTTP, partial admit guidance | int +5 → 56 | ✅ |
| 12 | Document download (service + HTTP) | int +4 → 60 | ✅ |
| 13 | Coverage backlog (admin, access, HTTP, domain unit) | int +17, unit +8 → 77 / 332 | ✅ |
| 14 | Unit gaps, admin scenarios, HTTP POST smoke | int +15, unit +44 → 92 / 376 | ✅ |
| 15 | Import xlsx parser, EmployeeHomeQueries | int +12, unit +6 → 104 / 382 | ✅ |
| 16 | Domain branch/guard coverage | unit +42 → 145 domain / 424 | ✅ |
| 17 | Application helpers, workflow badges, list projections | unit +52 → 476 | ✅ |
| 18 | Application gap-fill + import/list integration | unit +28, int +7 → 504 / 111 | ✅ |

Деталі по хвилях — [test-progress.md](./test-progress.md).

---

## Рекомендовані пропорції

| Шар | Unit | Integration |
|-----|------|-------------|
| Domain | ~100% rules | — |
| Guidance / builders / validators | ~90% | smoke узгодження |
| Application services (workflow) | validators only | основне покриття |
| Authorization | матриця (mock) | реальні ролі в БД |
| Infrastructure queries | — | через scenarios |
| Web mappers | smoke | обмежений HTTP smoke |

---

## Backlog (короткий)

### Закрито ✅
- `DiplomaAuthorizationServiceTests` (unit)
- Integration job у CI
- Негативні workflow-сценарії
- `SecretaryDiplomaListProjection` integration
- Import partial failure
- Admin preview HTTP (Set + SetUser)
- Web mapper smoke tests
- `OverrideAdmissionStepAsync` + audit
- `AdmittedReportService`
- Employee home pending tasks
- Head reject topic / supervisor reject student
- Guidance alignment smoke
- Secretary HTTP render (fix EF query `ListAccessibleSecretarySessionsAsync`)
- Checkpoint edges, archive cascade, submit topic guard
- Anti-plagiarism employee home
- Secretary list HTTP + guidance admit/override
- Archive blocks student upload
- Archive blocks declare work ready
- Secretary reports HTTP (Admitted + AdmittedCsv)
- Employee supervisor checkpoint HTTP smoke
- Guidance admit after partial checkpoints
- Document download (local-files URL + HTTP auth/404)
- DefenceSession update/details/getAll
- Secretary access service + HTTP cookie guard
- Student admin integration (list/details/update)
- HTTP formatting/reviewer checkpoints
- Student upload + declare work ready HTTP
- Admin import HTTP smoke
- Domain branches: WorkReadiness, DiplomaAdmission, AdmissionWorkflow archived
- `DiplomaDetailsAssembler` unit tests
- **Fix:** `StorageFolderId` / `StorageFileId` → 1024 chars (довгі local paths)
- Import parser edge cases (CanParse, BOM, empty lines)
- Document format/naming helpers
- Employee admin integration
- Study group update/delete integration
- Secretary list combo filter
- Local file storage duplicate filename
- HTTP POST: anti-plagiarism, head/supervisor topic, supervisor confirm, secretary actions, session select
- `AdvanceAfterReviewerAssignment` domain unit
- `AdmissionStepQueries.FindWritableAsync` integration
- Import xlsx parser unit tests
- `EmployeeHomeQueries` integration coverage
- Domain guards: admission workflow, topic review, supervisor confirm, entity navigation smoke
- Application helpers: DefenceWorkLabel, ImportRowProcessor, labels/sort/naming/email
- StudentWorkflowProgress checkpoint badges + list projection unit smoke
- Import guards (session missing/archived, unsupported format)
- Secretary list filters + unknown session integration

### Відкрито

*(немає — coverage backlog закрито; E2E не планується)*

---

## Пов’язані файли

- Unit: `tests/DiplomaManagementSystem.{Domain,Application,Web}.Tests/`
- Integration: `tests/DiplomaManagementSystem.Integration.Tests/`
- Support: `IntegrationScenarioBuilder`, `WorkflowScenarioRunner`, `IntegrationScenarioAssertions`, `IntegrationTestWebClient`
- CI: `.github/workflows/ci.yml`
- **Каталог кейсів:** [test-cases.md](./test-cases.md)
- **Прогрес імплементації:** [test-progress.md](./test-progress.md)

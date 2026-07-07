# ViewModels

Довідник ViewModels для Razor-екранів. DTO → ViewModel маппінг: `Web/Mapping/`:

| Mapper | Призначення |
|--------|-------------|
| `StudentDiplomaViewModelMapper` | Student `Diploma/Index`, submit topic |
| `SecretaryDiplomaDetailsMapper` | Secretary diploma details |
| `SecretaryListViewModelMapper` | Secretary list + filters |
| `SecretaryDashboardViewModelMapper` | Secretary dashboard |
| `SecretaryReportsViewModelMapper` | Звіт допущених |
| `EmployeeViewModelMapper` | Employee home, checkpoints, topic reviews |
| `AdminDefenceSessionViewModelMapper` | Admin defence sessions list/details |
| `WorkflowProgressMapper` | Чекліст етапів workflow |
| `DiplomaDocumentMapper` | Блок документів диплома |
| `TopicHistoryMapper` | Історія версій теми |

Спільні partials: `_TempDataAlerts` (flash), `_TopicHistory`, `_TopicReviewTable`, `_DiplomaDocuments`, `_PendingCheckpointList`.

Валідація форм — FluentValidation на **DTO** (`Application`), не на ViewModels (окрім `[Required]` у Razor для UX).

---

## Спільні (`Web/Models/Shared`)

### `WorkflowProgressViewModel` / `WorkflowStepViewModel`

Використовується на екранах студента та секретаря (чекліст етапів workflow).

| Поле | Опис |
|------|------|
| `ProgressPercent` | 0–100 |
| `CompletedSteps` / `TotalSteps` | Лічильники |
| `CurrentStepHintLabel` | Підпис підказки («Наступний крок:» / «Поточний етап:») |
| `CurrentStepHint` | Текст поточного кроку |
| `Steps` | Список `WorkflowStepViewModel` |

`WorkflowStepViewModel`: `Order`, `Title`, `State` (`StudentWorkflowStepState`), `StateCssClass`, `Detail`, `Metadata`, `Comment`, `IsSecretaryOverride`.

### `DiplomaDocumentsViewModel` / `DiplomaDocumentItemViewModel`

Блок документів диплома (файли роботи, відгук, рецензія, антиплагіат).

| `DiplomaDocumentsViewModel` | Опис |
|-----------------------------|------|
| `StudentWorkVersions` | Усі версії роботи студента |
| `LatestSupervisorFeedback` | Останній відгук керівника |
| `LatestExternalReview` | Остання рецензія |
| `LatestAntiPlagiarismReport` | Останній звіт антиплагіату |

`DiplomaDocumentItemViewModel`: `Id`, `Kind`, `KindDisplay`, `VersionNumber`, `FileName`, `ViewUrl`, `SizeBytes`, `UploadedAt`.

### Інші shared

- `StudentAdmissionStepViewModel` — крок допуску (студентський чекліст)
- `TopicVersionItemViewModel` / `CommentItemViewModel` — історія тем і коментарів
- `WorkNavViewModel` — навігація employee-ролей

---

## Student (`Areas/Student/Models`)

### `MyDiplomaViewModel` — `Diploma/Index`

| Група | Поля |
|-------|------|
| Сесія / диплом | `DiplomaId`, `HasDiploma`, `SessionType`, `SessionLabel` |
| Керівник / тема | `SupervisorName`, `SupervisorAssignmentStatus`, `SupervisorAssignmentDisplay`, `LifecycleStatus`, `LifecycleDisplay`, `CurrentTopicTitle`, `TopicStatus`, `TopicStatusDisplay` |
| Списки | `AdmissionSteps`, `TopicVersions`, `Comments` |
| Керівник (дії) | `ShowSupervisorSection`, `CanSelectSupervisor`, `SelectSupervisorBlockedReason`, `SupervisorPool`, `SelectedSupervisorId` |
| Тема | `ShowTopicSubmissionSection`, `CanSubmitTopic`, `SubmitTopicBlockedReason` |
| Перевірки / робота | `ShowCheckpointsSection`, `ShowWorkReadinessSection`, `CanDeclareWorkReady`, `DeclareWorkReadyBlockedReason` |
| **Завантаження роботи** | `ShowWorkUploadSection`, `CanUploadWork`, `UploadWorkBlockedReason` |
| **Документи** | `Documents` (`DiplomaDocumentsViewModel?`) |
| **Workflow** | `WorkflowProgress` (`WorkflowProgressViewModel?`) |

### `UploadWorkViewModel` — POST `Diploma/UploadWork`

| Поле | Валідація |
|------|-----------|
| `DiplomaId` | hidden |
| `WorkFile` | `IFormFile?` — PDF/DOCX/ODT; обов'язковий у контролері |

### Інші student forms

- `SelectSupervisorViewModel` — `DiplomaId`, `SupervisorId`
- `SubmitTopicViewModel` — `DiplomaId`, `Title`

---

## Secretary (`Areas/Secretary/Models`)

### `DiplomaDetailsViewModel` — `Diplomas/Details`

Окрім полів студента/супервізора/рецензента та чеклістів допуску (`AdmissionSteps`, `AttemptHistory`, `TopicVersions`, `Comments`):

| Група | Поля |
|-------|------|
| Дії секретаря | `ShowOverrideSupervisorSection`, `CanOverrideSupervisor`, `OverrideSupervisorBlockedReason`, `ShowAssignReviewerSection`, `CanAssignReviewer`, `AssignReviewerBlockedReason`, `ShowAdmitSection`, `CanAdmit`, `AdmitBlockedReason`, `ShowOverrideAdmissionStepSection`, `CanOverrideAdmissionStep`, `OverrideAdmissionStepBlockedReason`, `ShowAddCommentSection`, `CanAddComment`, `AddCommentBlockedReason` |
| **Workflow** | `WorkflowProgress` |
| **Документи** | `Documents` |
| Довідники | `EmployeePool` (`SelectListItem`) |

POST-моделі на тій же сторінці: `AssignReviewerViewModel`, `AdmitDiplomaViewModel`, `OverrideSupervisorViewModel`, `AddCommentViewModel`, `OverrideAdmissionStepViewModel`.

### Список / дашборд

- `DiplomaListViewModel`, `DiplomaListFilterViewModel`, `DiplomaListItemViewModel`
- `SecretaryDashboardViewModel`, `SessionSelectViewModel`, `SessionSelectorViewModel`
- `AdmittedReportViewModel`, `AdmittedReportItemViewModel`

---

## Employee (`Areas/Employee/Models`)

### `CompleteCheckpointViewModel` — checkpoint з файлом

| Поле | Опис |
|------|------|
| `DiplomaId` | hidden |
| `Outcome` | `CheckpointOutcome` |
| `Comment` | опційно |
| `Document` | `IFormFile?` — обов'язковий коли `RequiresDocumentFile` |
| `RequiresDocumentFile` | `false` для normocontrol (formatting) без файлу |

Інші: `EmployeeHomeViewModel`, `PendingCheckpointsViewModel`, `TopicReviewsViewModel`, тощо.

---

## Admin (`Areas/Admin/Models`)

CRUD: `DefenceSession*`, `StudyGroup*`, `Student*`, `Employee*`, `AnnualRole*`.  
Імпорт: `ImportStudentsViewModel`, `ImportEmployeesViewModel`, `ImportResultViewModel` (`IFormFile` для CSV/XLSX).

---

## Root (`Web/Models`)

- `AdminPreviewViewModel` та пов'язані — імперсонація адміна
- `ErrorViewModel`

---

## Джерела в коді

| Area | Файл |
|------|------|
| Shared | `Web/Models/Shared/SharedViewModels.cs` |
| Student | `Web/Areas/Student/Models/StudentViewModels.cs` |
| Secretary | `Web/Areas/Secretary/Models/SecretaryViewModels.cs` |
| Employee | `Web/Areas/Employee/Models/EmployeeViewModels.cs` |
| Admin | `Web/Areas/Admin/Models/*.cs` |
| Mapping | `Web/Mapping/*.cs` |

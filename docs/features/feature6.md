# Feature 6 — обов’язковий рецензент перед виконанням роботи

## Мета

Зробити призначення рецензента **обов’язковим кроком** між затвердженням теми та виконанням/здачею роботи студентом.

- Новий lifecycle-статус: **`ReviewerAssigned`** («Рецензента призначено»)
- Призначає **лише секретар ДЕК** своєї сесії
- До призначення рецензента студент **не** може завантажувати роботу і **не** може заявити готовність до перевірок

## Цільовий workflow

```text
AwaitingSupervisor
  → SupervisorConfirmed
  → TopicInReview
  → TopicApproved              // тема затверджена, очікує рецензента
  → ReviewerAssigned           // фаза виконання роботи студентом
  → DocumentsInProgress        // після DeclareWorkReady
  → ReadyForAdmission
  → Admitted
```

| Before | Action | After |
|--------|--------|-------|
| `TopicInReview` (pending head) | Завідувач затверджує тему | `TopicApproved`, `ReviewAssignmentStatus=NotAssigned` |
| `TopicApproved` | Секретар призначає рецензента | `ReviewerAssigned`, `ReviewAssignmentStatus=Assigned` |
| `ReviewerAssigned` | Студент завантажує роботу | `ReviewerAssigned` (без старту перевірок) |
| `ReviewerAssigned` + є файл | Студент `DeclareWorkReady` | `DocumentsInProgress` (`SupervisorFeedback`) |

## Бізнес-правила

| # | Правило |
|---|---------|
| R1 | Призначати рецензента може **лише секретар** сесії (`DiplomaAction.AssignReviewer`) |
| R2 | Рецензент має бути **активним працівником кафедри** сесії диплома |
| R3 | Призначення дозволене **лише** зі статусу `TopicApproved` |
| R4 | Призначення заборонене, якщо вже є `CurrentAdmissionStep` або admission attempts |
| R5 | Після призначення зміна рецензента через UI/сервіс **не** дозволена, доки статус не `TopicApproved` знову (нормальний шлях — без зняття) |
| R6 | Upload роботи дозволений з `ReviewerAssigned` (+ пізніші статуси до `Admitted`); з `TopicApproved` — **ні** |
| R7 | `DeclareWorkReady` дозволений **лише** з `ReviewerAssigned` і за наявності файлу роботи |
| R8 | Перший upload **не** стартує admission review автоматично |
| R9 | `ReviewerAssigned` замінює колишній `WorkInProgressByStudent` (той самий numeric value `4`) |

## Не в scope

- Зняття/заміна рецензента після початку виконання роботи
- Видалення кроку `AdmissionStep.ReviewerAssignment` з admission pipeline (стає pass-through, бо рецензент уже призначений)
- EF global query filters
- Зміна UI Student/Employee, окрім blocked reasons і прогресу workflow

## Persistence / міграція

`LifecycleStatus` зберігається як `smallint`. Enum:

| Value | Було | Стало |
|------:|------|-------|
| 3 | `TopicApproved` | без змін (тепер реально використовується) |
| 4 | `WorkInProgressByStudent` | **`ReviewerAssigned`** |
| 5–7 | без змін | без змін |

### Data migration `RequireReviewerBeforeWork`

Для **не admitted** дипломів без рецензента (`ReviewerId IS NULL` або `ReviewAssignmentStatus = NotAssigned`):

1. Видалити `diploma_admission_step_attempts`
2. Очистити `CurrentAdmissionStep`
3. Встановити `LifecycleStatus = TopicApproved (3)`
4. **Зберегти** студентські файли (`diploma_documents`) і audit log

Admitted записи **не** змінюються.

Дипломи з уже призначеним рецензентом і `LifecycleStatus = 4` семантично стають `ReviewerAssigned` без SQL remap.

## UI / локалізація

- Badge: «Рецензента призначено»
- Dashboard секретаря: окремі buckets `TopicApproved` і `ReviewerAssigned` (без merge `TopicApproved` у work-bucket)
- Hint студенту на `TopicApproved`: очікуйте призначення рецензента
- Hint секретарю на `TopicApproved`: призначте рецензента

## Acceptance

- [ ] Після затвердження теми статус = `TopicApproved`
- [ ] Секретар може призначити рецензента → `ReviewerAssigned`
- [ ] Не-секретар не може призначити
- [ ] До призначення upload / DeclareWorkReady заблоковані
- [ ] Після призначення upload не стартує checks
- [ ] DeclareWorkReady → `DocumentsInProgress`
- [ ] Legacy без рецензента скинуті до `TopicApproved` без втрати файлів

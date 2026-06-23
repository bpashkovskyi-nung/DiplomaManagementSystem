# Словник імен: код ↔ UI

У коді — англійські імена без кальки з української. В інтерфейсі — офіційні українські терміни.

## Сутності

| Код (C# / БД) | UI (українська) | Було в wiki |
|---------------|-----------------|-------------|
| `DefenceSession` | Сесія захисту / Захист | Defense |
| `Diploma` | Дипломна робота | Work |
| `DiplomaTopicVersion` | Тема | TopicVersion |
| `DiplomaComment` | Коментар | Comment |
| `DiplomaAdmissionCheckpoint` | Вимога на допуск | Document |
| `StudyGroup` | Група | Group |
| `SupervisorPoolEntry` | Керівник у пулі сесії | SupervisorPool |

## Ролі на рік (`AnnualRoleType`)

| Код | UI (українська) | Не використовувати |
|-----|-----------------|-------------------|
| `DepartmentHead` | Завідувач кафедри | — |
| `ExamCommissionSecretary` | Секретар ДЕК | `DekSecretary` |
| `AntiPlagiarismOfficer` | Відповідальний за антиплагіат | — |
| `FormattingReviewer` | Нормоконтролер | `NormController`, `NormControl` |

## Вимоги на допуск (`AdmissionCheckpointType`)

| Код | UI (українська) |
|-----|-----------------|
| `SupervisorFeedback` | Відгук керівника |
| `ExternalReview` | Рецензія |
| `AntiPlagiarismClearance` | Звіт антиплагіату |
| `FormattingReview` | Нормоконтроль |

## Статуси нормоконтролю (`FormattingReviewOutcome`)

| Код | UI |
|-----|-----|
| `Approved` | Допущено |
| `ApprovedWithRemarks` | Допущено з зауваженнями |
| `NotApproved` | Не допущено |

## Принцип

- **Код:** зрозумілі англійські доменні терміни (`ExamCommission`, `FormattingReview`).
- **UI:** `.resx` / `IStringLocalizer` — лише українські підписи для користувачів.

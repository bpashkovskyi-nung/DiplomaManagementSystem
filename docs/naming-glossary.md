# Словник імен: код ↔ UI

У коді — англійські імена без кальки з української. В інтерфейсі — офіційні українські терміни.

## Сутності

| Код (C# / БД) | UI (українська) | Було в wiki |
|---------------|-----------------|-------------|
| `DefenceSession` | Сесія захисту / Захист | Defense |
| `Diploma` | Бакалаврська / магістерська робота (залежно від `DefenceSessionType`) | Work |
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

## Склад екзаменаційної комісії

| Код | UI (українська) | Примітка |
|-----|-----------------|----------|
| `ExaminationCommissionParticipant` | Учасник ЕК | Session-scoped snapshot; не `AnnualRoleAssignment` |
| `ExaminationCommissionRole.Chair` | Голова ЕК | Рівно 1 на сесію |
| `ExaminationCommissionRole.Member` | Член ЕК | Мінімум 3 на сесію |
| `EmployeeId` (nullable) | — | `null` = зовнішня особа; інакше працівник кафедри |
| `FullName` / `Position` | ПІБ / Посада | Snapshot на момент збереження |

## Вимоги на допуск (`AdmissionCheckpointType`)

| Код | UI (українська) |
|-----|-----------------|
| `SupervisorFeedback` | Відгук керівника |
| `ExternalReview` | Рецензія |
| `AntiPlagiarismClearance` | Звіт антиплагіату |
| `FormattingReview` | Нормоконтроль |

## Результат перевірки кроку допуску (`CheckpointOutcome`)

| Код | UI |
|-----|-----|
| `Approved` | Допущено |
| `NotApproved` | Не допущено |

## Принцип

- **Код:** зрозумілі англійські доменні терміни (`ExamCommission`, `FormattingReview`).
- **UI:** `.resx` / `IStringLocalizer` — лише українські підписи для користувачів.

## Заголовки сторінок (`EmployeePageTitles`)

| Код | UI |
|-----|-----|
| `MyStudents` | Мої студенти — керівник |
| `MyReviewStudents` | Мої студенти — рецензент |
| `ConfirmStudentRequest` | Керівник — підтвердити заявку студента |
| `ApproveTopicAsSupervisor` | Керівник — затвердити тему |
| `SubmitSupervisorFeedback` | Керівник — надіслати відгук |
| `SubmitExternalReview` | Рецензент — надіслати рецензію |
| `AntiPlagiarism` | Антиплагіат — перевірка |
| `FormattingReview` | Нормоконтроль — перевірка |

## Заголовки сторінок секретаря (`SecretaryPageTitles`)

| Код | UI |
|-----|-----|
| `Home` | Кабінет секретаря |
| `TopicOrder` | Наказ на теми |
| `AdmittedReport` | Звіт допущених |
| `SelectSession` | Оберіть сесію захисту |

## Заголовки сторінок адміністратора (`AdminPageTitles`)

| Код | UI |
|-----|-----|
| `Home` | Кабінет адміністратора |
| `DefenceSessions` | Сесії захисту |
| `DefenceSession` | Сесія захисту |
| `Employees` | Викладачі |
| `Students` | Студенти |
| `AnnualRoles` | Ролі на сесію захисту |
| `EmployeeWorkloadLimits` | Ліміти викладачів |
| `ImportEmployees` | Імпорт викладачів |
| `ImportStudents` | Імпорт студентів |

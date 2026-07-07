# Feature 3 — генерація документів секретарем (MVP: наказ на теми)

## Мета

Великий модуль **генерації службових документів** для секретаря ДЕК.  
**MVP** — один документ: **наказ про затвердження тем, наукових керівників, консультантів та рецензентів** (формат як у зразку `Теми КІ-22-1,2.pdf`).

Вихідний формат: **DOCX** (завантаження файлу).

## Бізнес-правила

| # | Правило |
|---|---------|
| R1 | Генерує **секретар** активної сесії (`/Secretary/Documents` або розширення `/Secretary/Reports`) |
| R2 | **§1** (студенти): лише дипломи з `TopicVersionStatus.Approved` **і** `SupervisorAssignmentStatus.Confirmed` |
| R3 | **§1**: сортування за прізвищем студента (`PersonNameSort`), нумерація 1.1, 1.2, … |
| R4 | **§2** (рецензенти): усі викладачі з `ReviewerId` у відібраних дипломах + кількість робіт; без рецензента — рядок не потрапляє |
| R5 | **§3** (нормоконтроль): `AnnualRoleType.FormattingReviewer` у сесії |
| R6 | **§4** (контроль): `AnnualRoleType.DepartmentHead` у сесії |
| R7 | Секретар обирає **одну або кілька груп** сесії; курс у преамбулі — з поля групи (див. модель) |
| R8 | **№ наказу** і **рік** у шапці — вводить секретар при генерації (рік за замовчуванням з `DefenceSession.Year`) |
| R9 | **Консультанти** — лише згадка в заголовку наказу (як у PDF); сутностей і § про консультантів **немає** |
| R10 | Якщо даних для § недостатньо (немає завідувача / нормоконтролера) — попередження на формі, у DOCX — плейсхолдер `—` або пропуск з warning у UI |

## Не в scope (MVP)

- Google Sheets / Google Docs API
- Інші типи наказів і довідок (наступні ітерації того ж модуля)
- Сутність «консультант» і призначення консультантів
- Multi-tenant (факультет/спеціальність поки в `appsettings`; пізніше — tenant)
- Автонумерація наказів у реєстрі

## Модель даних

### `StudyGroup` — нове поле

- `Course` (`int?`, 1–6) — курс для преамбули («четвертого курсу»). Обов'язковий для генерації, якщо група обрана.

### `ApplicationUser` (викладачі) — нові поля

- `AcademicRank` (`EmployeeAcademicRank?`) — звання для наказу
- `ShortDisplayName` (`string?`, max 64) — скорочене ПІБ (`Гарасимів Т.Г.`); якщо порожнє — **автогенерація** з `FullName`

### `EmployeeAcademicRank` (enum)

| Значення | Скорочення в наказі |
|----------|-------------------|
| `Assistant` | асист. |
| `Lecturer` | викладач |
| `SeniorLecturer` | ст. викладач |
| `AssociateProfessor` | доц. |
| `Professor` | проф. |

Формат рядка керівника: `{rankAbbr} {ShortDisplayName}` → `доц. Гарасимів Т.Г.`

### `OrganizationOptions` (`appsettings`)

Тимчасово замість tenant; секція `Organization`:

- `MinistryName`
- `UniversityName`
- `City` (напр. `м. Івано-Франківськ`)
- `RectorName`
- `SpecialtyCode` (напр. `123`)
- `SpecialtyName` (напр. `Комп'ютерна інженерія`)
- `FacultyName` (напр. `факультет інформаційних технологій`)
- `StudyForm` (напр. `очної форми навчання`)
- `DepartmentName` (для §4, напр. `кафедри комп'ютерних систем і мереж`)

## Джерела даних для DOCX

| Блок наказу | Джерело |
|-------------|---------|
| Шапка (міністерство, ВНЗ, місто) | `OrganizationOptions` |
| Рік | форма (default `DefenceSession.Year`) |
| № наказу | форма |
| Преамбула: спеціальність, факультет, форма | `OrganizationOptions` |
| Преамбула: групи | обрані `StudyGroup.Name`, через кому |
| Преамбула: курс | `StudyGroup.Course` обраних груп (якщо різні — помилка валідації або найбільший з warning) |
| Преамбула: рівень (бакалавр/магістр) | `DefenceSession.Type` |
| §1 ПІБ | `ApplicationUser.FullName` (студент) |
| §1 тема | `DiplomaTopicVersion.Title` (`Approved`) |
| §1 керівник | `AcademicRank` + `ShortDisplayName` / автоген |
| §2 рецензенти | `ReviewerId` + rank + short name + `COUNT(*)` |
| §3 нормоконтроль | `FormattingReviewer` annual role |
| §4 контроль | `DepartmentHead` annual role + rank + short name |
| Підпис ректора | `OrganizationOptions.RectorName` |
| Згадка консультантів у заголовку | **фіксований текст шаблону** |

## План імплементації

### 1. Domain / Infrastructure

1. Enum `EmployeeAcademicRank`
2. Міграція: `StudyGroup.Course`, `ApplicationUser.AcademicRank`, `ApplicationUser.ShortDisplayName`
3. `AcademicNameFormatter` — автогенерація `Прізвище І.П.` з `FullName` (українська локаль)
4. `AcademicRankLabels` — enum → `доц.` / `асист.` / …
5. `OrganizationOptions` + реєстрація в DI (`IOptions`)

### 2. Admin — групи та викладачі

6. `StudyGroupFormDto` / форма адміна — поле **Курс**
7. `EmployeeFormDto` / форма адміна — **Звання**, **Скорочене ПІБ** (з підказкою автозаповнення)
8. **Імпорт викладачів** — опційні колонки `AcademicRank`, `ShortDisplayName` (CSV/XLSX)
9. При імпорті без `ShortDisplayName` — автогенерація; без rank — `null` (у наказі без префікса або `—` + warning)

### 3. Application — збір даних

10. `ITopicOrderDocumentQueries` — дипломи сесії за групами з фільтром R2
11. `TopicOrderDocumentService` — `BuildAsync` → `TopicOrderDocumentDto` (аналог `AdmittedReportService`)
12. Валідація форми: обрана ≥1 група, № наказу не порожній, курс заданий, попередження про відсутніх рецензентів/ролей

### 4. Application — генерація DOCX

13. Пакет `DocumentFormat.OpenXml`
14. Шаблон `App_Data/templates/topic-order.docx` з content controls / плейсхолдерами `{{OrderNumber}}`, `{{Section1Rows}}`, …
15. `TopicOrderDocxGenerator` — заповнення шаблону
16. Заголовок наказу **з текстом про консультантів** (як у PDF), без динамічного § консультантів

### 5. Secretary Web

17. `Secretary/Documents/TopicOrder` — форма (№, рік, мультиселект груп, preview кількості рядків)
18. `POST` → `FileResult` `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
19. Посилання з dashboard / Reports

### 6. Тести

20. `AcademicNameFormatterTests` — різні формати ПІБ
21. `AcademicRankLabelsTests`
22. `TopicOrderDocumentServiceTests` — фільтр R2, сортування, підрахунок рецензентів
23. `TopicOrderDocxGeneratorTests` — smoke: генерує валідний DOCX, містить ПІБ студента
24. Integration: повний сценарій seed → generate → перевірка ключових рядків
25. Web: секретар отримує 200 + content-type docx

### 7. Wiki / конфіг

26. `appsettings.Development.local.json.example` — секція `Organization`
27. `User-Manual/Secretary.md` — розділ «Документи»
28. `User-Manual/Admin.md` — курс групи, звання викладача, імпорт

## Структура модуля (майбутні документи)

```
Application/Secretary/Documents/
  Contracts/
  Dtos/
  TopicOrder/
    TopicOrderDocumentService.cs
    TopicOrderDocxGenerator.cs
  (майбутнє: AdmittedOrder, Schedule, …)
```

## Критерії приймання

- [x] Адмін задає курс групи
- [x] Адмін/імпорт: звання та скорочене ПІБ викладача (з автогенерацією)
- [x] Секретар генерує DOCX наказ на теми за обраними групами
- [x] У §1 лише студенти з затвердженою темою та підтвердженим керівником
- [x] §2–§4 заповнюються з наявних даних; заголовок містить згадку консультантів
- [x] Організаційні реквізити з `appsettings`
- [x] Тести зелені (unit); integration для TopicOrder — наступна ітерація

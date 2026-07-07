# Feature 1 — список студентів рецензента

## Мета

Рецензент бачить **список усіх студентів**, на яких він призначений рецензентом, з тими самими фільтрами та таблицею, що й керівник на `/Employee/Supervisor/Students`.

Окремо лишається сторінка **«Рецензент — надіслати рецензію»** (`/Employee/Reviewer/Assignments`) — черга активних рецензій на кроці `ExternalReview`.

## Вимоги

| # | Вимога |
|---|--------|
| R1 | `GET /Employee/Reviewer/Students` — таблиця з ПІБ, групою, темою, статусом, кроком допуску |
| R2 | Фільтри: lifecycle, admission step, група, пошук (як у керівника) |
| R3 | У таблиці показувати колонку **керівника** (`ShowSupervisorColumn = true`) |
| R4 | Без посилання «Деталі» в MVP (немає `Reviewer/Details`) |
| R5 | Плитка «Мої студенти — рецензент» з лічильником **Студентів**; одразу після «Мої студенти — керівник» |
| R6 | Умова відбору: `ReviewerId == user`, активна сесія захисту; **без** фільтра за статусом роботи чи рецензії |
| R7 | Плитки списків студентів виділені кольором; лічильник «Студентів», не «Очікує дій» |

## Не в scope

- Сторінка деталей диплома для рецензента
- Зміна логіки checkpoint `Assignments`

## План імплементації

### Application

1. `IDiplomaQueries.ListForReviewerReadAsync`
2. `IReviewerDiplomaListService` + `ReviewerDiplomaListService` (копія патерну `SupervisorDiplomaListService`)
3. `ReviewerDiplomaListPageDto`
4. `EmployeeHomeService` — картка `ReviewerStudents` після `SupervisorStudents`

### Web

5. `ReviewerController.Students`
6. `Views/Reviewer/Students.cshtml`
7. `SecretaryListViewModelMapper.MapReviewerStudents`
8. `EmployeePageTitles.MyReviewStudents`
9. Стиль `employee-students-card` для плиток списків студентів

### Тести

9. `ReviewerDiplomaListServiceTests` (unit)
10. `ReviewerStudentsEndpointTests` (integration)
11. Оновити `FakeDiplomaQueries`

### Wiki

12. `User-Manual/Reviewer.md` — розділ про список студентів

## Критерії приймання

- [x] Після призначення рецензентом секретарем студент з’являється на `/Employee/Reviewer/Students`
- [x] Фільтри працюють
- [x] На home є плитка з посиланням на `Students` одразу після «Мої студенти — керівник»
- [x] Список містить студентів незалежно від статусу роботи / завершення рецензії
- [x] Плитки списків студентів виділені кольором
- [x] `Assignments` не зламано
- [x] Тести зелені

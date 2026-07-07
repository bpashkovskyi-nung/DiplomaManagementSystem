# Feature 2 — ліміти керівника та рецензента в сесії

## Мета

У межах **сесії захисту** адміністратор задає для кожного викладача максимальну кількість студентів, у яких він може бути **керівником** і **рецензентом**.

## Бізнес-правила

| # | Правило |
|---|---------|
| R1 | Ліміти задає **адмін** на сторінці сесії (`/Admin/EmployeeWorkloadLimits`) |
| R2 | Ліміт **не встановлено** (немає запису або поле порожнє) → обмеження не діє |
| R3 | **Студент** може подати заявку керівнику навіть якщо у того вже досягнуто ліміт підтверджених студентів |
| R4 | **Керівник** не може **підтвердити** заявку, якщо після підтвердження перевищиться ліміт |
| R5 | **Секретар** не може **призначити/змінити керівника** (override), якщо перевищиться ліміт |
| R6 | **Секретар** не може **призначити рецензента**, якщо перевищиться ліміт рецензента |
| R7 | У підрахунок керівника входять лише `SupervisorAssignmentStatus == Confirmed` |
| R8 | У підрахунок рецензента входять усі дипломи з `ReviewerId == employee` у цій сесії |

## Не в scope

- Блокування подання заявки студентом
- Фільтрація списку викладачів у dropdown (лише runtime-відмова)
- Ліміти поза сесією (глобальні)

## Модель даних

`EmployeeSessionWorkloadLimit`:

- `DefenceSessionId`, `EmployeeId` (unique)
- `MaxSupervisorStudents` (int?, null = без ліміту)
- `MaxReviewerStudents` (int?, null = без ліміту)

## План імплементації

### Domain / Application

1. `EmployeeWorkloadLimitPolicy` — pure validation
2. `IEmployeeWorkloadLimitQueries` + `EmployeeWorkloadLimitQueries`
3. `IEmployeeWorkloadLimitService` — `EnsureCanAssignSupervisorAsync`, `EnsureCanAssignReviewerAsync`
4. Виклики в `SupervisorWorkflowService.ConfirmStudentAsync`, `SecretaryDiplomaActionService` (override + reviewer)
5. **Не** чіпати `SupervisorSelectionService`

### Admin Web

6. `IEmployeeWorkloadLimitAdminService` + CRUD upsert
7. `EmployeeWorkloadLimitsController` + `Index.cshtml`
8. Посилання з `DefenceSessions/Details`

### Тести

9. `EmployeeWorkloadLimitPolicyTests` (domain)
10. `EmployeeWorkloadLimitServiceTests` (application)
11. Integration: confirm blocked at limit; student request still allowed; secretary override blocked

### Wiki

12. `User-Manual/Admin.md` — розділ про ліміти
13. Оновити `Supervisor.md`, `Secretary.md`

## Критерії приймання

- [x] Адмін задає ліміти на сесії
- [x] Студент подає заявку при повному ліміті керівника
- [x] Керівник не підтверджує понад ліміт
- [x] Секретар не змінює керівника понад ліміт
- [x] Секретар не призначає рецензента понад ліміт
- [x] Тести зелені (domain, application, integration, web)

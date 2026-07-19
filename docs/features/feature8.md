# Feature 8 — прогрес майлстоунів і дати захисту

## Мета

1. Секретар ДЕК бачить лише сесії, де він призначений секретарем (вже є).
2. На сесію — **три майлстоуни** (дата + очікуваний % виконання).
3. Керівник заповнює **фактичний %** по кожному майлстоуну для своїх студентів.
4. Усі викладачі кафедри бачать **звіт прогресу** по сесії (групи за керівником).
5. Секретар задає **доступні дати захисту**.
6. Студент або керівник після допуску подає **одне незмінне побажання** дати.
7. Секретар **окремо** підтверджує фінальну дату (може відрізнятися від побажання).

## Цільовий workflow

```text
Secretary → Session config
  → 3 milestones (date + expected %)
  → available defence dates

Supervisor → Progress table (own students × 3 milestones)
Student/Supervisor → (after Admitted) one immutable date preference

Employee (any dept teacher) → Progress report for selected session
  → grouped by supervisor

Secretary → Preferences queue + Confirm final DefenceDate
Secretary → Admit (no date) → later ConfirmDefenceDate
```

## Бізнес-правила

| # | Правило |
|---|---------|
| R1 | Рівно **3** майлстоуни на сесію; дати унікальні й зростаючі; expected % ∈ 0..100 |
| R2 | Секретар може редагувати майлстоуни протягом **активної** сесії (і після actual %) |
| R3 | Керівник у будь-який момент активної сесії редагує actual % (0..100) лише своїх студентів |
| R4 | Actual % — один запис на (Diploma, Milestone); повторний запис оновлює значення |
| R5 | Звіт прогресу: активний `DepartmentEmployee` + обрана сесія **поточної** кафедри; усі студенти сесії, grouped by supervisor |
| R6 | Доступні дати захисту задає секретар; duplicate дат заборонено |
| R7 | Дату з побажанням або фінальним призначенням **не можна видалити** |
| R8 | Допуск **не** встановлює дату; `Diploma.DefenceDate` лишається nullable до підтвердження |
| R9 | Після `Admitted` студент **або** керівник може створити **одне** побажання; перший виграє; наступні заборонені |
| R10 | Побажання immutable; зберігає RequestedAt, PreferredDate, RequesterType, RequesterUserId |
| R11 | Фінальну дату секретар призначає лише з доступних дат; з побажанням або без; може ≠ preferred |
| R12 | Writes на archived session — заборонені |

## Модель даних

```csharp
DefenceSessionMilestone
{
    Guid Id
    Guid DefenceSessionId
    int Ordinal              // 1..3
    DateOnly DueDate
    int ExpectedPercent      // 0..100
}

DefenceDateOption
{
    Guid Id
    Guid DefenceSessionId
    DateOnly Date
}

DiplomaMilestoneProgress
{
    Guid Id
    Guid DiplomaId
    Guid MilestoneId
    int ActualPercent        // 0..100
    Guid RecordedByUserId
    DateTimeOffset RecordedAt
}

DefenceDatePreference
{
    Guid Id
    Guid DiplomaId           // unique
    Guid DefenceDateOptionId
    DefenceDateRequesterType RequesterType  // Student=0, Supervisor=1
    Guid RequesterUserId
    DateTimeOffset RequestedAt
}
```

Таблиці:

- `defence_session_milestones`
- `defence_date_options`
- `diploma_milestone_progress`
- `defence_date_preferences`

Індекси / constraints:

- unique `(DefenceSessionId, Ordinal)` на milestones
- unique `(DefenceSessionId, DueDate)` на milestones
- unique `(DefenceSessionId, Date)` на date options
- unique `(DiplomaId, MilestoneId)` на progress
- unique `DiplomaId` на preferences
- check percent 0..100; ordinal 1..3

FK: session/diploma → Cascade; milestone/option → Restrict для progress/preference; users → Restrict.

`Diploma.DefenceDate` — єдина фінальна дата.

## UI / локалізація

- Secretary: `/Secretary/SessionSetup` — milestones + dates; `/Secretary/DefenceDates` — queue побажань; Details — ConfirmDefenceDate
- Supervisor: `/Employee/Supervisor/Progress` — таблиця %; Details — request date
- Student: `/Student/Diploma` — request date після допуску
- Employee: `/Employee/Reports/Progress` — session selector + grouped report
- Підписи українською: «Майлстоуни», «Очікуваний %», «Фактичний %», «Доступні дати захисту», «Побажання дати», «Підтвердити дату захисту»

## Не в scope

- Ліміт студентів на день захисту
- Автоматичне планування розкладу захисту
- Зміна ролі секретаря / session scoping (вже існує)

## Acceptance

- [x] Секретар зберігає рівно 3 майлстоуни з валідацією дат/%
- [x] Керівник оновлює actual % своїх студентів
- [x] Викладач кафедри бачить звіт по сесії, grouped by supervisor
- [x] Секретар керує доступними датами; захищені від видалення
- [x] Admit без дати; одне immutable побажання після допуску
- [x] Секретар підтверджує фінальну дату з доступних
- [x] Тести + coverage нового коду ≈100%; глобальний gate ≥90%
- [x] Wiki оновлено

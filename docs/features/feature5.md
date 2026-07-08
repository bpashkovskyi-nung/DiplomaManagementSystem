# Feature 5 — спеціальності на кафедрі, форма навчання на групі

## Мета

Розділити оргструктуру та академічні атрибути:

- **Кафедра** — лише назва + факультет (без `SpecialtyCode`, `SpecialtyName`, `StudyForm`)
- **Спеціальність (`Specialty`)** — багато на кафедру; унікальність `(DepartmentId, Code)`
- **Група (`StudyGroup`)** — обов'язкові `SpecialtyId` + `StudyForm`; спеціальність має належати кафедрі сесії

## Цільова модель

```
Faculty 1──* Department 1──* Specialty
Department 1──* DefenceSession 1──* StudyGroup *──1 Specialty
```

| Сутність | Ключові поля |
|----------|--------------|
| `Department` | `Id`, `FacultyId`, `Name`, `IsActive` |
| `Specialty` | `Id`, `DepartmentId`, `Code`, `Name`, `IsActive`, `CreatedAt` |
| `StudyGroup` | `Id`, `DefenceSessionId`, `SpecialtyId`, `StudyForm`, `Name`, `Course` |

## Бізнес-правила

| # | Правило |
|---|---------|
| R1 | На кафедрі може бути **N спеціальностей**; код унікальний в межах кафедри |
| R2 | Група **завжди** має одну спеціальність і форму навчання |
| R3 | При створенні/редагуванні групи `SpecialtyId` має належати кафедрі сесії захисту |
| R4 | **Наказ про теми**: обрані групи мають однакові `SpecialtyId` і `StudyForm` (як для курсу) |
| R5 | Спеціальність **не деактивується**, якщо на неї посилаються групи |
| R6 | JSON-імпорт структури: лише `faculty.name` + `department.name` (без спеціальностей) |
| R7 | Bootstrap seed (`OrganizationOptions`) лишається для першої кафедри + першої спеціальності |

## Міграція `MoveSpecialtyToStudyGroup` (expand → backfill → contract)

1. **Expand** — таблиця `specialties`; nullable `SpecialtyId`, `StudyForm` на `study_groups`
2. **Backfill specialties** — один запис на кафедру з legacy-полів `departments`; порожній код → `000`
3. **Backfill study_groups** — через `defence_sessions` → `departments` → `specialties` + `StudyForm` з кафедри
4. **Contract** — `NOT NULL` на групах; видалення `SpecialtyCode`, `SpecialtyName`, `StudyForm` з `departments`

> Після міграції на кафедрі залишається **одна** спеціальність з legacy-даних. Додаткові — вручну через SuperAdmin UI.

## UI

### SuperAdmin — редагування кафедри

- Форма кафедри: лише факультет + назва
- Секція **«Спеціальності»**: таблиця + додавання code/name, деактивація (якщо немає груп)

### Admin — група

- Dropdown спеціальностей кафедри сесії
- Поле **«Форма навчання»**

## Ризики

- Кафедри з порожнім legacy `SpecialtyCode` отримають код `000` — виправити в SA UI
- Створення групи заблоковано, доки на кафедрі немає хоча б однієї активної спеціальності
- Перед deploy: backup БД; міграція атомарна, downtime не потрібен

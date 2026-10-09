# Feature 7 — склад екзаменаційної комісії в ролях сесії

## Мета

На сторінці редагування ролей сесії додати **склад ЕК**:

- **Голова ЕК** — рівно 1
- **Члени ЕК** — мінімум 3, можна більше

Учасником може бути:

- працівник кафедри сесії (внутрішній), або
- зовнішня особа без облікового запису (ПІБ і посада вручну)

## Цільовий workflow

```text
Admin → Сесія захисту → Ролі
  → операційні ролі (як і раніше: завідувач, секретар ДЕК, антиплагіат, нормоконтроль)
  → секція «Екзаменаційна комісія»
       → Голова ЕК (internal | external)
       → Члени ЕК (список ≥ 3)
       → Зберегти склад ЕК
```

Операційні `AnnualRoleAssignment` **не змінюються**. Склад ЕК — окрема сутність.

## Бізнес-правила

| # | Правило |
|---|---------|
| R1 | Рівно **один** голова на сесію |
| R2 | Мінімум **три** члени; верхньої межі немає |
| R3 | Внутрішній учасник: активний `DepartmentEmployee` кафедри сесії |
| R4 | Для внутрішнього: `FullName` копіюється з профілю; `Position` = display `AcademicRank` і **не редагується** |
| R5 | Внутрішній без `AcademicRank` — помилка («заповніть вчене звання») |
| R6 | Зовнішній: `EmployeeId = null`; ПІБ і посада обов’язкові (ручний ввід) |
| R7 | Один і той самий працівник не може бути двічі в складі (голова + член / дубль членів) |
| R8 | Склад **інформаційний**: не дає authorization / employee workspace |
| R9 | Збереження — replace-all для сесії |
| R10 | Існуючі сесії стартують з порожнім складом (без backfill) |

## Модель даних

```csharp
ExaminationCommissionParticipant
{
    Guid Id
    Guid DefenceSessionId
    ExaminationCommissionRole Role // Chair=0, Member=1
    Guid? EmployeeId                 // null = зовнішня особа
    string FullName                  // snapshot
    string Position                  // snapshot / free-text
    int SortOrder
    DateTimeOffset CreatedAt
}
```

Таблиця: `examination_commission_participants`

Індекси:

- `(DefenceSessionId, Role)`
- unique filtered `(DefenceSessionId, EmployeeId) WHERE EmployeeId IS NOT NULL`
- unique filtered `(DefenceSessionId) WHERE Role = Chair`

FK:

- session → `Cascade`
- employee (`users`) → `SetNull`

## UI / локалізація

- Секція під таблицею операційних ролей на `/Admin/AnnualRoles`
- Голова: Internal/External + select або ПІБ/посада
- Члени: рядки з add/remove; порожній стан показує 3 порожні слоти
- Для internal — посада read-only з звання
- POST `SaveCommission` окремо від `Assign` операційних ролей
- Підписи: «Голова ЕК», «Члени ЕК», «Екзаменаційна комісія»

## Не в scope

- Authorization / кабінет для голови чи членів ЕК
- Підстановка складу в DOCX/звіти (окремий follow-up)
- Зміна моделі `AnnualRoleType` / `AnnualRoleAssignment`

## Acceptance

- [x] На сторінці ролей видно секцію складу ЕК
- [x] Можна зберегти 1 голову + ≥3 членів (internal/external мікс)
- [x] Internal бере ПІБ і звання з профілю; без звання — помилка
- [x] External вимагає ПІБ і посаду
- [x] <3 членів або ≠1 голова — валідація
- [x] Операційні ролі як і раніше (4 слоти)
- [x] Міграція створює порожню таблицю без зміни існуючих ролей

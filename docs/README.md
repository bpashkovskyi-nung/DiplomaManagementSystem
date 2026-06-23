# Документація розробки

Технічна документація для імплементації (актуальніша за [wiki v1](https://github.com/bpashkovskyi/diploma-management-system/wiki), де зафіксовані бізнес-вимоги MVP).

| Документ | Опис |
|----------|------|
| [Доменна модель](domain-model.md) | Сутності, перейменування, інваріанти |
| [Implementation plan](implementation-plan.md) | Детальний план розробки ASP.NET Core MVC + PostgreSQL |
| [Словник імен (код ↔ UI)](naming-glossary.md) | Англійські імена в коді та українські підписи в UI |

## Відмінності docs vs wiki (v1)

| Тема | Wiki (бізнес) | Docs (імплементація) |
|------|---------------|----------------------|
| Сутність «Захист» | `Defense` | `DefenceSession` |
| Робота студента | `Work` | `Diploma` |
| Група ↔ захист | M:N, потік груп | **1 група → 1 сесія** |
| Файли документів | PDF/Word у системі | **v1 без файлів** — лише статуси; файли → v2 (Google Drive) |
| Ролі в коді | DekSecretary, NormControl | `ExamCommissionSecretary`, `FormattingReviewer` |

Wiki оновлюється окремо для roadmap (інтеграції v2+).

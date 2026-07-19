# Документація розробки

Технічна документація для імплементації (актуальніша за [wiki v1](https://github.com/bpashkovskyi/diploma-management-system/wiki), де зафіксовані бізнес-вимоги MVP).

| Документ | Опис |
|----------|------|
| [coding-style.md](coding-style.md) | Канонічний coding style, analyzers, coverage ratchet |
| [features/feature1.md](features/feature1.md) | Feature 1: список студентів рецензента |
| [features/feature2.md](features/feature2.md) | Feature 2: ліміти керівника/рецензента в сесії |
| [features/feature3.md](features/feature3.md) | Feature 3: генерація документів (MVP — наказ на теми, DOCX) |
| [features/feature8.md](features/feature8.md) | Feature 8: майлстоуни прогресу та дати захисту |
| [Implementation plan](implementation-plan.md) | Детальний план розробки ASP.NET Core MVC + PostgreSQL |
| [Словник імен (код ↔ UI)](naming-glossary.md) | Англійські імена в коді та українські підписи в UI |
| [Routing table](routing-table.md) | Маршрути Controller/Action по Areas |
| [DDL draft](ddl-draft.sql) | Чернетка схеми PostgreSQL |
| [ViewModels](viewmodels.md) | ViewModels по екранах |
| [appsettings template](appsettings.template.json) | Шаблон конфігурації |
| [Progress tracker](progress.md) | % прогресу по спринтах |
| [Workflow — загальний флов](workflow-flow.md) | Кроки A/B/C, checkpoint outcomes, прогрес-бар |
| [Тестові сценарії](test-scenarios.md) | Матриця 43 студентів по всіх фловах + `scripts/seed-*.sql` |
| [План: admission step history](plans/admission-review-refactor.md) | Рефакторинг current step + історія спроб (замість 4 checkpoint-рядків) |
| [UAT scenarios](uat-scenarios.md) | Автоматизовані acceptance-тести |

## Відмінності docs vs wiki (v1)

| Тема | Wiki (бізнес) | Docs (імплементація) |
|------|---------------|----------------------|
| Сутність «Захист» | `Defense` | `DefenceSession` |
| Робота студента | `Work` | `Diploma` |
| Група ↔ захист | M:N, потік груп | **1 група → 1 сесія** |
| Файли документів | PDF/Word у системі | **v1 без файлів** — лише статуси; файли → v2 (Google Drive) |
| Ролі в коді | DekSecretary, NormControl | `ExamCommissionSecretary`, `FormattingReviewer` |

Wiki оновлюється окремо для roadmap (інтеграції v2+).

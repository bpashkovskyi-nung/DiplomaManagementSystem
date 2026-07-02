# Diploma Management System

Система управління підготовкою дипломних робіт (бакалавр / магістр): тема, керівництво, перевірки, захист.

**Стек:** ASP.NET Core MVC, PostgreSQL, EF Core, Google OAuth, Google Drive (файли).

## Документація

| Джерело | Зміст |
|---------|--------|
| **[Wiki](https://github.com/bpashkovskyi-nung/DiplomaManagementSystem/wiki)** | Користувацький посібник, ролі, процеси, roadmap |
| **[docs/](docs/README.md)** | Імплементація: доменна модель, план, naming, маршрути |
| **[docs/test-plan.md](docs/test-plan.md)** | План і прогрес автоматизованих тестів |

## Швидкий старт (розробка)

1. Скопіюйте `src/DiplomaManagementSystem.Web/appsettings.Development.local.json.example` → `appsettings.Development.local.json` і заповніть підключення до БД та Google OAuth.
2. Застосуйте міграції EF Core до PostgreSQL.
3. Запустіть `src/DiplomaManagementSystem.Web`.

Деталі конфігурації та деплою — у [implementation plan](docs/implementation-plan.md).

## License

MIT — see [LICENSE](LICENSE).

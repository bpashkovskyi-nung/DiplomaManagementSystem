# Coding style

Канонічний профіль стилю та якості для `diploma-management-system`.  
Автоматизація: [`.editorconfig`](../.editorconfig), [`Directory.Build.props`](../Directory.Build.props), [`.github/workflows/ci.yml`](../.github/workflows/ci.yml), [`coverlet.runsettings`](../coverlet.runsettings).

Історичний audit findings: [code-style-audit.md](code-style-audit.md) (архів; не є джерелом правил).

---

## 1. Format (enforced)

| Правило | Значення | Як enforced |
|---------|----------|-------------|
| Line endings | CRLF | `.editorconfig` |
| Indent | 4 spaces (C#); 2 для yml/json/md/csproj | `.editorconfig` |
| Braces | Allman; завжди `{}` | `.editorconfig` + IDE0011 as error |
| Namespaces | file-scoped | `.editorconfig` + IDE0161 as error |
| Usings | outside namespace; System first; **groups separated** | `.editorconfig` + IDE0005 unused as error |
| Max line | 120 | `.editorconfig` |
| Blank lines | max 1 consecutive | IDE2000 warning |
| Final newline / trim | yes | `.editorconfig` |

Команди:

```bash
dotnet format
dotnet format --verify-no-changes --severity error
```

CI виконує `dotnet format --verify-no-changes --severity error` на кожному PR (whitespace + diagnostics з severity error; CA-warnings лишаються в `dotnet build`).

---

## 2. Idioms (mostly review + IDE suggestions)

- `var` коли тип очевидний (`csharp_style_var_when_type_is_apparent`)
- Primary constructors для DI
- Expression-bodied для простих members
- Prefer `new()` / collection expressions `[]` коли тип очевидний
- Prefer pattern matching; `is null` / `is not null` (IDE0041 as error)
- Records для immutable DTO; positional records ок
- Prefer `readonly` де можливо
- `this.` лише при конфлікті імен
- Modifier order — .NET preferred order

---

## 3. Architecture (review-only policy)

Не закодовано в editorconfig; перевіряється в code review:

| Тема | Правило |
|------|---------|
| Visibility | `internal sealed` за замовчуванням; `public` лише на boundaries |
| Boundaries | Interfaces на Application ↔ Infrastructure / Web |
| DI | Constructor injection; primary ctors |
| Async | `Async` suffix для I/O; `CancellationToken` на всіх I/O API |
| Errors | `DomainException` для бізнес-помилок |
| Validation | FluentValidation + domain invariants |
| Logging | Structured logging (message templates, без string concat) |
| Config | `IOptions<T>` + `ValidateOnStart` |
| HTTP | Controllers (не minimal APIs) |
| EF | `AsNoTracking()` для read-only queries |
| IDs / time | `Guid` + `DateTimeOffset` |

---

## 4. Naming & structure

- Types / members: PascalCase; interfaces `I…`
- Locals / params: camelCase
- Private fields: `_camelCase`
- Acronyms як слова (`Http`, не `HTTP` у середині імені)
- Один головний тип на файл
- Без `#region`
- XML docs — лише для public API на boundaries
- Nullable reference types: `enable` (solution-wide)

---

## 5. Analyzers & severity

| Рівень | Що |
|--------|----|
| Build | `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended` |
| Errors (selective) | `IDE0005` (unused using), `IDE0011` (braces), `IDE0161` (file-scoped ns), `IDE0041` (`is null`) |
| Warnings | решта style/naming з `.editorconfig`; nullable `CS86xx` |
| Opt-in | Sonar analyzers через `RunSonarAnalyzers=true` (не в кожному CI) |

Повний `TreatWarningsAsErrors=true` **не** увімкнено. Підвищувати severity точково в follow-up PR.

SDK pin: [`global.json`](../global.json) (`10.0.x`, `rollForward: latestFeature`).

---

## 6. Tests & coverage

| Тема | Правило |
|------|---------|
| Структура | AAA |
| Імена | `Method_Scenario_Expected` |
| Doubles | fakes-first (mocks лише коли потрібно) |
| Піраміда | багато unit → менше integration → мало E2E |
| Integration DB | PostgreSQL (CI service / Testcontainers де доречно) |
| Coverage target | **90% line** глобально (Domain + Application + Infrastructure + Web) |
| Coverage gate | baseline → ratchet; початковий measured baseline **90.6%** → CI floor **90** |

Exclusions: EF migrations / generated (`coverlet.runsettings`). Не виключати Web/Infrastructure wholesale.

### Локально зібрати coverage

```bash
dotnet test -c Release --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./artifacts/coverage
```

CI: unit + integration збирають Cobertura → job `coverage` мержить через ReportGenerator і гейтить `lineCoverage` проти `COVERAGE_LINE_FLOOR` (зараз **90**).

Початковий measured baseline (unit+integration, 2026-07-18): **90.6%** line. Floor уже на цілі; далі лише не опускати без review.

---

## 7. Enforced vs review-only

| Automated (CI / build) | Review-only |
|------------------------|-------------|
| Format verify | Architecture (sealed/internal, boundaries) |
| Selective IDE as errors | Async/CancellationToken discipline |
| Style warnings in build | FluentValidation / DomainException usage |
| Coverage floor (ratchet) | Test pyramid balance; fakes-first |
| Nullable enable | XML docs completeness |

---

## 8. Related docs

- [naming-glossary.md](naming-glossary.md) — код ↔ UI імена
- [code-style-audit.md](code-style-audit.md) — архів findings (історія)

# MicroclimateMonitor

Програмна система інтелектуального моніторингу мікроклімату приміщення
(навчальний проєкт з дисципліни «Мультиплатформне програмування», C# / .NET).

Система приймає показники датчиків (температура, вологість, CO₂), перевіряє їх,
оцінює комфорт, знаходить загрозливі значення та різкі аномалії, зберігає історію
в SQLite і показує звіти в консолі. Працює на **Windows, Linux та macOS**.

## Вимоги

- [.NET SDK 10](https://dotnet.microsoft.com/download) (або .NET 8 — див. коментар у `Directory.Build.props`)
- Git

## Запуск з нуля

```bash
git clone <URL-репозиторію>
cd MicroclimateMonitor
dotnet restore
dotnet build
dotnet run --project src/Microclimate.App -- add-room Office "2nd floor"
dotnet run --project src/Microclimate.App -- simulate Office 40 --anomaly
dotnet run --project src/Microclimate.App -- report Office
dotnet run --project src/Microclimate.App -- alerts Office
```

БД — файл `microclimate.db` у поточній теці (шлях можна змінити змінною середовища `MICROCLIMATE_DB`).

## Команди

| Команда | Опис |
|---|---|
| `add-room <name> [location]` | додати приміщення |
| `rooms` | список приміщень |
| `simulate <room> [count] [--anomaly]` | згенерувати вимірювання, проаналізувати й зберегти |
| `report <room>` | середні значення, максимум CO₂, індекс комфорту, точка роси |
| `alerts <room> [limit]` | останні сповіщення |

## Архітектура

```
src/
  Microclimate.Core   — моделі, валідація, аналіз комфорту, виявлення аномалій, симулятор датчиків
  Microclimate.Data   — SQLite: схема, репозиторії (параметризовані запити, транзакції)
  Microclimate.App    — консольний застосунок (CommandRunner)
tests/
  Microclimate.Tests  — xUnit-тести всіх шарів
docs/                 — журнал рішень, тест-кейси, крайні випадки, схема БД
```

## Тести, покриття, статичний аналіз

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line
```

Статичний аналіз (SonarAnalyzer) виконується під час `dotnet build` — попередження виводяться в консоль.
CI (GitHub Actions) збирає проєкт і ганяє тести на ubuntu / windows / macos.

## Документація

- [Журнал рішень](docs/DECISIONS.md)
- [Тест-кейси](docs/TEST_CASES.md)
- [Крайні випадки](docs/EDGE_CASES.md)
- [Цілісність даних / схема БД](docs/DB_SCHEMA.md)
- [Задачі та відповідальні](docs/TASKS.md)

## Команда

| Учасник | Зона відповідальності |
|---|---|
| A | Core: моделі, валідація, аналіз комфорту, виявлення аномалій |
| B | Data: схема БД, репозиторії, транзакції |
| C | App: CLI, симулятор датчиків, CI, документація |

## Типові проблеми

- `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0` — встановіть .NET SDK 10
  (`winget install Microsoft.DotNet.SDK.10`) і перезапустіть термінал. Перевірка: `dotnet --list-sdks`.
- Файл БД `microclimate.db` можна безпечно видалити — він створиться заново.
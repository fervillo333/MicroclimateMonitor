# Крайні випадки (C2)

| № | Крайній випадок | Як оброблено | Підтвердження | Оброблено |
|---|---|---|---|---|
| 1 | Датчик віддав NaN / ∞ / вологість 101 % | `ReadingValidator` відхиляє; `ArgumentException` до аналізу й запису | `ReadingValidatorTests.Validate_InvalidValues_ReturnsErrors` | так |
| 2 | Значення рівно на межі порога (26.0 °C, CO₂ 1000) | Строге порівняння: на межі — норма | `ComfortAnalyzerTests.Analyze_TemperatureBoundary`, `Analyze_Co2` | так |
| 3 | Порожня назва / дубль приміщення | `ArgumentException` / `InvalidOperationException`, CLI повертає код 1 | `RoomRepositoryTests`, `CommandRunnerTests` | так |
| 4 | Запис вимірювання в неіснуюче приміщення | FK-помилка → `InvalidOperationException`, транзакція відкочується | `ReadingRepositoryTests.Save_UnknownRoom_Throws_AndStoresNothing` | так |
| 5 | SQL-ін'єкція в назві приміщення | Параметризовані запити, назва зберігається як текст | `RoomRepositoryTests.SqlInjectionInName_IsStoredAsPlainText` | так |
| + | Звіт для приміщення без вимірювань; некоректний count у CLI | Повідомлення «No readings», код 1 | `CommandRunnerTests` | так |

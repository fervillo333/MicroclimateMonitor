# Цілісність і безпека даних (C5)

Схема: `rooms (1) —— (N) readings (1) —— (N) alerts`. SQL — у `src/Microclimate.Data/Schema.cs`.

| Критерій | Виконано | Підтвердження |
|---|---|---|
| Нормалізація ≥ 3НФ | так | Кожен факт зберігається один раз: назва кімнати — в `rooms`, показники — в `readings`, сповіщення — в `alerts`; немає транзитивних залежностей |
| PK/FK та обмеження цілісності | так | `PRIMARY KEY`, `REFERENCES … ON DELETE CASCADE`, `UNIQUE(name)`, `CHECK` на діапазони; `PRAGMA foreign_keys=ON` у `SqliteDatabase.Open`; тест `SchemaTests.ForeignKeys_AreEnforced` |
| Індекси на полях пошуку/JOIN | так | `ix_readings_room_time(room_id, timestamp_utc)`, `ix_alerts_reading(reading_id)`; тест `SchemaTests.Indexes_Exist` |
| Параметризовані запити | так | Усі запити з `$param`; тест `SqlInjectionInName_IsStoredAsPlainText` |
| Транзакції | так | `ReadingRepository.SaveWithAlerts` — вимірювання + сповіщення атомарно |

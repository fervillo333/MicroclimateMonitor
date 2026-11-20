# Тест-кейси (C1)

Усі кроки виконуються з кореня репозиторію; `microclimate` = `dotnet run --project src/Microclimate.App --`.

| ID | Опис | Кроки | Очікуваний результат | Результат (скріншот) |
|---|---|---|---|---|
| TC-1 | Додавання приміщення | `add-room Office "2nd floor"` → `rooms` | «Room added…», у списку є Office | ☐ |
| TC-2 | Нормальна симуляція | `simulate Office 30` | «Saved 30 readings, 0 alerts» | ☐ |
| TC-3 | Звіт | `report Office` | Readings: 30, середні значення, comfort score | ☐ |
| TC-4 | Симуляція з аномалією | `simulate Office 40 --anomaly` → `alerts Office` | Є Anomaly, TemperatureHigh, Co2High (Critical) | ☐ |
| TC-5 | Невідоме приміщення | `report Nope` | «Room 'Nope' not found», код виходу 1 | ☐ |
| TC-6 | Дубль приміщення | двічі `add-room Office` | Друга спроба: «already exists», код 1 | ☐ |
| TC-7 | Запуск на іншій ОС | CI-матриця ubuntu/windows/macos | Усі три джоби зелені | ☐ |

Автоматизовано: TC-1…TC-6 покриті `CommandRunnerTests`.

# Bombardiro Crocodilo v04 — QA

Дата: 13 октября 2026 года

Версия игры: 0.6.1 (`versionCode 38`)

Устройство: realme RMX3151 / ARM64 / Mali-G57 MC2

## Модель

- Исходник `Bombardiro_v03.blend` сохранён без перезаписи.
- Новая версия создана в Blender 5.2.2 LTS: `Bombardiro_v04.blend` и `Bombardiro_v04.fbx`.
- Добавлено 164 элемента детализации: увеличенные клыки, складка пасти, бронированные надбровья, янтарные глаза, чешуя, ноздри, швы и заклёпки фюзеляжа, выхлопы, детали мотогондол и винтов, три подвесные бомбы.
- Сохранены 8 костей, 7 renderer-секций и все 8 анимаций: Drop, Idle, Jump, Land, Run, Special, Victory, VictoryShow.
- Основная секция выросла с 22 728 до 32 684 вершин; челюсть — с 1 076 до 1 310; каждый винт — с 650 до 820.
- В меню добавлен отдельный фронтальный трёхчетвертной ракурс и более крупное кадрирование Bombardiro. Игровой масштаб не менялся.

## Unity

- Android batch build: PASS, exit code 0.
- `BOMBARDIRO_ANIMATOR_READY clips=8`.
- `HERO_AUTHORED_CLIPS_OK` для Drop, Special и VictoryShow.
- Общие smoke-тесты сборки: PASS.

## Realme

- Выбор Bombardiro и 3D-портрет: PASS.
- FBX rig и перекраска 7/7 секций: PASS.
- Drop и приземление: PASS.
- Run: PASS.
- Special «3 бомбы» и VFX: PASS.
- Fatal/Exception в игровом логе: нет.
- Стабилизированные игровые окна: 57,5–59,5 FPS.

## APK

- Файл: `Builds/Android/BrainrotPlanet-v0.6.1.apk`
- Размер: 53 852 238 байт.
- SHA-256: `9CE08C2E7B7CDF16C1B24448B26C62308208CA471A12C6DCC7729DCAD9398794`.

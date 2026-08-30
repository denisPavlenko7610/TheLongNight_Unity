# Unity Gaming Services + Netcode for GameObjects

Учебная папка для `com.unity.services.multiplayer 2.1.3` и `com.unity.netcode.gameobjects 2.13.0`.
Каждый `ExampleXX_*.cs` вводит одну новую идею. `UgsNetcodeLearningContext.cs` — только общая
инфраструктура примеров.

## Какие сервисы здесь нужны

| Слой | Задача | Где изучать |
|---|---|---|
| Authentication | Даёт постоянный в рамках аккаунта `PlayerId` и access token | `Example01` |
| Lobby через Sessions API | Хранит группу игроков, join code, свойства и события | `Example02`–`05`, `07`–`12` |
| Relay через Sessions API | Соединяет client-hosted игру через интернет без открытия порта | `Example02`, `03`, `10`, `12` |
| Matchmaker | Подбирает Session по queue/rules/QoS | `Example06` |
| NGO | Передаёт realtime gameplay state, RPC, spawn и NetworkVariable | существующий курс `../Netcode` |

Современный Multiplayer Services SDK объединяет Lobby, Relay и Matchmaker под `MultiplayerService.Instance`.
Для обычной client-hosted игры сначала изучайте Sessions API. Низкоуровневые отдельные Lobby/Relay API нужны
только когда готовая Session-модель не подходит.

Economy, Cloud Save, Leaderboards, Remote Config, Analytics и Vivox полезны игре, но не устанавливают NGO
соединение, поэтому в этот курс не включены.

## Подготовка проекта

1. Свяжите Unity-проект с Cloud Project в `Project Settings > Services`.
2. В Unity Dashboard выберите нужный Environment (по умолчанию в контексте стоит `production`).
3. В сцене должен быть ровно один активный `NetworkManager` с `UnityTransport`.
4. На тот же GameObject добавьте `UgsNetcodeLearningContext` и нужный `ExampleXX`.
5. Для `Example06` создайте Matchmaker queue `Friendly` и client-hosted pool либо измените `_queueName`.
6. Для реальной проверки запустите Host и Client в разных процессах через Multiplayer Play Mode или build.
7. У процессов должны быть разные Authentication Profile/локальные данные, иначе оба экземпляра могут
   авторизоваться одним `PlayerId`.

UGS вызовы работают только у проекта с корректно настроенными Dashboard services и доступом в интернет.
Ошибки `SessionException` содержат `Error`, по которому UI должен различать timeout, full session,
rate limit, unauthorized и другие случаи.

## Порядок уроков

| Шаг | Файл | Один вопрос шага |
|---:|---|---|
| 01 | `Example01_InitializeAndAuthenticate.cs` | Почему UGS сначала требует Initialize и Sign-In? |
| 02 | `Example02_CreateRelaySession.cs` | Как Session + Relay автоматически запускают NGO Host? |
| 03 | `Example03_JoinByCode.cs` | Как join code приводит клиента в Session и NGO? |
| 04 | `Example04_BrowseSessions.cs` | Как запросить публичные Session и войти по ID? |
| 05 | `Example05_QuickJoin.cs` | Как быстро найти или создать подходящую игру? |
| 06 | `Example06_Matchmaker.cs` | Как отправить ticket в Matchmaker queue и отменить поиск? |
| 07 | `Example07_SessionProperties.cs` | Чем Lobby/session data отличается от NetworkVariable? |
| 08 | `Example08_PlayerProperties.cs` | Как игрок сохраняет ready/character/displayName? |
| 09 | `Example09_SessionEvents.cs` | Чем UGS events отличаются от NGO callbacks? |
| 10 | `Example10_DelayedNgoStart.cs` | Как сначала собрать Lobby, а Relay и NGO запустить позже? |
| 11 | `Example11_Reconnect.cs` | Когда возможен reconnect к прежней Session? |
| 12 | `Example12_LeaveAndDelete.cs` | Чем остановка NGO отличается от Leave/Delete Session? |

## Главная модель

```text
Authentication PlayerId
        |
        v
UGS Session (Lobby/Matchmaker, игроки, join code, metadata)
        |
        v
Relay allocation + connection data
        |
        v
NGO NetworkManager (Host/Client, RPC, spawn, NetworkVariable)
```

`PlayerId`, `Session.Id`, `Session.Code`, NGO `clientId` и `NetworkObjectId` — разные идентификаторы.
Не подменяйте один другим.

## Что хранить где

- Session property: карта, режим, версия build, стадия waiting/playing, фильтры поиска.
- Player property: ready, выбранный персонаж, отображаемое имя.
- NGO `NetworkVariable`: здоровье, счёт, дверь, authoritative gameplay state.
- NGO RPC: одноразовая просьба или событие.

Lobby-backed свойства обновляются backend-запросами, имеют rate limits и не предназначены для каждого кадра.
Любым клиентским Session/Player property нельзя доверять как server-authoritative gameplay-данным.

## Production-заметки

- Anonymous Sign-In нужно связать с настоящим аккаунтом, чтобы игрок не потерял доступ после очистки данных.
- Обрабатывайте `SessionException`, отмену, timeout, offline и повторные запросы в UI.
- Не храните секреты сервера в клиентском Unity build.
- При использовании UGS Authentication проверьте актуальные требования Unity к уведомлениям и DSA compliance.
- Для dedicated server изменяется hosting flow: Matchmaker обычно выдаёт прямой endpoint сервера, а не
  запускает client-hosted Relay.

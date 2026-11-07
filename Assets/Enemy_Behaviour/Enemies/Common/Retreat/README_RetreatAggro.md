# Отступление, восстановление и агро

Готовый пример: `Demo/Scenes/RetreatAggro_Test.unity`.
Основной враг в этой сцене отступает после одного удара игрока: пороги **90 / 95 / 100%**.
Новый префаб: `Enemies/Base/Prefabs/PrototypeEnemy_Retreat.prefab`.
В нём обычные значения по умолчанию: **25 / 50 / 60%**.

## Как устроено

`Base_Behaviour` остаётся enum FSM в Behaviour Graph:
`Patrol = 0`, `Attack = 1`, `Idle = 2`, **`Retreat = 3`**.
Перед обычным Switch стоит проверка `Retreat check CanStart`.
Её `Abort If` прерывает текущий патруль, паузу или атаку при необходимости отступить.
Отдельного C# FSM нет. `EnemyRetreat` хранит настройки, счётчик, цель и наблюдения;
ноды графа выбирают этап и выполняют движение, контратаку и лечение.

Общие графы можно использовать у других типов врагов:

- `Common/Aggro/Behavior/Aggro_Engage`: запомнить цель и установить State = Attack.
  Физическая рассылка агро работает в `EnemyAggro`/`EnemySensor`, без Event Channels.
- `Common/Retreat/Behavior/Retreat_Recover`: Moving → Surviving → Recovering.
  Пауза Surviving — стандартная нода **Wait**, а не таймер внутри ноды движения.
  Урон перезапускает ожидание. Потеря дистанции возвращает к движению.
  Лечение постепенное; урон, близость игрока или превышение порога HP возвращают Attack.

Контратака вызывает текущий динамический граф из переменной **Attack** с
`AttackTarget = RetreatTarget`, `AttackOnce = true`. Параллельный Wait ограничивает
попытку параметром Counter Attack Window: враг не будет бесконечно догонять цель
внутри контратаки. Обычная атака сохраняет `AttackOnce = false`.
Если другой граф атаки должен поддерживать ровно один удар, он должен учитывать
`AttackOnce` и возвращать Success после удара; общий контракт — AttackBlackboard.
После контратаки враг снова набирает безопасную дистанцию и ждёт полное Survival Time.

Стены, другие препятствия и края платформ отменяют отступление; прыжков нет.
Непрерывная близость игрока дольше Corner Check Time тоже отменяет его.
Отменённое отступление считается использованным. При прерывании самого графа
cleanup-ветка останавливает Rigidbody и сбрасывает активное отступление.

## Настройка конкретного врага

Все проценты в Inspector задаются **от 0 до 100**, например **90**, а не 0.9.

| Компонент / поле | По умолчанию | Назначение |
|---|---:|---|
| EnemyRetreat / Retreat Enabled | true | Включить возможность отступления |
| Retreat Hp Percent | 25 | Отступать при HP ≤ заданного процента |
| Max Retreats | 3 | Лимит за жизнь/включение; -1 без лимита, 0 отключает |
| Retreat Cooldown | 8 s | Минимальное время между входами |
| Retreat Speed Multiplier | 1.2 | Множитель AttackSpeed |
| Safe Distance | 6 | Дистанция между поверхностями коллайдеров |
| Safety Reach Tolerance | 0.02 | Допуск прибытия, защищает от погрешности физики |
| Survival Time | 5 s | Полный период без урона перед лечением |
| Regen Duration | 6 s | Время линейного лечения к Recover Target Percent |
| Recover Target Percent | 60 | Целевой процент HP |
| Resume Attack Hp Percent | 50 | Выход в бой при HP **выше** порога |
| Counter Attack Distance | 1.2 | Дистанция запроса контратаки |
| Counter Attack Cooldown | 2.5 s | Пауза между попытками контратаки |
| Counter Attack Window | 0.45 s | Максимальное время одной попытки |
| Corner Check Time | 1 s | Допустимое время непрерывной близости/застревания |
| Obstacle Layers / Avoid Ledges | all / true | Проверки препятствий и пола |
| EnemyAggro / Alert Radius | 7 | Радиус одной рассылки |
| Enemy Layers / Enemy Tag | all / empty | Слои союзников и дополнительный фильтр по тегу |
| Alert Hold Time | 4 s | Память о полученной цели вне прямой видимости |
| Alert Cooldown | 2 s | Защита от частых рассылок |
| Damage Aggro Enabled | true | Захват игрока-атакующего при уроне без цели |
| Alert On Retreat | true | Попытка рассылки при входе в отступление |

Для осторожного врага: **Retreat Hp Percent = 90**, **Resume Attack Hp Percent = 95**,
**Recover Target Percent = 100**. Порог возврата и цель лечения независимы:
если текущий HP уже выше Resume Attack Hp Percent, восстановление сразу закончится.
Для нескольких циклов можно увеличить Max Retreats или поставить -1.
Отключение/включение врага сбрасывает счётчик; для пула восстанавливай здоровье через
`Health.ResetHealth()` отдельно. Автоматического лечения старых врагов при OnEnable нет.

В проекте есть **слой Enemy**, но нет тега Enemy. Поэтому Enemy Tag по умолчанию
пустой; союзниками считаются объекты подходящих слоёв с EnemySensor и включённым
EnemyAggro. Можно сузить Enemy Layers до Enemy и назначать этот слой новым префабам.
Если в проекте появится нужный тег, его можно указать в Enemy Tag.
Приём alert не рассылает его дальше. Собственное обнаружение игрока новым врагом
может вызвать его собственную рассылку — это прямое обнаружение, а не цепочка alert.
Во время отступления/восстановления входящие alert игнорируются.

## Переменные и подключение нового типа врага

Контракты субграфов и настройки находятся во внешних `.asset` Blackboard.
Существующие переменные и их GUID сохранены.

Добавлено в EnemyBlackboard:
`RetreatStage` (RetreatStage), `RetreatSignal` (Int), `RetreatSurvivalTime` (Float),
`RetreatCounterWindow` (Float), `RetreatTarget` (GameObject).
Последние четыре — служебные значения; EnemyRetreat обновляет их только при изменении.
В AttackBlackboard добавлен `AttackOnce` (Bool, false).

Внешние контракты:

- AggroBlackboard: State, AttackTarget, AttackTargetContinue.
- RetreatBlackboard: State, Attack, AttackSpeed, AttackDistance, AttackInterval,
  RetreatStage, RetreatSignal, RetreatSurvivalTime, RetreatCounterWindow, RetreatTarget.

В Base всё уже подключено. Для нового графа:

1. Добавь EnemySensor, Health, Rigidbody2D, Collider2D и опциональные EnemyRetreat /
   EnemyAggro на корень врага с BehaviorGraphAgent. Настрой слои сенсора и препятствий.
   Назначь Boundary1 / Boundary2 на отдельные точки сцены в параметрах агента.
2. Добавь указанные переменные во внешний Blackboard и ветку Switch `State → Retreat`.
3. Перед Switch поставь Branch On `Retreat check CanStart`: True → Set State Retreat;
   False → Abort If `Retreat check CanStart` → прежний Switch. Всё под Repeat Forever.
4. В ветке Retreat используется динамический селектор **Retreat**, по умолчанию
   `Retreat_Recover`. В Inspector вызова обязательно назначь **RetreatBlackboard**
   и свяжи **все 10** одноимённых полей контракта с переменными родителя.
   Self динамический вызов подставляет автоматически.
5. Входы в бой из Patrol/Idle используют динамический селектор **Aggro**, по умолчанию
   `Aggro_Engage`. На обоих вызовах назначь **AggroBlackboard** и свяжи State,
   AttackTarget и AttackTargetContinue. Одного выбора графа в переменной недостаточно:
   без Required Blackboard параметры и изменения State не передаются родителю.
   AttackTarget обязательно GameObject. Своему динамическому графу атаки назначь
   AttackBlackboard; поддержи AttackOnce для однократной контратаки.
6. Задай индивидуальные значения на компоненте EnemyRetreat префаба или экземпляра.
   Gizmos выбранного врага показывают safe/counter дистанции и радиус агро.

## Изменённые файлы

Существующие: Health.cs (события урона/HP, Heal/ResetHealth), EnemySensor.cs
(alert-память, опциональные модули), EnemyState.cs (Retreat = 3), AttackPlayerAction.cs
(AttackOnce), PrototypePlayer2D.cs (прямой урон через Health и Collider2D вместо
пересылки сообщений, без изменения управления), Base_Behaviour.asset,
EnemyBlackboard.asset, AttackBlackboard.asset, Attack_Melee.asset, README_Prototype.txt.
Attack_AfterTargetLost.asset автоматически перекомпилирован Unity с обновлённым
контрактом атаки; его исходные ноды и логика продолжения погони сохранены.

Добавлены: EnemyRetreat.cs, RetreatStage.cs, EnemyAggro.cs,
RetreatCheckCondition.cs, BeginRetreatAction.cs, EndRetreatAction.cs,
RetreatCleanupAction.cs, MoveToRetreatSafetyAction.cs, RecoverRetreatHealthAction.cs,
BeginCounterAttackAction.cs, EndCounterAttackAction.cs,
Aggro_Engage.asset / AggroBlackboard.asset, Retreat_Recover.asset /
RetreatBlackboard.asset, PrototypeEnemy_Retreat.prefab, RetreatAggro_Test.unity,
этот документ. `.meta` создаёт Unity.

Defender, исходный PrototypeEnemy.prefab, исходная Test_Scene и Project Settings
не требуют изменений. Атака врага теперь снимает HP через Collider2D и Health; получение урона
игроком обрабатывается общим Health.

## Проверка

Система проверена в Unity Play Mode на временных объектах: прежний цикл
Patrol → Attack → продолжение погони → Idle → Patrol, отступление и контратака,
препятствие, сброс ожидания от урона, прерывание восстановления от урона/близости,
порог 90%, стандартные 25/50/60% и 5 секунд ожидания, постепенное лечение,
лимит и -1, сброс счётчика при включении, cleanup при прерывании графа,
уничтожение цели, одношаговое агро/время памяти/cooldown и реальный удар PrototypePlayer2D.
Повторяемая проверка боя: **Tools → Verify Collider Combat**. Она открывает демосцену,
временно меняет настройки в Play Mode и проверяет урон по составным коллайдерам,
агро от удара и оповещение союзника, отход, постепенное лечение и удар врага.
После проверки Play Mode останавливается; результат записывается в
`Library/ColliderCombatVerification.txt`. Перед запуском сохрани изменения сцены.
Пакетный запуск в отдельной копии проекта:
`Unity.exe -batchmode -nographics -projectPath <copy> -executeMethod ColliderCombatVerification.Run -logFile <log>`.
Не добавляй `-quit`: редактор завершится сам после проверки с кодом 0 или 1.
Графы автоматически не пересоздаются.

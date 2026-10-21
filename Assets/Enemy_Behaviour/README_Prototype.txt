Enemy_Behaviour — вертикальный срез прототипа боя с врагами

Запуск: открой Demo/Scenes/Test_Scene.unity и нажми Play.
A/D или стрелки — движение; Space — прыжок; J или ЛКМ — удар.
Голубой — игрок, красный — враг, жёлтый — область удара.

Структура организована по функциональности:

Player/
  Runtime/ — PrototypePlayer2D: движение, прыжок и удар.
  Prefabs/ — префаб игрока.

Enemies/Base/
  Behavior/ — основной Base_Behaviour с enum Patrol / Attack / Idle.
  Prefabs/ — префаб базового врага.

Enemies/Defender/
  Runtime/ — EnemyAllySensor для поиска союзника.
  Behavior/ — Defender_Behaviour.
  Prefabs/ — префаб защитника.

Enemies/Common/
  Runtime/ — общие Health, EnemyState, EnemySensor и EnemyGraphLifecycle.
  Behavior/Actions/ — ноды движения, атаки и остановки.
  Behavior/Conditions/ — условия наличия и отсутствия цели.
  Behavior/Blackboards/ — EnemyBlackboard, AttackBlackboard, LostTargetBlackboard.
  Behavior/Subgraphs/ — Attack_Melee и Attack_AfterTargetLost.

Demo/Scenes/ — тестовая сцена целиком.
Shared/Art/ — общие материалы и спрайты прототипа.

Настройки:
- Игрок: компонент PrototypePlayer2D на игроке.
- Радиус обнаружения: компонент EnemySensor на враге.
- Пауза на точках: EnemyBlackboard.PatrolPause.
- Продолжение погони после потери игрока: EnemyBlackboard.ContinueAttackAfterLostPlayer.
- Пауза после погони: EnemyBlackboard.IdleDuration.
- Тип атаки: переменная Attack; параметры AttackSpeed, AttackDistance, AttackInterval.
- В Run Subgraph для динамического вызова выбирай соответствующий Blackboard и связывай его параметры с EnemyBlackboard.

Удар игрока передаёт OnPrototypeHit(float damage) и отталкивает динамические Rigidbody2D.
Health принимает урон; атака врага пока выводит attacked player в консоль.

Новый тип врага добавляй рядом с Base и Defender, со своими Behavior, Prefabs и, при необходимости, Runtime.
Общую логику нескольких типов держи в Enemies/Common. Классы, namespaces и GUID при этой реорганизации сохранены.

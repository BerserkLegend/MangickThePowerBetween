Enemy_Behaviour — вертикальный срез прототипа боя с врагами

Запуск: открой Demo/Scenes/Test_Scene.unity и нажми Play.
Пример отступления и агро: Demo/Scenes/RetreatAggro_Test.unity.
Настройки и подключение: Enemies/Common/Retreat/README_RetreatAggro.md.
A/D или стрелки — движение; Space — прыжок; J или ЛКМ — удар.
Голубой — игрок, красный — враг, жёлтый — область удара.

Структура организована по функциональности:

Player/
  Runtime/ — PrototypePlayer2D: движение, прыжок и удар.
  Prefabs/ — префаб игрока.

Enemies/Base/
  Behavior/ — основной Base_Behaviour с enum Patrol / Attack / Idle / Retreat.
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
  Retreat/ — настройки, отдельные ноды, общий Retreat_Recover и внешний Blackboard.
  Aggro/ — рассылка агро, общий Aggro_Engage и внешний Blackboard.

Demo/Scenes/ — тестовая сцена целиком.
Shared/Art/ — общие материалы и спрайты прототипа.
Demo/Editor/ — пакетная проверка ColliderCombatVerification.Run.

Настройки:
- Игрок: компонент PrototypePlayer2D на игроке.
- Радиус обнаружения: компонент EnemySensor на враге.
- Пауза на точках: EnemyBlackboard.PatrolPause.
- Продолжение погони после потери игрока: EnemyBlackboard.ContinueAttackAfterLostPlayer.
- Пауза после погони: EnemyBlackboard.IdleDuration.
- Тип атаки: переменная Attack; параметры AttackSpeed, AttackDistance, AttackInterval.
- В Run Subgraph для динамического вызова выбирай соответствующий Blackboard и связывай его параметры с EnemyBlackboard.

Удар игрока ищет Health через Collider2D в Physics2D.OverlapBoxAll и вызывает TakeDamage(1f, attacker) напрямую; составные коллайдеры получают один удар. Динамические Rigidbody2D отталкиваются.
Атака врага проверяет расстояние между Collider2D и снимает HP игрока. Health сохранён на префабе PrototypePlayer и настраивается в Inspector до запуска игры. События Health уведомляют агро и отступление об уже полученном уроне.

Новый тип врага добавляй рядом с Base и Defender, со своими Behavior, Prefabs и, при необходимости, Runtime.
Общую логику нескольких типов держи в Enemies/Common. Классы, namespaces и GUID при этой реорганизации сохранены.

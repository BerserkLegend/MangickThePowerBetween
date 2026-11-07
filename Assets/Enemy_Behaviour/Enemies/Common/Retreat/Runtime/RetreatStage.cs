using Unity.Behavior;

namespace EnemyBehaviourPrototype
{
    [BlackboardEnum]
    public enum RetreatStage { Moving = 0, Surviving = 1, Recovering = 2 }

    [BlackboardEnum]
    public enum RetreatCheck { CanStart = 0, Active = 1, ShouldEnd = 2, CounterNeeded = 3, NeedsMoreSpace = 4 }
}

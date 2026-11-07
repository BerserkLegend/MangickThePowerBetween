using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(name: "Retreat Check", story: "Retreat check [Check]", category: "Enemy/Retreat", id: "c2acaf31e49747e9a530cd1be5fd5301")]
public partial class RetreatCheckCondition : Condition
{
    [SerializeReference] public BlackboardVariable<RetreatCheck> Check;
    private EnemyRetreat retreat;
    public override void OnStart() { retreat = GameObject != null ? GameObject.GetComponent<EnemyRetreat>() : null; }
    public override bool IsTrue()
    {
        if (retreat == null || Check == null) return false;
        switch (Check.Value)
        {
            case RetreatCheck.CanStart: return retreat.CanBegin;
            case RetreatCheck.Active: return retreat.IsRetreating;
            case RetreatCheck.ShouldEnd: return retreat.ShouldEnd;
            case RetreatCheck.CounterNeeded: return retreat.NeedsCounter;
            case RetreatCheck.NeedsMoreSpace: return retreat.NeedsMoreSpace;
            default: return false;
        }
    }
}

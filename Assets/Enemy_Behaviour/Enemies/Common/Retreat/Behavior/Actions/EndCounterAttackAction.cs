using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "End Retreat Counter", story: "Finish counterattack and continue retreat", category: "Enemy/Retreat", id: "ba1efb7c5d76498792dce650ec6da107")]
public partial class EndCounterAttackAction : Unity.Behavior.Action
{
    protected override Status OnStart()
    {
        var retreat = GameObject.GetComponent<EnemyRetreat>(); if (retreat != null) retreat.EndCounter();
        return Status.Success;
    }
}

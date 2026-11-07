using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Begin Retreat Counter", story: "Begin one cooldown-limited counterattack", category: "Enemy/Retreat", id: "ba1efb7c5d76498792dce650ec6da106")]
public partial class BeginCounterAttackAction : Unity.Behavior.Action
{
    protected override Status OnStart()
    {
        var retreat = GameObject.GetComponent<EnemyRetreat>(); if (retreat != null) retreat.BeginCounter();
        return Status.Success;
    }
}

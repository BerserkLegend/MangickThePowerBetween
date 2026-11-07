using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Begin Retreat", story: "Begin configured retreat", category: "Enemy/Retreat", id: "ba1efb7c5d76498792dce650ec6da101")]
public partial class BeginRetreatAction : Unity.Behavior.Action
{
    protected override Status OnStart()
    {
        var retreat = GameObject.GetComponent<EnemyRetreat>();
        if (retreat != null) retreat.BeginRetreat();
        // An invalid/disabled entry becomes an empty loop and exits through the graph's cleanup.
        return Status.Success;
    }
}

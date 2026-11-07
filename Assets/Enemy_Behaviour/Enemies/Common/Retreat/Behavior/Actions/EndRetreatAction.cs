using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "End Retreat", story: "End retreat and stop", category: "Enemy/Retreat", id: "ba1efb7c5d76498792dce650ec6da102")]
public partial class EndRetreatAction : Unity.Behavior.Action
{
    protected override Status OnStart()
    {
        var retreat = GameObject.GetComponent<EnemyRetreat>();
        if (retreat != null) retreat.EndRetreat();
        return Status.Success;
    }
}

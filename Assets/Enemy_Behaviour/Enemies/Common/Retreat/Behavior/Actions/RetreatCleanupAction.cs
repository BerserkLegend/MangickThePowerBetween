using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Retreat Cleanup Scope", story: "Clean up retreat on interruption", category: "Enemy/Retreat", id: "ba1efb7c5d76498792dce650ec6da103")]
public partial class RetreatCleanupAction : Unity.Behavior.Action
{
    private EnemyRetreat retreat;
    protected override Status OnStart() { retreat = GameObject.GetComponent<EnemyRetreat>(); return Status.Running; }
    protected override Status OnUpdate() => Status.Running;
    protected override void OnEnd() { if (retreat != null) retreat.EndRetreat(); }
}

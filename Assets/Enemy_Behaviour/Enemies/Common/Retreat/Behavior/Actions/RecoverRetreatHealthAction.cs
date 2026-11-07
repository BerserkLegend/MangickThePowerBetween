using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Recover Retreat Health", story: "Regenerate health progressively", category: "Enemy/Retreat", id: "ba1efb7c5d76498792dce650ec6da105")]
public partial class RecoverRetreatHealthAction : Unity.Behavior.Action
{
    private EnemyRetreat retreat;
    private float started, startingHealth, targetHealth, duration;
    protected override Status OnStart()
    {
        retreat = GameObject.GetComponent<EnemyRetreat>();
        if (retreat == null || retreat.Health == null) return Status.Success;
        retreat.Stop(); retreat.BeginRecovery(); started = Time.time; startingHealth = retreat.Health.CurrentHealth;
        targetHealth = Mathf.Max(startingHealth, retreat.RecoveryTargetHealth); duration = retreat.RegenDuration;
        return Tick();
    }
    protected override Status OnUpdate() => Tick();
    private Status Tick()
    {
        if (retreat == null || !retreat.IsRetreating || retreat.Health == null || retreat.Health.IsDead || retreat.ShouldEnd) return Status.Success;
        float elapsed = Mathf.Clamp01((Time.time - started) / duration);
        float desired = Mathf.Lerp(startingHealth, targetHealth, elapsed);
        retreat.Health.Heal(Mathf.Max(0f, desired - retreat.Health.CurrentHealth));
        return elapsed >= 1f || retreat.Health.Fraction > retreat.ResumeFraction ? Status.Success : Status.Running;
    }
    protected override void OnEnd() { if (retreat != null) { retreat.EndRecovery(); retreat.Stop(); } }
}

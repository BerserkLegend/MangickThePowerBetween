using System;
using EnemyBehaviourPrototype;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move To Retreat Safety", story: "Move away to configured safety distance", category: "Enemy/Retreat", id: "ba1efb7c5d76498792dce650ec6da104")]
public partial class MoveToRetreatSafetyAction : Unity.Behavior.Action
{
    private EnemyRetreat retreat;
    private float progressStarted, progressOrigin;
    protected override Status OnStart()
    {
        retreat = GameObject.GetComponent<EnemyRetreat>();
        progressStarted = Time.time; progressOrigin = retreat != null && retreat.Body != null ? retreat.Body.position.x : 0f;
        return Tick();
    }
    protected override Status OnUpdate() => Tick();
    private Status Tick()
    {
        if (retreat == null || !retreat.IsRetreating || retreat.Target == null || retreat.Body == null) return Status.Failure;
        if (retreat.AtSafety) { retreat.Stop(); return Status.Success; }
        float direction = Mathf.Sign(retreat.Body.position.x - retreat.Target.transform.position.x);
        if (direction == 0f) direction = -Mathf.Sign(GameObject.transform.localScale.x);
        float speed = Mathf.Min(retreat.MoveSpeed, Mathf.Max(0f, retreat.SafeDistance - retreat.DistanceToThreat) / Time.fixedDeltaTime);
        if (retreat.EscapeRouteBlocked(new Vector2(direction, 0f), speed))
        { retreat.ReportBlocked(); retreat.Stop(); return Status.Failure; }
        if (Time.time - progressStarted >= retreat.CornerCheckTime)
        {
            if (speed > 0f && Mathf.Abs(retreat.Body.position.x - progressOrigin) < retreat.MinimumRetreatProgress)
            { retreat.ReportBlocked(); retreat.Stop(); return Status.Failure; }
            progressStarted = Time.time; progressOrigin = retreat.Body.position.x;
        }
        retreat.Body.linearVelocity = new Vector2(direction * speed, retreat.Body.linearVelocity.y);
        Vector3 scale = GameObject.transform.localScale; scale.x = Mathf.Abs(scale.x) * direction; GameObject.transform.localScale = scale;
        return Status.Running;
    }
    protected override void OnEnd() { if (retreat != null) retreat.Stop(); }
}

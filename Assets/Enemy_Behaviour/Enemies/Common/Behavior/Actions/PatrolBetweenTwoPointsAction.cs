using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PatrolBetweenTwoPoints", story: "Walk between [Point1] and [Point2]", category: "Action", id: "9f33dd8ecb3d27aa7751d12f0c1164d5")]
public partial class PatrolBetweenTwoPointsAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Point1;
    [SerializeReference] public BlackboardVariable<Transform> Point2;
    private Transform target;
    private const float reachPointOffsetDistance = 0.2f;
    private const float speed = 4f;
    private Rigidbody2D rb;
    private Transform selfTransform;
    protected override void OnSetup()
    {
        rb = GameObject.GetComponent<Rigidbody2D>();
        selfTransform = GameObject.transform;
    }

    protected override Status OnStart()
    {
        if (rb == null || Point1?.Value == null || Point2?.Value == null) return Status.Failure;

        // Keep the destination after interruption; advance it only on arrival.
        if (target == null) target = Point1.Value;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (rb == null || Point1?.Value == null || Point2?.Value == null || target == null) return Status.Failure;

        float offset = target.position.x - selfTransform.position.x;
        if (Mathf.Abs(offset) <= reachPointOffsetDistance)
        {
            Stop();
            target = target == Point1.Value ? Point2.Value : Point1.Value;
            return Status.Success;
        }

        float direction = Mathf.Sign(offset);
        rb.linearVelocity = new Vector2(direction * Mathf.Min(speed, Mathf.Abs(offset) / Time.fixedDeltaTime), rb.linearVelocity.y);
        
        Vector3 scale = selfTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        selfTransform.localScale = scale;
        return Status.Running;
    }

    private void Stop()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    protected override void OnEnd()
    {
        Stop();
    }
}



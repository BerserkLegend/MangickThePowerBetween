using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PatrolBetweenTwoPoints", story: "Patrol [Point1] / [Point2]", category: "Action", id: "9f33dd8ecb3d27aa7751d12f0c1164d5")]
public partial class PatrolBetweenTwoPointsAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Point1;
    [SerializeReference] public BlackboardVariable<Transform> Point2;
    [SerializeReference] public BlackboardVariable<float> DelayTime;


    private Transform currentPoint;
    private const float reachPointOffsetDistance = 0.2f;
    private const float speed = 4f;
    private bool reachedPoint;
    private bool isWaitingBeforeSelectPoint;
    private float currentTimerOfDelay;
    private Rigidbody2D rb;
    private Transform selfTransform;

    protected override Status OnStart()
    {
        rb = GameObject.GetComponent<Rigidbody2D>();
        selfTransform = GameObject.transform;
        if (rb == null || Point1?.Value == null || Point2?.Value == null) return Status.Failure;

        if (currentPoint == null) currentPoint = Point1.Value;
        // Chase interruptions resume the same waypoint. Only arrival starts the pause.
        isWaitingBeforeSelectPoint = reachedPoint;
        currentTimerOfDelay = Mathf.Max(0f, DelayTime?.Value ?? 0f);
        reachedPoint = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {

        if (currentPoint == null) return Status.Failure;
        if (isWaitingBeforeSelectPoint)
        {
            currentTimerOfDelay -= Time.deltaTime;
            if (currentTimerOfDelay > 0f) return Status.Running;
            isWaitingBeforeSelectPoint = false;
            currentPoint = currentPoint == Point2.Value ? Point1.Value : Point2.Value;
        }

        float offset = currentPoint.position.x - selfTransform.position.x;
        if (Mathf.Abs(offset) <= reachPointOffsetDistance)
        {
            reachedPoint = true;
            Stop();
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
        // Preserve a pause interrupted by detection so patrol changes waypoint on return.
        if (isWaitingBeforeSelectPoint) reachedPoint = true; //reachedPoint |= isWaitingBeforeSelectPoint;
        isWaitingBeforeSelectPoint = false;
    }
}



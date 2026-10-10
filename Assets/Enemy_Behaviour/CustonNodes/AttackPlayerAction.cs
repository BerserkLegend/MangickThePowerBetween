using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AttackPlayer", story: "Attack [AttackTarget] within [StopDistance]", category: "Action", id: "ea67bfda46ae42c5b6b321ce76c1b9a8")]
public partial class AttackPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AttackTarget;
    [SerializeReference] public BlackboardVariable<float> Speed = new BlackboardVariable<float>(4f);
    [SerializeReference] public BlackboardVariable<float> StopDistance = new BlackboardVariable<float>(0.5f);
    [SerializeReference] public BlackboardVariable<float> AttackInterval = new BlackboardVariable<float>(1f);

    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private float nextAttack;

    protected override Status OnStart()
    {
        rb = GameObject.GetComponent<Rigidbody2D>();
        ownCollider = GameObject.GetComponent<Collider2D>();
        nextAttack = Time.time;
        return TickAttack();
    }

    protected override Status OnUpdate() => TickAttack();

    private Status TickAttack()
    {
        GameObject target = AttackTarget?.Value;
        if (target == null)
        {
            Stop();
            return Status.Success;
        }
        if (rb == null) return Status.Failure;

        Collider2D targetCollider = target.GetComponent<Collider2D>();
        if (targetCollider == null) targetCollider = target.GetComponentInChildren<Collider2D>();
        float distance;
        float horizontalGap;
        float direction = Mathf.Sign(target.transform.position.x - rb.position.x);
        if (ownCollider != null && targetCollider != null)
        {
            distance = Mathf.Max(0f, ownCollider.Distance(targetCollider).distance);
            horizontalGap = Mathf.Max(0f, direction > 0f
                ? targetCollider.bounds.min.x - ownCollider.bounds.max.x
                : ownCollider.bounds.min.x - targetCollider.bounds.max.x);
        }
        else
        {
            distance = Vector2.Distance(rb.position, target.transform.position);
            horizontalGap = Mathf.Abs(target.transform.position.x - rb.position.x);
        }

        float stopDistance = Mathf.Max(0f, StopDistance.Value);
        if (distance <= stopDistance + 0.05f)
        {
            Stop();
            if (Time.time >= nextAttack)
            {
                Debug.Log("attacked player", GameObject);
                nextAttack = Time.time + Mathf.Max(0.1f, AttackInterval.Value);
            }
        }
        else
        {
            float speed = Mathf.Min(Mathf.Max(0f, Speed.Value),
                Mathf.Max(0f, horizontalGap - stopDistance) / Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        }

        if (Mathf.Abs(target.transform.position.x - rb.position.x) > 0.01f)
        {
            Vector3 scale = GameObject.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            GameObject.transform.localScale = scale;
        }
        return Status.Running;
    }

    private void Stop()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    protected override void OnEnd() => Stop();
}


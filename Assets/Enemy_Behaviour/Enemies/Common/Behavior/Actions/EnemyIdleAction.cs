using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Stop Enemy Movement", story: "Stop horizontal movement", category: "Action/Enemy", id: "7f54d0fea34c4e388b87d5ffcf50c03e")]
public partial class EnemyIdleAction : Action
{
    private Rigidbody2D body;
    protected override Status OnStart()
    {
        body = GameObject.GetComponent<Rigidbody2D>();
        if (body == null) return Status.Failure;
        Stop();
        return Status.Success;
    }
    private void Stop()
    {
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }
    protected override void OnEnd() => Stop();
}

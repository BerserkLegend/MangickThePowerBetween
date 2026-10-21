using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(name: "Has No Attack Target", story: "[AttackTarget] does not exist", category: "Enemy", id: "68091e94137947e1b775c6e7cf71b581")]
public partial class HasNotAttackTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> AttackTarget;

    // Unity's overloaded null check also handles destroyed objects and empty native wrappers.
    public override bool IsTrue() => AttackTarget != null && AttackTarget.Value == null;
}

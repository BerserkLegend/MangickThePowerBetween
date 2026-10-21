using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(name: "Has Attack Target", story: "[AttackTarget] exists", category: "Enemy", id: "4fb612cbd1d24a7cb3751d4a820aedb6")]
public partial class HasAttackTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> AttackTarget;

    // Unity's overloaded null check also handles destroyed objects and empty native wrappers.
    public override bool IsTrue() => AttackTarget != null && AttackTarget.Value != null;
}

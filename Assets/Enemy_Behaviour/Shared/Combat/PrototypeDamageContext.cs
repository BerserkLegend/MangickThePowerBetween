using System;
using UnityEngine;

namespace EnemyBehaviourPrototype
{
    // SendMessage is synchronous. Preserve the existing float hook (including other
    // receivers) while carrying its source through a stack-safe, allocation-free scope.
    internal static class PrototypeDamageContext
    {
        internal static GameObject Attacker { get; private set; }

        internal readonly struct Scope : IDisposable
        {
            private readonly GameObject previous;
            internal Scope(GameObject source) { previous = Attacker; Attacker = source; }
            public void Dispose() { Attacker = previous; }
        }
    }
}

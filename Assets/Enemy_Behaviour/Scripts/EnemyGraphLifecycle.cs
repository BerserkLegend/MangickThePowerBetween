using Unity.Behavior;
using UnityEngine;

namespace EnemyBehaviourPrototype
{
    // Recreate the graph instance when Unity's Domain/Scene Reload options are disabled.
    // This component does not read or change State; the Behaviour Graph owns transitions.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BehaviorGraphAgent))]
    public sealed class EnemyGraphLifecycle : MonoBehaviour
    {
        [SerializeField] private BehaviorGraph graphTemplate;
        public BehaviorGraph GraphTemplate { get => graphTemplate; set => graphTemplate = value; }

        private void OnEnable()
        {
            if (!Application.isPlaying || graphTemplate == null) return;
            BehaviorGraphAgent agent = GetComponent<BehaviorGraphAgent>();
            agent.Graph = graphTemplate;
            agent.Restart();
        }
    }
}

using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

namespace EnemyBehaviourPrototype
{
    [DisallowMultipleComponent]
    public sealed class EnemySensor : MonoBehaviour
    {
        [SerializeField] private BehaviorGraphAgent graphAgent;
        [SerializeField, Min(0.1f)] private float radius = 3f;
        [SerializeField] private LayerMask playerLayers = ~0;
        [SerializeField] private string targetVariable = "AttackTarget";


        [SerializeField] private float checkInterval = 0.2f;
        private float nextCheck;
        public GameObject AttackTarget { get; private set; }

        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        private bool warnedAboutSetup;

        private void Awake()
        {
            if (graphAgent == null) graphAgent = GetComponent<BehaviorGraphAgent>();
        }

        private void OnEnable()
        {
            nextCheck = 0f;
            warnedAboutSetup = false;
            AttackTarget = null;
        }

        private void Update()
        {
            if (graphAgent == null || graphAgent.Graph == null)
            {
                WarnAboutSetup("Assign a BehaviorGraphAgent with a graph to EnemySensor.");
                return;
            }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + checkInterval;
            
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(playerLayers);
            filter.useTriggers = true;
            overlaps.Clear();
            Physics2D.OverlapCircle(transform.position, radius, filter, overlaps);

            GameObject nearestPlayer = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (Collider2D overlap in overlaps)
            {
                Transform candidate = overlap.transform;
                // Child colliders also work when the Player tag is on a parent.
                while (candidate != null && !candidate.CompareTag("Player"))
                    candidate = candidate.parent;
                if (candidate == null || candidate == transform || candidate.IsChildOf(transform)) continue;

                float distance = ((Vector2)candidate.position - (Vector2)transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestPlayer = candidate.gameObject;
            }
            // Use the agent API: it updates this enemy's graph instance, not the shared asset.
            if (nearestPlayer != AttackTarget)
            {
                AttackTarget = nearestPlayer;
                if (!graphAgent.SetVariableValue<GameObject>(targetVariable, AttackTarget))
                    WarnAboutSetup("Blackboard must contain a GameObject variable named '" + targetVariable + "'.");
            }
        }

        private void OnDisable()
        {
            AttackTarget = null;
            if (graphAgent != null && graphAgent.Graph != null)
                graphAgent.SetVariableValue<GameObject>(targetVariable, null);
        }

        private void WarnAboutSetup(string message)
        {
            if (warnedAboutSetup) return;
            warnedAboutSetup = true;
            Debug.LogWarning(message, this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = AttackTarget != null ? Color.red : Color.yellow;
            Vector3 center = transform.position;
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            const int segments = 64;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}


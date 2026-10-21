using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

namespace EnemyBehaviourPrototype
{
    /// <summary>
    /// Finds the nearest ally (by tag and layer) and writes it to a GameObject
    /// variable on this enemy's Behavior Graph. Put it only on enemies that need it (e.g. the defender).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAllySensor : MonoBehaviour
    {
        [SerializeField] private BehaviorGraphAgent graphAgent;
        [SerializeField, Min(0.1f)] private float radius = 5f;
        [SerializeField] private LayerMask allyLayers = ~0;
        [SerializeField] private string allyTag = "Enemy";
        [SerializeField] private string allyVariable = "Ally";
        [SerializeField, Min(0.05f)] private float checkInterval = 0.2f;

        private float nextCheck;
        public GameObject Ally { get; private set; }

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
            Ally = null;
        }

        private void Update()
        {
            if (graphAgent == null || graphAgent.Graph == null)
            {
                WarnAboutSetup("Assign a BehaviorGraphAgent with a graph to AllySensor.");
                return;
            }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + checkInterval;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(allyLayers);
            filter.useTriggers = true;
            overlaps.Clear();
            Physics2D.OverlapCircle(transform.position, radius, filter, overlaps);

            GameObject nearestAlly = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (Collider2D overlap in overlaps)
            {
                // Child colliders also work when the tag is on a parent.
                Transform candidate = overlap.transform;
                while (candidate != null && !candidate.CompareTag(allyTag))
                    candidate = candidate.parent;
                if (candidate == null || candidate == transform || candidate.IsChildOf(transform)) continue;

                float distance = ((Vector2)candidate.position - (Vector2)transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestAlly = candidate.gameObject;
            }

            // Write to the graph only when the ally actually changed.
            // A destroyed ally compares equal to null, so it is cleared automatically.
            if (nearestAlly != Ally)
            {
                Ally = nearestAlly;
                if (!graphAgent.SetVariableValue<GameObject>(allyVariable, Ally))
                    WarnAboutSetup("Blackboard must contain a GameObject variable named '" + allyVariable + "'.");
            }
        }

        private void OnDisable()
        {
            Ally = null;
            if (graphAgent != null && graphAgent.Graph != null)
                graphAgent.SetVariableValue<GameObject>(allyVariable, null);
        }

        private void WarnAboutSetup(string message)
        {
            if (warnedAboutSetup) return;
            warnedAboutSetup = true;
            Debug.LogWarning(message, this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Ally != null ? Color.green : Color.cyan;
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
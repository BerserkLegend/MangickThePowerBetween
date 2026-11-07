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


        [SerializeField, Min(0.01f), Tooltip("Seconds between direct-vision scans.")] private float checkInterval = 0.2f;
        private float nextCheck;
        public GameObject AttackTarget { get; private set; }

        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        private bool warnedAboutSetup;
        private ContactFilter2D visionFilter;
        private EnemyAggro aggro;
        private EnemyRetreat retreat;
        private Health health;
        private float alertUntil;
        private BehaviorGraph publishedGraph;

        public bool HasDirectVision { get; private set; }
        public bool IgnoresAlerts => retreat != null && retreat.IsRetreating;

        private void Awake()
        {
            if (graphAgent == null) graphAgent = GetComponent<BehaviorGraphAgent>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            nextCheck = 0f;
            warnedAboutSetup = false;
            AttackTarget = null;
            alertUntil = 0f;
            HasDirectVision = false;
            publishedGraph = null;
            visionFilter = new ContactFilter2D { useTriggers = true };
            visionFilter.SetLayerMask(playerLayers);
            aggro = GetComponent<EnemyAggro>();
            retreat = GetComponent<EnemyRetreat>();
            if (health == null) health = GetComponent<Health>();
            if (health != null) health.OnDamaged += HandleDamage;
        }

        private void Update()
        {
            if (graphAgent == null || graphAgent.Graph == null)
            {
                WarnAboutSetup("Assign a BehaviorGraphAgent with a graph to EnemySensor.");
                return;
            }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + Mathf.Max(0.01f, checkInterval);
            overlaps.Clear();
            Physics2D.OverlapCircle(transform.position, radius, visionFilter, overlaps);

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
            HasDirectVision = nearestPlayer != null;
            // Retreat keeps its original threat, even after running beyond the vision circle.
            if (retreat != null && retreat.IsRetreating && IsValidTarget(retreat.Target))
                PublishTarget(retreat.Target, false);
            else if (nearestPlayer != null) PublishTarget(nearestPlayer, true);
            else if (IsValidTarget(AttackTarget) && Time.time < alertUntil) PublishTarget(AttackTarget, false);
            else PublishTarget(null, false);
        }

        private static bool IsValidTarget(GameObject target) => target != null && target.activeInHierarchy;

        private void PublishTarget(GameObject target, bool directObservation)
        {
            bool gainedTarget = AttackTarget == null && target != null;
            bool changed = !ReferenceEquals(target, AttackTarget);
            AttackTarget = target;
            if (graphAgent != null && graphAgent.Graph != null && (changed || publishedGraph != graphAgent.Graph))
            {
                publishedGraph = graphAgent.Graph;
                if (!graphAgent.SetVariableValue<GameObject>(targetVariable, target))
                    WarnAboutSetup("Blackboard must contain a GameObject variable named '" + targetVariable + "'.");
            }
            if (gainedTarget && directObservation && aggro != null) aggro.TryBroadcast(target);
        }

        public void ReceiveAlert(GameObject target)
        {
            if (!isActiveAndEnabled || (health != null && health.IsDead) || IgnoresAlerts || aggro == null || !aggro.AlertsEnabled
                || !IsValidTarget(target) || !target.CompareTag("Player")) return;
            alertUntil = Time.time + aggro.HoldTime;
            PublishTarget(target, false);
        }

        private void HandleDamage(float amount, GameObject attacker)
        {
            if (aggro == null || !aggro.DamageAggroEnabled || IgnoresAlerts || AttackTarget != null
                || !IsValidTarget(attacker) || !attacker.CompareTag("Player")) return;
            alertUntil = Time.time + aggro.HoldTime;
            PublishTarget(attacker, false);
            aggro.TryBroadcast(attacker);
        }

        internal void RegisterAggro(EnemyAggro value) { aggro = value; }
        internal void UnregisterAggro(EnemyAggro value) { if (aggro == value) { aggro = null; alertUntil = 0f; } }
        internal void RegisterRetreat(EnemyRetreat value) { retreat = value; }
        internal void UnregisterRetreat(EnemyRetreat value) { if (retreat == value) retreat = null; }
        internal void AlertOnRetreat(GameObject target) { if (aggro != null && aggro.AlertOnRetreat) aggro.TryBroadcast(target); }

        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            checkInterval = Mathf.Max(0.01f, checkInterval);
            visionFilter = new ContactFilter2D { useTriggers = true };
            visionFilter.SetLayerMask(playerLayers);
        }

        private void OnDisable()
        {
            if (health != null) health.OnDamaged -= HandleDamage;
            AttackTarget = null;
            HasDirectVision = false;
            alertUntil = 0f;
            publishedGraph = null;
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


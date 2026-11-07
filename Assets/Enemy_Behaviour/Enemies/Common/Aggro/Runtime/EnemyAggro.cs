using System.Collections.Generic;
using UnityEngine;

namespace EnemyBehaviourPrototype
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemySensor))]
    public sealed class EnemyAggro : MonoBehaviour
    {
        [SerializeField, Tooltip("Opt-in squad alerts. Removing/disabling this component keeps legacy sensing.")]
        private bool alertsEnabled = true;
        [SerializeField, Min(0f), Tooltip("Radius of the single neighbour scan when this enemy directly spots the player.")]
        private float alertRadius = 7f;
        [SerializeField, Tooltip("Layers containing enemy colliders. Receivers must have an enabled EnemyAggro and EnemySensor.")]
        private LayerMask enemyLayers = ~0;
        [SerializeField, Tooltip("Optional tag filter on the receiver. Leave empty to identify allies by layers and EnemySensor. This project has an Enemy layer, but no Enemy tag.")]
        private string enemyTag = "";
        [SerializeField, Min(0f), Tooltip("Seconds to remember a received alert without direct vision.")]
        private float alertHoldTime = 4f;
        [SerializeField, Min(0f), Tooltip("Minimum seconds between this enemy's broadcasts.")]
        private float alertCooldown = 2f;
        [SerializeField, Tooltip("Acquire a Player-tagged attacker immediately when damaged without a target.")]
        private bool damageAggroEnabled = true;
        [SerializeField, Tooltip("Attempt one cooldown-limited broadcast when retreat begins.")]
        private bool alertOnRetreat = true;

        private readonly List<Collider2D> neighbours = new List<Collider2D>(16);
        private readonly HashSet<EnemySensor> alerted = new HashSet<EnemySensor>();
        private ContactFilter2D filter;
        private EnemySensor sensor;
        private float nextAlert;

        public bool AlertsEnabled => isActiveAndEnabled && alertsEnabled;
        public bool DamageAggroEnabled => AlertsEnabled && damageAggroEnabled;
        public bool AlertOnRetreat => AlertsEnabled && alertOnRetreat;
        public float HoldTime => Mathf.Max(0f, alertHoldTime);
        public int BroadcastsSent { get; private set; }

        private void OnEnable()
        {
            sensor = GetComponent<EnemySensor>();
            filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(enemyLayers);
            nextAlert = 0f;
            BroadcastsSent = 0;
            sensor.RegisterAggro(this);
        }

        private void OnDisable() { if (sensor != null) sensor.UnregisterAggro(this); }

        private void OnValidate()
        {
            alertRadius = Mathf.Max(0f, alertRadius);
            alertHoldTime = Mathf.Max(0f, alertHoldTime);
            alertCooldown = Mathf.Max(0f, alertCooldown);
            filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(enemyLayers);
        }

        public bool TryBroadcast(GameObject target)
        {
            if (!AlertsEnabled || target == null || !target.activeInHierarchy || Time.time < nextAlert) return false;
            nextAlert = Time.time + alertCooldown;
            BroadcastsSent++;
            neighbours.Clear();
            alerted.Clear();
            Physics2D.OverlapCircle(transform.position, alertRadius, filter, neighbours);
            foreach (Collider2D neighbour in neighbours)
            {
                if (neighbour == null) continue;
                EnemySensor receiver = neighbour.GetComponentInParent<EnemySensor>();
                if (receiver == null || receiver == sensor || !receiver.isActiveAndEnabled
                    || receiver.gameObject == gameObject || (!string.IsNullOrEmpty(enemyTag) && receiver.tag != enemyTag) || !alerted.Add(receiver)) continue;
                // ReceiveAlert deliberately never broadcasts, so this is exactly one hop.
                receiver.ReceiveAlert(target);
            }
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, alertRadius);
        }
    }
}

using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

namespace EnemyBehaviourPrototype
{
    // Configuration, runtime budget and observations only. Graph nodes own the phase
    // transitions, movement, waiting, counterattack selection and return to Attack.
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(EnemySensor), typeof(Rigidbody2D))]
    public sealed class EnemyRetreat : MonoBehaviour
    {
        [Header("Entry and budget")]
        [SerializeField, Tooltip("Opt-in retreat. Existing enemies without this component keep their old behaviour.")]
        private bool retreatEnabled = true;
        [SerializeField, Range(0f, 100f), Tooltip("Start at or below this percentage of maximum HP. Enter 90 for 90%.")]
        private float retreatHpPercent = 25f;
        [SerializeField, Min(-1), Tooltip("Maximum entries per enable/respawn; -1 is unlimited, 0 disables retreat.")]
        private int maxRetreats = 3;
        [SerializeField, Min(0f), Tooltip("Minimum seconds between retreat entries. Cancelled retreats still count.")]
        private float retreatCooldown = 8f;
        [SerializeField, Tooltip("Runtime counter, reset when the enemy is enabled.")]
        private int retreatsUsed;

        [Header("Escape and survival")]
        [SerializeField, Min(0f), Tooltip("Multiplier applied to the graph's AttackSpeed while fleeing.")]
        private float retreatSpeedMultiplier = 1.2f;
        [SerializeField, Min(0f), Tooltip("Fallback speed if the graph has no AttackSpeed variable.")]
        private float fallbackMoveSpeed = 4f;
        [SerializeField, Min(0f), Tooltip("Required surface-to-surface distance from the threat before the survival wait.")]
        private float safeDistance = 6f;
        [SerializeField, Min(0f), Tooltip("Arrival tolerance in units. Prevents floating-point rounding from falsely treating a reached safe distance as a stuck enemy.")]
        private float safetyReachTolerance = 0.02f;
        [SerializeField, Min(0f), Tooltip("Unhurt seconds at safety before recovery. Damage restarts the graph's Wait.")]
        private float survivalTime = 5f;
        [SerializeField, Min(0f), Tooltip("Distance slack before a waiting enemy has to move away again.")]
        private float safetyHysteresis = 0.25f;

        [Header("Progressive recovery")]
        [SerializeField, Min(0.01f), Tooltip("Seconds for linear regeneration from current HP to Recover Target Percent.")]
        private float regenDuration = 6f;
        [SerializeField, Range(0f, 100f), Tooltip("Recovery destination in percent. For a 90% entry threshold, consider 100%.")]
        private float recoverTargetPercent = 60f;
        [SerializeField, Range(0f, 100f), Tooltip("Stop recovery and attack once HP is ABOVE this percentage. For a 90% entry threshold, consider 95%.")]
        private float resumeAttackHpPercent = 50f;

        [Header("Counterattack and corner detection")]
        [SerializeField, Min(0f), Tooltip("Surface distance at which a quick counterattack can be requested.")]
        private float counterAttackDistance = 1.2f;
        [SerializeField, Min(0f), Tooltip("Minimum seconds between counterattack attempts.")]
        private float counterAttackCooldown = 2.5f;
        [SerializeField, Min(0.01f), Tooltip("Maximum time allowed for one normal dynamic attack before escaping again.")]
        private float counterAttackWindow = 0.45f;
        [SerializeField, Min(0f), Tooltip("Cancel retreat if the player stays close or movement makes no progress for this long.")]
        private float cornerCheckTime = 1f;
        [SerializeField, Min(0f), Tooltip("Minimum horizontal progress during Corner Check Time.")]
        private float minimumRetreatProgress = 0.05f;

        [Header("Obstacle checks")]
        [SerializeField, Tooltip("Solid obstacle and ground layers; triggers and the threat are ignored.")]
        private LayerMask obstacleLayers = ~0;
        [SerializeField, Min(0.001f), Tooltip("Extra clearance for wall and ground probes.")]
        private float obstacleSkin = 0.05f;
        [SerializeField, Tooltip("Treat platform edges as blocked escape routes instead of walking off them.")]
        private bool avoidLedges = true;
        [SerializeField, Min(0.01f), Tooltip("Downward probe length at the next foot position when avoiding ledges.")]
        private float groundProbeDistance = 0.3f;

        private Health health;
        private EnemySensor sensor;
        private Rigidbody2D body;
        private Collider2D ownCollider;
        private Collider2D targetCollider;
        private BehaviorGraphAgent agent;
        private BehaviorGraph boundGraph;
        private BlackboardVariable<EnemyState> state;
        private BlackboardVariable<RetreatStage> stage;
        private BlackboardVariable<int> signalVariable;
        private BlackboardVariable<float> survivalVariable, counterWindowVariable, attackSpeed;
        private BlackboardVariable<GameObject> targetVariable;
        private readonly List<RaycastHit2D> obstacleHits = new List<RaycastHit2D>(8);
        private readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>(8);
        private ContactFilter2D obstacleFilter;
        private float nextRetreat, nextCounter, closeSince = -1f;
        private int damageVersion, counterDamageVersion, signal, previousMask;
        private bool active, counterActive, blocked, recoveryActive, recoveryInterrupted;

        public Health Health => health;
        public Rigidbody2D Body => body;
        public GameObject Target { get; private set; }
        public bool IsRetreating => active && isActiveAndEnabled;
        public bool IsRecovering => IsRetreating && Stage == RetreatStage.Recovering;
        public RetreatStage Stage => stage != null ? stage.Value : RetreatStage.Moving;
        public int RetreatsUsed => retreatsUsed;
        public int CounterAttacksPerformed { get; private set; }
        public bool CounterActive => counterActive;
        public float SafeDistance => safeDistance;
        public float SurvivalTime => survivalTime;
        public float RegenDuration => Mathf.Max(0.01f, regenDuration);
        public float RecoveryTargetHealth => health != null ? health.MaxHealth * recoverTargetPercent * 0.01f : 0f;
        public float ResumeFraction => resumeAttackHpPercent * 0.01f;
        public float CornerCheckTime => cornerCheckTime;
        public float MinimumRetreatProgress => minimumRetreatProgress;
        public float MoveSpeed => Mathf.Max(0f, attackSpeed != null ? attackSpeed.Value : fallbackMoveSpeed) * retreatSpeedMultiplier;
        public bool AtSafety => DistanceToThreat >= Mathf.Max(0f, safeDistance - safetyReachTolerance);
        public bool NeedsMoreSpace => IsRetreating && Stage == RetreatStage.Surviving
            && DistanceToThreat < Mathf.Max(0f, safeDistance - Mathf.Max(safetyHysteresis, safetyReachTolerance));
        public bool NeedsCounter => IsRetreating && (counterActive || (Time.time >= nextCounter
            && (DistanceToThreat <= counterAttackDistance || damageVersion != counterDamageVersion)));
        public bool ShouldEnd => IsRetreating && (!retreatEnabled || health == null || health.IsDead
            || Target == null || !Target.activeInHierarchy || blocked
            || (closeSince >= 0f && Time.time - closeSince >= cornerCheckTime)
            || (Stage == RetreatStage.Recovering && (recoveryInterrupted || DistanceToThreat <= counterAttackDistance
                || health.Fraction > ResumeFraction)));
        public bool CanBegin => isActiveAndEnabled && retreatEnabled && !active && health != null && !health.IsDead
            && health.Fraction <= retreatHpPercent * 0.01f && (maxRetreats == -1 || retreatsUsed < maxRetreats)
            && Time.time >= nextRetreat && sensor != null && sensor.AttackTarget != null
            && (state == null || state.Value != EnemyState.Retreat);

        public float DistanceToThreat
        {
            get
            {
                if (Target == null) return float.PositiveInfinity;
                if (ownCollider != null && targetCollider != null && ownCollider.enabled && targetCollider.enabled)
                    return Mathf.Max(0f, ownCollider.Distance(targetCollider).distance);
                return Vector2.Distance(transform.position, Target.transform.position);
            }
        }

        private void OnEnable()
        {
            health = GetComponent<Health>(); sensor = GetComponent<EnemySensor>(); body = GetComponent<Rigidbody2D>();
            ownCollider = GetComponent<Collider2D>(); agent = GetComponent<BehaviorGraphAgent>();
            obstacleFilter = new ContactFilter2D { useTriggers = false }; obstacleFilter.SetLayerMask(obstacleLayers);
            retreatsUsed = 0; nextRetreat = 0f; nextCounter = 0f; closeSince = -1f;
            active = counterActive = blocked = recoveryActive = recoveryInterrupted = false;
            damageVersion = counterDamageVersion = signal = previousMask = 0; CounterAttacksPerformed = 0;
            Target = null; targetCollider = null; boundGraph = null;
            health.OnDamaged += HandleDamage; sensor.RegisterRetreat(this);
        }

        private void OnDisable()
        {
            if (health != null) health.OnDamaged -= HandleDamage;
            EndRetreat();
            if (sensor != null) sensor.UnregisterRetreat(this);
            boundGraph = null;
        }

        private void Update()
        {
            TryBindGraph();
            if (survivalVariable != null && !Mathf.Approximately(survivalVariable.Value, survivalTime)) survivalVariable.Value = survivalTime;
            if (counterWindowVariable != null && !Mathf.Approximately(counterWindowVariable.Value, counterAttackWindow)) counterWindowVariable.Value = counterAttackWindow;
            if (!active) return;
            bool close = DistanceToThreat <= counterAttackDistance;
            if (close && closeSince < 0f) closeSince = Time.time;
            else if (!close) closeSince = -1f;
            int mask = (ShouldEnd ? 1 : 0) | (NeedsCounter ? 2 : 0) | (NeedsMoreSpace ? 4 : 0);
            if (mask != previousMask) { previousMask = mask; Interrupt(); }
        }

        private void TryBindGraph()
        {
            if (agent == null) return;
            var graph = agent.Graph;
            if (graph == null || graph == boundGraph || graph.BlackboardReference == null
                || !graph.BlackboardReference.GetVariable("Self", out BlackboardVariable<GameObject> self) || self.Value != gameObject) return;
            boundGraph = graph;
            agent.GetVariable("State", out state);
            agent.GetVariable("RetreatStage", out stage);
            agent.GetVariable("RetreatSignal", out signalVariable);
            agent.GetVariable("RetreatSurvivalTime", out survivalVariable);
            agent.GetVariable("RetreatCounterWindow", out counterWindowVariable);
            agent.GetVariable("RetreatTarget", out targetVariable);
            agent.GetVariable("AttackSpeed", out attackSpeed);
            if (signalVariable != null && signalVariable.Value != signal) signalVariable.Value = signal;
        }

        public bool BeginRetreat()
        {
            TryBindGraph();
            // State is already Retreat here: entry eligibility was checked by the main graph.
            if (active) return true;
            if (!isActiveAndEnabled || !retreatEnabled || health == null || health.IsDead || sensor.AttackTarget == null
                || health.Fraction > retreatHpPercent * 0.01f || Time.time < nextRetreat
                || (maxRetreats != -1 && retreatsUsed >= maxRetreats)) return false;
            Target = sensor.AttackTarget;
            targetCollider = Target.GetComponent<Collider2D>();
            if (targetCollider == null) targetCollider = Target.GetComponentInChildren<Collider2D>();
            active = true; counterActive = blocked = recoveryActive = recoveryInterrupted = false;
            retreatsUsed++; nextRetreat = Time.time + retreatCooldown;
            counterDamageVersion = damageVersion;
            closeSince = DistanceToThreat <= counterAttackDistance ? Time.time : -1f;
            if (targetVariable != null && targetVariable.Value != Target) targetVariable.Value = Target;
            previousMask = (ShouldEnd ? 1 : 0) | (NeedsCounter ? 2 : 0);
            sensor.AlertOnRetreat(Target);
            return true;
        }

        public void EndRetreat()
        {
            bool wasActive = active;
            active = counterActive = blocked = recoveryActive = recoveryInterrupted = false;
            closeSince = -1f; Target = null; targetCollider = null;
            if (targetVariable != null && targetVariable.Value != null) targetVariable.Value = null;
            Stop();
            if (wasActive) Interrupt();
        }

        public void BeginCounter()
        {
            counterActive = true; nextCounter = Time.time + counterAttackCooldown;
            counterDamageVersion = damageVersion; Stop();
        }
        public void EndCounter() { counterActive = false; counterDamageVersion = damageVersion; Stop(); }
        public void NotifyCounterStrike() { if (counterActive) CounterAttacksPerformed++; }
        public void BeginRecovery() { recoveryActive = true; recoveryInterrupted = false; }
        public void EndRecovery() { recoveryActive = false; }
        public void ReportBlocked() { if (!blocked) { blocked = true; Interrupt(); } }
        public void Stop() { if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y); }

        private void HandleDamage(float amount, GameObject attacker)
        {
            damageVersion++;
            if (Time.time < nextCounter) counterDamageVersion = damageVersion;
            if (!active) return;
            if (recoveryActive) recoveryInterrupted = true;
            // A counter gets its short execution window; new hits do not restart it into multiple attacks.
            if (!counterActive || recoveryInterrupted) Interrupt();
        }
        private void Interrupt()
        {
            unchecked { signal++; }
            if (signalVariable != null && signalVariable.Value != signal) signalVariable.Value = signal;
        }

        public bool EscapeRouteBlocked(Vector2 direction, float speed)
        {
            if (ownCollider == null) return false;
            obstacleHits.Clear();
            ownCollider.Cast(direction, obstacleFilter, obstacleHits, speed * Time.fixedDeltaTime + obstacleSkin);
            foreach (var hit in obstacleHits)
            {
                if (IgnoreHit(hit.collider) || Vector2.Dot(hit.normal, direction) >= -0.1f) continue;
                return true;
            }
            if (!avoidLedges) return false;
            Bounds bounds = ownCollider.bounds;
            var foot = new Vector2(bounds.center.x + direction.x * (bounds.extents.x + obstacleSkin), bounds.min.y + obstacleSkin);
            groundHits.Clear();
            Physics2D.Raycast(foot, Vector2.down, obstacleFilter, groundHits, groundProbeDistance + obstacleSkin);
            foreach (var hit in groundHits)
                if (!IgnoreHit(hit.collider) && hit.normal.y > 0f) return false;
            return true;
        }
        private bool IgnoreHit(Collider2D hit) => hit == null || hit.attachedRigidbody == body
            || hit.transform.IsChildOf(transform) || (Target != null && (hit.gameObject == Target || hit.transform.IsChildOf(Target.transform)));

        private void OnValidate()
        {
            retreatHpPercent = Mathf.Clamp(retreatHpPercent, 0f, 100f); recoverTargetPercent = Mathf.Clamp(recoverTargetPercent, 0f, 100f);
            resumeAttackHpPercent = Mathf.Clamp(resumeAttackHpPercent, 0f, 100f); maxRetreats = Mathf.Max(-1, maxRetreats);
            retreatCooldown = Mathf.Max(0f, retreatCooldown); retreatSpeedMultiplier = Mathf.Max(0f, retreatSpeedMultiplier);
            safeDistance = Mathf.Max(0f, safeDistance); survivalTime = Mathf.Max(0f, survivalTime);
            safetyReachTolerance = Mathf.Max(0f, safetyReachTolerance);
            regenDuration = Mathf.Max(0.01f, regenDuration); counterAttackDistance = Mathf.Max(0f, counterAttackDistance);
            counterAttackCooldown = Mathf.Max(0f, counterAttackCooldown); counterAttackWindow = Mathf.Max(0.01f, counterAttackWindow);
            cornerCheckTime = Mathf.Max(0f, cornerCheckTime); fallbackMoveSpeed = Mathf.Max(0f, fallbackMoveSpeed);
            minimumRetreatProgress = Mathf.Max(0f, minimumRetreatProgress); safetyHysteresis = Mathf.Max(0f, safetyHysteresis);
            obstacleSkin = Mathf.Max(0.001f, obstacleSkin); groundProbeDistance = Mathf.Max(0.01f, groundProbeDistance);
            obstacleFilter = new ContactFilter2D { useTriggers = false }; obstacleFilter.SetLayerMask(obstacleLayers);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, safeDistance);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, counterAttackDistance);
        }
    }
}

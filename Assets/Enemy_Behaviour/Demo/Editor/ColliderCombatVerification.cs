using System;
using System.Collections;
using System.Reflection;
using EnemyBehaviourPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Run in a separate Unity instance: -batchmode -projectPath <copy> -executeMethod ColliderCombatVerification.Run
[InitializeOnLoad]
public static class ColliderCombatVerification
{
    private const string Key = "ColliderCombatVerification.Pending";
    private static IEnumerator checks;
    private static double deadline;

    static ColliderCombatVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            checks = Verify();
            deadline = EditorApplication.timeSinceStartup + 45;
            EditorApplication.update += Tick;
        };
    }

    [MenuItem("Tools/Verify Collider Combat")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Enemy_Behaviour/Demo/Scenes/RetreatAggro_Test.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Combat verification timed out");
            if (checks.MoveNext()) return;
            Finish(0, "PASS: collider damage, compound colliders, damage aggro, ally alert, retreat, progressive recovery, enemy melee and range");
        }
        catch (Exception error) { Finish(1, error.ToString()); }
    }

    private static void Finish(int code, string result)
    {
        Debug.Log(result);
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Library/ColliderCombatVerification.txt"), result);
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.isPlaying = false;
    }

    private static void Set(object instance, string name, object value)
    {
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Debug.Log("CHECK: " + message);
    }

    private static IEnumerator Verify()
    {
        yield return null;
        var player = Object.FindFirstObjectByType<PrototypePlayer2D>();
        var retreat = Object.FindFirstObjectByType<EnemyRetreat>();
        Require(player != null && retreat != null, "Demo contains player and retreat enemy");
        player.enabled = false;
        Set(player, "hitImpulse", 0f);
        var enemy = retreat.gameObject;
        var sensor = enemy.GetComponent<EnemySensor>();
        foreach (var other in Object.FindObjectsByType<EnemySensor>(FindObjectsSortMode.None))
            if (other != sensor) other.gameObject.SetActive(false);
        player.transform.position = new Vector3(0, 2, 0);
        enemy.transform.position = new Vector3(1, 2, 0);
        player.GetComponent<Rigidbody2D>().gravityScale = 0;
        retreat.Body.gravityScale = 0;
        retreat.Body.linearVelocity = Vector2.zero;
        Set(sensor, "radius", 0.1f); // Damage, rather than vision, must acquire the attacker.
        Set(retreat, "safeDistance", 2f);
        Set(retreat, "avoidLedges", false);
        Set(retreat, "obstacleLayers", (LayerMask)0);
        Set(retreat, "survivalTime", 0.3f);
        Set(retreat, "regenDuration", 0.8f);
        Set(retreat, "counterAttackDistance", 0.1f);
        sensor.enabled = false;
        sensor.enabled = true;
        var aggro = enemy.GetComponent<EnemyAggro>();
        aggro.enabled = false;
        aggro.enabled = true;
        var ally = new GameObject("Verification ally");
        ally.layer = enemy.layer;
        ally.transform.position = new Vector3(5, 2, 0);
        ally.AddComponent<BoxCollider2D>();
        ally.AddComponent<Health>();
        var allySensor = ally.AddComponent<EnemySensor>();
        ally.AddComponent<EnemyAggro>();
        var child = new GameObject("Second hurt collider");
        child.transform.SetParent(enemy.transform, false);
        child.AddComponent<BoxCollider2D>();
        Physics2D.SyncTransforms();
        float before = retreat.Health.CurrentHealth;
        typeof(PrototypePlayer2D).GetMethod("Attack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
        Require(Mathf.Approximately(retreat.Health.CurrentHealth, before - 1), "Compound colliders take exactly one hit");
        Require(sensor.AttackTarget == player.gameObject, "Collider hit acquires attacker through damage aggro");
        Require(allySensor.AttackTarget == player.gameObject, "Damage aggro alerts a nearby ally");
        Object.Destroy(ally);
        Object.Destroy(child);
        float until = Time.time + 12;
        bool escaped = false, recovering = false, progressive = false;
        float damaged = retreat.Health.CurrentHealth;
        while (Time.time < until)
        {
            escaped |= retreat.IsRetreating && retreat.Body.position.x > 1.2f;
            recovering |= retreat.IsRecovering;
            progressive |= retreat.IsRecovering && retreat.Health.CurrentHealth > damaged && retreat.Health.CurrentHealth < retreat.Health.MaxHealth * 0.95f;
            if (escaped && recovering && progressive && !retreat.IsRetreating) break;
            yield return null;
        }
        Require(escaped, "Real Behavior graph moves enemy away after collider damage");
        Require(recovering && progressive, "Real retreat graph waits and restores HP progressively");
        Require(!retreat.IsRetreating && retreat.Health.Fraction > 0.95f, "Recovery ends above configured HP threshold");
        Set(retreat, "maxRetreats", 0);
        sensor.ReceiveAlert(player.gameObject);
        enemy.transform.position = player.transform.position + Vector3.right;
        retreat.Body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        float playerBefore = player.GetComponent<Health>().CurrentHealth;
        until = Time.time + 5;
        while (Time.time < until && player.GetComponent<Health>().CurrentHealth >= playerBefore) yield return null;
        Require(player.GetComponent<Health>().CurrentHealth < playerBefore, "Enemy graph melee damages player Health");
        player.transform.position += Vector3.left * 100;
        Physics2D.SyncTransforms();
        float farHealth = player.GetComponent<Health>().CurrentHealth;
        until = Time.time + 1.3f;
        while (Time.time < until) yield return null;
        Require(Mathf.Approximately(player.GetComponent<Health>().CurrentHealth, farHealth), "Enemy cannot damage an out-of-range collider");
    }
}

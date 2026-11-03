using System.Collections;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected PlayerStats stats;
    protected PlayerControl player;
    protected float targetFPS = 60f;

    public virtual void Initialize(PlayerStats playerStats, PlayerControl playerControl)
    {
        stats = playerStats;
        player = playerControl;
    }

    public abstract void ExecuteBasicAttack();
    public abstract void ExecuteSpecialAttack();
}
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Настройки UI")]
    public Image healthFill;
    public Image manaFill;

    [Header("Ссылка на игрока")]
    public PlayerStats playerStats;

    void Update()
    {
        if (playerStats == null) return;

        float maxHp = playerStats.unlockedHpSectors * playerStats.hpPerSector;
        healthFill.fillAmount = (float)playerStats.currentHealth / maxHp;

        float maxMana = playerStats.unlockedManaSectors * playerStats.manaPerSector;
        manaFill.fillAmount = playerStats.currentMana / maxMana;
    }
}
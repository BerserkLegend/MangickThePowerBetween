using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Settings")]
    public Image healthFill;
    public Image manaFill;
    public Image staminaFill;

    [Header("Link to Player")]
    public PlayerStats playerStats;

    void Update()
    {
        if (playerStats == null) return;

        float maxHp = playerStats.unlockedHpSectors * playerStats.hpPerSector;
        healthFill.fillAmount = (float)playerStats.currentHealth / maxHp;

        float maxMana = playerStats.unlockedManaSectors * playerStats.manaPerSector;
        manaFill.fillAmount = playerStats.currentMana / maxMana;

        if (staminaFill != null)
        {
            staminaFill.fillAmount = playerStats.currentStamina / playerStats.maxStamina;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField]
    private EnemyBTController enemy;

    [SerializeField]
    private Slider healthSlider;

    private void Awake()
    {
        // Cari EnemyBTController otomatis dari parent
        if (enemy == null)
        {
            enemy =
                GetComponentInParent<EnemyBTController>();
        }

        // Cari Slider otomatis dari child
        if (healthSlider == null)
        {
            healthSlider =
                GetComponentInChildren<Slider>();
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
        }
    }

    private void Update()
    {
        if (enemy == null ||
            healthSlider == null)
        {
            return;
        }

        healthSlider.value =
            enemy.HealthNormalized;
    }
}
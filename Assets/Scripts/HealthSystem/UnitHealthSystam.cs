using Unity.VisualScripting;
using UnityEngine;

public class UnitHealthSystam : MonoBehaviour
{
    public HealthBar healthBar;
    public float HealthWithoutUS = 100f;
    public event System.Action<GameObject> OnDamaged;
    public event System.Action OnDead;

    private float currentHealth;
    private UnitStats stats;

    private void Start()
    {
        stats = GetComponent<UnitStats>();
        float maxHealth = stats != null ? stats.MaxHealth : HealthWithoutUS;
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }
    }

    public void TakeDamage(float damage, GameObject attacker)
    {
        if (stats == null) stats = GetComponent<UnitStats>();
        float armor = stats != null ? stats.Armor : 0;
        float damageScale = 100f / (100f + armor);
        float finalDamage = Mathf.Max(damage * damageScale, 1f);

        currentHealth -= finalDamage;
        var messageSpawner = gameObject.GetComponent<MessageSpawner>();
        if (messageSpawner != null)
        {
            messageSpawner.SpawnMessage(finalDamage.ToString());
        }

        float maxHealth = stats != null ? stats.MaxHealth : HealthWithoutUS;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            currentHealth = 0;
        }
        OnDamaged?.Invoke(attacker);
    }

    public void Heal(float amount)
    {
        if (amount <= 0 || currentHealth == stats.MaxHealth) return;
        if (stats == null) stats = GetComponent<UnitStats>();
        float maxHealth = stats != null ? stats.MaxHealth : HealthWithoutUS;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        var messageSpawner = gameObject.GetComponent<MessageSpawner>();
        if (messageSpawner != null)
        {
            messageSpawner.SpawnMessage("+" + amount.ToString(), Color.green);
        }
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    private void Die()
    {
        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
            collider2D.enabled = false;
        OnDead?.Invoke();
        if (gameObject.GetComponent<BaseUnitAI>() == null)
        {
            Destroy(gameObject, 1f);
        }
        else
        {
            gameObject.GetComponent<BaseUnitAI>().Die();
        }
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    void OnDestroy()
    {
        if (AstarPath.active == null) return;
        Bounds bounds = new Bounds(transform.position, transform.localScale);
        AstarPath.active.UpdateGraphs(bounds);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}

using TMPro;
using UnityEngine;

public class FloatingMessage : MonoBehaviour, IInGameMessage
{
    private Rigidbody2D rb;
    private TMP_Text damageValue;

    public float InitialYVelocity = 7f;
    public float InitialXVelocityRange = 3f;
    public float Lifetime = 0.8f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        damageValue = GetComponentInChildren<TMP_Text>();
    }
    private void Start()
    {
        // Set initial velocity
        rb.linearVelocity = new Vector2(Random.Range(-InitialXVelocityRange, InitialXVelocityRange), InitialYVelocity);

        // Destroy the message after its lifetime
        Destroy(gameObject, Lifetime);
    }

    public void SetMessage(string message, Color? color = null)
    {
        if (damageValue != null)
        {
            float num;
            if (float.TryParse(message, out num))
            {
                if (Mathf.Approximately(num % 1f, 0f))
                    damageValue.SetText(((int)num).ToString());
                else
                    damageValue.SetText(num.ToString("F1"));
            }
            else
                damageValue.SetText(message);
            damageValue.color = color ?? Color.red;
        }
    }
}

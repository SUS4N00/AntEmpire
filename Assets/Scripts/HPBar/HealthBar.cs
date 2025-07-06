using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill;
    public void SetMaxHealth(float health)
    {
        slider.maxValue = health;
        slider.value = health; // Set the initial value to max

        fill.color = gradient.Evaluate(1f); // Set the fill color to the max value color
    }
    public void SetHealth(float health)
    {
        slider.value = health;
        fill.color = gradient.Evaluate(slider.normalizedValue); // Update the color based on current health
    }
}
using System.Collections.Generic;
using UnityEngine;

public enum AttackEffectType { Poison, Burn, Stun }

public class AttackEffectHandler : MonoBehaviour
{
    public List<AttackEffectType> effects = new List<AttackEffectType>();

    public void AddEffect(AttackEffectType effect)
    {
        if (!effects.Contains(effect))
            effects.Add(effect);
    }

    // Gọi hàm này khi tấn công để áp dụng hiệu ứng lên target
    public void ApplyEffects(GameObject target)
    {
        var status = target.GetComponent<UnitStatusEffect>();
        if (status == null) return;
        foreach (var effect in effects)
        {
            status.Apply(effect);
        }
    }
}
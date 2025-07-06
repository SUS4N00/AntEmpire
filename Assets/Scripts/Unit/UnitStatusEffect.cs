using UnityEngine;

public class UnitStatusEffect : MonoBehaviour
{
    public void Apply(AttackEffectType effect)
    {
        var baseAI = GetComponent<BaseUnitAI>();
        switch (effect)
        {
            case AttackEffectType.Poison:
                Debug.Log($"{gameObject.name} bị dính hiệu ứng Poison!");
                //if (baseAI != null) baseAI.OnPoisoned();
                break;
            case AttackEffectType.Burn:
                Debug.Log($"{gameObject.name} bị dính hiệu ứng Burn!");
                //if (baseAI != null) baseAI.OnBurned();
                break;
            case AttackEffectType.Stun:
                Debug.Log($"{gameObject.name} bị dính hiệu ứng Stun!");
                //if (baseAI != null) baseAI.OnStunned();
                break;
        }
    }
}

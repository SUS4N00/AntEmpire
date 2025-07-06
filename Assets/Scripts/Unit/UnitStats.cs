using UnityEngine;

public class UnitStats : MonoBehaviour
{
    // Chỉ số gốc
    public float baseMaxHealth = 100;
    public float baseDamage = 10;
    public float baseMoveSpeed = 5;
    public float baseAttackSpeed = 1;
    public float baseArmor = 0;
    public float baseAttackRange = 1;

    // Bonus từ reward/buff
    public float bonusMaxHealth = 0;
    public float bonusDamage = 0;
    public float bonusMoveSpeed = 0;
    public float bonusAttackSpeed = 0;
    public float bonusArmor = 0;
    public float bonusAttackRange = 0;
    public float bonusLifeSteal = 0;
    public float bonusHealthRegen = 0;

    // Getter tổng hợp
    public float MaxHealth => baseMaxHealth * (1 + bonusMaxHealth);
    public float Damage => baseDamage + bonusDamage;
    public float MoveSpeed => baseMoveSpeed * (1 + bonusMoveSpeed);
    public float AttackSpeed => baseAttackSpeed * (1 + bonusAttackSpeed);
    public float Armor => baseArmor + bonusArmor;
    public float AttackRange => baseAttackRange + (bonusAttackRange * baseAttackRange);
    public float LifeSteal => bonusLifeSteal;
    public float HealthRegen => bonusHealthRegen;
}

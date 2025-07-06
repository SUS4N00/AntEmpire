using UnityEngine;

public enum RewardType
{
    MoveSpeed,
    MaxHealth,
    Damage,
    AttackSpeed,
    AttackRange,
    Armor,
    LifeSteal,
    HealthRegen,
    // PoisonAttack,
    // BurnAttack,
    // StunAttack,
    // ...add more as needed
}

[CreateAssetMenu(fileName = "RewardData", menuName = "Reward/RewardData", order = 1)]
public class RewardData : ScriptableObject
{
    public string rewardName;
    [TextArea]
    public string description;
    public Sprite icon;
    public RewardType type;
    public float value; // Giá trị tăng (vd: +10% máu, +2 damage...)
}

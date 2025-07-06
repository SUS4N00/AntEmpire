using UnityEngine;

[System.Serializable]
public class RewardUnitCombo
{
    public RewardData reward;
    public UnitSpawnData unit;
    public RewardUnitCombo(RewardData reward, UnitSpawnData unit)
    {
        this.reward = reward;
        this.unit = unit;
    }
}

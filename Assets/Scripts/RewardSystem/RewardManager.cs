using UnityEngine;
using System.Collections.Generic;

// Struct lưu thông tin reward, loại unit và số lần áp dụng
[System.Serializable]
public struct RewardUnitCount {
    public RewardData reward;
    public UnitSpawnData unitData;
    public int count;
    public RewardUnitCount(RewardData r, UnitSpawnData u, int c) { reward = r; unitData = u; count = c; }
}

public class RewardManager : MonoBehaviour
{
    public List<RewardData> allRewards;
    // Lưu danh sách reward đã chọn, kèm loại unit và số lần
    private List<RewardUnitCount> rewardUnitCounts = new List<RewardUnitCount>();

    // Trả về danh sách reward đang active (không lặp)
    public List<RewardData> activeRewards {
        get {
            var set = new HashSet<RewardData>();
            foreach (var r in rewardUnitCounts) set.Add(r.reward);
            return new List<RewardData>(set);
        }
    }

    // Áp dụng lại toàn bộ reward đã chọn cho 1 unit (dùng khi unit mới spawn)
    public void ApplyAllActiveRewards(GameObject unit)
    {
        if (rewardUnitCounts == null || rewardUnitCounts.Count == 0) return;
        var stats = unit.GetComponent<UnitStats>();
        var effectHandler = unit.GetComponent<AttackEffectHandler>();
        if (stats == null) return;
        foreach (var entry in rewardUnitCounts)
        {
            // So sánh prefab hoặc tên unit
            if (entry.unitData != null && unit.name.StartsWith(entry.unitData.unitPrefab.name))
            {
                for (int i = 0; i < entry.count; i++)
                {
                    switch (entry.reward.type)
                    {
                        case RewardType.MoveSpeed: stats.bonusMoveSpeed += entry.reward.value; break;
                        case RewardType.MaxHealth: stats.bonusMaxHealth += entry.reward.value; break;
                        case RewardType.Damage: stats.bonusDamage += entry.reward.value; break;
                        case RewardType.AttackSpeed: stats.bonusAttackSpeed += entry.reward.value; break;
                        case RewardType.AttackRange: stats.bonusAttackRange += entry.reward.value; break;
                        case RewardType.Armor: stats.bonusArmor += entry.reward.value; break;
                        case RewardType.LifeSteal: stats.bonusLifeSteal += entry.reward.value; break;
                        case RewardType.HealthRegen: stats.bonusHealthRegen += entry.reward.value; break;
                    }
                }
            }
        }
    }

    // Áp dụng reward cho 1 loại unit cụ thể
    public void ApplyRewardToUnit(RewardData reward, UnitSpawnData unitData)
    {
        Debug.Log($"[RewardManager] Apply reward: {reward.rewardName} cho unit: {unitData.unitName}");
        // Tìm entry đã có
        bool found = false;
        for (int i = 0; i < rewardUnitCounts.Count; i++)
        {
            if (rewardUnitCounts[i].reward == reward && rewardUnitCounts[i].unitData == unitData)
            {
                var entry = rewardUnitCounts[i];
                entry.count++;
                rewardUnitCounts[i] = entry;
                found = true;
                break;
            }
        }
        if (!found)
            rewardUnitCounts.Add(new RewardUnitCount(reward, unitData, 1));
        var units = GameObject.FindGameObjectsWithTag("PlayerUnit");
        if (units == null || units.Length == 0)
        {
            Debug.LogWarning("[RewardManager] No player units found to apply reward.");
            return;
        }
        foreach (var unit in units)
        {
            var stats = unit.GetComponent<UnitStats>();
            var effectHandler = unit.GetComponent<AttackEffectHandler>();
            if (stats == null) continue;
            if (unitData != null && unit.name.StartsWith(unitData.unitPrefab.name))
            {
                switch (reward.type)
                {
                    case RewardType.MoveSpeed: stats.bonusMoveSpeed += reward.value; break;
                    case RewardType.MaxHealth: stats.bonusMaxHealth += reward.value; break;
                    case RewardType.Damage: stats.bonusDamage += reward.value; break;
                    case RewardType.AttackSpeed: stats.bonusAttackSpeed += reward.value; break;
                    case RewardType.AttackRange: stats.bonusAttackRange += reward.value; break;
                    case RewardType.Armor: stats.bonusArmor += reward.value; break;
                    case RewardType.LifeSteal: stats.bonusLifeSteal += reward.value; break;
                    case RewardType.HealthRegen: stats.bonusHealthRegen += reward.value; break;
                }
            }
        }
    }
}

// Các reward mẫu cho hệ thống RewardData
// Đặt file này trong thư mục Scripts/RewardSystem để tham khảo hoặc dùng tạo ScriptableObject trong Editor

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RewardDataSampleCreator
{
    [MenuItem("Reward/Create Sample Rewards")]
    public static void CreateSampleRewards()
    {
        // Đảm bảo thư mục tồn tại
        string folder = "Assets/Resources/Rewards";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Rewards");

        CreateReward("Tăng tốc chạy", "Tăng tốc độ di chuyển cho toàn bộ unit +10%", RewardType.MoveSpeed, 0.1f);
        CreateReward("Tăng máu tối đa", "Tăng máu tối đa cho toàn bộ unit +20%", RewardType.MaxHealth, 0.2f);
        CreateReward("Tăng damage", "Tăng sát thương cho toàn bộ unit +5", RewardType.Damage, 5f);
        CreateReward("Tăng tốc đánh", "Tăng tốc đánh cho toàn bộ unit +15%", RewardType.AttackSpeed, 0.15f);
        CreateReward("Tăng tầm đánh", "Tăng tầm đánh cho toàn bộ unit +1", RewardType.AttackRange, 1f);
        CreateReward("Tăng giáp", "Tăng giáp cho toàn bộ unit +3", RewardType.Armor, 3f);
        CreateReward("Tăng hút máu", "Đòn đánh hồi máu bằng 5% sát thương gây ra", RewardType.LifeSteal, 0.05f);
        CreateReward("Tăng hồi máu", "Tăng hồi máu mỗi giây cho toàn bộ unit +2", RewardType.HealthRegen, 2f);
        // CreateReward("Đòn đánh gây độc", "Đòn đánh gây hiệu ứng độc lên kẻ địch", RewardType.PoisonAttack, 0f);
        // CreateReward("Đòn đánh thiêu đốt", "Đòn đánh gây hiệu ứng thiêu đốt lên kẻ địch", RewardType.BurnAttack, 0f);
        // CreateReward("Đòn đánh tê liệt", "Đòn đánh có thể làm tê liệt kẻ địch", RewardType.StunAttack, 0f);
        Debug.Log("Đã tạo các reward mẫu trong thư mục Resources/Rewards");
    }

    private static void CreateReward(string name, string desc, RewardType type, float value)
    {
        var asset = ScriptableObject.CreateInstance<RewardData>();
        asset.rewardName = name;
        asset.description = desc;
        asset.type = type;
        asset.value = value;
        AssetDatabase.CreateAsset(asset, $"Assets/Resources/Rewards/{name}.asset");
    }
}
#endif

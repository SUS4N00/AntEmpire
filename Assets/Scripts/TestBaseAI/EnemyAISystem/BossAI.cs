using Pathfinding;
using UnityEngine;

public class BossAI : EnemyAI
{
    private bool isSecondPhase = false;
    override protected void Start()
    {
        base.Start();
        anim?.SetLayerWeight(1, 1f);
        // Thêm các khởi tạo đặc biệt cho Boss nếu cần
    }

    override protected void Update()
    {
        base.Update();
        HandleSecondPhase();
        // Thêm logic đặc biệt cho Boss nếu cần
    }

    private void HandleSecondPhase()
    {
        var healthSys = gameObject.GetComponent<UnitHealthSystam>();
        var stats = gameObject.GetComponent<UnitStats>();
        float maxHealth = stats != null ? Mathf.RoundToInt(stats.MaxHealth) : (healthSys != null ? healthSys.GetCurrentHealth() : 100);
        if (healthSys != null && healthSys.GetCurrentHealth() < maxHealth / 2 && !isSecondPhase)
        {
            isSecondPhase = true;
            EnterSecondPhase();
        }
    }
    
    private void EnterSecondPhase()
    {
        anim?.SetBool("SecondPhase", true);
        anim?.SetLayerWeight(1, 0f);
        anim?.SetLayerWeight(2, 1f);
        anim?.SetTrigger("Transform");
        // Đổi sang màu RGB(120, 200, 255)
        gameObject.GetComponent<SpriteRenderer>().color = new Color32(69, 234, 255, 255);
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        Seeker seeker = GetComponent<Seeker>();
        if (seeker != null)
        {
            seeker.graphMask = 1 << 1;
        }
    }
}
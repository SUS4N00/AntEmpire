using UnityEngine;
using Pathfinding;
using System;

public class BaseUnitAI : MonoBehaviour, IUnitAI
{
    protected AIPath aiPath;
    protected Animator anim;
    protected bool isDead = false;
    protected bool isInteracting = false;
    protected GameObject interactTarget;
    protected float speed; // Không còn dùng trực tiếp, chỉ lấy từ UnitStats
    protected UnitStats stats;

    protected virtual void Start()
    {
        anim = GetComponent<Animator>();
        aiPath = GetComponent<AIPath>();
        stats = GetComponent<UnitStats>();
        speed = stats != null ? stats.MoveSpeed : (aiPath != null ? aiPath.maxSpeed : 5f);
        if (aiPath != null)
            aiPath.maxSpeed = speed;
        // Bắt đầu regen máu nếu có
        if (stats != null)
            InvokeRepeating(nameof(HandleHealthRegen), 1f, 1f);
    }

    // Di chuyển đến mục tiêu
    public virtual void MoveToTarget(Vector3 targetPosition)
    {
        aiPath.destination = targetPosition;
        float moveSpeed = stats != null ? stats.MoveSpeed : speed;
        aiPath.maxSpeed = moveSpeed;
        anim.SetBool("IsMoving", aiPath.velocity.magnitude > 0.1f);
    }

    // Tấn công mục tiêu
    public virtual void Attack(GameObject target)
    {
        
    }

    // Thu thập tài nguyên
    public virtual void CollectResource(GameObject resource)
    {
        //Debug.Log("Collecting resource: " + resource.name);
    }

    // Tương tác với mục tiêu
    public virtual void InteractWithTarget(GameObject target)
    {
        //Debug.Log("Interacting with " + target.name);
    }

    // Xử lý khi chết
    public virtual void Die()
    {
        isDead = true;
        aiPath.canMove = false; // Dừng di chuyển khi chết
        aiPath.enableRotation = false; // Tắt quay khi chết
        if (anim != null)
            anim.SetTrigger("Die");
        if (aiPath != null)
            aiPath.canMove = false; // Dừng di chuyển khi chết
        Destroy(gameObject, 2f);  // Hủy đối tượng sau animation chết
    }

    // Đặt mục tiêu cho AI
    public virtual void SetTarget(GameObject target)
    {
        interactTarget = target;
    }

    protected void SmoothRotateToTarget()
    {
        // Tính toán hướng từ vị trí hiện tại của con kiến đến mục tiêu
        Vector3 direction = (interactTarget.transform.position - transform.position).normalized;

        // Kiểm tra nếu hướng khác không phải vector không (Vector3.zero)
        if (direction != Vector3.zero)
        {
            // Tính toán khoảng cách giữa con kiến và mục tiêu
            float distance = Vector2.Distance(transform.position, interactTarget.transform.position);

            // Tăng tốc độ quay khi con kiến gần mục tiêu
            // Công thức tính: tốc độ quay = maxSpeed / (khoảng cách + 1) để tránh chia cho 0 và giúp tốc độ tăng khi gần
            float rotationSpeed = Mathf.Clamp(1f / distance * 100f, 100f, 500f); // Điều chỉnh tỷ lệ cho phù hợp

            // Quay đối tượng về phía mục tiêu
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    protected void HandleHealthRegen()
    {
        if (stats != null && stats.HealthRegen > 0 && !isDead)
        {
            var healthSystam = GetComponent<UnitHealthSystam>();
            healthSystam.Heal(stats.HealthRegen);
        }
    }
}

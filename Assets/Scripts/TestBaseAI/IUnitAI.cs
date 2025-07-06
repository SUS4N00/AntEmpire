using UnityEngine;

public interface IUnitAI
{
    void Attack(GameObject target);
    void CollectResource(GameObject resource);
    void InteractWithTarget(GameObject target);
    void Die();
    void SetTarget(GameObject target); // Phương thức để thiết lập mục tiêu cho AI
}

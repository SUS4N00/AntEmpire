using UnityEngine;

public class BulletBase : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    private GameObject target;
    private float damage = 10f;
    private GameObject owner;
    private bool hasHit = false;
    private Vector3 targetPos;
    private bool useTargetPos = false;
    private LayerMask hitMask;

    public void SetTarget(GameObject t)
    {
        target = t;
    }
    public void SetDamage(float d)
    {
        damage = d;
    }
    public void SetOwner(GameObject o)
    {
        owner = o;
    }

    // Gọi hàm này để bắn đạn đến vị trí cụ thể (không cần target object)
    public void SetTargetPosition(Vector3 pos, LayerMask mask)
    {
        targetPos = pos;
        useTargetPos = true;
        hitMask = mask;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (hasHit)
            return;
        if (useTargetPos)
        {
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
            // Xoay đầu đạn về hướng bay (sprite mặc định hướng lên trên trục Y)
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            float dist = Vector2.Distance(transform.position, targetPos);
            if (dist < 0.1f)
            {
                Destroy(gameObject); // Khi đến vị trí, tự hủy, OnDestroy sẽ xử lý damage
            }
        }
        else if (target != null)
        {
            Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
            // Xoay đầu đạn về hướng bay (sprite mặc định hướng lên trên trục Y)
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < 0.2f)
            {
                Destroy(gameObject); // Khi đến target, tự hủy, OnDestroy sẽ xử lý damage
            }
        }
    }

    void OnDestroy()
    {
        if (hasHit) return;
        hasHit = true;
        if (useTargetPos)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, 0.25f, hitMask);
            bool didLifeSteal = false;
            foreach (var col in hits)
            {
                var health = col.GetComponent<UnitHealthSystam>();
                if (health != null && health.IsAlive())
                {
                    health.TakeDamage(damage, owner);
                    // Hút máu cho owner nếu có
                    if (!didLifeSteal && owner != null)
                    {
                        var ownerStats = owner.GetComponent<UnitStats>();
                        var ownerHealth = owner.GetComponent<UnitHealthSystam>();
                        if (ownerStats != null && ownerHealth != null)
                        {
                            ownerHealth.Heal(damage * ownerStats.LifeSteal);
                            didLifeSteal = true;
                        }
                    }
                }
            }
        }
        else if (target != null && target.GetComponent<UnitHealthSystam>() != null && target.GetComponent<UnitHealthSystam>().IsAlive())
        {
            target.GetComponent<UnitHealthSystam>().TakeDamage(damage, owner);
            // Hút máu cho owner nếu có
            if (owner != null)
            {
                var ownerStats = owner.GetComponent<UnitStats>();
                var ownerHealth = owner.GetComponent<UnitHealthSystam>();
                if (ownerStats != null && ownerHealth != null)
                {
                    ownerHealth.Heal(damage * ownerStats.LifeSteal);
                }
            }
        }
    }
}

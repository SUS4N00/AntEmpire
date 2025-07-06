using UnityEngine;
using Pathfinding;
using UnityEngine.XR;

public class EnemyAI : BaseUnitAI
{
    public float attackCooldown = 1.0f;
    public LayerMask targetLayerMask;
    private float lastAttackTime;
    private float size;
    // Xóa biến damage, attackRange, armor riêng, dùng stats
    protected override void Start()
    {
        base.Start();
        var healthSystem = GetComponent<UnitHealthSystam>();
        if (healthSystem != null)
        {
            healthSystem.OnDamaged += HandleDamage;
        }
        size = transform.localScale.x;
        aiPath = GetComponent<AIPath>();
        anim = GetComponent<Animator>();
        FindNewTarget();
    }

    protected virtual void Update()
    {
        if(!isDead)
        {
            HandleMovementAnimation();
        }
        if (interactTarget == null || !IsTargetAlive(interactTarget))
        {
            FindNewTarget();
            return;
        }

        aiPath.destination = interactTarget.transform.position;
        float attackRange = stats != null ? stats.AttackRange : 1f;
        float distance = Vector2.Distance(transform.position, interactTarget.transform.position);
        if (distance <= (attackRange + size / 2 + interactTarget.transform.localScale.x / 2))
        {
            aiPath.maxSpeed = 0f;
            //anim?.SetTrigger("Attack");
            SmoothRotateToTarget();
            TryAttack();
        }
        else
        {
            aiPath.maxSpeed = stats != null ? stats.MoveSpeed : speed;
        }
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            anim?.SetTrigger("Attack");
            lastAttackTime = Time.time;
        }
    }

    public void AppliedDamage()
    {
        if (interactTarget == null) return;
        var target = interactTarget.GetComponent<UnitHealthSystam>();
        float attackRange = stats != null ? stats.AttackRange : 1f;
        float damage = stats != null ? Mathf.RoundToInt(stats.Damage) : 10;
        var distance = Vector2.Distance(transform.position, interactTarget.transform.position);
        if (target != null && target.IsAlive() && distance <= (attackRange + size / 2 + interactTarget.transform.localScale.x / 2))
        {
            target.TakeDamage(damage, gameObject);
        }
    }

    void FindNewTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1000f, targetLayerMask);
        float minDist = float.MaxValue;
        GameObject nearest = null;
        foreach (var col in colliders)
        {
            var health = col.GetComponent<UnitHealthSystam>();
            if (health != null && health.IsAlive())
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = col.gameObject;
                }
            }
        }
        interactTarget = nearest;
    }

    bool IsTargetAlive(GameObject target)
    {
        var health = target.GetComponent<UnitHealthSystam>();
        return health != null && health.IsAlive();
    }

    private void HandleDamage(GameObject attacker)
    {
        if (attacker != null)
        {
            float attackRange = stats != null ? stats.AttackRange : 1f;
            float dist = Vector2.Distance(transform.position, attacker.transform.position);
            if (dist > attackRange + size / 2 + attacker.transform.localScale.x / 2)
            {
                FindNewTarget();
            }
            else
            {
                interactTarget = attacker;
            }
        }
    }

    private void HandleMovementAnimation()
    {
        if (anim != null)
        {
            bool isMoving = aiPath.velocity.magnitude > 0.1f;
            anim.SetBool("IsMoving", isMoving);
        }
    }
}

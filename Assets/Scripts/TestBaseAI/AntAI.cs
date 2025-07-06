using UnityEngine;
using Pathfinding;
using Unity.Cinemachine;
using System;
using System.Collections.Generic;
using System.Collections;
public class AntAI : BaseUnitAI, IUnitAI
{
    // Grouped variables for better organization
    // State variables
    private bool isSelected;
    private bool isMoving;
    private bool isAttacking;
    private bool isColecting;

    // References
    [SerializeField] private GameObject selectedEffect;
    [SerializeField] private GameObject dirtEffect;
    [SerializeField] private GameObject leafEffect;
    [SerializeField] private GameObject shellEffect;
    [SerializeField] private GameObject honeyEffect;
    private GameObject targetGroup;
    private IAstarAI iaStarAi;
    private Transform tmpMovePoint;

    // Parameters
    private float attackCooldown;
    private float colectCooldown;
    private float movementThreshold;
    private InteractType resourceType;
    private int resourceCapacity;
    private int resourceAmount;
    private InteractType interactCommand;

    private float lastTimeAttack;
    private float lastTimeCollect;
    private float collectRage; // Khoảng cách thu thập tài nguyên

    // Thêm biến trạng thái
    private bool isDepositing = false;
    private bool isReturningResource = false;
    private GameObject depositTarget = null;
    private GameObject lastResourceObj;

    // Add public getters for the properties

    // Add public setters for the properties
    public void SetResourceCapacity(int value) => resourceCapacity = value;


    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của BaseUnitAI để khởi tạo các thành phần cơ bản
        iaStarAi = GetComponent<IAstarAI>();
        collectRage = (float)Math.Sqrt(2);
        attackCooldown = 1f;
        colectCooldown = 1f;
        movementThreshold = 0.1f; // Ngưỡng di chuyển để xác định trạng thái di chuyển
        resourceCapacity = 5; // Sức chứa tài nguyên tối đa
        resourceAmount = 0; // Số lượng tài nguyên hiện tại
        interactCommand = InteractType.AttackMe;

        isSelected = false;
        targetGroup = GameObject.Find("Target Group");

        // Áp dụng lại toàn bộ reward đã chọn cho unit mới sinh ra
        var rewardManager = FindFirstObjectByType<RewardManager>();
        if (rewardManager != null)
        {
            rewardManager.ApplyAllActiveRewards(gameObject);
        }

        ResetResourceStatus();
        Deselect();
        lastTimeAttack = Time.time - attackCooldown;
        lastTimeCollect = Time.time - colectCooldown;
    }

    void Update()
    {
        if (!isDead)
        {
            HandleMovementAnimation();
            HandleInteraction();

            // Phần 1: Xử lý khi đến kho
            if (isDepositing && depositTarget != null)
            {
                float dist = Vector2.Distance(transform.position, depositTarget.transform.position);
                if (dist <= collectRage + transform.localScale.x/2 + depositTarget.transform.localScale.x/2)
                {
                    HandleDepositResource();
                }
                return;
            }
            else if (interactTarget != null && interactTarget.GetComponent<InteractCmd>().interactCommand == InteractType.FoodWarehouse && resourceType != InteractType.CollectDirt && resourceType != InteractType.CollectShell)
            {
                float dist = Vector2.Distance(transform.position, interactTarget.transform.position);
                if (dist <= collectRage + transform.localScale.x/2 + depositTarget.transform.localScale.x/2)
                {
                    HandleDepositResource();
                }
                return;
            }
            else if (interactTarget != null && interactTarget.GetComponent<InteractCmd>().interactCommand == InteractType.ResourceWarehouse && (resourceType == InteractType.CollectDirt || resourceType == InteractType.CollectShell))
            {
                float dist = Vector2.Distance(transform.position, interactTarget.transform.position);
                if (dist <= collectRage + transform.localScale.x/2 + depositTarget.transform.localScale.x/2)
                {
                    HandleDepositResource();
                }
                return;
            }

            // Phần 2: Xử lý từ kho về lại tài nguyên
            if (isReturningResource && lastResourceObj != null)
            {
                float dist = Vector2.Distance(transform.position, lastResourceObj.transform.position);
                if (dist <= collectRage)
                {
                    WareHouseToResource(lastResourceObj);
                }
            }
        }
        else
        {
            handleDeath();
            return;
        }
    }

    private void HandleMovementAnimation()
    {
        if (anim != null)
        {
            isMoving = aiPath.velocity.magnitude > movementThreshold;
            anim.SetBool("IsMoving", isMoving);

            if (!isMoving)
            {
                DestroyTempMovePoint();
            }
        }
    }

    private void HandleInteraction()
    {
        if (isInteracting && interactTarget != null)
        {
            Interacting(isInteracting);
        }
    }

    private void handleDeath()
    {
        anim.SetTrigger("Die");
        // Thêm logic xử lý khi chết, ví dụ: hủy đối tượng, cập nhật UI, v.v.
        isDead = true;
        Destroy(gameObject, 2f); // Hủy đối tượng sau 2 giây
    }

    public void Select()
    {
        if (targetGroup != null)
        {
            targetGroup.GetComponent<CinemachineTargetGroup>().AddMember(transform, 1, 1);
        }
        selectedEffect.SetActive(true);
        isSelected = true;
        // Thêm các hiệu ứng khác khi được chọn
    }

    public void Deselect()
    {
        if (targetGroup != null)
        {
            targetGroup.GetComponent<CinemachineTargetGroup>().RemoveMember(transform);
        }
        selectedEffect.SetActive(false);
        isSelected = false;
        // Hủy các hiệu ứng khi bỏ chọn
    }

    public void Attack(float distance, float cooldown)
    {
        base.Attack(interactTarget);
        float attackRemaining = Time.time - lastTimeAttack;
        float attackRange = stats != null ? stats.AttackRange : 0.3f;
        if (distance <= attackRange + transform.localScale.x/2 + interactTarget.transform.localScale.x/2)
        {
            iaStarAi.maxSpeed = 0f;
            SmoothRotateToTarget();
            if (attackRemaining >= cooldown)
            {
                Hit();
                lastTimeAttack = Time.time;
                // Nếu mục tiêu đã chết sau đòn đánh, tìm mục tiêu mới ở cả hai layer
                if (interactTarget != null && !interactTarget.GetComponent<UnitHealthSystam>().IsAlive())
                {
                    int enemyLayer = LayerMask.NameToLayer("Enemy");
                    int enemyFlyLayer = LayerMask.NameToLayer("EnemyFly");
                    int mask = (1 << enemyLayer) | (1 << enemyFlyLayer);
                    FindNearest(InteractType.AttackMe, mask, (nearestObject) =>
                    {
                        if (nearestObject != null)
                        {
                            Debug.Log($"[AntAI] Found new target: {nearestObject.name}");
                            SetTarget(nearestObject);
                            isInteracting = true;
                        }
                        else
                        {
                            Debug.Log("[AntAI] No more targets found.");
                            ResetAllStatus();
                        }
                    });
                }
            }
        }
        else
        {
            Respeed();
        }
    }

    public void Hit()
    {
        Debug.Log("Attack!");
        anim.SetTrigger("IsAttacking");
    }

    public void ApplyDamageToTarget()
    {
        if (interactTarget == null) return;
        float distance = Vector2.Distance(transform.position, interactTarget.transform.position);
        float attackRange = stats != null ? stats.AttackRange : 0.3f;
        if (distance <= (attackRange + transform.localScale.x/2 + interactTarget.transform.localScale.x/2) && interactTarget.TryGetComponent<InteractCmd>(out var interactCmd) && interactTarget.GetComponent<UnitHealthSystam>().IsAlive())
        {
            float damage = stats != null ? Mathf.RoundToInt(stats.Damage) : 10;
            interactTarget.GetComponent<UnitHealthSystam>().TakeDamage(damage, gameObject);
            //hút máu từ mục tiêu
            var healthSystam = GetComponent<UnitHealthSystam>();
            if (healthSystam != null)
            {
                healthSystam.Heal(damage * stats.LifeSteal); // Hút máu
            }
            // Áp dụng hiệu ứng nếu có
                var effectHandler = GetComponent<AttackEffectHandler>();
            if (effectHandler != null)
                effectHandler.ApplyEffects(interactTarget);
            Debug.Log($"Dealing {damage} damage to {interactTarget.name}");
        }
        else
        {
            Debug.LogWarning("Target is out of range or not alive.");
        }
    }

    [Header("Ranged Attack")]
    public bool isRanged = false; // Gán true nếu là unit tầm xa
    public GameObject bulletPrefab; // Prefab đạn, gán trong Inspector nếu là tầm xa
    public Transform firePoint; // Vị trí bắn đạn, gán trong Inspector

    public void Shoot()
    {
        if (interactTarget == null) return;
        float distance = Vector2.Distance(transform.position, interactTarget.transform.position);
        float attackRange = stats != null ? stats.AttackRange : 0.3f;
        if (distance <= (attackRange + transform.localScale.x/2 + interactTarget.transform.localScale.x/2) && interactTarget.TryGetComponent<InteractCmd>(out var interactCmd) && interactTarget.GetComponent<UnitHealthSystam>().IsAlive())
        {
            if (isRanged && bulletPrefab != null && firePoint != null)
            {
                // Bắn đạn về phía mục tiêu
                Vector2 dir = (interactTarget.transform.position - firePoint.position).normalized;
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                var bulletComp = bullet.GetComponent<BulletBase>(); // Nếu chưa có BulletBase, hãy tạo script kế thừa MonoBehaviour
                if (bulletComp != null)
                {
                    bulletComp.SetTarget(interactTarget);
                    bulletComp.SetDamage(stats != null ? stats.Damage : 10);
                    bulletComp.SetOwner(gameObject);
                }
                else
                {
                    // Nếu không có script BulletBase, tự di chuyển đạn
                    float speed = 10f;
                    var rb = bullet.GetComponent<Rigidbody2D>();
                    if (rb != null)
                        rb.linearVelocity = dir * speed;
                }
            }
            else
            {
                // Nếu không phải tầm xa, gọi ApplyDamageToTarget như cận chiến
                ApplyDamageToTarget();
            }
        }
        else
        {
            Debug.LogWarning("Target is out of range or not alive.");
        }
    }

    public void Collect(float distance, float cooldown, float range, InteractType type)
    {
        float colectRemaining = Time.time - lastTimeCollect;
        if (distance <= range)
        {
            iaStarAi.maxSpeed = 0f;
            SmoothRotateToTarget();
            if (colectRemaining >= cooldown)
            {
                //lastResourcePos = interactTarget.transform.position;
                CollectResource(type);
                lastTimeCollect = Time.time;
            }
        }
        else
        {
            Respeed();
        }
    }
    private void CollectResource(InteractType type)
    {
        if (interactTarget != null)
        {
            if (resourceType != type && resourceAmount > 0)
            {
                // Nếu đang thu thập tài nguyên khác, reset trạng thái
                gameObject.GetComponent<Rigidbody2D>().mass-= resourceAmount;
                resourceAmount = 0;
                ResetResourceStatus();
            }
            switch (type)
            {
                case InteractType.CollectHoney:
                    honeyEffect.SetActive(true);
                    break;
                case InteractType.CollectLeaf:
                    leafEffect.SetActive(true);
                    break;
                case InteractType.CollectDirt:
                    dirtEffect.SetActive(true);
                    break;
                case InteractType.CollectShell:
                    shellEffect.SetActive(true);
                    break;
                default:
                    Debug.LogWarning($"Unknown resource type: {type}");
                    ResetResourceStatus();
                    break;
            }
            resourceType = type;
            if (resourceAmount < resourceCapacity)
            {
                Debug.Log($"Collecting {type}!");
                anim.SetTrigger("IsCollecting");
                resourceAmount++;
                gameObject.GetComponent<Rigidbody2D>().mass++;
                interactTarget.GetComponent<UnitHealthSystam>().TakeDamage(1, gameObject);
            }
            // Chỉ khi đã đầy mới đi trả về kho
            if (resourceAmount >= resourceCapacity)
            {
                Debug.Log($"Resource capacity reached! Going to FoodWarehouse.");
                lastResourceObj = interactTarget;
                // Tìm FoodWarehouse gần nhất
                InteractType wereHouseType = InteractType.FoodWarehouse;
                switch (type)
                {
                    case InteractType.CollectHoney:
                        wereHouseType = InteractType.FoodWarehouse;
                        break;
                    case InteractType.CollectLeaf:
                        wereHouseType = InteractType.FoodWarehouse;
                        break;
                    case InteractType.CollectDirt:
                        wereHouseType = InteractType.ResourceWarehouse;
                        break;
                    case InteractType.CollectShell:
                        wereHouseType = InteractType.ResourceWarehouse;
                        break;
                }
                FindNearest(wereHouseType, LayerMask.GetMask("Interact"), (wareHouse) =>
                {
                    if (wareHouse != null)
                    {
                        SetTarget(wareHouse);
                        isDepositing = true;
                        depositTarget = wareHouse;
                    }
                    else
                    {
                        Debug.LogWarning("No FoodWarehouse found!");
                    }
                });
            }
        }
    }

    private void HandleDepositResource()
    {
        var resourceBar = FindAnyObjectByType<ResourceBar>();
        if (resourceBar != null)
        {
            switch (resourceType)
            {
                case InteractType.CollectHoney:
                    resourceBar.SetHoney(resourceBar.honey + resourceAmount);
                    break;
                case InteractType.CollectDirt:
                    resourceBar.SetDirt(resourceBar.dirt + resourceAmount);
                    break;
                case InteractType.CollectLeaf:
                    resourceBar.SetLeaf(resourceBar.leaf + resourceAmount);
                    break;
                case InteractType.CollectShell:
                    resourceBar.SetShell(resourceBar.shell + resourceAmount);
                    break;
            }
            resourceBar.UpdateAllResources();
        }
        ResetResourceStatus(); // Reset trạng thái tài nguyên
        gameObject.GetComponent<Rigidbody2D>().mass -= resourceAmount; // Giảm khối lượng của con kiến
        resourceAmount = 0;
        Debug.Log("Deposited resources!");
        // Chuyển sang trạng thái quay lại tài nguyên cũ
        isDepositing = false;
        depositTarget = null;
        isReturningResource = true;

        // Nếu tài nguyên cũ không null, quay lại vị trí tài nguyên cũ để tiếp tục thu thập
        if (lastResourceObj != null)
        {
            SetTarget(lastResourceObj);  // Quay lại tài nguyên cũ
            isInteracting = true;
            lastTimeCollect = Time.time - colectCooldown;  // Cập nhật thời gian thu thập lại
        }
        else
        {
            Debug.LogWarning("No last resource found to return to.");
            ResetAllStatus();  // Nếu không tìm thấy tài nguyên cũ, reset trạng thái
        }
    }

    public void WareHouseToResource(GameObject oldResource)
    {
        // Khi đã về lại resource, reset trạng thái và tiếp tục thu thập
        interactTarget = oldResource;
        SetTarget(oldResource);
        isReturningResource = false;
        isInteracting = true;
        Debug.Log("Returned to resource, continue collecting!");
    }

    public void Build()
    {

    }

    private void Interacting(bool isInteracting)
    {
        if (isInteracting && interactTarget != null)
        {
            if (interactTarget.GetComponent<UnitHealthSystam>().IsAlive())
            {
                string interactCmd = interactTarget.GetComponent<InteractCmd>().interactCommand.ToString();
                gameObject.GetComponent<AIDestinationSetter>().target = interactTarget.transform;
                float distance = Vector2.Distance(transform.position, interactTarget.transform.position);
                float attackRange = stats != null ? stats.AttackRange : 0.3f;
                switch (interactCmd)
                {
                    case "AttackMe":
                        Attack(distance, attackCooldown);
                        break;
                    case "CollectHoney":
                        Collect(distance, colectCooldown, collectRage, InteractType.CollectHoney);
                        break;
                    case "CollectLeaf":
                        Collect(distance, colectCooldown, collectRage, InteractType.CollectLeaf);
                        break;
                    case "CollectDirt":
                        Collect(distance, colectCooldown, collectRage, InteractType.CollectDirt);
                        break;
                    case "CollectShell":
                        Collect(distance, colectCooldown, collectRage, InteractType.CollectShell);
                        break;
                    case "BuildMe":
                        Build();
                        break;
                    case "FoodWarehouse":
                        if (distance <= collectRage + transform.localScale.x/2 + interactTarget.transform.localScale.x/2 && resourceType != InteractType.CollectDirt && resourceType != InteractType.CollectShell && resourceAmount > 0)
                        {
                            SetTarget(interactTarget);
                            isInteracting = true;
                            HandleDepositResource();
                            ResetAllStatus();
                        }
                        else
                        {
                            ResetAllStatus();
                        }
                        break;
                    case "ResourceWarehouse":
                        if (distance <= collectRage + transform.localScale.x/2 + interactTarget.transform.localScale.x/2 && (resourceType == InteractType.CollectDirt || resourceType == InteractType.CollectShell) && resourceAmount > 0)
                        {
                            SetTarget(interactTarget);
                            isInteracting = true;
                            HandleDepositResource();
                            ResetAllStatus();
                        }
                        else
                        {
                            ResetAllStatus();
                        }
                        break;
                    default:
                        ResetAllStatus();
                        break;
                }
            }
            else
            {
                // Luôn tìm cả hai layer khi mục tiêu chết
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                int enemyFlyLayer = LayerMask.NameToLayer("EnemyFly");
                int mask = (1 << enemyLayer) | (1 << enemyFlyLayer);
                FindNearest(interactTarget.GetComponent<InteractCmd>().interactCommand, mask, (nearestObject) =>
                {
                    if (nearestObject != null)
                    {
                        interactTarget = nearestObject;
                        gameObject.GetComponent<AIDestinationSetter>().target = interactTarget.transform;
                    }
                    else
                    {
                        ResetAllStatus();
                    }
                });
            }
        }
        else
        {
            ResetAllStatus();
            DestroyTempMovePoint();
        }
    }

    public bool GetSelectedStatus()
    {
        return isSelected;
    }

    public bool GetInteractStatus()
    {
        return isInteracting;
    }

    public void SetInteractStatus(bool status)
    {
        isInteracting = status;
    }

    public bool GetAttackStatus()
    {
        return isAttacking;
    }

    public bool GetColectStatus()
    {
        return isColecting;
    }

    public GameObject GetTarget()
    {
        return interactTarget;
    }
    public override void SetTarget(GameObject something)
    {
        if (GetComponent<AIDestinationSetter>().target != null && GetComponent<AIDestinationSetter>().target.CompareTag("Temp"))
        {
            GameObject tmp = GetComponent<AIDestinationSetter>().target.gameObject;
            GetComponent<AIDestinationSetter>().target = null;
            Destroy(tmp);
        }
        interactTarget = something;
    }

    public void FindNearest(InteractType interactType, int layerMask, Action<GameObject> onComplete)
    {
        List<Collider2D> colliders = new List<Collider2D>();
        float attackRange = stats != null ? stats.AttackRange : 0.3f;
        if (interactType == InteractType.AttackMe)
        {
            float searchRadius = transform.localScale.x + attackRange + 7f;
            var found = Physics2D.OverlapCircleAll(transform.position, searchRadius, layerMask);
            colliders.AddRange(found);
            Debug.Log($"[AntAI] FindNearest found {colliders.Count} colliders in radius {searchRadius}.");
        }
        else
        {
            InteractCmd[] AllCmd = FindObjectsByType<InteractCmd>(FindObjectsSortMode.None);
            foreach (var cmd in AllCmd)
            {
                Debug.Log($"Checking {cmd.gameObject.name}, interactCommand: {cmd.interactCommand}");
                if (cmd.interactCommand == interactType)
                {
                    Collider2D collider = cmd.GetComponent<Collider2D>();
                    if (collider == null)
                    {
                        return;
                    }
                    else
                    {
                        colliders.Add(collider);
                    }
                }
            }
        }
        GameObject closestGO = null;
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            var GO = col.gameObject;
            if (GO.TryGetComponent<InteractCmd>(out var cmd))
            {
                if (cmd.interactCommand == interactType && GO.GetComponent<UnitHealthSystam>() != null && GO.GetComponent<UnitHealthSystam>().IsAlive())
                {
                    float dist = Vector2.Distance(transform.position, GO.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestGO = GO;
                    }
                }
            }
        }

        if (closestGO == null)
        {
            Debug.LogWarning($"No objects found matching the criteria: interactType={interactType}, layerMask={layerMask}");
        }
        onComplete(closestGO);
    }

    public void ResetAllStatus()
    {
        isInteracting = false;
        isAttacking = false;
        isColecting = false;
        interactTarget = null;
        Respeed();
    }

    private void ResetResourceStatus()
    {
        dirtEffect.SetActive(false);
        leafEffect.SetActive(false);
        shellEffect.SetActive(false);
        honeyEffect.SetActive(false);
    }

    public void Respeed()
    {
        iaStarAi.maxSpeed = stats != null ? stats.MoveSpeed : speed;
    }
    public void DestroyTempMovePoint()
    {
        if ((tmpMovePoint = gameObject.GetComponent<AIDestinationSetter>().target) != null && tmpMovePoint.CompareTag("Temp"))
        {
            Destroy(tmpMovePoint.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        float attackRange = stats != null ? stats.AttackRange : 0.3f;
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(gameObject.transform.position, attackRange + transform.localScale.x/2);
    }
}


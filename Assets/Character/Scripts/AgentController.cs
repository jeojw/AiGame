// File: AgentController.cs
using UnityEngine;
using System.Collections;

public abstract class AgentController : MonoBehaviour
{
    // ... 모든 변수와 Awake, Start, Update 등 다른 메소드들은 그대로 ...

    // --- [수정] PerformAttack 메소드 ---
    // 이제 공격 애니메이션만 시작시키고, 실제 데미지 판정은 애니메이션 이벤트로 넘깁니다.
    public virtual NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        Debug.Log("<color=blue>" + gameObject.name + " - PerformAttack: 공격 애니메이션 시작! (적용될 배율: " + damageMultiplier + ")</color>");
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);
        blackboard.canCounterAttack = false; // 공격 시 반격 기회 초기화

        // [추가] 애니메이션 이벤트에서 사용할 데미지 배율을 블랙보드에 저장
        blackboard.currentAttackDamageMultiplier = damageMultiplier;

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }
        // 실제 데미지 판정 로직(SphereCast 등)은 아래 ActuallyDealDamage() 메소드로 이동했습니다.
        return NodeStatus.SUCCESS;
    }

    // --- [추가] 애니메이션 이벤트에서 호출될 실제 데미지 처리 메소드 ---
    public void ActuallyDealDamage() // 이 메소드를 공격 애니메이션의 특정 프레임에 이벤트로 추가해야 합니다.
    {
        Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: 애니메이션 이벤트 발생! 실제 공격 판정 시작!</color>");

        // 블랙보드에서 현재 공격의 데미지 배율을 가져옴
        float finalDamage = this.attackDamage * blackboard.currentAttackDamageMultiplier;

        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out RaycastHit hit, attackRange))
        {
            Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: SphereCast 적중! -> " + hit.collider.gameObject.name + "</color>");
            if (hit.collider.CompareTag("Enemy"))
            {
                AgentController enemyController = hit.collider.GetComponent<AgentController>();
                if (enemyController != null)
                {
                    Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: " + enemyController.gameObject.name + "에게 HandleDamage 호출.</color>");
                    enemyController.HandleDamage(finalDamage, this);
                }
            }
        }
        else
        {
            Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: SphereCast 헛스윙.</color>");
        }
    }

    // (전체 코드를 원하시면 이전 답변의 AgentController 코드에서 위 두 메소드와 Blackboard 수정을 적용하시면 됩니다.)
    #region Unchanged_Methods_And_Variables
    public Transform enemy;
    public float detectionRadius = 20f;
    public float attackRange = 2f;
    public float closeRangeThreshold = 5f;
    public float lowHealthThreshold = 30f;
    public float evadeDistance = 2.5f;
    public float rotationSpeed = 10f;
    public float evadeDuration = 0.3f;
    public float attackDamage = 10f;
    public float defenseGracePeriod = 0.2f;
    protected AgentBlackboard blackboard;
    protected BTNode rootNode;
    private Animator animator;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private readonly float gravityValue = -9.81f;
    private Coroutine activeEvadeCoroutine = null;

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f;
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.attackCooldownDuration = 2.5f;
        blackboard.defendCooldownDuration = 2.5f;
        blackboard.evadeCooldownDuration = 5f;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }
    protected virtual void Start()
    {
        InitializeBehaviorTree();
    }
    protected abstract void InitializeBehaviorTree();
    protected virtual void Update()
    {
        if (enemy == null) FindEnemy();
        bool isPerformingAction = false;
        if (animator != null)
        {
            var currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentStateInfo.IsTag("Attack") || currentStateInfo.IsTag("Defend") || currentStateInfo.IsTag("Evade"))
                isPerformingAction = true;
        }
        if (enemy != null)
        {
            float enemyCurrentHealth = 100f;
            AgentController enemyCtrl = enemy.GetComponent<AgentController>();
            if (enemyCtrl != null) enemyCurrentHealth = enemyCtrl.blackboard.currentHealth;
            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), enemyCurrentHealth);
            if (!isPerformingAction) SmoothLookAtEnemy();
        }
        else
        {
            blackboard.enemyTransform = null;
        }
        if (!isPerformingAction)
        {
            if (characterController.isGrounded && playerVelocity.y < 0) playerVelocity.y = 0f;
            playerVelocity.y += gravityValue * Time.deltaTime;
            characterController.Move(playerVelocity * Time.deltaTime);
        }
        if (rootNode != null) rootNode.Tick();
    }
    void SmoothLookAtEnemy()
    {
        Vector3 direction = enemy.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    void FindEnemy()
    {
        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy");
        if (enemyObject != null)
        {
            enemy = enemyObject.transform;
        }
    }
    public virtual NodeStatus MoveTowards(Vector3 targetPosition, float speed, float stopDistance)
    {
        if (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0;
            direction.Normalize();
            characterController.Move(direction * speed * Time.deltaTime);
            if (animator != null) animator.SetFloat("Speed", speed);
            return NodeStatus.RUNNING;
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
        return NodeStatus.SUCCESS;
    }
    public virtual NodeStatus MoveAwayFrom(Vector3 targetPosition, float speed, float moveDistance)
    {
        Vector3 direction = (transform.position - targetPosition);
        direction.y = 0;
        direction.Normalize();
        characterController.Move(direction * speed * Time.deltaTime);
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        if (animator != null) animator.SetFloat("Speed", speed);
        return NodeStatus.SUCCESS;
    }
    public virtual NodeStatus PerformDefend()
    {
        Debug.Log(gameObject.name + " - PerformDefend: 호출됨. 방어 시작 시도.");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY);
        blackboard.StartInvincibility(blackboard.defendCooldownDuration);
        blackboard.defenseInitiationTime = Time.time;
        Debug.Log(gameObject.name + " - PerformDefend: isInvincible = " + blackboard.isInvincible + ", defenseInitiationTime = " + blackboard.defenseInitiationTime);
        if (animator != null)
        {
            animator.SetTrigger("IsDefending");
        }
        Invoke(nameof(StopDefendInvincibility), blackboard.defendCooldownDuration);
        return NodeStatus.SUCCESS;
    }
    public virtual NodeStatus PerformEvade()
    {
        Debug.Log("행동: 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);
        blackboard.StartInvincibility(evadeDuration);
        Invoke(nameof(StopEvadeInvincibility), evadeDuration);
        if (animator != null)
        {
            animator.SetTrigger("IsEvading");
        }
        if (activeEvadeCoroutine != null)
        {
            StopCoroutine(activeEvadeCoroutine);
        }
        activeEvadeCoroutine = StartCoroutine(EvadeCoroutine());
        return NodeStatus.SUCCESS;
    }
    private IEnumerator EvadeCoroutine()
    {
        float randomDirection = Random.value > 0.5f ? 1f : -1f;
        Vector3 evadeStartDirection = transform.right * randomDirection;
        float elapsedTime = 0f;
        while (elapsedTime < evadeDuration)
        {
            Vector3 movement = evadeStartDirection * (evadeDistance / evadeDuration) * Time.deltaTime;
            characterController.Move(movement);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        activeEvadeCoroutine = null;
    }
    public virtual NodeStatus Idle()
    {
        Debug.Log("행동: 대기 중");
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
        return NodeStatus.SUCCESS;
    }
    private void StopDefendInvincibility()
    {
        blackboard.EndInvincibility();
        blackboard.defenseInitiationTime = -1f;
        Debug.Log(gameObject.name + " 방어 무적 상태 및 유예 시간 종료.");
    }
    private void StopEvadeInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log(gameObject.name + " 회피 무적 상태 종료.");
    }
    public void HandleDamage(float damage, AgentController attacker)
    {
        Debug.Log(gameObject.name + " - HandleDamage: 호출됨! 공격자: " + attacker.gameObject.name + ", isInvincible: " + blackboard.isInvincible);
        if (animator != null)
        {
            AnimatorStateInfo currentAnimState = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log(gameObject.name + " - HandleDamage: 현재 애니메이션 태그 'Defend' 여부: " + currentAnimState.IsTag("Defend"));
        }
        float damageTaken = damage;
        if (blackboard.isInvincible)
        {
            bool inDefendAnim = animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Defend");
            bool inGracePeriod = blackboard.defenseInitiationTime > -0.5f && (Time.time - blackboard.defenseInitiationTime) < defenseGracePeriod;
            if (inDefendAnim || inGracePeriod)
            {
                Debug.Log("<color=green>" + gameObject.name + ": ★★★ 방어 성공! (애니메이션: " + inDefendAnim + ", 유예시간: " + inGracePeriod + ") 카운터 기회 활성화! ★★★" + "</color>");
                blackboard.canCounterAttack = true;
                blackboard.defenseInitiationTime = -1f;
                damageTaken = damage * 0.1f;
            }
            else if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Evade"))
            {
                Debug.Log(gameObject.name + ": 회피 성공! 데미지 없음.");
                damageTaken = 0f;
            }
            else
            {
                Debug.LogWarning(gameObject.name + ": 알 수 없는 무적 상태에서 피격. 일단 칩데미지 적용.");
                damageTaken = damage * 0.1f;
            }
            blackboard.TakeDamage(damageTaken);
        }
        else
        {
            blackboard.TakeDamage(damageTaken);
        }
        string logMessage = string.Format("[데미지] {0} -> {1} | 요청데미지: {2}, 실제 입은 데미지: {3} | 남은 체력: ({0}) {4}/{5}, ({1}) {6}/{7}",
            attacker.gameObject.name,
            this.gameObject.name,
            damage.ToString("F1"),
            damageTaken.ToString("F1"),
            attacker.blackboard.currentHealth.ToString("F0"),
            attacker.blackboard.maxHealth,
            this.blackboard.currentHealth.ToString("F0"),
            this.blackboard.maxHealth
        );
        Debug.Log(logMessage);
        if (blackboard.currentHealth <= 0)
        {
            Die();
        }
    }
    protected virtual void Die()
    {
        Debug.Log(gameObject.name + "이(가) 죽었습니다.");
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        this.enabled = false;
        Destroy(gameObject, 3f);
    }
    #endregion
}
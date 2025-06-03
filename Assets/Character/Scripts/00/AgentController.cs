// File: AgentController.cs
using UnityEngine;
using System.Collections;

public abstract class AgentController : MonoBehaviour
{
    public Transform enemy;
    public float detectionRadius = 20f;
    public float attackRange = 2f;
    public float closeRangeThreshold = 5f;
    public float lowHealthThreshold = 30f;
    public float evadeDistance = 2.5f;
    public float rotationSpeed = 2f;
    public float evadeDuration = 0.3f;
    public float attackDamage = 10f;
    public float defenseGracePeriod = 0.2f;

    protected AgentBlackboard blackboard;
    protected BTNode rootNode;

    private Animator animator;
    private Rigidbody rb;
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
        rb = GetComponent<Rigidbody>();
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
            rb.MovePosition(playerVelocity * Time.deltaTime);
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
        GameObject enemyObject;
        if (gameObject.CompareTag("Offensiver"))
        {
            enemyObject = GameObject.FindGameObjectWithTag("Defensiver");
        }
        else
        {
            enemyObject = GameObject.FindGameObjectWithTag("Offensiver");
        }
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
            Vector3 moveVector = speed * Time.deltaTime * direction;
            rb.MovePosition(transform.position + moveVector);
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
        Vector3 moveVector = speed * Time.deltaTime * direction;
        rb.MovePosition(transform.position + moveVector);
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
        if (animator != null) animator.SetFloat("Speed", speed);
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        Debug.Log("<color=blue>" + gameObject.name + " - PerformAttack: 공격 애니메이션 시작! (배율: " + damageMultiplier + ")</color>");
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);
        blackboard.canCounterAttack = false;
        blackboard.currentAttackDamageMultiplier = damageMultiplier;
        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }
        return NodeStatus.SUCCESS;
    }

    public void ActuallyDealDamage()
    {
        Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: 애니메이션 이벤트 발생! 실제 공격 판정 시작!</color>");
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
            rb.MovePosition(movement);
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

    // --- [추가] GetAttack 메소드 (기본 형태) ---
    // GetAttackAction 노드가 호출할 메소드입니다.
    // 이 메소드 안에 어떤 로직을 넣을지는 사용자님께서 결정해주셔야 합니다.
    public virtual NodeStatus GetAttack()
    {
        Debug.Log(gameObject.name + " - GetAttack: 호출됨 (현재는 아무 동작도 하지 않음)");
        // 예시: 피격 애니메이션을 재생하거나, 특정 상태로 전환하는 로직
        // if (animator != null) animator.SetTrigger("IsHit");
        return NodeStatus.SUCCESS; // 또는 상황에 따라 FAILURE/RUNNING 반환
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

    // --- [추가] GetAttackAction 내부 클래스 ---
    // (사용자님의 Actions.cs 파일에 있던 내용을 AgentController 내부로 옮겨왔습니다.
    // 이렇게 하면 AgentController와 밀접하게 관련된 작은 노드들을 한 곳에서 관리하기 편할 수 있습니다.
    // 별도의 Actions.cs 파일에 그대로 두셔도 무방합니다.)
    public class GetAttackAction : BTActionNode
    {
        public GetAttackAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

        public override NodeStatus Tick()
        {
            AgentController controller = agentTransform.GetComponent<AgentController>();
            if (controller != null)
            {
                // AgentController에 추가된 GetAttack() 메소드를 호출합니다.
                return controller.GetAttack();
            }
            return NodeStatus.FAILURE;
        }
    }
}
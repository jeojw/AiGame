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
    public float rotationSpeed = 5f; // 회전 속도를 약간 높여 반응성을 개선할 수 있습니다.
    public float evadeDuration = 0.3f;
    public float attackDamage = 10f;
    public float defenseGracePeriod = 0.2f;

    protected AgentBlackboard blackboard;
    protected BTNode rootNode;

    private Animator animator;
    private Rigidbody rb;
    // private Vector3 playerVelocity; // 이 변수의 사용처가 명확하지 않아 주석 처리 또는 삭제 고려
    // private readonly float gravityValue = -9.81f; // 현재 사용되지 않음
    private Coroutine activeEvadeCoroutine = null;
    private bool isManuallyRotating = false; // 수동 회전 중인지 나타내는 플래그 (선택적)

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

        bool isPerformingAction = false; // 공격, 방어, 회피 등 주요 행동 중인지
        bool isMoving = false;           // 이동 중인지 (애니메이터 Speed 기준)

        if (animator != null)
        {
            var currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentStateInfo.IsTag("Attack") || currentStateInfo.IsTag("Defend") || currentStateInfo.IsTag("Evade"))
            {
                isPerformingAction = true;
            }
            if (animator.GetFloat("Speed") > 0.1f)
            {
                isMoving = true;
            }
        }

        if (enemy != null)
        {
            float enemyCurrentHealth = 100f;
            AgentController enemyCtrl = enemy.GetComponent<AgentController>();
            if (enemyCtrl != null) enemyCurrentHealth = enemyCtrl.blackboard.currentHealth;

            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), enemyCurrentHealth);

            // SmoothLookAtEnemy 호출 조건:
            // 1. isPerformingAction이 false (공격, 방어, 회피 중이 아님)
            // 2. isMoving이 false (제자리에 멈춰있을 때)
            // 3. isManuallyRotating이 false (PerformAttack 등에서 직접 회전 제어 중이 아닐 때)
            // * 이동 중 바라보기는 MoveTowards 메소드 내부에서 처리합니다.
            if (!isPerformingAction && !isMoving && !isManuallyRotating)
            {
                SmoothLookAtEnemy();
            }
        }
        else
        {
            blackboard.enemyTransform = null;
        }

        if (rootNode != null) rootNode.Tick();
    }

    void SmoothLookAtEnemy()
    {
        if (enemy == null) return;

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
        // 이동 중에는 항상 적을 부드럽게 바라보도록 합니다.
        if (enemy != null)
        {
            SmoothLookAtEnemy();
        }

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
        // 도망갈 때는 적을 등지고 가는 것이 자연스러울 수 있으나, 현재는 바라보는 로직을 추가하지 않습니다.
        // 필요하다면 이동 방향으로 캐릭터를 회전시키는 로직을 추가할 수 있습니다.
        Vector3 direction = (transform.position - targetPosition);
        direction.y = 0;
        direction.Normalize();
        Vector3 moveVector = speed * Time.deltaTime * direction;
        rb.MovePosition(transform.position + moveVector);

        // 이동 방향으로 즉시 또는 부드럽게 회전
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime)); // Rigidbody 회전 사용 시
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // Transform 회전 사용 시
        }

        if (animator != null) animator.SetFloat("Speed", speed);
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        isManuallyRotating = true; // 수동 회전 시작 (선택적)
        if (enemy != null)
        {
            // 공격 시작 시 적을 즉시 바라보도록 수정
            Vector3 directionToEnemy = enemy.position - transform.position;
            directionToEnemy.y = 0; // 수평으로만 회전
            if (directionToEnemy.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToEnemy);
            }
        }
        // 짧은 시간 후 isManuallyRotating을 false로 되돌리는 코루틴 또는 Invoke를 고려할 수 있으나,
        // 공격 애니메이션이 끝날 때까지는 isPerformingAction 플래그가 SmoothLookAtEnemy를 막아줄 것입니다.
        // 필요하다면 공격 애니메이션 이벤트 등을 활용하여 isManuallyRotating = false; 처리
        // 여기서는 공격 애니메이션이 끝난 후 isPerformingAction이 false가 되면 자연스럽게 Update의 SmoothLookAtEnemy가 작동하도록 둡니다.
        isManuallyRotating = false; // 또는 공격 애니메이션 길이에 맞춰서 false로 변경


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
        // ... (기존 코드와 동일)
        Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: 애니메이션 이벤트 발생! 실제 공격 판정 시작!</color>");
        float finalDamage = this.attackDamage * blackboard.currentAttackDamageMultiplier;
        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out RaycastHit hit, attackRange))
        {
            Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: SphereCast 적중! -> " + hit.collider.gameObject.name + "</color>");
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Offensiver") || hit.collider.CompareTag("Defensiver"))
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
        // 방어 시에도 적을 바라보도록 할 수 있습니다.
        if (enemy != null)
        {
            Vector3 directionToEnemy = enemy.position - transform.position;
            directionToEnemy.y = 0;
            if (directionToEnemy.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToEnemy);
            }
        }

        Debug.Log(gameObject.name + " - PerformDefend: 호출됨. 방어 시작 시도.");
        // ... (기존 코드와 동일)
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
        // 회피 시에는 특정 방향으로 빠르게 움직이므로, 적을 계속 바라보는 것이 어색할 수 있습니다.
        // 현재 로직은 회피 방향으로 캐릭터가 자연스럽게 향하도록 수정할 필요는 없어 보입니다 (이미 transform.right 기반).
        Debug.Log("행동: 회피 수행!");
        // ... (기존 코드와 동일)
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
        float randomSign = Random.value > 0.5f ? 1f : -1f; // 회피 방향 결정 (오른쪽: 1, 왼쪽: -1)

        // 1. 캐릭터의 로컬 오른쪽 또는 왼쪽 방향을 기준으로 실제 월드 공간에서의 회피 방향 벡터를 구합니다.
        Vector3 evadeDirectionWorld = transform.right * randomSign;
        evadeDirectionWorld.y = 0; // 수평 이동만 하도록 y축 성분 제거
        evadeDirectionWorld.Normalize(); // 방향 벡터 정규화 (길이를 1로 만듦)

        // 2. 캐릭터가 회피 방향을 바라보도록 즉시 회전시킵니다.
        if (evadeDirectionWorld.sqrMagnitude > 0.001f) // 방향 벡터가 거의 0이 아닐 때만 회전 (오류 방지)
        {
            transform.rotation = Quaternion.LookRotation(evadeDirectionWorld);
        }

        float elapsedTime = 0f;
        // Vector3 startPosition = transform.position; // 만약 정확한 이동 거리를 제어하고 싶다면 시작 위치 기록

        // 회피 애니메이션이 있다면, isPerformingAction 플래그가 Update()의 SmoothLookAtEnemy를 막아줄 것입니다.
        // 만약을 위해 isManuallyRotating 플래그를 여기서도 사용할 수 있습니다.
        // isManuallyRotating = true; // EvadeCoroutine 시작 시

        while (elapsedTime < evadeDuration)
        {
            // 3. 정해진 회피 방향(evadeDirectionWorld)으로 이동합니다.
            Vector3 movement = evadeDirectionWorld * (evadeDistance / evadeDuration) * Time.deltaTime;
            rb.MovePosition(transform.position + movement);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // isManuallyRotating = false; // EvadeCoroutine 종료 시
        activeEvadeCoroutine = null;
    }

    public virtual NodeStatus Idle()
    {
        // 대기 상태에서는 Update의 SmoothLookAtEnemy가 자연스럽게 작동하도록 둡니다.
        Debug.Log("행동: 대기 중");
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus GetAttack()
    {
        // ... (기존 코드와 동일)
        Debug.Log(gameObject.name + " - GetAttack: 호출됨 (현재는 아무 동작도 하지 않음)");
        return NodeStatus.SUCCESS;
    }

    private void StopDefendInvincibility()
    {
        // ... (기존 코드와 동일)
        blackboard.EndInvincibility();
        blackboard.defenseInitiationTime = -1f;
        Debug.Log(gameObject.name + " 방어 무적 상태 및 유예 시간 종료.");
    }

    private void StopEvadeInvincibility()
    {
        // ... (기존 코드와 동일)
        blackboard.EndInvincibility();
        Debug.Log(gameObject.name + " 회피 무적 상태 종료.");
    }

    public void HandleDamage(float damage, AgentController attacker)
    {
        // ... (기존 코드와 동일)
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
        // ... (기존 코드와 동일)
        Debug.Log(gameObject.name + "이(가) 죽었습니다.");
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        this.enabled = false;
        Destroy(gameObject, 3f);
    }

    // GetAttackAction 내부 클래스는 Actions.cs 파일에 있으므로 AgentController 내에는 없습니다.
    // public class GetAttackAction : BTActionNode ...
}
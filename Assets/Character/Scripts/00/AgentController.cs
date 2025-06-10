// File: AgentController.cs
using UnityEditor;
using UnityEngine;
using System.Collections;

public abstract class AgentController : MonoBehaviour
{
    [SerializeField] private Transform enemy; // 인스펙터에서 할당
    private float detectionRadius = 20f; // 적 감지 반경
    public float attackRange = 2f;       // 공격 범위
    private float closeRangeThreshold = 1f; // "너무 가까움" 판단 기준 거리
    private float lowHealthThreshold = 30f; // 체력 낮음 판단 기준 (비율 또는 절대값)

    [SerializeField] private float walkSpeed = 5f; // 일반 걷기/이동 속도 추가
    [SerializeField] private float evadeSpeed = 10f; // 회피 이동 속도를 더 빠르게 설정

    [SerializeField] private float evadeMoveDistance = 2.0f; // 회피 시 이동할 거리
    [SerializeField] private float undefendableDuration = 1.0f; // 방어 불가능 상태 지속 시간

    private Rigidbody rb;
    private AnimationController animationController;
    private bool attackFinished = false;
    private bool evadeFinished = false;
    private bool getAttackFinished = false;

    

    private Animator enemyAnimator;

    protected AgentBlackboard _blackboard; // 블랙보드 참조
    public AgentBlackboard blackboard
    {
        get { return _blackboard; }
        set { _blackboard = value; }
    }
    protected BTNode rootNode;             // 행동 트리의 루트 노드

    private Animator animator; // Animator 참조 변수

    private Vector3 evadeTargetPosition;
    private float currentEvadeSpeed; // 이 변수는 이제 evadeSpeed 값을 받게 됩니다.
    private bool isEvadingMovement = false; // 현재 회피 이동 중인지 나타내는 플래그

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f;
        blackboard.currentHealth = blackboard.maxHealth;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        animationController = GetComponent<AnimationController>();
    }

    protected virtual void Start()
    {
        Debug.Log($"[{gameObject.name}] animationController: {animationController}");

        InitializeBehaviorTree();

        if (animationController != null)
        {

            animationController.onAttackFinished += () =>
            {
                attackFinished = true;
            };

            animationController.onEvadeFinished += () =>
            {
                evadeFinished = true;
            };

            animationController.onGetAttackFinished += () =>
            {
                getAttackFinished = true;
            };
        }
    }

    protected abstract void InitializeBehaviorTree();

    protected virtual void FixedUpdate()
    {
        if (enemy == null)
        {
            FindEnemy();
        }

        if (enemy != null)
        {
            float enemyCurrentHealth = 100f;
            AgentController enemyController = enemy.GetComponent<AgentController>();
            if (enemyController != null)
            {
                enemyCurrentHealth = enemyController.blackboard.currentHealth;
                if (enemyController.blackboard.isAttacking)
                {
                    blackboard.lastEnemyAttackTime = Time.time;
                }
            }
            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), enemyCurrentHealth);
        }
        else
        {
            blackboard.enemyTransform = null;
        }

        if (isEvadingMovement)
        {
            // 회피 이동 중에는 evadeSpeed를 사용
            NodeStatus evadeStatus = MoveTowards(evadeTargetPosition, evadeSpeed, 0.1f);
            if (evadeStatus == NodeStatus.SUCCESS)
            {
                EndEvade();
            }
        }


        if (rootNode != null)
        {
            rootNode.Tick();
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
        float currentDistance = Vector3.Distance(transform.position, targetPosition);

        if (currentDistance > stopDistance)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));

            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

            if (animator != null) animator.SetFloat("Speed", speed); // 애니메이션 속도도 같이 조절
            animationController?.PlayWalk();
            return NodeStatus.RUNNING;
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            animationController?.PlayIdle();
            Debug.Log("행동: 목표 지점 도착");
            return NodeStatus.SUCCESS;
        }
    }

    public virtual NodeStatus MoveAwayFrom(Vector3 targetPosition, float speed, float desiredDistance)
    {
        Vector3 direction = (transform.position - targetPosition).normalized;
        float currentDistance = Vector3.Distance(transform.position, targetPosition);

        if (currentDistance >= desiredDistance)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (animator != null) animator.SetFloat("Speed", 0f);
            Debug.Log("행동: 목표 지점으로부터 멀어지는 중 - 완료 (정지)");
            return NodeStatus.SUCCESS;
        }

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        if (animator != null) animator.SetFloat("Speed", speed);
        Debug.Log("행동: 목표 지점으로부터 멀어지는 중");
        return NodeStatus.RUNNING;
    }

    public virtual NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        Debug.Log($"{gameObject.name}이(가) 행동: 공격 수행!");

        if (!blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        if (!blackboard.isAttacking)
        {
            blackboard.isAttacking = true;
            if (enemy != null)
                transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

            if (animator != null)
                animator.SetTrigger("IsAttacking");

            const float chargeSpeedThreshold = 1.0f;
            const float chargeDamageBonus = 2.0f;

            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

            // 돌진 공격은 회피 이동 중에만 적용되도록 조건 추가
            if (isEvadingMovement && forwardSpeed > chargeSpeedThreshold)
            {
                Debug.Log("돌진 공격! 데미지 보너스 적용!");
                damageMultiplier *= chargeDamageBonus;
            }

            StartCoroutine(AttackWithPreDelay(0.5f, damageMultiplier));
        }

        if (!attackFinished)
            return NodeStatus.RUNNING;

        animationController.StopAttack();
        attackFinished = false;
        blackboard.isAttacking = false;
        blackboard.attackCount += 1;
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        return NodeStatus.SUCCESS;
    }

    private IEnumerator AttackWithPreDelay(float delayTime, float damageMultiplier = 1.0f)
    {
        Debug.Log("공격 선딜레이 시작...");
        yield return new WaitForSeconds(delayTime);
        Debug.Log("선딜레이 종료, 공격 실행!");

        float attackDamage = 10f * damageMultiplier;

        if (Physics.SphereCast(transform.position + Vector3.up, 0.8f, transform.forward, out RaycastHit hit, attackRange))
        {
            AgentController enemyController = hit.collider.GetComponentInParent<AgentController>();
            if (enemyController != null && enemyController != this)
            {
                Debug.Log($"{gameObject.name}이(가) {enemy.name}을(를) 공격하여 {attackDamage}(x{damageMultiplier}) 데미지를 입혔습니다.");
                enemyController.HandleDamage(attackDamage);
            }
        }
    }

    public virtual NodeStatus PerformDefend()


    {

        if (blackboard.isAttacking || blackboard.isEvading)
        {
            Debug.Log("현재 다른 행동 중이라 방어 불가");
            return NodeStatus.FAILURE;
        }
        Debug.Log("행동: 방어 수행!");

        

        blackboard.isDefending = true;
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY);
        blackboard.StartInvincibility(blackboard.defendCooldownDuration);

        // [추가] recentlyDefended 플래그 설정 및 리셋 코루틴 시작
        blackboard.recentlyDefended = true;
        StartCoroutine(ResetRecentlyDefendedFlag(2.5f)); // 1.5초 후에 리셋 (회피 쿨타임보다 길게)
                                                         // 이 줄을 추가합니다
        if (animator != null)
        {
            animator.SetTrigger("IsDefending");
        }
        Invoke(nameof(StopDefendInvincibility), 1.0f);

        StartCoroutine(CompleteDefendActionAfterDelay(1.0f));

        blackboard.defendCount += 1;
        return NodeStatus.SUCCESS;
    }

    // [추가] 플래그 리셋 코루틴
    private IEnumerator ResetRecentlyDefendedFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
        blackboard.recentlyDefended = false;
    }

    // [추가] 방어 행동 완료를 지연시키는 코루틴
    private IEnumerator CompleteDefendActionAfterDelay(float delayTime)
    {

        yield return new WaitForSeconds(delayTime);
        blackboard.isDefending = false;

    }

    public virtual NodeStatus PerformChangeDefendToAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsDefendSuccess");
            animator.SetTrigger("ChangeDefendToAttack");
        }
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformEvade()
    {
        // 이 메서드는 랜덤 방향 회피를 시작하는 역할만 하도록 하고,
        // 실제 이동 로직은 PerformDirectionalEvade로 위임
        if (blackboard.isAttacking || blackboard.isDefending)
        {
            Debug.Log("현재 다른 행동 중이라 회피 불가");
            return NodeStatus.FAILURE;
        }

            if (!blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        Debug.Log("행동: 무작위 방향으로 회피 수행!");
        
        return PerformDirectionalEvade(Random.Range(0, 2)); // 0:전방, 1:후방, 2:좌, 3:우
    }

    public virtual NodeStatus PerformDirectionalEvade(int direction)
    {
        if (!blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
            return NodeStatus.FAILURE;
        

        // 이미 회피 이동 중이면 중복 시작 방지
        if (isEvadingMovement)
        {
            return NodeStatus.RUNNING;
        }

        Debug.Log($"행동: {direction} 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);

        blackboard.isEvading = true;

        Vector3 initialPosition = transform.position;
        Vector3 targetDirection = Vector3.zero;

        if (enemy != null) // 적이 있을 경우에만 상대방 기준으로 회피 방향 설정
        {
            Vector3 directionToEnemy = (enemy.position - transform.position);
            directionToEnemy.y = 0; // Y축은 무시하고 수평 방향으로만 계산
            directionToEnemy.Normalize(); // 단위 벡터로 정규화

            switch (direction)
            {
                case 0: // Forward (상대방을 향하는 앞)
                    targetDirection = directionToEnemy;
                    // [핵심 수정]: 상대방의 방어 불가능 상태 설정
                    AgentController enemyAgentController = enemy.GetComponent<AgentController>();
                    if (enemyAgentController != null)
                    {
                        enemyAgentController.blackboard.canBeDefended = false;
                        Invoke(nameof(ResetEnemyDefendableState), undefendableDuration); // 일정 시간 후 방어 가능 상태로 되돌림
                        Debug.Log($"상대방 ({enemy.name})을(를) 향해 회피, 일시적으로 방어 불가능 상태로 설정됨.");
                    }
                    break;
                case 1: // Backward (상대방으로부터 멀어지는 뒤)
                    targetDirection = -directionToEnemy;
                    break;
                
            }
        }
        else // 적이 없을 경우, 기존처럼 자신을 기준으로 설정 (비상 fallback)
        {
            Debug.LogWarning("적을 찾을 수 없어 자신을 기준으로 회피 방향을 설정합니다.");
            switch (direction)
            {
                case 0: // Forward
                    targetDirection = transform.forward;
                    break;
                case 1: // Backward
                    targetDirection = -transform.forward;
                    break;
                
            }
        }

        evadeTargetPosition = initialPosition + targetDirection * evadeMoveDistance;
        currentEvadeSpeed = evadeSpeed; // 회피 시 더 빠른 속도 적용

        if (animator != null)
        {
            animator.SetFloat("Speed", currentEvadeSpeed);
            animator.SetBool("isRun", true); // 'isRun' 파라미터가 있다면 활용
        }

        isEvadingMovement = true; // 회피 이동 시작 플래그 설정

        animationController?.PlayWalk(); // 회피는 '빠르게 걷는' 모션으로 연출

        return NodeStatus.RUNNING; // 이동이 완료될 때까지 RUNNING 반환
    }

    private void ResetEnemyDefendableState()
    {
        if (enemy != null)
        {
            AgentController enemyAgentController = enemy.GetComponent<AgentController>();
            if (enemyAgentController != null)
            {
                enemyAgentController.blackboard.canBeDefended = true;
                Debug.Log($"상대방 ({enemy.name})의 방어 불가능 상태가 해제되었습니다.");
            }
        }
    }

    private void EndEvade()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("isRun", false);
        }

        evadeFinished = true;
        blackboard.isEvading = false;
        isEvadingMovement = false; // 회피 이동 종료 플래그 해제

        Debug.Log("회피 이동 종료!");
    }

    public virtual NodeStatus Idle()
    {
        Debug.Log("행동: 대기 중");

        if (enemy != null)
            transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
        return NodeStatus.SUCCESS;
    }

    private void StopDefendInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("방어 무적 상태 종료.");
    }

    public virtual NodeStatus GetAttack()
    {
        animationController.PlayGetAttack();
        if (!getAttackFinished)
            return NodeStatus.RUNNING;

        animationController.StopGetAttack();
        getAttackFinished = false;
        return NodeStatus.SUCCESS;
    }

    public void HandleDamage(float damage)
    {
        // [수정]: blackboard.isInvincible && blackboard.canBeDefended 조건 추가
        // 무적 상태이거나 방어 불가능 상태가 아니라면 데미지를 받음
        if (blackboard.isInvincible)
        {
            Debug.Log(gameObject.name + "이(가) 공격을 무효화했습니다 (무적 상태).");
            return;
        }

        if (!blackboard.canBeDefended) // 방어 불가능 상태일 경우 방어 메커니즘을 무시하고 데미지 적용
        {
            Debug.Log(gameObject.name + "이(가) 방어 불가능 상태이므로 공격을 막을 수 없습니다.");
            blackboard.TakeDamage(damage); // Blackboard의 TakeDamage는 이제 canBeDefended를 내부적으로 확인
        }
        else // 무적 상태도 아니고 방어 불가능 상태도 아니면, 일반적인 데미지 처리
        {
            blackboard.TakeDamage(damage);
        }

        if (blackboard.isDead)
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
    }

    private void ResetIsAttacking()
    {
        blackboard.isAttacking = false;
    }

    public virtual NodeStatus PerformProactiveAttack()
    {
        Debug.Log("행동: 선제 공격 수행!");

        if (!blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        blackboard.isAttacking = true;

        if (enemy != null)
            transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }

        Invoke(nameof(ResetIsAttacking), 0.3f);

        float attackDamage = 40f;
        if (Physics.SphereCast(transform.position + Vector3.up, 0.8f, transform.forward, out RaycastHit hit, attackRange))
        {
            AgentController enemyController = hit.collider.GetComponentInParent<AgentController>();
            if (enemyController != null && enemyController != this)
            {
                Debug.Log($"{gameObject.name}이(가) {enemy.name}에게 선제공격으로 {attackDamage} 데미지를 입혔습니다.");
                enemyController.HandleDamage(attackDamage);
            }
        }

        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        return NodeStatus.SUCCESS;
    }

    public void ResetAllFlags()
    {
        attackFinished = false;
        evadeFinished = false;
        getAttackFinished = false;
        isEvadingMovement = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
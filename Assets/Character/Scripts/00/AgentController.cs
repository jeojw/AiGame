// File: AgentController.cs (AgentController.cs 파일)
using UnityEditor;
using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    [SerializeField] private Transform enemy; // 인스펙터에서 할당
    private float detectionRadius = 20f; // 적 감지 반경
    protected float attackRange = 2f;      // 공격 범위
    private float closeRangeThreshold = 1f; // "너무 가까움" 판단 기준 거리
    private float lowHealthThreshold = 30f; // 체력 낮음 판단 기준 (비율 또는 절대값)
    private float evadeDistance = 2.0f; // 이 변수는 이제 사용하지 않거나 대시 거리로 활용할 수 있습니다.
    private float dashSpeed = 15f; // [추가] 대시 속도를 위한 변수

    private Rigidbody rb;
    private AnimationController animationController;
    private bool attackFinished = false;
    private bool evadeFinished = false;
    private bool getAttackFinished = false;

    // [추가] 적의 Animator를 캐싱하기 위한 변수
    private Animator enemyAnimator;

    protected AgentBlackboard _blackboard; // 블랙보드 참조
    public AgentBlackboard blackboard
    {
        get { return _blackboard; }
        set { _blackboard = value; }
    }
    protected BTNode rootNode;            // 행동 트리의 루트 노드

    private Animator animator; // Animator 참조 변수

    // --- 방어/회피 상태 플래그 추가 ---
    private bool isDefending = false;
    private bool isEvading = false;
    private float defendDamageReduction = 0.5f; // 방어 시 대미지 50%만 받음

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f; // 문서에 따라 설정
        blackboard.currentHealth = blackboard.maxHealth;

        animator = GetComponent<Animator>(); // Animator 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();
        animationController = GetComponent<AnimationController>();
    }

    protected virtual void Start()
    {
        Debug.Log($"[{gameObject.name}] animationController: {animationController}");

        InitializeBehaviorTree(); // 행동 트리 초기화

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

    // 행동 트리를 초기화하는 추상 메소드 (파생 클래스에서 구현)
    protected abstract void InitializeBehaviorTree();

    protected virtual void FixedUpdate()
    {
        if (enemy == null)
        {
            FindEnemy(); // 적이 없으면 찾기
        }

        // 블랙보드 업데이트
        if (enemy != null)
        {
            // [수정됨] 적의 실제 체력을 가져오도록 수정
            float enemyCurrentHealth = 100f; // 기본값
            AgentController enemyController = enemy.GetComponent<AgentController>();
            if (enemyController != null)
            {
                enemyCurrentHealth = enemyController.blackboard.currentHealth;

                // "Attack" 태그를 가진 애니메이션 상태가 실행 중이면
                if (enemyController.blackboard.isAttacking)
                {
                    // 마지막 공격 시간을 현재 시간으로 갱신
                    blackboard.lastEnemyAttackTime = Time.time;
                }
            }
            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), enemyCurrentHealth);
        }
        else
        {
            blackboard.enemyTransform = null; // 적 없음
        }

        if (rootNode != null) // 루트 노드가 있다면
        {
            rootNode.Tick(); // 행동 트리 실행
        }
    }

    void FindEnemy()
    {
        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy"); // "Enemy" 태그로 적 찾기
        if (enemyObject != null)
        {
            enemy = enemyObject.transform;
        }
    }

    // --- 행동 메소드 (ActionNode에서 호출됨) ---
    public virtual NodeStatus MoveTowards(Vector3 targetPosition, float speed, float stopDistance)
    {
        float currentDistance = Vector3.Distance(transform.position, targetPosition);

        if (currentDistance > stopDistance)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z)); // Y축 고정하여 바라보기

            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

            if (animator != null) animator.SetFloat("Speed", speed);
            Debug.Log("행동: 목표 지점으로 이동 중 (CharacterController)");

            animationController?.PlayWalk(); // Null check 추가
            return NodeStatus.RUNNING;
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0f);

            // 도착했을 경우
            animationController?.PlayIdle(); // 도착했으니 대기 모션
            Debug.Log("행동: 목표 지점 도착");
            return NodeStatus.SUCCESS;
        }
    }

    public virtual NodeStatus MoveAwayFrom(Vector3 targetPosition, float speed, float desiredDistance)
    {
        Vector3 direction = (transform.position - targetPosition).normalized;
        float currentDistance = Vector3.Distance(transform.position, targetPosition);

        // 목표 거리보다 충분히 멀면 멈추기
        if (currentDistance >= desiredDistance)
        {
            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

            if (animator != null) animator.SetFloat("Speed", 0f);

            Debug.Log("행동: 목표 지점으로부터 멀어지는 중 - 완료");

            return NodeStatus.SUCCESS; // 거리 확보 완료
        }

        // 아직 거리 부족하면 이동
        Vector3 moveVector = direction * speed * Time.deltaTime;
        transform.LookAt(transform.position + direction);

        if (animator != null) animator.SetFloat("Speed", speed);

        Debug.Log("행동: 목표 지점으로부터 멀어지는 중");

        return NodeStatus.RUNNING; // 이동 중임을 알림
    }

    public virtual NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        // 인터럽트
        if (blackboard.enemyHealth > 0 && blackboard.enemyTransform != null)
        {
            AgentController enemyController = blackboard.enemyTransform.GetComponent<AgentController>();
            if (enemyController != null && enemyController.blackboard.isAttacking)
            {
                Debug.Log("공격 중 적이 반격함 → 공격 중단하고 회피/방어 고려");
                blackboard.isAttacking = false;
                attackFinished = false;
                return NodeStatus.FAILURE; // 공격 행동 실패로 간주하고 상위 BT가 다시 판단하게
            }
        }

        Debug.Log("행동: 공격 수행!");

        if (!blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        if (!blackboard.isAttacking)
        {
            blackboard.isAttacking = true;
            if (enemy != null)
                transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

            if (animator != null)
                animator.SetTrigger("IsAttacking");

            // --- [추가 시작] 돌진 공격 보너스 로직 ---
            const float chargeSpeedThreshold = 1.0f; // '돌진'으로 인정할 최소 전진 속도
            const float chargeDamageBonus = 1.5f;    // 돌진 시 추가 데미지 배율 (기존 배율에 곱해짐)

            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

            if (forwardSpeed > chargeSpeedThreshold)
            {
                Debug.Log("돌진 공격! 데미지 보너스 적용!");
                damageMultiplier *= chargeDamageBonus;
            }
            // --- [추가 끝] ---

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

        if (!attackFinished)
            return NodeStatus.RUNNING;

        animationController.StopAttack();
        attackFinished = false;
        blackboard.isAttacking = false;
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        return NodeStatus.SUCCESS;
    }

    // ----------- 방어/회피 로직 수정 시작 -----------
    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("행동: 방어 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY);

        isDefending = true;

        if (animator != null)
        {
            animator.SetTrigger("IsDefending");
        }
        Invoke(nameof(StopDefend), blackboard.defendCooldownDuration);
        return NodeStatus.SUCCESS;
    }

    private void StopDefend()
    {
        isDefending = false;
        Debug.Log("방어 상태 종료.");
    }

    public virtual NodeStatus PerformEvade()
    {
        Debug.Log("행동: 무작위 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);

        int randomDirection = Random.Range(0, 4);
        PerformDirectionalDash(randomDirection);

        isEvading = true;
        Invoke(nameof(StopEvade), blackboard.evadeDuration);
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformDirectionalEvade(int direction)
    {
        if (!blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        Debug.Log($"행동: {direction} 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);
        PerformDirectionalDash(direction);

        isEvading = true;
        Invoke(nameof(StopEvade), blackboard.evadeDuration);
        return NodeStatus.SUCCESS;
    }

    private void StopEvade()
    {
        isEvading = false;
        Debug.Log("회피 상태 종료.");
    }
    // ----------- 방어/회피 로직 수정 끝 -----------

    private void PerformDirectionalDash(int direction)
    {
        Vector3 dashVector = Vector3.zero;

        switch (direction)
        {
            case 0: // Forward
                dashVector = transform.forward;
                break;
            case 1: // Backward
                dashVector = -transform.forward;
                break;
            case 2: // Left
                dashVector = -transform.right;
                break;
            case 3: // Right
                dashVector = transform.right;
                break;
        }

        if (animator != null)
        {
            animator.SetTrigger("IsEvading");
        }

        if (rb != null)
        {
            rb.AddForce(dashVector * dashSpeed, ForceMode.Impulse);
        }
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

    public virtual NodeStatus PerformChangeDefendToAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsDefendSuccess");
            animator.SetTrigger("ChangeDefendToAttack");
        }
        return NodeStatus.SUCCESS;
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

    // ----------- 대미지 처리 로직 수정 시작 -----------
    public void HandleDamage(float damage)
    {
        if (isEvading)
        {
            Debug.Log(gameObject.name + "이(가) 회피로 공격을 완전히 무효화했습니다.");
            return;
        }
        if (isDefending)
        {
            float reducedDamage = damage * defendDamageReduction;
            Debug.Log(gameObject.name + $"이(가) 방어로 인해 피해를 {reducedDamage} (원래 {damage})만큼만 받았습니다.");
            blackboard.TakeDamage(reducedDamage);
        }
        else
        {
            blackboard.TakeDamage(damage);
        }

        if (blackboard.currentHealth <= 0)
        {
            Die();
        }
    }
    // ----------- 대미지 처리 로직 수정 끝 -----------

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

        Invoke(nameof(ResetIsAttacking), 1.5f);

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
}
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

    private Vector3 initialPosition; // [추가] 초기 위치 저장을 위해
    private Quaternion initialRotation; // [추가] 초기 회전 저장을 위해

    [SerializeField] private float damageInvincibilityDuration = 0.1f; // 데미지 처리 후 짧은 무적 시간 (조절 필요)



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

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f; // 문서에 따라 설정
        blackboard.currentHealth = blackboard.maxHealth;

        animator = GetComponent<Animator>(); // Animator 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();
        animationController = GetComponent<AnimationController>();

        // [추가] 에피소드 시작 시의 위치/회전 저장
        initialPosition = transform.position;
        initialRotation = transform.rotation;
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

            // [수정] 죽었을 때는 아무 처리도 하지 않도록 맨 위에 추가
            if (blackboard.isDead)
            {
                return;
            }

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
        //Debug.Log($"현재 거리: {currentDistance}, 목표 거리: {stopDistance}");

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

        // 안전 장치: 정상 흐름이라면 여기까지 절대 도달하지 않음
        Debug.LogWarning("이동 행동에서 비정상 경로로 도달함");
        return NodeStatus.FAILURE;
    }


    // [수정]
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


    // [수정] 메소드 시그니처에 데미지 배율 인자 추가
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
            // --- [수정 시작] ---

            // 1. 돌진 공격 보너스를 먼저 계산합니다.
            const float chargeSpeedThreshold = 1.0f;
            const float chargeDamageBonus = 1.5f;
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (forwardSpeed > chargeSpeedThreshold)
            {
                Debug.Log("돌진 공격! 데미지 보너스 적용!");
                damageMultiplier *= chargeDamageBonus;
            }

            // 2. 최종 계산된 배율을 블랙보드에 저장합니다.
            // (반격의 경우, damageMultiplier에 이미 3.0f가 들어온 상태에서 이 로직을 타게 됩니다)
            blackboard.currentAttackMultiplier = damageMultiplier;
            Debug.Log($"최종 공격 배율 {blackboard.currentAttackMultiplier}을 블랙보드에 저장.");

            // --- [수정 끝] ---

            blackboard.isAttacking = true;
            if (enemy != null)
                transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

            if (animator != null)
                animator.SetTrigger("IsAttacking");
        }


        // 공격이 아직 끝나지 않았으면 RUNNING
        if (!attackFinished)
            return NodeStatus.RUNNING;

        // 공격 종료 시점
        animationController.StopAttack();
        attackFinished = false;
        // [추가] 공격이 끝났으므로, 배율을 기본값 1.0f로 초기화합니다.
        blackboard.currentAttackMultiplier = 1.0f;

        // 공격 중 종료 플래그
        blackboard.isAttacking = false;

        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("행동: 방어 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY); // 방어 쿨타임 설정
                                                                           //blackboard.StartInvincibility(blackboard.defendCooldownDuration); // 방어 시간 동안 무적

        // [추가] 방어 상태 플래그를 true로 설정
        blackboard.isDefending = true;

        if (animator != null)
        {
            animator.SetTrigger("IsDefending");
        }

        // [추가] 방어 지속시간(예: 1초)이 지나면 방어 상태를 해제하는 메소드를 예약
        // 이 시간은 실제 방어 애니메이션 길이에 맞게 조절하는 것이 가장 좋습니다.
        Invoke(nameof(StopDefendInvincibility), 0.7f);

        //Invoke(nameof(StopDefendInvincibility), blackboard.defendCooldownDuration);
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

    public virtual NodeStatus PerformEvade()
    {
        Debug.Log("행동: 무작위 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);

        // 0:앞, 1:뒤, 2:왼쪽, 3:오른쪽 중 하나를 무작위로 선택
        int randomDirection = Random.Range(0, 4);
        PerformDirectionalDash(randomDirection); // 아래에서 만들 새로운 메소드 호출

        return NodeStatus.SUCCESS;
    }

    // [추가] RL 에이전트가 방향을 지정하여 호출할 메소드
    public virtual NodeStatus PerformDirectionalEvade(int direction)
    {
        if (!blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        Debug.Log($"행동: {direction} 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);
        PerformDirectionalDash(direction);
        return NodeStatus.SUCCESS;
    }

    // [추가] 실제 물리적인 대시를 처리하는 메소드
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



    }

    public virtual NodeStatus Idle()
    {
        //Debug.Log("행동: 대기 중");

        // [추가] 대기중 서로 상대방을 보도록
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
        if (animator != null)
        {
            animator.SetTrigger("IsDefendSuccess");
        }
        blackboard.EndInvincibility();
        blackboard.isDefending = false; // <--- 이 줄을 추가해야 합니다!
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
        if (blackboard.isInvincible) // 방어나 회피 중에는 데미지 무효화
        {
            Debug.Log(gameObject.name + "이(가) 공격을 무효화했습니다.");
            return;
        }

        blackboard.TakeDamage(damage);

        // --- 데미지를 받았으니 즉시 짧은 무적 상태로 진입합니다. ---
        blackboard.StartInvincibility(damageInvincibilityDuration);
        Invoke(nameof(StopDamageInvincibility), damageInvincibilityDuration);
        // --------------------------------------------------------


        if (blackboard.currentHealth <= 0)
        {
            Die(); // 체력이 0 이하이면 죽음 처리
        }
    }

    // [추가] HandleDamage에서 설정한 짧은 무적 시간을 해제하는 메소드
    private void StopDamageInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log($"[{gameObject.name}] 데미지 무적 상태 종료.");
    }

    protected virtual void Die()
    {
        Debug.Log(gameObject.name + "이(가) 죽었습니다.");

        blackboard.isDead = true; // [수정] isDead 플래그를 true로 설정

        if (animator != null)
        {
            animator.SetTrigger("Die"); // 죽음 애니메이션 실행
        }

        // 스크립트 비활성화하여 더 이상 행동하지 않도록 함
        //this.enabled = false;

        // 일정 시간 후 오브젝트 파괴
        //Destroy(gameObject, 3f);
    }

    /// <summary>
    /// [추가] 에이전트의 상태를 초기화하고 부활시키는 메소드
    /// </summary>
    public virtual void ResetAgent()
    {
        Debug.Log($"[{gameObject.name}] 에이전트 상태를 리셋합니다.");

        // 상태 리셋
        blackboard.isDead = false;
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.isAttacking = false;
        // 필요에 따라 다른 블랙보드 상태들도 초기화

        // 물리 상태 리셋
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 위치 및 회전 리셋
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // 애니메이터 리셋 (가장 중요!)
        // 'Die' 상태에서 벗어나 기본 상태(예: Idle)로 돌아가도록 합니다.
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // 스크립트가 비활성화되었을 경우를 대비해 활성화 (안전장치)
        if (!this.enabled)
        {
            this.enabled = true;
        }
    }

    // [추가] isAttacking 플래그를 리셋하기 위한 메소드
    private void ResetIsAttacking()
    {
        blackboard.isAttacking = false;
    }

    public virtual NodeStatus PerformProactiveAttack()
    {
        Debug.Log("행동: 선제 공격 수행!");

        if (!blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        // --- [수정 시작] ---

        // 1. 자신의 상태를 '공격 중'으로 변경
        blackboard.isAttacking = true;

        if (enemy != null)
            transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }

        // 2. 공격 애니메이션이 끝날 시간 즈음에 isAttacking 플래그를 false로 되돌리도록 예약
        //    (애니메이션 길이에 맞춰 1.5f 값을 조정하세요)
        Invoke(nameof(ResetIsAttacking), 1.5f);

        // 데미지 판정 로직 (기존과 동일)
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

        // --- [수정 끝] ---

        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        return NodeStatus.SUCCESS;
    }
}
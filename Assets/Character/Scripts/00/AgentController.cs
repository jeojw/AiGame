// File: AgentController.cs (AgentController.cs 파일)
using UnityEditor;
using UnityEditor.Searcher;
using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    [SerializeField] private Transform enemy; // 인스펙터에서 할당
    private float detectionRadius = 20f; // 적 감지 반경
    protected float attackRange = 2f;      // 공격 범위
    private float closeRangeThreshold = 1f; // "너무 가까움" 판단 기준 거리
    private float lowHealthThreshold = 30f; // 체력 낮음 판단 기준 (비율 또는 절대값)
    private float dashSpeed = 15f; // [추가] 대시 속도를 위한 변수

    private Rigidbody rb;
    private AnimationController animationController;
    private bool attackFinished = false;
    private bool defendFinished = false;
    private bool evadeFinished = false;
    private bool getAttackFinished = false;

    private float evadeDuration = 1.5f; // 회피 지속 시간 예시
    private float evadeTimer = 0f;

    private HitboxController hitbox;

    // [추가] 적의 Animator를 캐싱하기 위한 변수
    private AgentController _enemyController;

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
        blackboard.owner = this;
        blackboard.maxHealth = 100f; // 문서에 따라 설정 [cite: 10]
        blackboard.currentHealth = blackboard.maxHealth;

        animator = GetComponent<Animator>(); // Animator 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();
        animationController = GetComponent<AnimationController>();
        hitbox = GetComponent<HitboxController>();
    }

    protected virtual void Start()
    {
        InitializeBehaviorTree(); // 행동 트리 초기화

        if (hitbox != null)
        {
            hitbox.OnHitReceived += OnHitDetected;
            hitbox.OnBlockReceived += OnBlockDetected;
        }


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
            _enemyController = enemy.GetComponent<AgentController>();
            if (_enemyController != null)
            {
                enemyCurrentHealth = _enemyController.blackboard.currentHealth;

                // "Attack" 태그를 가진 애니메이션 상태가 실행 중이면
                if (_enemyController.blackboard.isAttacking)
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

        rootNode?.Tick(); // 행동 트리 실행
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
        //// 인터럽트
        //if (blackboard.enemyHealth > 0 && blackboard.enemyTransform != null)
        //{
        //    AgentController enemyController = blackboard.enemyTransform.GetComponent<AgentController>();
        //    if (enemyController != null && enemyController.blackboard.isAttacking)
        //    {
        //        Debug.Log("공격 중 적이 반격함 → 공격 중단하고 회피/방어 고려");
        //        blackboard.isAttacking = false;
        //        attackFinished = false;
        //        return NodeStatus.FAILURE; // 공격 행동 실패로 간주하고 상위 BT가 다시 판단하게
        //    }
        //}

        Debug.Log("행동: 공격 수행!");

        if (!blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        if (!blackboard.isAttacking)
        {
            blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

            blackboard.isAttacking = true;
            blackboard.isDefending = false;
            if (enemy != null)
                transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

            if (animator != null)
                animator.SetTrigger("IsAttacking");

            // --- [추가 시작] 돌진 공격 보너스 로직 ---
            const float chargeSpeedThreshold = 1.0f; // '돌진'으로 인정할 최소 전진 속도
            const float chargeDamageBonus = 1.5f;    // 돌진 시 추가 데미지 배율 (기존 배율에 곱해짐)

            // 현재 캐릭터가 바라보는 정면 방향으로의 속도를 계산합니다.
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

            // 만약 전진 속도가 기준치 이상이면, 돌진 보너스를 적용합니다.
            if (forwardSpeed > chargeSpeedThreshold)
            {
                Debug.Log("돌진 공격! 데미지 보너스 적용!");
                damageMultiplier *= chargeDamageBonus; // 기존 배율에 추가 배율을 곱합니다.
            }
            // 데미지 계산 시 최종 배율을 곱해줍니다.
            float attackDamage = 10f * damageMultiplier;
            blackboard.canCounterAttack = false;

            enemy.GetComponent<AgentController>().blackboard.totalDamage = attackDamage;
        }

        //if (enemy.GetComponent<AgentController>().blackboard.canCounterAttack)
        //{
        //    if (animator != null)
        //    {
        //        animator.SetTrigger("AttackCancel");
        //        animator.ResetTrigger("isDefending");
        //    }
                
        //    blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        //    return NodeStatus.SUCCESS;
        //}
            

        // 공격이 아직 끝나지 않았으면 RUNNING
        if (!attackFinished)
            return NodeStatus.RUNNING;

        // 공격 종료 시점
        animationController.StopAttack();
        attackFinished = false;

        // 공격 중 종료 플래그
        blackboard.isAttacking = false;
        blackboard.canCounterAttack = false;

        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("행동: 방어 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY); // 방어 쿨타임 설정 [cite: 10]
        if (!blackboard.isInvincible)
        {
            blackboard.StartInvincibility(blackboard.defendCooldownDuration);
            CancelInvoke(nameof(StopDefendInvincibility));
            Invoke(nameof(StopDefendInvincibility), blackboard.defendCooldownDuration);
        }

        blackboard.isDefending = true;

        if (animator != null)
        {
            animator.SetTrigger("IsDefending");
        }
        

        if (!blackboard.canCounterAttack)
        {
            return NodeStatus.RUNNING;
        }
        else
        {
            animator.ResetTrigger("IsDefending");
            hitbox.ResetBlockFlag();
        }

        blackboard.isDefending = false;

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
        if (!blackboard.isEvading)
        {
            // 회피 시작
            Debug.Log("행동: 무작위 방향으로 회피 수행!");
            blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);
            blackboard.isEvading = true;
            evadeTimer = 0f;

            if (animator != null)
            {
                animator.SetTrigger("IsEvading");
            }
        }

        if (blackboard.isEvading)
        {
            evadeTimer += Time.deltaTime;

            if (evadeTimer >= evadeDuration)
            {
                // 회피 완료
                blackboard.isEvading = false;
                return NodeStatus.SUCCESS;
            }
            else
            {
                // 회피 중
                return NodeStatus.RUNNING;
            }
        }

        return NodeStatus.FAILURE; // 예외 상황
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

        if (animator != null)
        {
            // 모든 방향 대시에 동일한 회피 애니메이션 Trigger를 사용할 수 있습니다.
            animator.SetTrigger("IsEvading");
        }

        // 순간적인 힘을 가해 캐릭터를 밀어냅니다.
        if (rb != null)
        {
            rb.AddForce(dashVector * dashSpeed, ForceMode.Impulse);
        }
    }
    public virtual NodeStatus Idle()
    {
        Debug.Log("행동: 대기 중");
        
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
        blackboard.EndInvincibility();
        Debug.Log("방어 무적 상태 종료.");
    }

    void OnHitDetected()
    {
        blackboard.isGetAttacked = true;
        blackboard.totalDamage = 10f;
    }

    void OnBlockDetected()
    {
        blackboard.canCounterAttack = true;
        blackboard.totalDamage = 0;
    }


    public virtual NodeStatus GetAttack()
    {
        Debug.Log($"{enemy.name}이(가) {gameObject.name}로부터 피격당하여 {blackboard.totalDamage} 데미지를 입었습니다.");
        blackboard.TakeDamage();

        blackboard.isGetAttacked = false;
        hitbox.ResetHitFlag();

        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus Dead()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        return NodeStatus.SUCCESS;
    }

    protected virtual void Die()
    {
        Debug.Log(gameObject.name + "이(가) 죽었습니다.");
        if (animator != null)
        {
            animator.SetTrigger("Die"); // 죽음 애니메이션 실행
        }

        // 스크립트 비활성화하여 더 이상 행동하지 않도록 함
        this.enabled = false;

        // 일정 시간 후 오브젝트 파괴
        //Destroy(gameObject, 3f);
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

        float attackDamage = 40f;

        enemy.GetComponent<AgentController>().blackboard.totalDamage = attackDamage;

        // 2. 공격 애니메이션이 끝날 시간 즈음에 isAttacking 플래그를 false로 되돌리도록 예약
        //    (애니메이션 길이에 맞춰 1.5f 값을 조정하세요)
        Invoke(nameof(ResetIsAttacking), 1.5f);

        // --- [수정 끝] ---

        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        return NodeStatus.SUCCESS;
    }
}
// File: AgentController.cs (AgentController.cs 파일)
using UnityEditor;
using UnityEngine;
//using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public abstract class AgentController : MonoBehaviour
{
    [SerializeField] private Transform enemy; // 인스펙터에서 할당
    private float detectionRadius = 20f; // 적 감지 반경
    public float attackRange = 2f;      // 공격 범위
    private float closeRangeThreshold = 1f; // "너무 가까움" 판단 기준 거리
    private float lowHealthThreshold = 30f; // 체력 낮음 판단 기준 (비율 또는 절대값)
    private float evadeDistance = 5.0f; // 이 변수는 이제 사용하지 않거나 대시 거리로 활용할 수 있습니다.
    private float dashSpeed = 10f; // [추가] 대시 속도를 위한 변수

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

            // --- [추가 시작] 돌진 공격 보너스 로직 ---
            const float chargeSpeedThreshold = 1.0f; // '돌진'으로 인정할 최소 전진 속도
            const float chargeDamageBonus = 2.0f;    // 돌진 시 추가 데미지 배율 (기존 배율에 곱해짐)

            // 현재 캐릭터가 바라보는 정면 방향으로의 속도를 계산합니다.
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

            // 만약 전진 속도가 기준치 이상이면, 돌진 보너스를 적용합니다.
            if (forwardSpeed > chargeSpeedThreshold)
            {
                Debug.Log("돌진 공격! 데미지 보너스 적용!");
                damageMultiplier *= chargeDamageBonus; // 기존 배율에 추가 배율을 곱합니다.
            }
            // --- [추가 끝] ---


            // 데미지 계산 시 최종 배율을 곱해줍니다.
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

        // 공격이 아직 끝나지 않았으면 RUNNING
        if (!attackFinished)
            return NodeStatus.RUNNING;

        // 공격 종료 시점
        animationController.StopAttack();
        attackFinished = false;

        // 공격 중 종료 플래그
        blackboard.isAttacking = false;

        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("행동: 방어 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY); // 방어 쿨타임 설정
        blackboard.StartInvincibility(blackboard.defendCooldownDuration); // 방어 시간 동안 무적

        if (animator != null)
        {
            animator.SetTrigger("IsDefending");
        }
        Invoke(nameof(StopDefendInvincibility), blackboard.defendCooldownDuration);
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
        if (!blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        Debug.Log("행동: 무작위 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);
        int randomDirection = Random.Range(0, 4);

        // 회피 시작 시 플래그 설정
        blackboard.isEvading = true; // Blackboard에도 isEvading 상태를 추가하여 관리
        evadeFinished = false; // 애니메이션 이벤트로 리셋될 플래그 초기화

        PerformDirectionalDash(randomDirection);
        //animationController?.PlayEvade(); // 회피 애니메이션 재생 (AnimationController에 PlayEvade 추가 필요)

        return NodeStatus.RUNNING; // 즉시 성공을 반환하지 않고 RUNNING 반환
    }

    // [수정] RL 에이전트가 방향을 지정하여 호출할 메소드에 속도와 거리 인자 추가
    public virtual NodeStatus PerformDirectionalEvade(int direction)
    {
        if (!blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        Debug.Log($"행동: {direction} 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);

        // 회피 시작 시 플래그 설정
        blackboard.isEvading = true; // Blackboard에도 isEvading 상태를 추가하여 관리
        evadeFinished = false; // 애니메이션 이벤트로 리셋될 플래그 초기화

        PerformDirectionalDash(direction);
        //animationController?.PlayEvade(); // 회피 애니메이션 재생

        return NodeStatus.RUNNING; // 즉시 성공을 반환하지 않고 RUNNING 반환
    }

    // [수정] 실제 물리적인 대시를 처리하는 메소드에 속도와 거리 인자 추가 및 대시 로직 구현
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



        // Rigidbody를 사용하여 대시 즉시 적용
        // 기존 속도에 대시 벡터를 추가하여 순간적으로 큰 힘을 줍니다.
        // ForceMode.Impulse를 사용하여 순간적인 힘을 가합니다.
        rb.AddForce(dashVector * dashSpeed, ForceMode.Impulse);

        // 일정 시간 후 대시 종료 처리 (예: 애니메이션 길이에 맞춰)
        // 실제로는 애니메이션 이벤트나 콜백을 통해 정확한 종료 시점을 잡는 것이 좋습니다.
        Invoke(nameof(EndEvade), 1.0f); // 0.5초 후 대시 종료 (임시 값, 애니메이션 길이에 따라 조절)
    }

    private void EndEvade()
    {
        // 대시 종료 후 필요한 로직 (예: 속도 초기화, 애니메이션 종료 등)
        rb.linearVelocity = Vector3.zero; // 대시 후 속도 초기화
        evadeFinished = true; // 회피 완료 플래그 설정
        Debug.Log("대시 종료!");
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
        if (blackboard.currentHealth <= 0)
        {
            Die(); // 체력이 0 이하이면 죽음 처리
        }
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

    // [추가] 모든 내부 플래그를 초기화하는 메서드
    public void ResetAllFlags()
    {
        attackFinished = false;
        evadeFinished = false;
        getAttackFinished = false;
        // Blackboard의 플래그는 RL 에이전트의 OnEpisodeBegin에서 직접 초기화합니다.
        // Rigidbody의 속도도 초기화하여 이전 에피소드 잔여 움직임을 제거
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
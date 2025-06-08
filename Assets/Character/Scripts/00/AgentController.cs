// File: AgentController.cs (AgentController.cs 파일)
using UnityEditor;
using UnityEngine;
//using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using System.Collections; // Coroutine 사용을 위해 추가

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

    // --- 추가될 변수 ---
    [SerializeField] private float attackPreDelay = 0.3f; // 공격 선딜레이 시간 (조절 가능)
    private bool isPreDelayingAttack = false; // 현재 공격 선딜레이 중인지 여부

    // ... (기존 변수들)

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
        blackboard.maxHealth = 100f; // 문서에 따라 설정 [cite: 10]
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


    // Scripts/00/AgentController.cs 파일
    public virtual NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        Debug.Log($"{gameObject.name}이(가) 행동: 공격 수행 요청!");

        

        // 공격 시작 시점에만 선딜레이 코루틴 시작
        if (!blackboard.isAttacking)
        {
            blackboard.isAttacking = true; // 공격 상태로 전환
            isPreDelayingAttack = true; // 선딜레이 시작 플래그 설정
            StartCoroutine(AttackWithPreDelay(damageMultiplier)); // 코루틴 시작
            Debug.Log($"공격 선딜레이 시작: {attackPreDelay}초");
        }

        // 선딜레이 중이므로 RUNNING 반환
        return NodeStatus.RUNNING;
    }

    // --- 추가될 코루틴 메서드 ---
    private IEnumerator AttackWithPreDelay(float damageMultiplier)
    {
        // 적을 바라보기
        if (enemy != null)
        { 
        transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));
        }
            

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking"); // 또는 "PrepareProactiveAttack"
        }

        // 선딜레이 시간만큼 대기
        yield return new WaitForSeconds(attackPreDelay);

        // 선딜레이 완료 후 실제 공격 로직 수행
        Debug.Log($"{gameObject.name}이(가) {attackPreDelay}초 선딜레이 후 실제 공격 수행!");

        // --- [기존 공격 로직 시작] ---

        // 돌진 공격 보너스 로직 (기존과 동일)
        const float chargeSpeedThreshold = 1.0f;
        const float chargeDamageBonus = 2.0f;

        float currentDamageMultiplier = damageMultiplier; // 전달받은 damageMultiplier 사용
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (forwardSpeed > chargeSpeedThreshold)
        {
            Debug.Log("돌진 공격! 데미지 보너스 적용!");
            currentDamageMultiplier *= chargeDamageBonus;
        }

        // 데미지 계산 (최종 배율 적용)
        float attackDamage = 10f * currentDamageMultiplier;

        // 데미지 판정 (기존과 동일)
        if (Physics.SphereCast(transform.position + Vector3.up, 0.8f, transform.forward, out RaycastHit hit, attackRange))
        {
            AgentController enemyController = hit.collider.GetComponentInParent<AgentController>();
            if (enemyController != null && enemyController != this)
            {
                Debug.Log($"{gameObject.name}이(가) {enemy.name}을(를) 공격하여 {attackDamage}(x{currentDamageMultiplier}) 데미지를 입혔습니다.");
                enemyController.HandleDamage(attackDamage);
            }
        }
        // --- [기존 공격 로직 끝] ---

        // 공격 애니메이션 종료 플래그는 애니메이션 이벤트로 리셋되므로 여기서는 건드리지 않음.
        // 대신 isAttacking과 isPreDelayingAttack 플래그를 여기서 바로 리셋.
        isPreDelayingAttack = false; // 선딜레이 종료
        blackboard.isAttacking = false; // 공격 상태 종료 (애니메이션이 끝난 후 다시 true로 설정될 수 있음)

        // 공격 쿨타임 설정
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        // 이 코루틴은 PerformAttack이 NodeStatus.RUNNING을 반환한 후 비동기적으로 실행됨.
        // 따라서 여기서 NodeStatus를 반환할 필요는 없음.
    }

    // ResetAllFlags 메서드에 선딜레이 플래그 초기화 추가
    public void ResetAllFlags()
    {
        attackFinished = false;
        evadeFinished = false;
        getAttackFinished = false;
        isPreDelayingAttack = false; // 추가된 플래그 초기화
                                     // ... (나머지 기존 초기화 로직)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("행동: 방어 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY); // 방어 쿨타임 설정 [cite: 10]
        blackboard.StartInvincibility(blackboard.defendCooldownDuration); // 방어 시간 동안 무적 [cite: 10]

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
        Debug.Log($"{gameObject.name}이(가) 행동: 선제 공격 수행 요청!");

        if (isPreDelayingAttack || !blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
        {
            // Debug.Log($"선제 공격 불가: 선딜레이 중이거나 쿨타임 미준비.");
            return NodeStatus.FAILURE;
        }

        if (!blackboard.isAttacking)
        {
            blackboard.isAttacking = true;
            isPreDelayingAttack = true;
            StartCoroutine(ProactiveAttackWithPreDelay());
            Debug.Log($"선제 공격 선딜레이 시작: {attackPreDelay}초");
        }

        return NodeStatus.RUNNING;
    }

    // --- 추가될 코루틴 메서드 ---
    private IEnumerator ProactiveAttackWithPreDelay()
    {
        if (enemy != null)
            transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking"); // 또는 "PrepareProactiveAttack"
        }

        yield return new WaitForSeconds(attackPreDelay);

        Debug.Log($"{gameObject.name}이(가) {attackPreDelay}초 선딜레이 후 실제 선제 공격 수행!");

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

        isPreDelayingAttack = false;
        blackboard.isAttacking = false;

        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);
    }


}
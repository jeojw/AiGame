// File: AgentController.cs (AgentController.cs 파일)
using UnityEditor;
using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    public Transform enemy; // 인스펙터에서 할당
    public float detectionRadius = 20f; // 적 감지 반경
    public float attackRange = 2f;      // 공격 범위
    public float closeRangeThreshold = 1f; // "너무 가까움" 판단 기준 거리
    public float lowHealthThreshold = 30f; // 체력 낮음 판단 기준 (비율 또는 절대값)
    public float evadeDistance = 2.0f; // 회피 시 이동할 거리

    private Rigidbody rb;
    private AnimationController animationController;
    private bool attackFinished = false;
    private bool getAttackFinished = false;

    protected AgentBlackboard blackboard; // 블랙보드 참조
    protected BTNode rootNode;            // 행동 트리의 루트 노드

    private Animator animator; // Animator 참조 변수
    private CharacterController characterController;

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f; // 문서에 따라 설정 [cite: 10]
        blackboard.currentHealth = blackboard.maxHealth;

        animator = GetComponent<Animator>(); // Animator 컴포넌트 가져오기
        characterController = GetComponent<CharacterController>(); // CharacterController 컴포넌트 가져오기
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

            animationController.onGetAttackFinished += () =>
            {
                getAttackFinished = true;
            };
        }
    }

    // 행동 트리를 초기화하는 추상 메소드 (파생 클래스에서 구현)
    protected abstract void InitializeBehaviorTree();

    protected virtual void Update()
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
            characterController.Move(direction * speed * Time.deltaTime); // CharacterController로 이동
            transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z)); // Y축 고정하여 바라보기

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


    public virtual NodeStatus MoveAwayFrom(Vector3 targetPosition, float speed, float moveDistance)
    {
        // [수정됨] CharacterController를 사용하여 충돌을 인식하며 이동하도록 수정
        Vector3 direction = (transform.position - targetPosition).normalized;
        characterController.Move(direction * speed * Time.deltaTime);
        transform.LookAt(transform.position + direction); // 이동 방향으로 바라보기

        if (animator != null) animator.SetFloat("Speed", speed); // 멀어지는 움직임도 이동 애니메이션 재생

        Debug.Log("행동: 목표 지점으로부터 멀어지는 중 (CharacterController)");
        // 이 행동은 한 스텝 이동 후 성공으로 간주하여, BT가 다음 판단을 빠르게 하도록 함
        return NodeStatus.SUCCESS;
    }
    
    // [수정]
    public virtual NodeStatus PerformAttack()
    {
        Debug.Log("행동: 공격 수행!");

        // 공격 중 시작 플래그
        blackboard.isAttacking = true;

        // 쿨타임 설정
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);

        // 적 바라보기
        if (enemy != null)
            transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z));

        // 공격 애니메이션 트리거
        if (animator != null)
            animator.SetTrigger("IsAttacking");

        // 데미지 판정
        float attackDamage = 10f;
        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out RaycastHit hit, attackRange))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                AgentController enemyController = hit.collider.GetComponent<AgentController>();
                if (enemyController != null)
                {
                    Debug.Log(gameObject.name + "이(가) " + enemy.name + "을(를) 공격하여 " + attackDamage + " 데미지를 입혔습니다.");
                    enemyController.HandleDamage(attackDamage);
                }
            }
        }

        // 애니메이션 제어
        animationController.StopWalk();
        animationController.PlayAttack();

        // 공격이 아직 끝나지 않았으면 RUNNING
        if (!attackFinished)
            return NodeStatus.RUNNING;

        // 공격 종료 시점
        animationController.StopAttack();
        attackFinished = false;

        // 공격 중 종료 플래그
        blackboard.isAttacking = false;

        return NodeStatus.SUCCESS;
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

    public virtual NodeStatus PerformEvade()
    {
        Debug.Log("행동: 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY); // 회피 쿨타임 설정 [cite: 10]
        blackboard.StartInvincibility(1.0f); // 회피 중 짧은 시간 무적 [cite: 10]
        Invoke(nameof(StopEvadeInvincibility), 1.0f);

        if (animator != null)
        {
            animator.SetTrigger("IsEvading");
        }



        return NodeStatus.SUCCESS;
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

    private void StopEvadeInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("회피 무적 상태 종료.");
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
        Destroy(gameObject, 3f);
    }
}
// File: AgentController.cs (AgentController.cs 파일)
using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    public Transform enemy; // 인스펙터에서 할당
    public float detectionRadius = 20f; // 적 감지 반경
    public float attackRange = 2f;      // 공격 범위
    public float closeRangeThreshold = 5f; // "너무 가까움" 판단 기준 거리
    public float lowHealthThreshold = 30f; // 체력 낮음 판단 기준 (비율 또는 절대값)

    protected AgentBlackboard blackboard; // 블랙보드 참조
    protected BTNode rootNode;            // 행동 트리의 루트 노드

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f; // 문서에 따라 설정
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.attackCooldownDuration = 2.5f; // 공격 쿨타임
        blackboard.defendCooldownDuration = 2.5f; // 방어 쿨타임
        blackboard.evadeCooldownDuration = 5f;    // 회피 쿨타임
    }

    protected virtual void Start()
    {
        InitializeBehaviorTree(); // 행동 트리 초기화
    }

    // 행동 트리를 초기화하는 추상 메소드 (파생 클래스에서 구현)
    protected abstract void InitializeBehaviorTree();

    protected virtual void Update()
    {
        if (enemy == null) // 적이 없다면
        {
            // 잠재적으로 적을 찾거나 대기 상태 유지
            // 현재는 적이 할당되었거나 찾아졌다고 가정
            FindEnemy(); // 간단한 적 찾기 로직
        }

        // 블랙보드 업데이트
        if (enemy != null)
        {
            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), 100f); // 적 체력은 100f로 가정, 실제 값으로 대체 필요
        }
        else
        {
            blackboard.enemyTransform = null; // 적 없음
        }


        if (rootNode != null) // 루트 노드가 있다면
        {
            rootNode.Tick(); // 행동 트리 실행
        }

        // 디버그용
        // Debug.Log($"에이전트 체력: {blackboard.currentHealth}, 적과의 거리: {blackboard.enemyDistance}");
    }

    void FindEnemy()
    {
        // 예시: 태그로 찾기, 더 정교한 방식 가능
        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy"); // 적이 "Enemy" 태그를 가지고 있는지 확인
        if (enemyObject != null)
        {
            enemy = enemyObject.transform;
        }
    }

    // --- 행동 메소드 (ActionNode에서 호출됨) ---
    public virtual NodeStatus MoveTowards(Vector3 targetPosition, float speed, float stopDistance)
    {
        if (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            transform.LookAt(targetPosition); // 기본적인 바라보기
            // 이동 애니메이션 재생
            Debug.Log("행동: 목표 지점으로 이동 중");
            return NodeStatus.RUNNING; // 아직 이동 중
        }
        return NodeStatus.SUCCESS; // 도착
    }

    public virtual NodeStatus MoveAwayFrom(Vector3 targetPosition, float speed, float moveDistance)
    {
        Vector3 direction = (transform.position - targetPosition).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.LookAt(transform.position + direction); // 이동 방향으로 바라보기
        // 이동 애니메이션 재생
        Debug.Log("행동: 목표 지점으로부터 멀어지는 중");
        // 이 행동은 한 스텝 이동 후 성공으로 간주하거나 일정 시간 동안 실행될 수 있습니다.
        // 단순화를 위해, 한 스텝 이동 후 SUCCESS를 반환하도록 합니다.
        return NodeStatus.SUCCESS;
    }


    public virtual NodeStatus PerformAttack()
    {
        Debug.Log("행동: 공격 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY); // 공격 쿨타임 설정
        // 공격 애니메이션 실행
        // 여기에 데미지 로직 구현 (예: SphereCast, 애니메이션 이벤트)
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("행동: 방어 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY); // 방어 쿨타임 설정
        blackboard.StartInvincibility(blackboard.defendCooldownDuration); // 방어는 지속 시간 동안 무적 상태로 만듦
        // 방어 애니메이션 실행
        // 실제 게임에서는 무적 시간이 더 짧거나 애니메이션 상태와 연동될 수 있습니다.
        // 무적 상태 해제 메커니즘이 필요합니다. 단순화를 위해 AgentController 또는 블랙보드에서 처리합니다.
        Invoke(nameof(StopDefendInvincibility), blackboard.defendCooldownDuration); // 방어 지속 시간 후 무적 해제
        return NodeStatus.SUCCESS;
    }
    private void StopDefendInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("방어 무적 상태 종료.");
    }


    public virtual NodeStatus PerformEvade()
    {
        Debug.Log("행동: 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY); // 회피 쿨타임 설정
        blackboard.StartInvincibility(1.0f); // 회피는 짧은 시간 동안 무적 상태 부여 (예: 1초)
        Invoke(nameof(StopEvadeInvincibility), 1.0f); // 예시: 1초간 무적
        // 회피 이동 구현 (예: 특정 방향으로 빠른 대쉬)
        // 예시: 뒤로 대쉬
        transform.Translate(Vector3.back * 2f, Space.Self); // 자신의 뒤쪽으로 2 유닛 대쉬
        // 회피 애니메이션 실행
        return NodeStatus.SUCCESS;
    }
    private void StopEvadeInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("회피 무적 상태 종료.");
    }

    public virtual NodeStatus Idle()
    {
        Debug.Log("행동: 대기 중");
        // 대기 애니메이션 실행
        return NodeStatus.SUCCESS;
    }

    // --- 충돌/데미지 처리 ---
    // 일반적으로 충돌 스크립트나 발사체 피격 시 호출됩니다.
    public void HandleDamage(float damage)
    {
        blackboard.TakeDamage(damage);
        if (blackboard.currentHealth <= 0)
        {
            Die(); // 체력이 0 이하이면 죽음 처리
        }
    }

    protected virtual void Die()
    {
        Debug.Log(gameObject.name + "이(가) 죽었습니다.");
        // 죽음 애니메이션 재생, 스크립트 비활성화 등
        Destroy(gameObject, 2f); // 예시: 2초 후 오브젝트 파괴
    }
}
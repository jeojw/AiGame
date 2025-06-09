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
    [SerializeField] private float evadeSpeed = 15f; // 회피 이동 속도를 더 빠르게 설정

    [SerializeField] private float evadeMoveDistance = 7.0f; // 회피 시 이동할 거리

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
    private bool isEvadingMovement = false;


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

            if (forwardSpeed > chargeSpeedThreshold)
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
        Debug.Log("행동: 방어 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY);
        blackboard.StartInvincibility(blackboard.defendCooldownDuration);

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

        blackboard.isEvading = true;

        return PerformDirectionalEvade(randomDirection);
    }

    public virtual NodeStatus PerformDirectionalEvade(int direction)
    {
        if (!blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
            return NodeStatus.FAILURE;

        Debug.Log($"행동: {direction} 방향으로 회피 수행!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);

        blackboard.isEvading = true;

        Vector3 initialPosition = transform.position;
        Vector3 targetDirection = Vector3.zero;

        switch (direction)
        {
            case 0: // Forward
                targetDirection = transform.forward;
                break;
            case 1: // Backward
                targetDirection = -transform.forward;
                break;
            case 2: // Left
                targetDirection = -transform.right;
                break;
            case 3: // Right
                targetDirection = transform.right;
                break;
        }

        evadeTargetPosition = initialPosition + targetDirection * evadeMoveDistance;
        currentEvadeSpeed = evadeSpeed; // 회피 시 더 빠른 속도 적용

        if (animator != null)
        {
            animator.SetFloat("Speed", currentEvadeSpeed);
            animator.SetBool("isRun", true); // 'isRun' 파라미터가 있다면 활용
        }

        isEvadingMovement = true;

        animationController?.PlayWalk(); // 회피는 '빠르게 걷는' 모션으로 연출

        return NodeStatus.RUNNING;
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
        isEvadingMovement = false;

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
        if (blackboard.isInvincible)
        {
            Debug.Log(gameObject.name + "이(가) 공격을 무효화했습니다.");
            return;
        }

        blackboard.TakeDamage(damage);
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
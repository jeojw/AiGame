using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections; // Coroutine을 위해 추가

// OffensiveAgentController의 기능을 ML-Agents Agent로 확장
public class OffensiveAgentMLAgent : Agent
{
    public Transform enemyTarget; // 적의 Transform
    public float detectionRadius = 20f;
    public float attackRange = 2f;
    public float offensiveAttackRange = 2.0f; // OffensiveAgentController에서 가져옴
    public float evadeDuration = 0.3f; // AgentController에서 가져옴
    public float attackDamage = 10f; // AgentController에서 가져옴
    public float rotationSpeed = 5f; // AgentController에서 가져옴

    private AgentBlackboard blackboard;
    private Animator animator;
    private Rigidbody rb;

    // 공격 애니메이션 완료 콜백
    private AnimationController animationController;

    private const float MOVE_SPEED = 3f; // 이동 속도 상수화
    private const float IDLE_SPEED = 0f; // 정지 속도 상수화

    // 에피소드 시작 시 호출
    public override void OnEpisodeBegin()
    {
        // 초기화 로직
        // 예: 위치, 체력 초기화
        transform.localPosition = new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.canCounterAttack = false;
        blackboard.defenseInitiationTime = -1f;
        blackboard.actionCooldowns.Clear(); // 쿨타임 초기화
        animator.SetFloat("Speed", 0f); // 애니메이터 초기화

        // 적 위치 설정 (환경에 따라 변경될 수 있음)
        if (enemyTarget != null)
        {
            // Offensive는 Defensive를 찾고, Defensive는 Offensive를 찾도록 설정
            if (gameObject.CompareTag("Offensiver") && enemyTarget.CompareTag("Defensiver"))
            {
                //enemyTarget.GetComponent<DefensiveAgentMLAgent>().OnEpisodeBegin(); // 적 에이전트도 초기화
            }
            else if (gameObject.CompareTag("Defensiver") && enemyTarget.CompareTag("Offensiver"))
            {
                enemyTarget.GetComponent<OffensiveAgentMLAgent>().OnEpisodeBegin(); // 적 에이전트도 초기화
            }
        }
    }

    void Awake()
    {
        blackboard = new AgentBlackboard(); // 새로운 블랙보드 인스턴스 생성
        blackboard.maxHealth = 100f;
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.attackCooldownDuration = 2.5f;
        blackboard.defendCooldownDuration = 2.5f; // 공격자는 방어 안하지만 일관성 위해 추가
        blackboard.evadeCooldownDuration = 5f;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        animationController = GetComponent<AnimationController>(); // AnimationController 참조
        if (animationController != null)
        {
            animationController.onAttackFinished = () =>
            {
                // 공격 애니메이션이 끝나면 리워드 부여 및 Done 처리 (선택적)
                // AddReward(0.1f); // 공격 성공 리워드 (예시)
                // Debug.Log("공격 애니메이션 종료!");
            };
        }
    }

    // 관측값 수집
    public override void CollectObservations(VectorSensor sensor)
    {
        // 자신의 정보
        sensor.AddObservation(blackboard.currentHealth / blackboard.maxHealth); // 정규화된 체력
        sensor.AddObservation(transform.localPosition); // 자신의 위치

        // 적의 정보 (있는 경우)
        if (enemyTarget != null)
        {
            sensor.AddObservation(enemyTarget.localPosition); // 적의 위치
            sensor.AddObservation(Vector3.Distance(transform.localPosition, enemyTarget.localPosition)); // 적과의 거리
            AgentController enemyCtrl = enemyTarget.GetComponent<AgentController>();
            if (enemyCtrl != null)
            {
                sensor.AddObservation(enemyCtrl.blackboard.currentHealth / enemyCtrl.blackboard.maxHealth); // 정규화된 적 체력
            }
            else
            {
                sensor.AddObservation(0f); // 적 컨트롤러 없으면 0
            }
        }
        else
        {
            // 적이 없으면 모든 관련 관측값에 0을 추가
            sensor.AddObservation(Vector3.zero); // 적 위치
            sensor.AddObservation(0f); // 적과의 거리
            sensor.AddObservation(0f); // 적 체력
        }

        // 쿨타임 상태 (정규화 필요)
        sensor.AddObservation(blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY) ? 1f : 0f);
        sensor.AddObservation(blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY) ? 1f : 0f);

        // 현재 애니메이션 상태 (선택적: 필요한 경우 추가)
        // AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // sensor.AddObservation(stateInfo.IsTag("Attack") ? 1f : 0f);
        // sensor.AddObservation(stateInfo.IsTag("Evade") ? 1f : 0f);

        // 관측값 총 개수: 1 (체력) + 3 (자신 위치) + 3 (적 위치) + 1 (거리) + 1 (적 체력) + 1 (공격 쿨타임) + 1 (회피 쿨타임) = 11개
    }

    // 행동 수신 및 실행
    public override void OnActionReceived(ActionBuffers actions)
    {
        // 0: 이동 (Vector2.x = 앞/뒤, Vector2.y = 좌/우) -> 사용하지 않고 직접 위치 이동
        // 1: 회전 (float) -> 사용하지 않고 LookAtEnemy로 처리
        // 2: 행동 (int) - 0:대기, 1:공격, 2:회피
        int actionType = actions.DiscreteActions[0]; // 첫 번째 이산 행동

        // 이전 프레임의 속도 초기화 (애니메이션 초기화를 위해)
        animator.SetFloat("Speed", IDLE_SPEED);

        // 현재 행동 중이 아니라면 (애니메이션 재생 중이 아니라면)
        bool isPerformingAction = false;
        if (animator != null)
        {
            var currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentStateInfo.IsTag("Attack") || currentStateInfo.IsTag("Defend") || currentStateInfo.IsTag("Evade"))
            {
                isPerformingAction = true;
            }
        }

        if (isPerformingAction)
        {
            // 행동 중일 때는 다른 행동을 막고 패널티 부여 (선택적)
            AddReward(-0.01f); // 행동 중 불필요한 입력에 패널티
            return;
        }

        switch (actionType)
        {
            case 0: // 대기 (Idle)
                // Debug.Log("행동: 대기");
                Idle();
                AddReward(-0.001f); // 대기 시 작은 패널티 (지속적인 행동 유도)
                break;
            case 1: // 공격 (Attack)
                // Debug.Log("행동: 공격 시도");
                if (blackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY) && blackboard.enemyDistance <= attackRange)
                {
                    PerformAttack(); // 공격 실행
                    AddReward(0.5f); // 공격 성공 시 잠정적 보상
                }
                else
                {
                    AddReward(-0.05f); // 공격 시도 실패 시 패널티 (쿨타임, 거리)
                }
                break;
            case 2: // 회피 (Evade)
                // Debug.Log("행동: 회피 시도");
                if (blackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY) /* && IsEnemyAttackingCondition() */) // 적 공격 감지 조건 추가 필요
                {
                    PerformEvade(); // 회피 실행
                    AddReward(0.3f); // 회피 성공 시 잠정적 보상
                }
                else
                {
                    AddReward(-0.02f); // 회피 시도 실패 시 패널티 (쿨타임)
                }
                break;
            case 3: // 적에게 접근 (Move Towards Enemy) - Offensive Agent 특화
                // Debug.Log("행동: 적에게 접근");
                if (enemyTarget != null && blackboard.enemyDistance > offensiveAttackRange)
                {
                    MoveTowards(enemyTarget.position, MOVE_SPEED, offensiveAttackRange * 0.9f);
                    AddReward(0.005f); // 적에게 가까워지는 것에 대한 작은 보상
                }
                else if (enemyTarget != null && blackboard.enemyDistance <= offensiveAttackRange)
                {
                    // 공격 범위 내에 있으면 더 이상 이동할 필요 없으므로 작은 패널티
                    AddReward(-0.001f);
                    animator.SetFloat("Speed", IDLE_SPEED);
                }
                else
                {
                    AddReward(-0.005f); // 적이 없거나 불가능한 이동 시도
                    animator.SetFloat("Speed", IDLE_SPEED);
                }
                break;
        }

        // 체력 감소에 따른 보상/패널티
        if (blackboard.currentHealth <= 0)
        {
            SetReward(-1.0f); // 죽으면 큰 패널티
            EndEpisode(); // 에피소드 종료
        }
        else if (enemyTarget != null)
        {
            AgentController enemyCtrl = enemyTarget.GetComponent<AgentController>();
            if (enemyCtrl != null && enemyCtrl.blackboard.currentHealth <= 0)
            {
                SetReward(1.0f); // 적을 죽이면 큰 보상
                EndEpisode(); // 에피소드 종료
            }
        }
    }

    // 개발 및 테스트를 위한 수동 제어 (선택적)
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0; // 기본 대기

        if (Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[0] = 1; // 공격
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            discreteActionsOut[0] = 2; // 회피
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 3; // 접근
        }
    }

    // HandleDamage는 AgentController에 정의되어 있으므로 그대로 사용
    // 추가적인 ML-Agents 관련 보상 로직을 HandleDamage 내부에 추가할 수 있음
    public void HandleDamageMLAgent(float damage, AgentController attacker)
    {
        float previousHealth = blackboard.currentHealth;
        // 기존 HandleDamage 로직 호출
        // AgentController의 HandleDamage는 protected이므로, AgentController를 상속받은 ML-Agents 스크립트에서 직접 호출하거나
        // 아니면 AgentController의 HandleDamage를 public으로 변경해야 합니다.
        // 여기서는 예시를 위해 직접 로직을 구현합니다.
        // OffensiveAgentMLAgent는 AgentController를 상속받지 않으므로, blackboard를 직접 업데이트해야 합니다.
        // blackboard.TakeDamage(damage); // AgentBlackboard의 TakeDamage 사용

        // DefensiveAgentController의 HandleDamage 로직을 여기에 가져와서 사용
        // 또는, AgentController의 HandleDamage를 public으로 변경하고 호출하는 것을 권장.
        // 현재는 AgentController를 상속받지 않으므로 AgentController.HandleDamage를 직접 호출할 수 없습니다.
        // 따라서, AgentController의 HandleDamage 로직을 여기에 복사하거나,
        // 이 스크립트를 AgentController를 상속받도록 변경해야 합니다.

        // 예시: 간단하게 체력 감소
        blackboard.currentHealth -= damage;
        if (blackboard.currentHealth < 0) blackboard.currentHealth = 0;

        float damageTaken = previousHealth - blackboard.currentHealth; // 실제로 입은 데미지
        if (damageTaken > 0)
        {
            AddReward(-damageTaken * 0.01f); // 데미지 입으면 패널티
        }

        if (blackboard.currentHealth <= 0)
        {
            SetReward(-1.0f); // 죽으면 큰 패널티
            EndEpisode();
        }
    }

    // 기존 AgentController의 함수들을 여기에 재정의하거나, AgentController를 상속받도록 변경
    // 현재는 AgentController를 상속받지 않는다고 가정하고 간단히 재정의
    public NodeStatus MoveTowards(Vector3 targetPosition, float speed, float stopDistance)
    {
        if (enemyTarget != null)
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

    public NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        if (enemyTarget != null)
        {
            Vector3 directionToEnemy = enemyTarget.position - transform.position;
            directionToEnemy.y = 0;
            if (directionToEnemy.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToEnemy);
            }
        }
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);
        blackboard.currentAttackDamageMultiplier = damageMultiplier;
        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }
        // 실제로 데미지를 주는 로직은 애니메이션 이벤트로 처리 (AnimationController의 OnAttackAnimationFinished)
        // 또는 여기에서 SphereCast 등을 사용하여 즉시 처리 (지금은 AgentController의 ActuallyDealDamage를 활용)
        // HitboxController와 연동하여 충돌 시 데미지 처리하도록 설정
        return NodeStatus.SUCCESS;
    }

    public NodeStatus PerformEvade()
    {
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);
        // blackboard.StartInvincibility(evadeDuration); // 무적 상태는 HitboxController 또는 다른 곳에서 처리
        // Invoke(nameof(StopEvadeInvincibility), evadeDuration); // 직접 Invoke 대신 코루틴 사용
        if (animator != null)
        {
            animator.SetTrigger("IsEvading");
        }
        StartCoroutine(EvadeCoroutine());
        return NodeStatus.SUCCESS;
    }

    private IEnumerator EvadeCoroutine()
    {
        float randomSign = Random.value > 0.5f ? 1f : -1f;
        Vector3 evadeDirectionWorld = transform.right * randomSign;
        evadeDirectionWorld.y = 0;
        evadeDirectionWorld.Normalize();

        if (evadeDirectionWorld.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(evadeDirectionWorld);
        }

        float elapsedTime = 0f;
        while (elapsedTime < evadeDuration)
        {
            Vector3 movement = evadeDirectionWorld * (evadeDuration / evadeDuration) * Time.deltaTime; // 회피 거리는 evadeDistance
            rb.MovePosition(transform.position + movement);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // blackboard.EndInvincibility(); // 무적 상태 종료 (HitboxController에서 처리하면 이 부분은 제거)
    }

    public NodeStatus Idle()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
        return NodeStatus.SUCCESS;
    }

    void SmoothLookAtEnemy()
    {
        if (enemyTarget == null) return;

        Vector3 direction = enemyTarget.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // HitboxController와 연동
    void OnCollisionEnter(Collision collision)
    {
        int thisLayer = this.gameObject.layer;
        int otherLayer = collision.gameObject.layer;

        // 공격자가 피격당했을 때
        if ((thisLayer == LayerMask.NameToLayer("OffensiverBody") && otherLayer == LayerMask.NameToLayer("DefensiverSword")))
        {
            // 상대방의 공격 컨트롤러를 가져와서 데미지 정보 획득
            AgentController otherAgent = collision.gameObject.GetComponentInParent<AgentController>();
            if (otherAgent != null)
            {
                // OffensiveAgentMLAgent의 HandleDamageMLAgent 호출
                HandleDamageMLAgent(otherAgent.attackDamage, otherAgent); // 원래는 otherAgent.attackDamage * otherAgent.blackboard.currentAttackDamageMultiplier를 사용해야 함
                // 피격당하면 추가 패널티 부여
                AddReward(-0.2f);
            }
        }
    }
}
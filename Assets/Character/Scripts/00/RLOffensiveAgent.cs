// File: RLOffensiveAgent.cs
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using System.Collections; // IEnumerator 사용을 위해 필요

public class RLOffensiveAgent : Agent
{
    [Header("Agent Components")]
    [SerializeField] private Transform enemyTransform;
    private AgentController myController; // 기존 AgentController를 참조하여 실제 행동을 실행
    private AgentBlackboard myBlackboard;

    private AgentController enemyController;
    private AgentBlackboard enemyBlackboard;

    // 에이전트의 초기 위치와 회전 값을 저장할 변수
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // '상대방'의 초기 위치와 회전 값을 저장할 변수
    private Vector3 enemyInitialPosition;
    private Quaternion enemyInitialRotation;

    private float previousDistanceToEnemy;
    private float previousMyHealth; // 이전 프레임의 내 체력을 저장할 변수
    private float previousEnemyHealth; // [추가] 이전 프레임의 적 체력을 저장할 변수

    // --- [추가 시작: 빠른 승리 보상을 위한 변수] ---
    private float episodeStartTime; // 에피소드 시작 시간
    [SerializeField] private float maxEpisodeTimeBonus = 50.0f; // 최대 시간 보상 값
    [SerializeField] private float episodeDurationForMaxBonus = 10.0f; // 이 시간 내에 이기면 최대 보상
    // --- [추가 끝] ---


    public override void Initialize()
    {
        myController = GetComponent<AgentController>();
        myBlackboard = myController.blackboard;

        if (enemyTransform != null)
        {
            enemyController = enemyTransform.GetComponent<AgentController>();
            enemyBlackboard = enemyController.blackboard;
        }
        else
        {
            // 초기 Initialize 시 enemyTransform이 null이면 enemyBlackboard도 null일 수 있습니다.
            // OnEpisodeBegin에서 확실히 처리할 예정입니다.
            enemyController = null;
            enemyBlackboard = null;
        }

        // 이 스크립트가 처음 초기화될 때, 시작 위치와 회전 값을 저장합니다.
        this.initialPosition = transform.position;
        this.initialRotation = transform.rotation;

        // 상대방의 초기 위치와 회전 값도 함께 저장합니다.
        if (enemyTransform != null)
        {
            this.enemyInitialPosition = enemyTransform.position;
            this.enemyInitialRotation = enemyTransform.rotation;
        }
    }

    /// <summary>
    /// 에피소드(라운드) 시작 시 호출
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // --- enemyController 및 enemyBlackboard 참조 및 초기화 로직 보강 ---
        // 에피소드 시작 시 기존 참조를 초기화합니다.
        enemyController = null;
        enemyBlackboard = null;

        if (enemyTransform != null)
        {
            previousDistanceToEnemy = Vector3.Distance(transform.position, enemyTransform.position);
            enemyController = enemyTransform.GetComponent<AgentController>();
            if (enemyController != null)
            {
                enemyBlackboard = enemyController.blackboard;
            }
            else
            {
                Debug.LogError($"'{enemyTransform.name}' 오브젝트에 AgentController 컴포넌트가 없습니다! 이 에이전트의 enemyBlackboard는 더미로 생성됩니다.", enemyTransform);
                // enemyController가 없으면 enemyBlackboard는 여기서 null로 유지
            }
        }
        else
        {
            Debug.LogError("'enemyTransform'이(가) 인스펙터에 할당되지 않았습니다! 이 에이전트의 enemyBlackboard는 더미로 생성됩니다.", this.gameObject);
            // enemyTransform이 null이면 enemyController와 enemyBlackboard는 여기서 null로 유지
        }

        // enemyBlackboard가 여전히 null이면 새로운 인스턴스를 생성하여 NullReferenceException을 방지합니다.
        if (enemyBlackboard == null)
        {
            enemyBlackboard = new AgentBlackboard(); // 새로운(더미) Blackboard 인스턴스 생성
        }
        // --- enemyController 및 enemyBlackboard 초기화 로직 보강 끝 ---

        // --- 체력 및 이전 상태 초기화 ---
        myBlackboard.currentHealth = myBlackboard.maxHealth;
        previousMyHealth = myBlackboard.maxHealth; // 에피소드 시작 시 내 체력 초기화

        // 적 체력 및 이전 상태 초기화 (enemyBlackboard는 이제 항상 유효한 인스턴스임을 보장합니다)
        enemyBlackboard.currentHealth = enemyBlackboard.maxHealth;
        previousEnemyHealth = enemyBlackboard.maxHealth; // [추가] 적 체력 초기화

        // --- 캐릭터 위치 및 물리 상태 리셋 로직 ---
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // --- 상대방 위치 리셋 ---
        if (enemyTransform != null)
        {
            enemyTransform.position = enemyInitialPosition;
            enemyTransform.rotation = enemyInitialRotation;

            Rigidbody enemyRb = enemyTransform.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector3.zero;
                enemyRb.angularVelocity = Vector3.zero;
            }
        }

        // --- 빠른 승리 보상을 위한 시간 초기화 ---
        episodeStartTime = Time.time;

        // --- 핵심 초기화 로직: 모든 관련 플래그와 상태 초기화 ---

        // 1. AgentController의 내부 플래그 초기화 및 활성화
        if (myController != null)
        {
            myController.ResetAllFlags();
            myController.enabled = true;
        }
        // 적 에이전트의 컨트롤러도 리셋 (상대방이 RL이든 BT든 일관된 초기화를 위해 필요)
        if (enemyController != null) // enemyController는 null일 수 있으므로 null 검사
        {
            enemyController.ResetAllFlags();
            enemyController.enabled = true;
        }

        // 2. AgentBlackboard의 모든 상태 플래그 초기화
        myBlackboard.isAttacking = false;
        myBlackboard.isDefending = false;
        myBlackboard.isEvading = false;
        myBlackboard.isInvincible = false;
        myBlackboard.isGetAttacked = false;
        myBlackboard.canCounterAttack = false;
        myBlackboard.isDead = false;
        myBlackboard.lastEnemyAttackTime = 0f;
        myBlackboard.recentlyDefended = false; // [추가]
        myBlackboard.score = 0; // [추가]
        myBlackboard.attackCount = 0; // [추가]
        myBlackboard.defendCount = 0; // [추가]
        myBlackboard.counterAttackCount = 0; // [추가]
        myBlackboard.evadeCount = 0; // [추가]


        // 상대방의 Blackboard도 초기화 (enemyBlackboard는 이제 항상 유효합니다)
        enemyBlackboard.isAttacking = false;
        enemyBlackboard.isDefending = false;
        enemyBlackboard.isEvading = false;
        enemyBlackboard.isInvincible = false;
        enemyBlackboard.isGetAttacked = false;
        enemyBlackboard.canCounterAttack = false;
        enemyBlackboard.isDead = false;
        enemyBlackboard.lastEnemyAttackTime = 0f; // [추가]
        enemyBlackboard.recentlyDefended = false; // [추가]
        enemyBlackboard.score = 0; // [추가]
        enemyBlackboard.attackCount = 0; // [추가]
        enemyBlackboard.defendCount = 0; // [추가]
        enemyBlackboard.counterAttackCount = 0; // [추가]
        enemyBlackboard.evadeCount = 0; // [추가]

        // 3. 현재 RL Agent 스크립트 자체도 활성화 확인 (안전 장치)
        this.enabled = true;
    }

    /// <summary>
    /// 관찰(Observation) 수집
    /// 에이전트가 상황을 판단하는데 필요한 모든 정보를 센서에 추가합니다.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // 내 정보
        sensor.AddObservation(myBlackboard.currentHealth / myBlackboard.maxHealth); // 정규화된 내 체력
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY)); // 공격 가능 여부
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY));  // 회피 가능 여부
        sensor.AddObservation(myBlackboard.isInvincible); // 무적 상태 여부

        // enemyBlackboard는 이제 항상 유효한 인스턴스임을 보장하므로,
        // enemyTransform == null 체크를 제거하고 enemyBlackboard를 직접 사용합니다.
        // 다만, enemyTransform이 null이면 상대적 위치 계산이 불가능합니다.
        // 이 경우 0으로 채우는 기존 로직을 유지하면서, enemyTransform이 유효한지 확인하는 것이 좋습니다.
        if (enemyTransform == null)
        {
            sensor.AddObservation(new float[6]); // 상대가 없으면 0으로 채움 (기존 5개 + isDefending 1개)
            // Debug.LogWarning("[RLOffensiveAgent] enemyTransform이 null입니다. 관찰값을 0으로 채웁니다.");
            return;
        }

        // 상대방 정보 (enemyTransform이 null이 아닐 때만 유효)
        Vector3 relativePos = transform.InverseTransformPoint(enemyTransform.position);
        sensor.AddObservation(relativePos.x); // 상대적 위치 X
        sensor.AddObservation(relativePos.z); // 상대적 위치 Z
        sensor.AddObservation(Vector3.Distance(transform.position, enemyTransform.position)); // 상대와의 거리

        // enemyBlackboard는 항상 유효하므로 직접 접근
        sensor.AddObservation(enemyBlackboard.currentHealth / enemyBlackboard.maxHealth); // 정규화된 상대 체력
        sensor.AddObservation(enemyBlackboard.isAttacking); // 상대가 공격 중인지 여부
        sensor.AddObservation(enemyBlackboard.isDefending); // 상대가 방어 중인지 여부 (총 6개 관찰)
    }

    /// <summary>
    /// 회피 성공 여부를 잠시 뒤에 체크하는 코루틴
    /// </summary>
    /// <param name="delay">체크 전 대기 시간</param>
    private IEnumerator CheckDodgeSuccess(float delay)
    {
        // enemyBlackboard는 이제 항상 유효하므로 null 검사 제거
        bool enemyWasAttacking = enemyBlackboard.isAttacking;

        if (!enemyWasAttacking)
        {
            yield break;
        }

        float healthBeforeDodge = myBlackboard.currentHealth;

        yield return new WaitForSeconds(delay);

        if (myBlackboard.currentHealth >= healthBeforeDodge)
        {
            Debug.Log("회피 성공! 보상 +0.5");
            AddReward(0.5f);
        }
    }

    /// <summary>
    /// 행동(Action) 실행
    /// 정책(Brain)으로부터 받은 행동 명령을 실제 게임 월드에서 실행합니다.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Debug.Log($"[RL Offensive Agent] OnActionReceived 호출됨. 받은 액션: {actions.DiscreteActions[0]}");

        int mainAction = actions.DiscreteActions[0];

        switch (mainAction)
        {
            case 0: // 대기
                myController.Idle();
                break;
            case 1: // 적에게 다가가기 (Forward)
                // enemyTransform이 null일 수 있으므로 검사 추가
                if (enemyTransform != null)
                {
                    myController.MoveTowards(enemyTransform.position, 5f, myController.attackRange * 0.8f);
                }
                else
                {
                    myController.Idle(); // 적이 없으면 대기
                }
                break;
            case 2: // 적에게서 멀어지기 (Backward)
                // enemyTransform이 null일 수 있으므로 검사 추가
                if (enemyTransform != null)
                {
                    myController.MoveAwayFrom(enemyTransform.position, 3f, myController.attackRange + 1.0f);
                }
                else
                {
                    myController.Idle(); // 적이 없으면 대기
                }
                break;
            case 3: // 공격
                if (myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
                {
                    myController.PerformAttack();
                    AddReward(0.05f); // 공격 시도 시 보상
                }
                break;
            case 4: // 회피
                int evadeDirection = actions.DiscreteActions[1];
                if (myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY)) // 회피 쿨타임 확인
                {
                    if (myController.PerformDirectionalEvade(evadeDirection) == NodeStatus.SUCCESS)
                    {
                        // 앞으로 회피(direction == 0) 시 보상 가중치 추가
                        float dodgeReward = 0.01f; // 기본 회피 보상
                        if (evadeDirection == 0) // 앞으로 회피 (전방)
                        {
                            dodgeReward += 0.2f; // 앞으로 회피 성공 시 추가 보상 (값은 훈련을 통해 조정)
                            Debug.Log("RLOffensiveAgent: 앞으로 회피 성공! 추가 보상!");

                            // [수정 시작] 앞으로 회피 후 바로 공격 시도
                            // Attack 쿨타임이 준비되었고, enemyTransform이 null이 아니며,
                            // 적이 공격 범위 내에 충분히 가까이 있다면 바로 공격을 시도합니다.
                            if (myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY) &&
                                enemyTransform != null &&
                                Vector3.Distance(transform.position, enemyTransform.position) <= myController.attackRange + 0.1f)
                            {
                                Debug.Log("RLOffensiveAgent: 앞으로 회피 후 바로 공격 시도!");
                                myController.PerformAttack();
                                AddReward(0.5f); // 연계 공격 시도에 대한 추가 보상 (값을 조정)
                            }
                            // [수정 끝]
                        }
                        AddReward(dodgeReward);

                        StartCoroutine(CheckDodgeSuccess(0.5f));
                    }
                }
                break;


        }

        HandleRewards();
    }

    /// <summary>
    /// 보상(Reward) 설계
    /// </summary>
    private void HandleRewards()
    {
        // 1. 기본 시간 페널티 (너무 늘어지는 것을 방지)
        AddReward(-0.005f);

        // 2. 적에게 다가가면 보상 (적극적인 움직임 유도)
        if (enemyTransform != null)
        {
            float currentDistance = Vector3.Distance(transform.position, enemyTransform.position);

            if (currentDistance <= myController.attackRange + 0.1f)
            {
                AddReward(0.002f);
            }
            else if (currentDistance > myController.attackRange + 2.0f)
            {
                AddReward(-0.001f);
            }
            previousDistanceToEnemy = currentDistance;
        }
        else // enemyTransform이 null인 경우 거리 관련 보상/페널티 방지
        {
            AddReward(-0.0005f); // 적이 없으면 소량의 페널티 (적을 찾도록 유도)
        }

        // 3. 체력 우위 보상 (enemyBlackboard는 이제 항상 유효하므로 null 검사 제거)
        float healthAdvantage = (myBlackboard.currentHealth - enemyBlackboard.currentHealth) / myBlackboard.maxHealth;
        AddReward(healthAdvantage * 0.003f);

        // 4. 잃은 체력에 대한 페널티
        if (myBlackboard != null)
        {
            float healthLost = previousMyHealth - myBlackboard.currentHealth;
            if (healthLost > 0)
            {
                AddReward(-healthLost / myBlackboard.maxHealth * 0.5f);
            }
            previousMyHealth = myBlackboard.currentHealth;
        }

        // 5. 적에게 데미지 적용 보상 (enemyBlackboard는 이제 항상 유효하므로 null 검사 제거)
        float enemyHealthLost = previousEnemyHealth - enemyBlackboard.currentHealth;
        if (enemyHealthLost > 0)
        {
            AddReward(enemyHealthLost / enemyBlackboard.maxHealth * 3.0f);
        }
        previousEnemyHealth = enemyBlackboard.currentHealth;

        // 6. 게임 종료 조건 
        if (myBlackboard != null && myBlackboard.currentHealth <= 0)
        {
            SetReward(-30.0f);
            EndEpisode();
        }
        else if (enemyBlackboard.currentHealth <= 0) // enemyBlackboard는 이제 항상 유효
        {
            float episodeDuration = Time.time - episodeStartTime;
            float timeBonus = 0f;

            if (episodeDuration < episodeDurationForMaxBonus)
            {
                timeBonus = maxEpisodeTimeBonus;
            }
            else
            {
                timeBonus = Mathf.Max(0, maxEpisodeTimeBonus * (1.0f - (episodeDuration / (episodeDurationForMaxBonus * 2.0f))));
            }

            SetReward(10.0f + timeBonus);
            EndEpisode();
        }
    }

    /// <summary>
    /// 휴리스틱 모드: 개발자가 직접 키보드로 조작하여 테스트할 때 사용
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("[RL Offensive Agent] Heuristic() 메소드가 호출되었습니다!");

        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0; // 기본값: 대기
        discreteActions[1] = 0; // 회피 방향 기본값: 전방

        // 키보드가 연결되어 있는지 확인
        if (Keyboard.current == null)
        {
            return;
        }

        // 새로운 Input System 방식으로 키 입력 확인
        if (Keyboard.current.wKey.isPressed) discreteActions[0] = 1; // 앞으로
        if (Keyboard.current.sKey.isPressed) discreteActions[0] = 2; // 뒤로
        if (Keyboard.current.spaceKey.isPressed) discreteActions[0] = 3; // 공격
        if (Keyboard.current.leftShiftKey.isPressed) discreteActions[0] = 4; // 회피

        if (discreteActions[0] == 4) // 회피 행동을 선택했을 때만 방향을 결정
        {
            if (Keyboard.current.upArrowKey.isPressed) discreteActions[1] = 0;    // 전방
            else if (Keyboard.current.downArrowKey.isPressed) discreteActions[1] = 1; // 후방
            else if (Keyboard.current.leftArrowKey.isPressed) discreteActions[1] = 2; // 왼쪽
            else if (Keyboard.current.rightArrowKey.isPressed) discreteActions[1] = 3; // 오른쪽
            else discreteActions[1] = Random.Range(0, 4); // 방향키 입력 없으면 랜덤 회피
        }
    }
}
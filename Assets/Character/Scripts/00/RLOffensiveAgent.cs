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
        // 에피소드가 시작될 때마다 상대방의 참조가 유효한지 다시 확인합니다.
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
                Debug.LogError($"'{enemyTransform.name}' 오브젝트에 AgentController 컴포넌트가 없습니다!", enemyTransform);
                enemyBlackboard = null;
            }
        }
        else
        {
            Debug.LogError("'enemyTransform'이(가) 인스펙터에 할당되지 않았습니다!", this.gameObject);
        }

        // --- 체력 및 이전 상태 초기화 ---
        myBlackboard.currentHealth = myBlackboard.maxHealth;
        previousMyHealth = myBlackboard.maxHealth; // 에피소드 시작 시 내 체력 초기화

        if (enemyBlackboard != null)
        {
            enemyBlackboard.currentHealth = enemyBlackboard.maxHealth;
            previousEnemyHealth = enemyBlackboard.maxHealth; // [추가] 적 체력 초기화
        }

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

        // --- [추가] 핵심 초기화 로직: 모든 관련 플래그와 상태 초기화 ---

        // 1. AgentController의 내부 플래그 초기화 및 활성화
        if (myController != null)
        {
            myController.ResetAllFlags(); // AgentController에 ResetAllFlags() 메서드 추가 필요
            myController.enabled = true; // 스크립트 활성화 확인
            // Debug.Log($"{gameObject.name}의 myController 활성화됨.");
        }
        // 적 에이전트의 컨트롤러도 리셋 (상대방이 RL이든 BT든 일관된 초기화를 위해 필요)
        if (enemyController != null)
        {
            enemyController.ResetAllFlags(); // AgentController에 ResetAllFlags() 메서드 추가 필요
            enemyController.enabled = true; // 스크립트 활성화 확인
            // Debug.Log($"{enemyTransform.name}의 enemyController 활성화됨.");
        }

        // 2. AgentBlackboard의 모든 상태 플래그 초기화
        myBlackboard.isAttacking = false;
        myBlackboard.isDefending = false; // 방어 상태 초기화
        myBlackboard.isEvading = false;
        myBlackboard.isInvincible = false;
        myBlackboard.isGetAttacked = false;
        myBlackboard.canCounterAttack = false;
        myBlackboard.lastEnemyAttackTime = 0f; // 적의 마지막 공격 시간 초기화

        if (enemyBlackboard != null)
        {
            // 상대방의 Blackboard도 초기화 (상대방도 RL 에이전트일 경우)
            enemyBlackboard.isAttacking = false;
            enemyBlackboard.isDefending = false; // 상대방 방어 상태 초기화
            enemyBlackboard.isEvading = false;
            enemyBlackboard.isInvincible = false;
            enemyBlackboard.isGetAttacked = false;
            enemyBlackboard.canCounterAttack = false;
            // enemyBlackboard.lastEnemyAttackTime은 상대방 에이전트의 blackboard에서 관리되므로 여기서 직접 초기화할 필요 없음
        }

        // 3. 현재 RL Agent 스크립트 자체도 활성화 확인 (안전 장치)
        this.enabled = true;
        // Debug.Log($"{gameObject.name}의 RLOffensiveAgent 활성화됨.");

        // --- [추가 끝] ---
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
        sensor.AddObservation(myBlackboard.isInvincible); // [추가] 무적 상태 여부

        if (enemyTransform == null)
        {
            sensor.AddObservation(new float[6]); // 상대가 없으면 0으로 채움 (기존 5개 + isDefending 1개)
            return;
        }

        // 상대방 정보
        Vector3 relativePos = transform.InverseTransformPoint(enemyTransform.position);
        sensor.AddObservation(relativePos.x); // 상대적 위치 X
        sensor.AddObservation(relativePos.z); // 상대적 위치 Z
        sensor.AddObservation(Vector3.Distance(transform.position, enemyTransform.position)); // 상대와의 거리

        sensor.AddObservation(enemyBlackboard.currentHealth / enemyBlackboard.maxHealth); // 정규화된 상대 체력
        sensor.AddObservation(enemyBlackboard.isAttacking); // 상대가 공격 중인지 여부
        sensor.AddObservation(enemyBlackboard.isDefending); // [추가] 상대가 방어 중인지 여부 (총 6개 관찰)
    }

    /// <summary>
    /// 회피 성공 여부를 잠시 뒤에 체크하는 코루틴
    /// </summary>
    /// <param name="delay">체크 전 대기 시간</param>
    private IEnumerator CheckDodgeSuccess(float delay)
    {
        bool enemyWasAttacking = (enemyBlackboard != null && enemyBlackboard.isAttacking);

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
        Debug.Log($"[RL Offensive Agent] OnActionReceived 호출됨. 받은 액션: {actions.DiscreteActions[0]}"); // 회피방향 로그 추가

        int mainAction = actions.DiscreteActions[0];

        switch (mainAction)
        {
            case 0: // 대기
                myController.Idle();
                break;
            case 1: // 적에게 다가가기 (Forward)
                // stopDistance를 0f 대신 공격 범위의 0.8f 정도로 조정하여, 에이전트가 공격 범위 근처까지 가면
                // 이동 행동이 SUCCESS를 반환하고 다음 행동(공격)으로 넘어갈 수 있도록 유도
                myController.MoveTowards(enemyTransform.position, 5f, myController.attackRange * 0.8f);
                break;
            case 2: // 적에게서 멀어지기 (Backward)
                myController.MoveAwayFrom(enemyTransform.position, 3f, myController.attackRange + 1.0f); // 공격 범위 밖으로 충분히 멀어지도록
                break;
            case 3: // 공격
                if (myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
                {
                    myController.PerformAttack();
                    AddReward(0.05f); // 공격 시도 시 보상 (0.005f -> 0.05f로 상향, RLDefensiveAgent와 균형 맞춤)
                }
                break;
            case 4: // 회피
                // 이산 행동 공간에서 회피 방향도 포함되어 있으므로, actions.DiscreteActions[1]를 사용
                int evadeDirection = actions.DiscreteActions[1];
                if (myController.PerformDirectionalEvade(evadeDirection) == NodeStatus.SUCCESS)
                {

                    StartCoroutine(CheckDodgeSuccess(0.5f));
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

            // [수정] 거리 보상을 더 정교하게: 공격 범위 내에 있으면 보상, 너무 멀면 페널티
            if (currentDistance <= myController.attackRange + 0.1f) // 공격 범위 근처에 있으면 보상
            {
                AddReward(0.002f); // 0.01f -> 0.02f로 상향
            }
            else if (currentDistance > myController.attackRange + 2.0f) // 공격 범위에서 너무 멀면 페널티
            {
                AddReward(-0.001f);
            }
            // 기존 previousDistanceToEnemy 기반 보상은 제거하거나 통합 (아래 체력 우위/열세에 집중)
            // if (currentDistance < previousDistanceToEnemy)
            // {
            //     AddReward(0.01f);
            // }
            // else if (currentDistance > previousDistanceToEnemy)
            // {
            //     AddReward(-0.01f);
            // }
            previousDistanceToEnemy = currentDistance; // 다음 프레임을 위해 거리 업데이트는 유지
        }

        // 3. 체력 우위 보상
        if (myBlackboard != null && enemyBlackboard != null)
        {
            float healthAdvantage = (myBlackboard.currentHealth - enemyBlackboard.currentHealth) / myBlackboard.maxHealth;
            AddReward(healthAdvantage * 0.003f);
        }

        // 4. 잃은 체력에 대한 페널티
        if (myBlackboard != null)
        {
            float healthLost = previousMyHealth - myBlackboard.currentHealth;
            if (healthLost > 0)
            {
                AddReward(-healthLost / myBlackboard.maxHealth * 0.5f); // 페널티 강도 조절 (1.0f는 임의 값)
            }
            previousMyHealth = myBlackboard.currentHealth;
        }

        // 5. 적에게 데미지 적용 보상
        // RLDefensiveAgent처럼 previousEnemyHealth를 사용하여 적의 체력 손실을 측정하는 것이 더 정확합니다.
        if (enemyBlackboard != null)
        {
            float enemyHealthLost = previousEnemyHealth - enemyBlackboard.currentHealth;
            if (enemyHealthLost > 0)
            {
                AddReward(enemyHealthLost / enemyBlackboard.maxHealth * 3.0f); // 데미지 적용 보상
            }
            previousEnemyHealth = enemyBlackboard.currentHealth;
        }

        // 6. 게임 종료 조건 (승리의 가치를 매우 높게 설정)
        if (myBlackboard != null && myBlackboard.currentHealth <= 0)
        {
            SetReward(-30.0f); // 패배의 고통
            EndEpisode();
        }
        else if (enemyBlackboard != null && enemyBlackboard.currentHealth <= 0)
        {
            float episodeDuration = Time.time - episodeStartTime; // 에피소드 진행 시간
            float timeBonus = 0f;

            if (episodeDuration < episodeDurationForMaxBonus)
            {
                timeBonus = maxEpisodeTimeBonus;
            }
            else
            {
                timeBonus = Mathf.Max(0, maxEpisodeTimeBonus * (1.0f - (episodeDuration / (episodeDurationForMaxBonus * 2.0f))));
            }

            SetReward(10.0f + timeBonus); // 기본 승리 보상 + 시간 보너스
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
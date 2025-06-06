using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class RLOffensiveAgent : Agent
{
    [Header("Agent Components")]
    [SerializeField] private Transform enemyTransform;
    private AgentController myController; // 기존 AgentController를 참조하여 실제 행동을 실행
    private AgentBlackboard myBlackboard;

    private AgentController enemyController;
    private AgentBlackboard enemyBlackboard;

    // --- [추가 시작] ---
    // 에이전트의 초기 위치와 회전 값을 저장할 변수
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    // --- [추가 끝] ---


    // --- [추가 시작] ---
    // '상대방'의 초기 위치와 회전 값을 저장할 변수
    private Vector3 enemyInitialPosition;
    private Quaternion enemyInitialRotation;
    // --- [추가 끝] ---

    private float previousDistanceToEnemy;

    public override void Initialize()
    {
        myController = GetComponent<AgentController>();
        myBlackboard = myController.blackboard;

        if (enemyTransform != null)
        {
            enemyController = enemyTransform.GetComponent<AgentController>();
            enemyBlackboard = enemyController.blackboard;
        }

        // --- [추가 시작] ---
        // 이 스크립트가 처음 초기화될 때, 시작 위치와 회전 값을 저장합니다.
        this.initialPosition = transform.position;
        this.initialRotation = transform.rotation;
        // --- [추가 끝] ---

        // --- [추가 시작] ---
        // 상대방의 초기 위치와 회전 값도 함께 저장합니다.
        if (enemyTransform != null)
        {
            this.enemyInitialPosition = enemyTransform.position;
            this.enemyInitialRotation = enemyTransform.rotation;
        }
        // --- [추가 끝] ---

    }

    /// <summary>
    /// 에피소드(라운드) 시작 시 호출
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // --- [수정된 부분 시작] ---
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
                // 디버깅을 위해 오류 메시지를 남깁니다.
                Debug.LogError($"'{enemyTransform.name}' 오브젝트에 AgentController 컴포넌트가 없습니다!", enemyTransform);
                enemyBlackboard = null; // 확실하게 null로 설정
            }
        }
        else
        {
             Debug.LogError("'enemyTransform'이(가) 인스펙터에 할당되지 않았습니다!", this.gameObject);
        }
        // 에이전트 및 상대방 위치, 체력 등 초기화
        // 예: 시작 위치로 리셋, 체력 100으로 리셋
        myBlackboard.currentHealth = myBlackboard.maxHealth;
        if (enemyBlackboard != null)
        {
            enemyBlackboard.currentHealth = enemyBlackboard.maxHealth;
        }
        // ... 캐릭터 위치 리셋 로직 ...
        // --- [추가] 캐릭터 위치 및 물리 상태 리셋 로직 ---
        // 저장해두었던 초기 위치와 회전 값으로 되돌립니다.
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // 물리적 충돌이나 움직임으로 인한 잔류 속도를 제거합니다.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // --- [추가 끝] ---

        // --- [추가 시작] 상대방 위치 리셋 ---
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

        if (enemyTransform == null)
        {
            sensor.AddObservation(new float[5]); // 상대가 없으면 0으로 채움
            return;
        }

        // 상대방 정보
        Vector3 relativePos = transform.InverseTransformPoint(enemyTransform.position);
        sensor.AddObservation(relativePos.x); // 상대적 위치 X
        sensor.AddObservation(relativePos.z); // 상대적 위치 Z
        sensor.AddObservation(Vector3.Distance(transform.position, enemyTransform.position)); // 상대와의 거리

        sensor.AddObservation(enemyBlackboard.currentHealth / enemyBlackboard.maxHealth); // 정규화된 상대 체력
        sensor.AddObservation(enemyBlackboard.isAttacking); // 상대가 공격 중인지 여부
    }

    /// <summary>
    /// 행동(Action) 실행
    /// 정책(Brain)으로부터 받은 행동 명령을 실제 게임 월드에서 실행합니다.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // [디버그 추가] 이 메소드가 호출되는지 확인
        Debug.Log($"[RL Agent] 1. OnActionReceived 호출됨. 받은 액션: {actions.DiscreteActions[0]}");

        int action = actions.DiscreteActions[0];

        switch (action)
        {
            case 0:
                myController.Idle();
                break;
            case 1:
                myController.MoveTowards(enemyTransform.position, 5f, 0f);
                break;
            case 2:
                myController.MoveAwayFrom(enemyTransform.position, 3f, 5f);
                break;
            case 3:
                if (myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
                {
                    myController.PerformAttack();
                    AddReward(0.005f);
                }

                    break;
            case 4:
                if (myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
                    myController.PerformEvade();
                break;
        }

        HandleRewards();
    }

    /// <summary>
    /// 보상(Reward) 설계
    /// </summary>
    private void HandleRewards()
    {
       

        // 2. 적에게 다가가면 보상 (적극적인 움직임 유도)
        if (enemyTransform != null)
        {
            float currentDistance = Vector3.Distance(transform.position, enemyTransform.position);
            // 거리가 가까워졌다면
            if (currentDistance < previousDistanceToEnemy)
            {
                AddReward(0.01f); // 작은 보상
            }
            previousDistanceToEnemy = currentDistance; // 현재 거리 업데이트
        }



        // 2. 체력 우위 보상 (가장 강력한 공격 유도 신호)
        // 내 체력이 상대보다 높을수록 계속 보상을 받으므로, 상대를 때릴 동기가 매우 강력해집니다.
        if (myBlackboard != null && enemyBlackboard != null)
        {
            float healthAdvantage = (myBlackboard.currentHealth - enemyBlackboard.currentHealth) / myBlackboard.maxHealth;
            AddReward(healthAdvantage * 0.03f); // 체력 차이에 비례하는 보상
        }

        // 3. 공격 성공 보상 (가치는 그대로 유지)
        if (enemyBlackboard != null && enemyBlackboard.isGetAttacked)
        {
            AddReward(2.0f);
        }

        // 4. 피격 페널티 대폭 감소
        // "맞는 건 좀 아프지만, 때리는 것에 비하면 아무것도 아니야!" 라는 생각을 갖게 합니다.
        if (myBlackboard != null && myBlackboard.isGetAttacked)
        {
            AddReward(-0.4f); // -1.0에서 -0.4로 페널티를 크게 줄입니다.
        }

        // 5. 게임 종료 조건 (승리의 가치를 매우 높게 설정)
        if (myBlackboard != null && myBlackboard.currentHealth <= 0)
        {
            SetReward(-10.0f); // 패배의 고통
            EndEpisode();
        }
        else if (enemyBlackboard != null && enemyBlackboard.currentHealth <= 0)
        {
            SetReward(10.0f); // 승리의 환희
            EndEpisode();
        }
    }

    /// <summary>
    /// 휴리스틱 모드: 개발자가 직접 키보드로 조작하여 테스트할 때 사용
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("[RL Agent] Heuristic() 메소드가 호출되었습니다!");

        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0; // 기본값: 대기

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
    }
}
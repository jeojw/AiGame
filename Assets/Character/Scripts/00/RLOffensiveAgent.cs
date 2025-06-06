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
                Debug.LogError($"'{enemyTransform.name}' 오브젝트에 AgentController 컴포넌트가 없습니다!", enemyTransform);
                enemyBlackboard = null;
            }
        }
        else
        {
            Debug.LogError("'enemyTransform'이(가) 인스펙터에 할당되지 않았습니다!", this.gameObject);
        }

        myBlackboard.currentHealth = myBlackboard.maxHealth;
        previousMyHealth = myBlackboard.maxHealth; // 에피소드 시작 시 내 체력 초기화

        if (enemyBlackboard != null)
        {
            enemyBlackboard.currentHealth = enemyBlackboard.maxHealth;
        }

        // --- [추가] 캐릭터 위치 및 물리 상태 리셋 로직 ---
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

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

        // --- [추가 시작: 빠른 승리 보상을 위한 시간 초기화] ---
        episodeStartTime = Time.time; // 에피소드 시작 시간 기록
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
        Debug.Log($"[RL Agent] 1. OnActionReceived 호출됨. 받은 액션: {actions.DiscreteActions[0]}");

        int mainAction = actions.DiscreteActions[0];

        switch (mainAction)
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
                    AddReward(0.005f); // 공격 시도 시 보상
                }
                break;
            case 4: // 회피
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
            if (currentDistance < previousDistanceToEnemy)
            {
                AddReward(0.01f);
            }
            else if (currentDistance > previousDistanceToEnemy)
            {
                AddReward(-0.01f);
            }
            previousDistanceToEnemy = currentDistance;
        }

        // 3. 체력 우위 보상
        if (myBlackboard != null && enemyBlackboard != null)
        {
            float healthAdvantage = (myBlackboard.currentHealth - enemyBlackboard.currentHealth) / myBlackboard.maxHealth;
            AddReward(healthAdvantage * 0.03f);
        }

        // 4. 잃은 체력에 대한 페널티
        if (myBlackboard != null)
        {
            float healthLost = previousMyHealth - myBlackboard.currentHealth;
            if (healthLost > 0)
            {
                AddReward(-healthLost / myBlackboard.maxHealth * 1.0f); // 페널티 강도 조절 (1.0f는 임의 값)
            }
            previousMyHealth = myBlackboard.currentHealth;
        }

        // 5. 공격 성공 보상
        if (enemyBlackboard != null && enemyBlackboard.isGetAttacked)
        {
            AddReward(2.0f);
        }

        // 6. 게임 종료 조건 (승리의 가치를 매우 높게 설정)
        if (myBlackboard != null && myBlackboard.currentHealth <= 0)
        {
            SetReward(-10.0f); // 패배의 고통
            EndEpisode();
        }
        else if (enemyBlackboard != null && enemyBlackboard.currentHealth <= 0)
        {
            // --- [수정 시작: 빠른 승리 보상 로직 추가] ---
            float episodeDuration = Time.time - episodeStartTime; // 에피소드 진행 시간
            float timeBonus = 0f;

            if (episodeDuration < episodeDurationForMaxBonus)
            {
                // 매우 빠르게 이겼다면 최대 보상
                timeBonus = maxEpisodeTimeBonus;
            }
            else
            {
                // 시간에 따라 보상이 감소 (선형 감소 예시)
                // 시간이 길어질수록 보상이 0에 가까워집니다.
                // 10초에 0 보상이라면, 5초에 0.5 * maxEpisodeTimeBonus
                timeBonus = Mathf.Max(0, maxEpisodeTimeBonus * (1.0f - (episodeDuration / (episodeDurationForMaxBonus * 2.0f))));
                // 위 수식은 예시이며, 에피소드 지속 시간이 (episodeDurationForMaxBonus * 2.0f)를 초과하면 보상이 0이 됩니다.
                // 더 부드러운 감소 곡선이 필요하면 다른 함수(예: 지수 함수)를 사용할 수 있습니다.
            }

            // 승리 보상에 시간 보너스 추가
            SetReward(10.0f + timeBonus); // 기본 승리 보상 + 시간 보너스
            // --- [수정 끝] ---
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
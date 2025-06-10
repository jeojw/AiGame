// File: RLDefensiveAgent.cs
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using System.Collections;

public class RLDefensiveAgent : Agent
{
    [Header("Agent Components")]
    [SerializeField] private Transform enemyTransform;
    private AgentController myController;
    private DefensiveAgentController myDefensiveController;
    private AgentBlackboard myBlackboard;

    private AgentController enemyController;
    private AgentBlackboard enemyBlackboard;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 enemyInitialPosition;
    private Quaternion enemyInitialRotation;

    private float previousDistanceToEnemy;
    private float previousMyHealth;
    private float previousEnemyHealth;

    private float episodeStartTime;
    [SerializeField] private float maxEpisodeTimeBonus = 50.0f;
    [SerializeField] private float episodeDurationForMaxBonus = 15.0f;


    public override void Initialize()
    {
        myController = GetComponent<AgentController>();
        myDefensiveController = GetComponent<DefensiveAgentController>();
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

        this.initialPosition = transform.position;
        this.initialRotation = transform.rotation;

        if (enemyTransform != null)
        {
            this.enemyInitialPosition = enemyTransform.position;
            this.enemyInitialRotation = enemyTransform.rotation;
        }
    }

    public override void OnEpisodeBegin()
    {
        // UIManager 활성화
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.enabled = true;
        }

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


        // 내 체력 및 이전 상태 초기화
        myBlackboard.currentHealth = myBlackboard.maxHealth;
        previousMyHealth = myBlackboard.maxHealth;

        // 적 체력 및 이전 상태 초기화
        // enemyBlackboard가 이제 항상 유효한 인스턴스임을 보장합니다.
        enemyBlackboard.currentHealth = enemyBlackboard.maxHealth;
        previousEnemyHealth = enemyBlackboard.maxHealth;

        // 위치 및 물리 상태 리셋
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

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

        // 에피소드 시작 시간 초기화
        episodeStartTime = Time.time;

        // 모든 관련 플래그와 상태 초기화
        if (myController != null)
        {
            myController.ResetAllFlags();
            myController.enabled = true;
        }
        if (enemyController != null) // 적 에이전트의 컨트롤러가 존재할 경우에만 리셋
        {
            enemyController.ResetAllFlags();
            enemyController.enabled = true;
        }

        // 내 AgentBlackboard 상태 초기화
        myBlackboard.isAttacking = false;
        myBlackboard.isDefending = false;
        myBlackboard.isEvading = false;
        myBlackboard.isInvincible = false;
        myBlackboard.isGetAttacked = false;
        myBlackboard.canCounterAttack = false;
        myBlackboard.isDead = false;
        myBlackboard.lastEnemyAttackTime = 0f;
        myBlackboard.recentlyDefended = false;

        // 적 AgentBlackboard 상태 초기화 (enemyBlackboard는 이제 항상 유효합니다)
        enemyBlackboard.isAttacking = false;
        enemyBlackboard.isDefending = false;
        enemyBlackboard.isEvading = false;
        enemyBlackboard.isInvincible = false;
        enemyBlackboard.isGetAttacked = false;
        enemyBlackboard.canCounterAttack = false;
        enemyBlackboard.isDead = false;
        enemyBlackboard.lastEnemyAttackTime = 0f; // [추가] 적의 마지막 공격 시간 명시적 초기화
        enemyBlackboard.recentlyDefended = false; // [추가] 적의 recentlyDefended 플래그 명시적 초기화
        enemyBlackboard.score = 0; // [추가] 적 점수 초기화 (관찰/보상에 영향 줄 수 있음)
        enemyBlackboard.attackCount = 0; // [추가] 적 스탯 초기화
        enemyBlackboard.defendCount = 0;
        enemyBlackboard.counterAttackCount = 0;
        enemyBlackboard.evadeCount = 0;

        // 현재 RL Agent 스크립트 활성화 확인
        this.enabled = true;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(myBlackboard.currentHealth / myBlackboard.maxHealth);
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY));
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.DEFEND_COOLDOWN_KEY));
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY));
        sensor.AddObservation(myBlackboard.isInvincible);

        // enemyBlackboard가 null일 경우를 대비하여 방어 코드 추가 (더미 생성으로 이제 불필요할 수 있지만 안전상 유지)
        if (enemyTransform == null)
        {
            sensor.AddObservation(new float[6]); // 상대가 없으면 0으로 채움
            return;
        }

        Vector3 relativePos = transform.InverseTransformPoint(enemyTransform.position);
        sensor.AddObservation(relativePos.x);
        sensor.AddObservation(relativePos.z);
        sensor.AddObservation(Vector3.Distance(transform.position, enemyTransform.position));

        // enemyBlackboard는 이제 항상 유효한 인스턴스이므로 NullReferenceException 걱정 없이 접근 가능
        sensor.AddObservation(enemyBlackboard.currentHealth / enemyBlackboard.maxHealth);
        sensor.AddObservation(enemyBlackboard.isAttacking);
        sensor.AddObservation(enemyBlackboard.isDefending);
    }

    private IEnumerator CheckDodgeSuccess(float delay)
    {
        // enemyBlackboard는 이제 항상 유효한 인스턴스임을 보장
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

    private IEnumerator CheckDefendSuccess(float delay)
    {
        // enemyBlackboard는 이제 항상 유효한 인스턴스임을 보장
        bool enemyWasAttacking = enemyBlackboard.isAttacking;

        if (!enemyWasAttacking)
        {
            yield break;
        }

        float healthBeforeDefend = myBlackboard.currentHealth;

        yield return new WaitForSeconds(delay);

        if (myBlackboard.currentHealth >= healthBeforeDefend - 0.1f)
        {
            Debug.Log("방어 성공! 보상 +1.0");
            AddReward(1.0f);
        }
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        // Debug.Log($"[RL Defensive Agent] 1. OnActionReceived 호출됨. 메인 액션: {actions.DiscreteActions[0]}, 회피방향: {actions.DiscreteActions[1]}");

        int mainAction = actions.DiscreteActions[0];
        int evadeDirection = actions.DiscreteActions[1];

        // 매 프레임마다 무적 상태가 아닌 경우 isInvincible을 false로 강제 설정
        if (mainAction != 4) // Action 4 is PerformDefend
        {
            myBlackboard.isInvincible = false;
        }

        switch (mainAction)
        {
            case 0:
                myController.Idle();
                break;
            case 1:
                myController.MoveTowards(enemyTransform.position, 3f, 5f);
                break;
            case 2:
                myController.MoveAwayFrom(enemyTransform.position, 3f, 7f);
                break;
            case 3: // 공격 (카운터 공격 포함)
                if (myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
                {
                    if (myBlackboard.canCounterAttack && myDefensiveController != null)
                    {
                        myController.PerformAttack(myDefensiveController.counterDamageMultiplier);
                        AddReward(1.0f);
                    }
                    else
                    {
                        myController.PerformAttack();
                        AddReward(0.005f);
                    }
                }
                break;
            case 4:
                if (myBlackboard.IsActionReady(AgentBlackboard.DEFEND_COOLDOWN_KEY))
                {
                    myController.PerformDefend();
                    AddReward(0.01f);
                    StartCoroutine(CheckDefendSuccess(0.5f));
                }
                break;
            case 5:
                if (myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
                {
                    if (myController.PerformDirectionalEvade(evadeDirection) == NodeStatus.SUCCESS)
                    {
                        AddReward(0.01f);
                        StartCoroutine(CheckDodgeSuccess(0.5f));
                    }
                }
                break;
        }

        HandleRewards();
    }

    private void HandleRewards()
    {
        AddReward(-0.002f);

        if (enemyTransform != null)
        {
            float currentDistance = Vector3.Distance(transform.position, enemyTransform.position);
            float idealMinDist = 5f;
            float idealMaxDist = 7f;

            if (currentDistance >= idealMinDist && currentDistance <= idealMaxDist)
            {
                AddReward(0.01f);
            }
            else if (currentDistance < idealMinDist)
            {
                AddReward(-0.005f);
            }
        }

        // myBlackboard와 enemyBlackboard는 이제 항상 유효한 인스턴스임을 보장
        float healthAdvantage = (myBlackboard.currentHealth - enemyBlackboard.currentHealth) / myBlackboard.maxHealth;
        AddReward(healthAdvantage * 0.05f);

        if (myBlackboard != null)
        {
            float healthLost = previousMyHealth - myBlackboard.currentHealth;
            if (healthLost > 0)
            {
                AddReward(-healthLost / myBlackboard.maxHealth * 0.5f);
            }
            previousMyHealth = myBlackboard.currentHealth;
        }

        // enemyBlackboard는 이제 항상 유효한 인스턴스임을 보장
        float enemyHealthLost = previousEnemyHealth - enemyBlackboard.currentHealth;
        if (enemyHealthLost > 0)
        {
            AddReward(enemyHealthLost / enemyBlackboard.maxHealth * 1.0f);
        }
        previousEnemyHealth = enemyBlackboard.currentHealth;

        if (myBlackboard != null && myBlackboard.currentHealth <= 0)
        {
            SetReward(-10.0f);
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

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("[RL Defensive Agent] Heuristic() 메소드가 호출되었습니다!");

        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;
        discreteActions[1] = 0;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.wKey.isPressed) discreteActions[0] = 1;
        if (Keyboard.current.sKey.isPressed) discreteActions[0] = 2;
        if (Keyboard.current.spaceKey.isPressed) discreteActions[0] = 3;
        if (Keyboard.current.qKey.isPressed) discreteActions[0] = 4;
        if (Keyboard.current.eKey.isPressed) discreteActions[0] = 5;

        if (discreteActions[0] == 5)
        {
            if (Keyboard.current.upArrowKey.isPressed) discreteActions[1] = 0;
            else if (Keyboard.current.downArrowKey.isPressed) discreteActions[1] = 1;
            else if (Keyboard.current.leftArrowKey.isPressed) discreteActions[1] = 2;
            else if (Keyboard.current.rightArrowKey.isPressed) discreteActions[1] = 3;
            else discreteActions[1] = Random.Range(0, 4);
        }
    }
}
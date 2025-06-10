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
    private DefensiveAgentController myDefensiveController; // [추가] DefensiveAgentController 타입 참조
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
        myDefensiveController = GetComponent<DefensiveAgentController>(); // [추가] DefensiveAgentController 컴포넌트 가져오기
        myBlackboard = myController.blackboard;

        if (enemyTransform != null)
        {
            enemyController = enemyTransform.GetComponent<AgentController>();
            enemyBlackboard = enemyController.blackboard;
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
        previousMyHealth = myBlackboard.maxHealth;

        if (enemyBlackboard != null)
        {
            enemyBlackboard.currentHealth = enemyBlackboard.maxHealth;
            previousEnemyHealth = enemyBlackboard.maxHealth;
        }

        // --- 위치 및 물리 상태 리셋 ---
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

        // --- 빠른 승리 보상을 위한 시간 초기화 ---
        episodeStartTime = Time.time;

        // --- [추가] 핵심 초기화 로직: 모든 관련 플래그와 상태 초기화 ---

        // 1. AgentController의 내부 플래그 초기화
        if (myController != null)
        {
            myController.ResetAllFlags(); // 위에서 추가한 메서드 호출
            myController.enabled = true; // 스크립트가 비활성화되어 있었다면 다시 활성화
        }
        if (enemyController != null) // 적 에이전트의 컨트롤러도 리셋
        {
            enemyController.ResetAllFlags();
            enemyController.enabled = true; // 적 에이전트 컨트롤러 활성화
        }


        // 2. AgentBlackboard의 모든 상태 플래그 초기화
        myBlackboard.isAttacking = false;
        myBlackboard.isDefending = false;
        myBlackboard.isEvading = false;
        myBlackboard.isInvincible = false;
        myBlackboard.isGetAttacked = false;
        myBlackboard.canCounterAttack = false;
        myBlackboard.isDead = false;
        myBlackboard.lastEnemyAttackTime = 0f; // 적의 마지막 공격 시간 초기화

        if (enemyBlackboard != null)
        {
            enemyBlackboard.isAttacking = false;
            enemyBlackboard.isDefending = false;
            enemyBlackboard.isEvading = false;
            enemyBlackboard.isInvincible = false;
            enemyBlackboard.isGetAttacked = false;
            enemyBlackboard.canCounterAttack = false;
            enemyBlackboard.isDead = false;
            // enemyBlackboard.lastEnemyAttackTime은 상대방 에이전트의 blackboard에서 초기화
        }

        // 3. 현재 RL Agent 스크립트 자체도 활성화 확인 (안전 장치)
        this.enabled = true;

        // --- [추가 끝] ---


    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(myBlackboard.currentHealth / myBlackboard.maxHealth);
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY));
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.DEFEND_COOLDOWN_KEY));
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY));
        sensor.AddObservation(myBlackboard.isInvincible);

        if (enemyTransform == null)
        {
            sensor.AddObservation(new float[6]);
            return;
        }

        Vector3 relativePos = transform.InverseTransformPoint(enemyTransform.position);
        sensor.AddObservation(relativePos.x);
        sensor.AddObservation(relativePos.z);
        sensor.AddObservation(Vector3.Distance(transform.position, enemyTransform.position));

        sensor.AddObservation(enemyBlackboard.currentHealth / enemyBlackboard.maxHealth);
        sensor.AddObservation(enemyBlackboard.isAttacking);
        sensor.AddObservation(enemyBlackboard.isDefending);
    }

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

    private IEnumerator CheckDefendSuccess(float delay)
    {
        bool enemyWasAttacking = (enemyBlackboard != null && enemyBlackboard.isAttacking);

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
        Debug.Log($"[RL Defensive Agent] 1. OnActionReceived 호출됨. 받은 액션: {actions.DiscreteActions[0]}, 회피방향: {actions.DiscreteActions[1]}");

        int mainAction = actions.DiscreteActions[0];
        int evadeDirection = actions.DiscreteActions[1];

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

        if (myBlackboard != null && enemyBlackboard != null)
        {
            float healthAdvantage = (myBlackboard.currentHealth - enemyBlackboard.currentHealth) / myBlackboard.maxHealth;
            AddReward(healthAdvantage * 0.05f);
        }

        if (myBlackboard != null)
        {
            float healthLost = previousMyHealth - myBlackboard.currentHealth;
            if (healthLost > 0)
            {
                AddReward(-healthLost / myBlackboard.maxHealth * 0.5f);
            }
            previousMyHealth = myBlackboard.currentHealth;
        }

        if (enemyBlackboard != null)
        {
            float enemyHealthLost = previousEnemyHealth - enemyBlackboard.currentHealth;
            if (enemyHealthLost > 0)
            {
                AddReward(enemyHealthLost / enemyBlackboard.maxHealth * 1.0f);
            }
            previousEnemyHealth = enemyBlackboard.currentHealth;
        }

        if (myBlackboard != null && myBlackboard.currentHealth <= 0)
        {
            SetReward(-10.0f);
            EndEpisode();
        }
        else if (enemyBlackboard != null && enemyBlackboard.currentHealth <= 0)
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
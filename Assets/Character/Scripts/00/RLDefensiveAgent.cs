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
    private float previousMyHealth;

    private float episodeStartTime;
    [SerializeField] private float maxSurvivalBonus = 50.0f;
    [SerializeField] private float survivalTimeForMaxBonus = 15.0f;

    public enum DefenseState
    {
        None,
        Evading,
        Guarding
    }

    private DefenseState currentDefenseState = DefenseState.None;
    private float guardDamageReduction = 0.5f; // 예: 방어 시 50%만 받음
    private float evadeWindow = 0.5f; // 회피 성공 판정 시간

    private IEnumerator ResetDefenseStateAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        currentDefenseState = DefenseState.None;
    }


    public override void Initialize()
    {
        myController = GetComponent<AgentController>();
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

        myBlackboard.currentHealth = myBlackboard.maxHealth;
        previousMyHealth = myBlackboard.maxHealth;

        if (enemyBlackboard != null)
            enemyBlackboard.currentHealth = enemyBlackboard.maxHealth;

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 상대방 위치 리셋
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

        episodeStartTime = Time.time;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 내 정보
        sensor.AddObservation(myBlackboard.currentHealth / myBlackboard.maxHealth);
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY)); // 회피 가능 여부
        sensor.AddObservation(myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY)); // 공격 가능 여부

        if (enemyTransform == null)
        {
            sensor.AddObservation(new float[5]);
            return;
        }

        // 상대방 정보
        Vector3 relativePos = transform.InverseTransformPoint(enemyTransform.position);
        sensor.AddObservation(relativePos.x);
        sensor.AddObservation(relativePos.z);
        sensor.AddObservation(Vector3.Distance(transform.position, enemyTransform.position));
        sensor.AddObservation(enemyBlackboard.currentHealth / enemyBlackboard.maxHealth);
        sensor.AddObservation(enemyBlackboard.isAttacking);
    }

    private IEnumerator CheckDodgeSuccess(float delay)
    {
        bool enemyWasAttacking = (enemyBlackboard != null && enemyBlackboard.isAttacking);

        if (!enemyWasAttacking)
            yield break;

        float healthBeforeDodge = myBlackboard.currentHealth;

        yield return new WaitForSeconds(delay);

        if (myBlackboard.currentHealth >= healthBeforeDodge)
        {
            AddReward(1.0f); // 방어적 회피 성공 시 큰 보상
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int mainAction = actions.DiscreteActions[0];

        switch (mainAction)
        {
            case 0:
                myController.Idle();
                currentDefenseState = DefenseState.None;
                break;
            case 1:
                myController.MoveAwayFrom(enemyTransform.position, 5f, 0f);
                currentDefenseState = DefenseState.None;
                break;
            case 2:
                myController.MoveTowards(enemyTransform.position, 3f, 5f);
                currentDefenseState = DefenseState.None;
                break;
            case 3: // 회피
                if (myBlackboard.IsActionReady(AgentBlackboard.EVADE_COOLDOWN_KEY))
                {
                    int evadeDirection = actions.DiscreteActions[1];
                    if (myController.PerformDirectionalEvade(evadeDirection) == NodeStatus.SUCCESS)
                    {
                        currentDefenseState = DefenseState.Evading;
                        StartCoroutine(ResetDefenseStateAfter(evadeWindow));
                    }
                }
                break;
            case 4: // 방어
                currentDefenseState = DefenseState.Guarding;
                break;
            case 5: // 공격
                if (myBlackboard.IsActionReady(AgentBlackboard.ATTACK_COOLDOWN_KEY))
                {
                    myController.PerformAttack();
                    AddReward(0.001f);
                }
                currentDefenseState = DefenseState.None;
                break;
        }

        HandleRewards();
    }


    private void HandleRewards()
    {
        // 1. 기본 시간 보상 (생존 시간 증가 유도)
        AddReward(0.005f);

        // 2. 적과의 거리 유지 보상 (멀어질수록 보상)
        if (enemyTransform != null)
        {
            float currentDistance = Vector3.Distance(transform.position, enemyTransform.position);
            if (currentDistance > previousDistanceToEnemy)
                AddReward(0.01f);
            else if (currentDistance < previousDistanceToEnemy)
                AddReward(-0.01f);
            previousDistanceToEnemy = currentDistance;
        }

        // 3. 체력 유지 보상
        if (myBlackboard != null)
        {
            float healthLost = previousMyHealth - myBlackboard.currentHealth;
            if (healthLost > 0)
                AddReward(-healthLost / myBlackboard.maxHealth * 2.0f); // 방어 실패시 페널티 강화
            previousMyHealth = myBlackboard.currentHealth;
        }

        // 4. 공격 회피 성공 보상 (CheckDodgeSuccess에서 별도 부여)

        // 5. 게임 종료 조건
        if (myBlackboard != null && myBlackboard.currentHealth <= 0)
        {
            SetReward(-10.0f); // 패배
            EndEpisode();
        }
        else if (enemyBlackboard != null && enemyBlackboard.currentHealth <= 0)
        {
            // 방어 에이전트가 이긴 경우 (예외적 상황)
            float episodeDuration = Time.time - episodeStartTime;
            float survivalBonus = 0f;
            if (episodeDuration >= survivalTimeForMaxBonus)
                survivalBonus = maxSurvivalBonus;
            else
                survivalBonus = Mathf.Max(0, maxSurvivalBonus * (episodeDuration / survivalTimeForMaxBonus));
            SetReward(10.0f + survivalBonus);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.sKey.isPressed) discreteActions[0] = 1; // 뒤로
        if (Keyboard.current.wKey.isPressed) discreteActions[0] = 2; // 앞으로
        if (Keyboard.current.leftShiftKey.isPressed) discreteActions[0] = 3; // 회피
        if (Keyboard.current.leftCtrlKey.isPressed) discreteActions[0] = 4; // 방어
        if (Keyboard.current.spaceKey.isPressed) discreteActions[0] = 5; // 공격
    }
}
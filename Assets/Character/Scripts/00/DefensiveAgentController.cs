// File: DefensiveAgentController.cs (DefensiveAgentController.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class DefensiveAgentController : AgentController
{
    private float defensiveStanceRange = 7f;
    private float counterAttackHealthThreshold = 50f;
    private float _counterDamageMultiplier = 2.0f; // private 필드로 변경

    // [추가] public 속성으로 외부에서 값을 읽을 수 있도록 함
    public float counterDamageMultiplier
    {
        get { return _counterDamageMultiplier; }
        // 필요에 따라 set도 추가할 수 있지만, 일반적으로는 읽기 전용으로 두는 것이 좋습니다.
    }
    protected override void InitializeBehaviorTree()
    {
        

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // --- [수정] 1순위: 적이 5초 이상 공격하지 않으면 '다가가서' 공격 ---
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                // 조건 1: 적이 5초 이상 가만히 있었는가? (기존과 동일)
                new IsEnemyIdleForDurationCondition(blackboard, transform, 5.0f),
                // 조건 2: 내 공격 쿨타임이 준비되었는가? (기존과 동일)
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY),

                // --- 핵심 수정: Selector를 이용한 거리별 행동 분기 ---
                new BTSelector(blackboard, transform, new List<BTNode>
                {
                    // 우선 순위 1: 이미 공격 범위 안이라면 즉시 공격
                    new BTSequence(blackboard, transform, new List<BTNode>
                    {
                        new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange),
                        new ProactiveAttackEnemyAction(blackboard, transform)
                    }),

                    // 우선 순위 2: 공격 범위 밖이라면 적에게 접근 (위 시퀀스가 실패했을 때만 실행됨)
                    // 적 공격 범위의 90% 지점까지 다가가도록 설정합니다.
                    new MoveTowardsEnemyAction(blackboard, transform, 5f, attackRange * 0.9f)
                })
            }),
            // --- [수정] 적의 공격에 반응하는 로직 ---
            // 이제 방어를 회피보다 항상 먼저 시도합니다.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyAttackingCondition(blackboard, transform), // 적이 공격 중인가?
                new BTSelector(blackboard, transform, new List<BTNode> // 방어 또는 회피 중 하나를 선택
                {
                    new BTSequence(blackboard, transform, new List<BTNode> {
                        //new IsNotCooldwonReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDown_KEY), // 이 조건은 필요 없습니다.
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.DEFEND_COOLDOWN_KEY),
                        new IsNotEvadingCondition(blackboard, transform), // [추가] 회피 중이 아닌지 확인
                        new DefendAction(blackboard, transform),
                        new DefendSuccessCondition(blackboard, transform),

                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY),

                        new ChangeDefendToAttack(blackboard, transform),
                        new CanCounterAttackCondition(blackboard, transform),
                        new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange),

                        new CounterAttackAction(blackboard, transform, this, counterDamageMultiplier)
                    }),
                    // 2순위: 방어가 불가능하면 회피 시도
                    new BTSequence(blackboard, transform, new List<BTNode> {

                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY), // 회피 쿨타임이 준비되었는가?
                        new NotNode(new WasRecentlyDefendedCondition(blackboard, transform)), // [추가] 최근에 방어하지 않았는가?
                        new IsNotDefendingCondition(blackboard, transform), // [추가] 방어 중이 아닌지 확인
                        new EvadeAction(blackboard, transform) // 회피 행동
                    }),
                    // 만약 방어와 회피 모두 쿨타임이라면, 이 Selector는 실패하고 에이전트는 공격을 맞게 됩니다.
                })
            }),
            // ------------------------------------

            // 3. 위치 선정: 이상적인 방어 거리를 유지 (이전과 동일)
            new BTSelector(blackboard, transform, new List<BTNode>
            {
                new BTSequence(blackboard, transform, new List<BTNode> {
                    new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, defensiveStanceRange)),
                    new MoveTowardsEnemyAction(blackboard, transform, 3f, defensiveStanceRange * 0.9f)
                })
            }),

            // 5. 기본 대기 상태 (이전과 동일)
            new IdleAction(blackboard, transform)
        });
        

    }

    // [추가] PerformAttack 메서드를 오버라이드하여 수비자 전용 인터럽트 로직을 포함
    public override NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        // [수비자 전용 인터럽트 로직 시작]
        if (blackboard.enemyTransform != null)
        {
            AgentController enemyController = blackboard.enemyTransform.GetComponent<AgentController>();
            if (enemyController != null && enemyController != this)
            {
                // [수정] 지금이 반격 상황(canCounterAttack == true)이 아닐 때만 이 안전장치를 작동시킵니다.
                if (!blackboard.canCounterAttack && // ★★★ 이 조건을 추가!
                    enemyController.blackboard.isAttacking &&
                    !blackboard.isInvincible &&
                    Vector3.Distance(transform.position, enemyController.transform.position) <= enemyController.attackRange)
                {
                    Debug.Log($"[Defensive Agent] 공격 중 적의 위협적인 반격 감지 → 공격 중단하고 방어/회피 고려");
                    blackboard.isAttacking = false;
                    return NodeStatus.FAILURE;
                }
            }
        }
        // [수비자 전용 인터럽트 로직 끝]

        return base.PerformAttack(damageMultiplier);
    }

    // --- 이 스크립트 안에서만 사용할 새로운 노드들 ---
    public class IsCounterAttackReadyCondition : BTConditionNode
    {
        public IsCounterAttackReadyCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
        public override bool CheckCondition()
        {
            return blackboard.canCounterAttack;
        }
    }

    public class CounterAttackAction : BTActionNode
    {
        private DefensiveAgentController controller;
        // [추가] 데미지 배율을 저장할 변수
        private float multiplier;

        // [수정] 생성자에서 배율 값을 받도록 변경
        public CounterAttackAction(AgentBlackboard blackboard, Transform agentTransform, DefensiveAgentController ownerController, float damageMultiplier)
            : base(blackboard, agentTransform)
        {
            this.controller = ownerController;
            this.multiplier = damageMultiplier;
        }

        public override NodeStatus Tick()
        {

            
            NodeStatus status = controller.PerformAttack(this.multiplier); // 첫 번째 호출

            if (status == NodeStatus.SUCCESS || status == NodeStatus.RUNNING) // 공격이 성공적으로 시작되거나 진행 중이라면
            {
                blackboard.isInvincible = false;
                blackboard.canCounterAttack = false; // 카운터 어택 플래그 리셋
                Debug.Log("카운터 어택 시작! canCounterAttack 플래그 리셋.");
            }
            return status; // 첫 번째 호출의 결과를 반환합니다.
        }
    }

    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate;
        public NotNode(BTConditionNode condition) : base(condition.Blackboard, condition.AgentTransform) { this.conditionToNegate = condition; }
        public override bool CheckCondition() { return conditionToNegate.Tick() == NodeStatus.FAILURE; }
    }


}
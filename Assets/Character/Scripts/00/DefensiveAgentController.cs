// File: DefensiveAgentController.cs (DefensiveAgentController.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class DefensiveAgentController : AgentController
{
    private float defensiveStanceRange = 7f;
    private float counterAttackHealthThreshold = 50f;
    private float counterDamageMultiplier = 3.0f;

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
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                        new EvadeAction(blackboard, transform)
                    }),

                    new BTSequence(blackboard, transform, new List<BTNode> {
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.DEFEND_COOLDOWN_KEY),
                        new DefendAction(blackboard, transform),
                        new DefendSuccessCondition(blackboard, transform),

                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY),

                        new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange),
                        new NotNode(new IsHealthLowCondition(blackboard, transform, counterAttackHealthThreshold)),
                        new CounterAttackAction(blackboard, transform, this, counterDamageMultiplier),
                    }),
                    // 2순위: 방어가 불가능하면 회피 시도
                    
                    // 만약 방어와 회피 모두 쿨타임이라면, 이 Selector는 실패하고 에이전트는 공격을 맞게 됩니다.
                }),
                
            }),
            // ------------------------------------
            new BTSequence(blackboard, transform, new List<BTNode> {
                new IsGetAttackCondition(blackboard, transform),
                new GetAttackAction(blackboard, transform),
            }),
            // 3. 위치 선정: 이상적인 방어 거리를 유지 (이전과 동일)
            new BTSelector(blackboard, transform, new List<BTNode>
            {
                new BTSequence(blackboard, transform, new List<BTNode> {
                    new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, defensiveStanceRange)),
                    new MoveTowardsEnemyAction(blackboard, transform, 3f, defensiveStanceRange * 0.9f)
                })
            }),

            new IdleAction(blackboard, transform),

            new BTSequence(blackboard, transform, new List<BTNode> {
                    new IsDeadCondition(blackboard, transform),
                    new DeadAction(blackboard, transform)
            }),
        });
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
            // [수정] PerformAttack 호출 시 저장된 배율 값을 전달
            return controller.PerformAttack(this.multiplier);
        }
    }

    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate;
        public NotNode(BTConditionNode condition) : base(condition.Blackboard, condition.AgentTransform) { this.conditionToNegate = condition; }
        public override bool CheckCondition() { return conditionToNegate.Tick() == NodeStatus.FAILURE; }
    }


}


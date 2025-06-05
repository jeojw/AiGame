// File: DefensiveAgentController.cs (DefensiveAgentController.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class DefensiveAgentController : AgentController
{
    public float defensiveStanceRange = 7f;
    public float counterAttackHealthThreshold = 50f;
    public float counterDamageMultiplier = 2.5f;

    protected override void InitializeBehaviorTree()
    {
        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // 1. 최우선 순위: 반격! (이전과 동일)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsCounterAttackReadyCondition(blackboard, transform),
                new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY),
                new NotNode(new IsHealthLowCondition(blackboard, transform, counterAttackHealthThreshold)),
                new CounterAttackAction(blackboard, transform, this)
            }),

            // --- [수정] 적의 공격에 반응하는 로직 ---
            // 이제 방어를 회피보다 항상 먼저 시도합니다.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyAttackingCondition(blackboard, transform), // 적이 공격 중인가?
                new BTSelector(blackboard, transform, new List<BTNode> // 방어 또는 회피 중 하나를 선택
                {
                    // 1순위: 방어 시도
                    new BTSequence(blackboard, transform, new List<BTNode> {
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.DEFEND_COOLDOWN_KEY),
                        new DefendAction(blackboard, transform)
                    }),
                    // 2순위: 방어가 불가능하면 회피 시도
                    new BTSequence(blackboard, transform, new List<BTNode> {
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                        new EvadeAction(blackboard, transform)
                    })
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

            // 4. 일반 공격 (이전과 동일)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY),
                new AttackEnemyAction(blackboard, transform)
            }),

            // 5. 기본 대기 상태 (이전과 동일)
            new IdleAction(blackboard, transform)
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
        public CounterAttackAction(AgentBlackboard blackboard, Transform agentTransform, DefensiveAgentController ownerController) : base(blackboard, agentTransform)
        {
            this.controller = ownerController;
        }
        public override NodeStatus Tick()
        {
            return controller.PerformAttack();
        }
    }

    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate;
        public NotNode(BTConditionNode condition) : base(condition.Blackboard, condition.AgentTransform) { this.conditionToNegate = condition; }
        public override bool CheckCondition() { return conditionToNegate.Tick() == NodeStatus.FAILURE; }
    }
}
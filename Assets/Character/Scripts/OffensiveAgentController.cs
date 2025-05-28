// File: OffensiveAgentController.cs (OffensiveAgentController.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class OffensiveAgentController : AgentController
{
    public float offensiveAttackRange = 2.0f;
    public float fleeHealthThreshold = 20f;

    protected override void InitializeBehaviorTree()
    {
        rootNode = new BTSelector(blackboard, transform, new List<BTNode>


        {

            // 1. 공격: 적이 공격 범위 내에 있고, 다른 위협이 없으면 즉시 공격합니다. 이것이 최우선 목표입니다.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY),
                new AttackEnemyAction(blackboard, transform)
            }),

            // 2. 선제적 회피: 체력이 낮을 때 뿐만 아니라, 적이 공격하면 일단 회피하여 공격 흐름을 끊습니다.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyAttackingCondition(blackboard, transform),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                new EvadeAction(blackboard, transform)
            }),

            

            // 3. 거리 좁히기: 공격 범위 밖에 있다면, 단순하고 저돌적으로 적을 향해 접근합니다.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                // NotNode를 이용해 '공격 범위 안에 있지 않다면' 이라는 조건을 만듭니다.
                new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange)),
                new MoveTowardsEnemyAction(blackboard, transform, 9f, offensiveAttackRange * 0.9f)
            }),

            // 4. 기본 대기 상태: 위 모든 조건에 해당하지 않을 때만 대기합니다.
            new IdleAction(blackboard, transform)
        });
    }

    // NotNode 클래스 정의
    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate;
        public NotNode(BTConditionNode condition) : base(condition.Blackboard, condition.AgentTransform)
        {
            this.conditionToNegate = condition;
        }
        protected override bool CheckCondition()
        {
            return conditionToNegate.Tick() == NodeStatus.FAILURE;
        }
    }
}
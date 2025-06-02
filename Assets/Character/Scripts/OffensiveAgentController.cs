// File: OffensiveAgentController.cs (OffensiveAgentController.cs ����)
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

            // 1. ����: ���� ���� ���� ���� �ְ�, �ٸ� ������ ������ ��� �����մϴ�. �̰��� �ֿ켱 ��ǥ�Դϴ�.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY),
                new AttackEnemyAction(blackboard, transform)
            }),

            // 2. ������ ȸ��: ü���� ���� �� �Ӹ� �ƴ϶�, ���� �����ϸ� �ϴ� ȸ���Ͽ� ���� �帧�� �����ϴ�.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyAttackingCondition(blackboard, transform),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                new EvadeAction(blackboard, transform)
            }),

            

            // 3. �Ÿ� ������: ���� ���� �ۿ� �ִٸ�, �ܼ��ϰ� ���������� ���� ���� �����մϴ�.
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                // NotNode�� �̿��� '���� ���� �ȿ� ���� �ʴٸ�' �̶�� ������ ����ϴ�.
                new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange)),
                new MoveTowardsEnemyAction(blackboard, transform, 9f, offensiveAttackRange * 0.9f)
            }),

            // 4. �⺻ ��� ����: �� ��� ���ǿ� �ش����� ���� ���� ����մϴ�.
            new IdleAction(blackboard, transform)
        });
    }

    // NotNode Ŭ���� ����
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
// File: OffensiveAgentController.cs (OffensiveAgentController.cs ����)
using UnityEngine;
using System.Collections.Generic;

public class OffensiveAgentController : AgentController
{
    public float offensiveAttackRange = 2.0f; // ������ ������Ʈ�� ���� ���� (�⺻���� �ٸ� �� ����)
    public float repositionDistance = 4.0f;   // ��ȣ�ϴ� ���� �Ÿ� (���ġ ����)
    public float fleeHealthThreshold = 20f;   // ������ ����� ü�� ����ġ

    protected override void InitializeBehaviorTree()
    {
        // ������ ������Ʈ�� �ൿ Ʈ�� ����
        // ����: ���� �켱, ü���� ���ų� ���� ���� ���̸� ȸ��, �ʿ�� ���ġ.

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // 1. �ֿ켱 ����: ü���� �ſ� ���� ȸ�ǰ� �غ�Ǿ����� ȸ��
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsHealthLowCondition(blackboard, transform, fleeHealthThreshold), // ü���� ���� ����ġ �����ΰ�?
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY), // ȸ�� ��Ÿ���� �غ�Ǿ��°�?
                new EvadeAction(blackboard, transform) // ȸ�� �ൿ (�Ǵ� FleeAction)
            }),

            // 2. ���� ���̰�, ���� ���� ������, ������ �غ�Ǿ����� ����
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // ���� ���̴°�?
                new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange), // ���� ���� ���� ���� �ִ°�?
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // ���� ��Ÿ���� �غ�Ǿ��°�?
                new AttackEnemyAction(blackboard, transform) // ���� �ൿ
            }),

            // 3. ���ġ: �ʹ� �ָ� ������ �ٰ����ų�, �Ÿ� ����
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // ���� ���̴°�?
                // �̵� ����: �� ������ ���ų�, �ʹ� ������ �ణ ���� (�������� ��õ��� �ʾ����� ���ݿ� ����)
                new BTSelector(blackboard, transform, new List<BTNode> {
                    new BTSequence(blackboard, transform, new List<BTNode> { // ���� ���� ���̸� �� ������ �̵�
                        //new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange)), // ����� ���� NotNode �Ǵ� ���� �籸�� �ʿ�
                        new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.9f) // ���� ���� �ణ ���ʱ��� �̵�
                    }),
                    // �ʿ�� �� ������ ���ġ �߰� (��: �¿� �̵�, Ư�� �Ÿ� ����).
                    // ����� ���� ������ ���� �͸� ����.
                }),
            }),

            // 4. �⺻ �ൿ: ���� �������� �ٸ� �ൿ ������ �������� ������ ���� (�ļ���)
            new BTSequence(blackboard, transform, new List<BTNode> {
                new IsEnemyVisibleCondition(blackboard, transform),
                new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.9f)
            }),

            // 5. ���� ���ų� �ٸ� ������ �������� ������ ���
            new IdleAction(blackboard, transform)
        });
    }

    // �ʿ�� ����� NotNode (�Ǵ� �̸� ���ϵ��� ���� �籸��)
    /*
    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate; // ������ ����
        public NotNode(BTConditionNode condition) : base(condition.blackboard, condition.agentTransform)
        { // blackboard�� transform ����
            this.conditionToNegate = condition;
        }
        protected override bool CheckCondition()
        {
            return !conditionToNegate.CheckCondition(); // ������ �ݴ� ��� ��ȯ
        }
    }
    */
}
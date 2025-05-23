// File: DefensiveAgentController.cs (DefensiveAgentController.cs ����)
using UnityEngine;
using System.Collections.Generic;

public class DefensiveAgentController : AgentController
{
    public float defensiveStanceRange = 7f;    // ���/�ݰ��� ���� ���� �Ÿ�
    public float counterAttackHealthThreshold = 50f; // ü���� ���� ���� �̻��� ���� �ݰ�

    protected override void InitializeBehaviorTree()
    {
        // ������ ������Ʈ�� �ൿ Ʈ�� ����
        // ����: ���/ȸ�� �켱, �ݰ� ��ȸ ���, ü�� ����.

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // 1. �ֿ켱 ����: ���� ���� ���̸� ��� �Ǵ� ȸ��
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                // [�߿�] �� ������ ���� ����� �۵��� ���Դϴ�.
                new IsEnemyAttackingCondition(blackboard, transform),
                new BTSelector(blackboard, transform, new List<BTNode> // ü���� ���� ȸ�� �����ϸ� ȸ��, �ƴϸ� ���
                {
                    new BTSequence(blackboard, transform, new List<BTNode> { // ü���� ������ ȸ��
                        new IsHealthLowCondition(blackboard, transform, lowHealthThreshold),
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                        new EvadeAction(blackboard, transform)
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // �� �غ�Ǿ����� ���
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.DEFEND_COOLDOWN_KEY),
                        new DefendAction(blackboard, transform)
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // �� �غ� �ȵ����� ȸ�Ǵ� �����ϸ� ȸ�� (�ļ���)
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                        new EvadeAction(blackboard, transform)
                    })
                })
            }),

            // 2. ��ȸ�� ����� (��: ���� ���� �� ����� ��) �����ϸ� �ݰ�
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                // new CanCounterAttackCondition(blackboard, transform), // �ݰ� Ÿ�̹��� ���� ���� ���� (���� ����)
                new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange), // �ݰ��Ϸ��� ���� ���� �־�� ��
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // ���� ��Ÿ�� �غ�
                // [������] �ʹ� �����ϸ� �ݰ����� �ʵ��� ü�� ���� �ٽ� Ȱ��ȭ
                new NotNode(new IsHealthLowCondition(blackboard, transform, counterAttackHealthThreshold)),
                new AttackEnemyAction(blackboard, transform) // ���� �ൿ
            }),

            // 3. ����� ��ġ ���� / ü�� ����
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform),
                new BTSelector(blackboard, transform, new List<BTNode> {
                    new BTSequence(blackboard, transform, new List<BTNode> { // ���� �ʹ� ������
                        new IsEnemyTooCloseCondition(blackboard, transform, closeRangeThreshold),
                        // [������] FleeAction(����) �ൿ �ٽ� Ȱ��ȭ
                        //new FleeAction(blackboard, transform, 4f)
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // ���� �̻����� ��� �Ÿ����� �ָ� ���ɽ����� ����
                        // [������] NotNode�� ����Ͽ� ���� �� �ٽ� Ȱ��ȭ
                        new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, defensiveStanceRange)),
                        new MoveTowardsEnemyAction(blackboard, transform, 3f, defensiveStanceRange * 0.9f)
                    })
                })
            }),

            // 4. �Ϲ������� ü���� ���� ȸ�ǰ� �غ�Ǿ����� ȸ�� (������ ����)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsHealthLowCondition(blackboard, transform, lowHealthThreshold + 10f),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                new EvadeAction(blackboard, transform)
            }),

            // 5. Ư�� �����̳� �ൿ�� ������ ���
            new IdleAction(blackboard, transform)
        });
    }

    // [�߰���] NotNode Ŭ���� ���ǰ� �������Ƿ� �ٽ� �߰��մϴ�. ������ ���� ����.
    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate;

        // [����] �����ڿ��� .blackboard ��� .Blackboard�� ����ϵ��� �����մϴ�.
        public NotNode(BTConditionNode condition) : base(condition.Blackboard, condition.AgentTransform)
        {
            this.conditionToNegate = condition;
        }

        protected override bool CheckCondition()
        {
            // �ڽ� ����� Tick()�� �����ϰ�, �� ����� FAILURE�̸� true�� ��ȯ�Ͽ�
            // NotNode �ڽ��� SUCCESS ���°� �ǵ��� �մϴ�. �̰��� ���� ������ �ٽ��Դϴ�.
            return conditionToNegate.Tick() == NodeStatus.FAILURE;
        }
    }
}
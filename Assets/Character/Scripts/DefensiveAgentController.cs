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
                new IsEnemyAttackingCondition(blackboard, transform), // �߰��� ���� �ʿ�
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
                // new CanCounterAttackCondition(blackboard, transform), // Ư�� ���� �ʿ� (��: ���� ȸ�� �ִϸ��̼� ��)
                new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange), // �ݰ��Ϸ��� ���� ���� �־�� ��
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // ���� ��Ÿ�� �غ�
                //new OffensiveAgentController.NotNode(new IsHealthLowCondition(blackboard, transform, counterAttackHealthThreshold)), // �ʹ� �����ϸ� �ݰ����� ����
                new AttackEnemyAction(blackboard, transform) // ���� �ൿ
            }),

            // 3. ����� ��ġ ���� / ü�� ����
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform),
                // ���� �ʹ� ������ �ణ �־�����, �ʹ� �ָ� ���ɽ����� �����ϰų� ����
                new BTSelector(blackboard, transform, new List<BTNode> {
                    new BTSequence(blackboard, transform, new List<BTNode> { // ���� �ʹ� ������
                        new IsEnemyTooCloseCondition(blackboard, transform, closeRangeThreshold),
                        new FleeAction(blackboard, transform, 4f) // �ణ �־�����
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // ���� �̻����� ��� �Ÿ����� �ָ� ���ɽ����� ���� �Ǵ� ����
                        //new OffensiveAgentController.NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, defensiveStanceRange)),
                        new MoveTowardsEnemyAction(blackboard, transform, 3f, defensiveStanceRange * 0.9f)
                    })
                })
            }),

            // 4. �Ϲ������� ü���� ���� ȸ�ǰ� �غ�Ǿ����� ȸ�� (������ ����)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsHealthLowCondition(blackboard, transform, lowHealthThreshold + 10f), // ������ ȸ�Ǹ� ���� �ణ ���� ����ġ
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                new EvadeAction(blackboard, transform)
            }),

            // 5. Ư�� �����̳� �ൿ�� ������ ���
            new IdleAction(blackboard, transform)
        });
    }
}
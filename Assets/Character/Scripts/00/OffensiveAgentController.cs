// File: OffensiveAgentController.cs (OffensiveAgentController.cs ����)
using System.Collections.Generic;
using UnityEngine;
using static DefendSuccessCondition;

public class OffensiveAgentController : AgentController
{
    private float offensiveAttackRange = 2.0f; // ������ ������Ʈ�� ���� ���� (�⺻���� �ٸ� �� ����)
    private float repositionDistance = 3.0f;   // ��ȣ�ϴ� ���� �Ÿ� (���ġ ����)
    //private float fleeHealthThreshold = 20f;   // ������ ������ ü�� ����ġ

    // [�߰�] ���� ���� ��� ���� (���� ������ �������� ���� ����)
    [SerializeField] private float attackRangeTolerance = 0.2f; // ���� ������ �� ����ŭ �߰� ���

    protected override void InitializeBehaviorTree()
    {
        /*
        // ������ ������Ʈ�� �ൿ Ʈ�� ����
        // ����: ���� �켱, ü���� ���ų� ���� ���� ���̸� ȸ��, �ʿ�� ���ġ.

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // 3. ���� ���̰�, ���� ���� ������, ������ �غ�Ǿ����� ����
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // ���� ���̴°�?
                // [����] ���� ������ ��� ���� ����
                new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange + attackRangeTolerance), // ���� ���� ���� ���� �ִ°�?
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // ���� ��Ÿ���� �غ�Ǿ��°�?
                new AttackEnemyAction(blackboard, transform), // ���� �ൿ
            }),

            // 4. ���� ���� ��� �Ÿ� ���� (���ġ ����)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform),
                new BTSelector(blackboard, transform, new List<BTNode>
                {
                    // ���� ���� ���� ���̸� ����
                    new BTSequence(blackboard, transform, new List<BTNode>
                    {
                        // [����] �̵� ��ǥ �Ÿ��� offensiveAttackRange * 0.8f�� ����
                        new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange * 0.8f)),
                        new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.8f) // ������Ʈ�� ���� ������ �� ������ �����ϵ��� ����
                    }),

                    // ���� ���� ���̳� �ʹ� ������ �ڷ� ��¦ �̵�
                    new BTSequence(blackboard, transform, new List<BTNode>
                    {
                        new IsEnemyTooCloseCondition(blackboard, transform, 1.0f),
                        new MoveAwayFromEnemyAction(blackboard, transform, 3f, 1.5f)
                    }),
                })
            }),

            // 5. �⺻ �ൿ: ���� �������� �ٸ� �ൿ ������ �������� ������ ���� (�ļ���)
            new BTSequence(blackboard, transform, new List<BTNode> {
                new IsEnemyVisibleCondition(blackboard, transform),
                // [����] �̵� ��ǥ �Ÿ��� offensiveAttackRange * 0.8f�� ����
                new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.8f)
            }),

            // 6. ���� ���ų� �ٸ� ������ �������� ������ ���
            new IdleAction(blackboard, transform)
        });
        */
    }
}
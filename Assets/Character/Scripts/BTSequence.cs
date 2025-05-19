// File: BTSequence.cs (BTSequence.cs ����)
// �ڽ� ������ ������� �õ��մϴ�. �ڽ� �� �ϳ��� �����ϸ� FAILURE�� ��ȯ�մϴ�.
// �ڽ� �� �ϳ��� RUNNING �����̸� RUNNING�� ��ȯ�մϴ�. ��� �ڽ��� �����ϸ� SUCCESS�� ��ȯ�մϴ�.
using System.Collections.Generic;
using UnityEngine;

public class BTSequence : BTCompositeNode
{
    private int currentNodeIndex = 0; // ���� ���� ���� �ڽ� ����� �ε���

    public BTSequence(AgentBlackboard blackboard, Transform agentTransform, List<BTNode> children) : base(blackboard, agentTransform, children) { }

    public override NodeStatus Tick()
    {
        if (currentNodeIndex >= children.Count) // �߻����� �ʾƾ� ��
        {
            currentNodeIndex = 0;
            return NodeStatus.FAILURE;
        }

        NodeStatus childStatus = children[currentNodeIndex].Tick(); // ���� �ڽ� ��� ����

        switch (childStatus)
        {
            case NodeStatus.FAILURE: // �ڽ� ��尡 �����ϸ�
                currentNodeIndex = 0; // ���� ��ü �򰡸� ���� �ε��� �ʱ�ȭ
                return NodeStatus.FAILURE; // Sequence�� ���� ��ȯ
            case NodeStatus.RUNNING: // �ڽ� ��尡 ���� ���̸�
                return NodeStatus.RUNNING; // Sequence�� ���� �� ��ȯ
            case NodeStatus.SUCCESS: // �ڽ� ��尡 �����ϸ�
                currentNodeIndex++; // ���� �ڽ����� �̵�
                if (currentNodeIndex >= children.Count) // ��� �ڽ��� ����������
                {
                    currentNodeIndex = 0; // ���� ��ü �򰡸� ���� �ε��� �ʱ�ȭ
                    return NodeStatus.SUCCESS; // Sequence�� ���� ��ȯ
                }
                // �ڽ��� �� �ִٸ�, �� Sequence�� ���� �ڽ����� �Ѿ�� ������ ���� ���Դϴ�.
                return Tick(); // ���� Tick ������ ���� �ڽ� ����. ��Ϳ� �����ϼ���.
                               // Selector�� �����ϰ�, �����Ӵ� Tick�� ��� RUNNING�� ��ȯ�ϴ� ���� ����� �� �ֽ��ϴ�.
        }
        return NodeStatus.FAILURE; // ������ ��Ȯ�ϴٸ� ������ �� ���� �κ�
    }
}
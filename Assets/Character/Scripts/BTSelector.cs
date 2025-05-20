// File: BTSelector.cs (BTSelector.cs ����)
// �ڽ� ������ ������� �õ��Ͽ� �ϳ��� ������ ������ �����մϴ�.
// �ڽ� �� �ϳ��� RUNNING �����̸� RUNNING�� ��ȯ�մϴ�. ��� �ڽ��� �����ϸ� FAILURE�� ��ȯ�մϴ�.
using System.Collections.Generic;
using UnityEngine;

public class BTSelector : BTCompositeNode
{
    private int currentNodeIndex = 0; // ���� ���� ���� �ڽ� ����� �ε���

    public BTSelector(AgentBlackboard blackboard, Transform agentTransform, List<BTNode> children) : base(blackboard, agentTransform, children) { }

    public override NodeStatus Tick()
    {
        if (currentNodeIndex >= children.Count) // �������� ��� �߻����� �ʾƾ� �� (���� ��ġ)
        {
            currentNodeIndex = 0; // ���� Tick�� ���� �ʿ�� �ʱ�ȭ
            return NodeStatus.FAILURE;
        }

        NodeStatus childStatus = children[currentNodeIndex].Tick(); // ���� �ڽ� ��� ����

        switch (childStatus)
        {
            case NodeStatus.SUCCESS: // �ڽ� ��尡 �����ϸ�
                currentNodeIndex = 0; // ���� ��ü �򰡸� ���� �ε��� �ʱ�ȭ
                return NodeStatus.SUCCESS; // Selector�� ���� ��ȯ
            case NodeStatus.RUNNING: // �ڽ� ��尡 ���� ���̸�
                return NodeStatus.RUNNING; // Selector�� ���� �� ��ȯ
            case NodeStatus.FAILURE: // �ڽ� ��尡 �����ϸ�
                currentNodeIndex++; // ���� �ڽ����� �̵�
                if (currentNodeIndex >= children.Count) // ��� �ڽ��� �õ�������
                {
                    currentNodeIndex = 0; // ���� ��ü �򰡸� ���� �ε��� �ʱ�ȭ
                    return NodeStatus.FAILURE; // Selector�� ���� ��ȯ
                }
                // �ڽ��� �� �ִٸ�, �� Selector�� ���� �ڽ��� �õ��ϸ� ������ ���� ���Դϴ�.
                // ������ ���������� Selector�� ���� Tick ������ ��� ���� �ڽ����� �Ѿ�ϴ�.
                // �ܼ�ȭ�� ����, �׸��� �� ������ �� ���� ��� Tick() ȣ���� ���ϱ� ����,
                // Tick()�� �����Ӵ� �� �� ȣ��ȴٰ� �����մϴ�.
                // �� ������ ���������� ���⼭ �ݺ�(loop)�� �� �ֽ��ϴ�.
                // ����� ���� ���� �� ���� Tick ������ ��� �ڽ��� �õ��ϵ��� �մϴ�.
                return Tick(); // ���� Tick ������ ���� �ڽ� �õ�. ���� ��Ϳ� �����ϼ���.
                               // �Ǵ� �̷��� ����� �� �ֽ��ϴ�: �ڽ��� �����ϸ�, �� Selector�� ���� �ڽ��� ���ϸ� "���� ��"�Դϴ�.
                               // �����Ӵ� Tick�� ���, ���� �����ӿ� ���� �ڽ��� �õ��ϵ��� RUNNING�� ��ȯ�ϴ� ���� �� ������ �� �ֽ��ϴ�.
        }
        return NodeStatus.FAILURE; // ������ ��Ȯ�ϴٸ� ������ �� ���� �κ�
    }
}
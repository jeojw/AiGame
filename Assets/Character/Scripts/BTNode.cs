// File: BTNode.cs (BTNode.cs ����)
using UnityEngine;

// ��� ���� ������
public enum NodeStatus
{
    RUNNING, // ���� ��
    SUCCESS, // ����
    FAILURE  // ����
}

// ��� ����� �߻� �⺻ Ŭ����
public abstract class BTNode
{
    protected AgentBlackboard blackboard; // ������Ʈ ������ ����
    protected Transform agentTransform; // ������Ʈ �ڽ��� Transform ���� (��ġ/����)

    public BTNode(AgentBlackboard agentBlackboard, Transform agentTransform)
    {
        this.blackboard = agentBlackboard;
        this.agentTransform = agentTransform;
    }

    // ����� ������ �����ϴ� �߻� �޼ҵ�
    public abstract NodeStatus Tick();
}
// File: BTConditionNode.cs (BTConditionNode.cs ����)
// ������ Ȯ���ϴ� �ܸ� ���(Leaf Node).
using UnityEngine;

public abstract class BTConditionNode : BTNode
{
    public BTConditionNode(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    // Tick() �޼ҵ�� ������ Ȯ���ϰ� ����� ��ȯ�մϴ�.
    public override NodeStatus Tick()
    {
        return CheckCondition() ? NodeStatus.SUCCESS : NodeStatus.FAILURE;
    }

    // ���� ���� Ȯ�� ������ �����ϴ� �߻� �޼ҵ�
    protected abstract bool CheckCondition();
}
// File: BTActionNode.cs (BTActionNode.cs ����)
// �ൿ�� �����ϴ� �ܸ� ���(Leaf Node).
using UnityEngine;

public abstract class BTActionNode : BTNode
{
    public BTActionNode(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    // Tick() �޼ҵ�� ��ü���� �ൿ ��忡�� �����˴ϴ�.
}
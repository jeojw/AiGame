// File: BTConditionNode.cs (BTConditionNode.cs 파일)
// 조건을 확인하는 단말 노드(Leaf Node).
using UnityEngine;

public abstract class BTConditionNode : BTNode
{
    public BTConditionNode(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    // Tick() 메소드는 조건을 확인하고 결과를 반환합니다.
    public override NodeStatus Tick()
    {
        return CheckCondition() ? NodeStatus.SUCCESS : NodeStatus.FAILURE;
    }

    // 실제 조건 확인 로직을 구현하는 추상 메소드
    protected abstract bool CheckCondition();
}
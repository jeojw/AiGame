// File: BTActionNode.cs (BTActionNode.cs 파일)
// 행동을 수행하는 단말 노드(Leaf Node).
using UnityEngine;

public abstract class BTActionNode : BTNode
{
    public BTActionNode(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    // Tick() 메소드는 구체적인 행동 노드에서 구현됩니다.
}
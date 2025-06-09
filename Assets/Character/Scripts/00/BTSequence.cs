// File: BTSequence.cs (BTSequence.cs 파일)
// 자식 노드들을 순서대로 시도합니다. 자식 중 하나라도 실패하면 FAILURE를 반환합니다.
// 자식 중 하나가 RUNNING 상태이면 RUNNING을 반환합니다. 모든 자식이 성공하면 SUCCESS를 반환합니다.
using System.Collections.Generic;
using UnityEngine;

public class BTSequence : BTCompositeNode
{
    private int currentNodeIndex = 0; // 현재 실행 중인 자식 노드의 인덱스

    public BTSequence(AgentBlackboard blackboard, Transform agentTransform, List<BTNode> children) : base(blackboard, agentTransform, children) { }

    public override NodeStatus Tick()
    {
        while (currentNodeIndex < children.Count)
        {
            var status = children[currentNodeIndex].Tick();
            if (status == NodeStatus.FAILURE)
            {
                currentNodeIndex = 0;
                return NodeStatus.FAILURE;
            }
            else if (status == NodeStatus.RUNNING)
            {
                return NodeStatus.RUNNING;
            }
            else // SUCCESS
            {
                currentNodeIndex++;
            }
        }
        currentNodeIndex = 0;
        return NodeStatus.SUCCESS;
    }
}
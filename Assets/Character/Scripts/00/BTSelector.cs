// File: BTSelector.cs (BTSelector.cs 파일)
// 자식 노드들을 순서대로 시도하여 하나가 성공할 때까지 실행합니다.
// 자식 중 하나가 RUNNING 상태이면 RUNNING을 반환합니다. 모든 자식이 실패하면 FAILURE를 반환합니다.
using System.Collections.Generic;
using UnityEngine;

public class BTSelector : BTCompositeNode
{
    private int currentNodeIndex = 0; // 현재 실행 중인 자식 노드의 인덱스

    public BTSelector(AgentBlackboard blackboard, Transform agentTransform, List<BTNode> children) : base(blackboard, agentTransform, children) { }

    public override NodeStatus Tick()
    {
        // Selector는 자식 순서대로 Tick
        // 만약 자식이 RUNNING 반환하면 Selector도 RUNNING 반환(멈춤)
        // 만약 자식이 SUCCESS 반환하면 Selector도 SUCCESS 반환(멈춤)
        // 만약 자식이 FAILURE면 다음 자식으로 Tick 계속

        while (currentNodeIndex < children.Count)
        {
            var status = children[currentNodeIndex].Tick();
            if (status == NodeStatus.SUCCESS)
            {
                currentNodeIndex = 0;
                return NodeStatus.SUCCESS;
            }
            else if (status == NodeStatus.RUNNING)
            {
                return NodeStatus.RUNNING;
            }
            else // FAILURE
            {
                currentNodeIndex++;
            }
        }

        currentNodeIndex = 0;
        return NodeStatus.FAILURE;
    }
}
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
        if (currentNodeIndex >= children.Count) // 발생하지 않아야 함
        {
            currentNodeIndex = 0;
            return NodeStatus.FAILURE;
        }

        NodeStatus childStatus = children[currentNodeIndex].Tick(); // 현재 자식 노드 실행

        switch (childStatus)
        {
            case NodeStatus.FAILURE: // 자식 노드가 실패하면
                currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
                return NodeStatus.FAILURE; // Sequence도 실패 반환
            case NodeStatus.RUNNING: // 자식 노드가 실행 중이면
                return NodeStatus.RUNNING; // Sequence도 실행 중 반환
            case NodeStatus.SUCCESS: // 자식 노드가 성공하면
                currentNodeIndex++; // 다음 자식으로 이동
                if (currentNodeIndex >= children.Count) // 모든 자식이 성공했으면
                {
                    currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
                    return NodeStatus.SUCCESS; // Sequence도 성공 반환
                }
                // 자식이 더 있다면, 이 Sequence는 다음 자식으로 넘어가며 여전히 실행 중입니다.
                return Tick(); // 같은 Tick 내에서 다음 자식 실행. 재귀에 주의하세요.
                               // Selector와 유사하게, 프레임당 Tick의 경우 RUNNING을 반환하는 것을 고려할 수 있습니다.
        }
        return NodeStatus.FAILURE; // 로직이 정확하다면 도달할 수 없는 부분
    }
}
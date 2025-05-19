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
        if (currentNodeIndex >= children.Count) // 정상적인 경우 발생하지 않아야 함 (안전 장치)
        {
            currentNodeIndex = 0; // 다음 Tick을 위해 필요시 초기화
            return NodeStatus.FAILURE;
        }

        NodeStatus childStatus = children[currentNodeIndex].Tick(); // 현재 자식 노드 실행

        switch (childStatus)
        {
            case NodeStatus.SUCCESS: // 자식 노드가 성공하면
                currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
                return NodeStatus.SUCCESS; // Selector도 성공 반환
            case NodeStatus.RUNNING: // 자식 노드가 실행 중이면
                return NodeStatus.RUNNING; // Selector도 실행 중 반환
            case NodeStatus.FAILURE: // 자식 노드가 실패하면
                currentNodeIndex++; // 다음 자식으로 이동
                if (currentNodeIndex >= children.Count) // 모든 자식을 시도했으면
                {
                    currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
                    return NodeStatus.FAILURE; // Selector도 실패 반환
                }
                // 자식이 더 있다면, 이 Selector는 다음 자식을 시도하며 여전히 실행 중입니다.
                // 하지만 관례적으로 Selector는 같은 Tick 내에서 즉시 다음 자식으로 넘어갑니다.
                // 단순화를 위해, 그리고 한 프레임 내 깊은 재귀 Tick() 호출을 피하기 위해,
                // Tick()은 프레임당 한 번 호출된다고 가정합니다.
                // 더 복잡한 구현에서는 여기서 반복(loop)할 수 있습니다.
                // 현재는 빠른 실패 시 같은 Tick 내에서 모든 자식을 시도하도록 합니다.
                return Tick(); // 같은 Tick 내에서 다음 자식 시도. 깊은 재귀에 주의하세요.
                               // 또는 이렇게 고려할 수 있습니다: 자식이 실패하면, 이 Selector는 다음 자식을 평가하며 "실행 중"입니다.
                               // 프레임당 Tick의 경우, 다음 프레임에 다음 자식을 시도하도록 RUNNING을 반환하는 것이 더 안전할 수 있습니다.
        }
        return NodeStatus.FAILURE; // 로직이 정확하다면 도달할 수 없는 부분
    }
}
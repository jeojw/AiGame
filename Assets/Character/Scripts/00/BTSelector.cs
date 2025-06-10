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
        // 현재 인덱스부터 모든 자식 노드를 순회합니다.
        for (int i = currentNodeIndex; i < children.Count; i++)
        {
            NodeStatus childStatus = children[i].Tick(); // 현재 자식 노드 실행

            switch (childStatus)
            {
                case NodeStatus.SUCCESS: // 자식 노드가 성공하면
                    currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
                    return NodeStatus.SUCCESS; // Selector도 성공 반환
                case NodeStatus.RUNNING: // 자식 노드가 실행 중이면
                    currentNodeIndex = i; // 현재 실행 중인 자식 노드의 인덱스 저장
                    return NodeStatus.RUNNING; // Selector도 실행 중 반환
                case NodeStatus.FAILURE: // 자식 노드가 실패하면
                    // 다음 자식 노드를 시도하기 위해 반복문을 계속합니다.
                    // 현재 인덱스를 업데이트하여 다음 프레임에 재평가 시 해당 자식부터 시작하지 않도록 합니다.
                    currentNodeIndex = 0; // Selector는 모든 자식에 대해 재평가하는 경우가 많으므로 실패 시 인덱스 초기화
                    break; // 다음 자식으로 넘어감
            }
        }

        // 모든 자식이 FAILURE를 반환했으면
        currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
        return NodeStatus.FAILURE; // Selector도 실패 반환
    }
}
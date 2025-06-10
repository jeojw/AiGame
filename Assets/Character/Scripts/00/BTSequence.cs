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
        // 현재 인덱스부터 모든 자식 노드를 순회합니다.
        // Sequence는 중간에 RUNNING이 나오면 그 위치를 기억하고 다음 프레임에 거기부터 다시 시작합니다.
        // 중간에 FAILURE가 나오면 즉시 전체 Sequence를 FAILURE로 만듭니다.
        for (int i = currentNodeIndex; i < children.Count; i++)
        {
            NodeStatus childStatus = children[i].Tick(); // 현재 자식 노드 실행

            switch (childStatus)
            {
                case NodeStatus.FAILURE: // 자식 노드가 실패하면
                    currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
                    return NodeStatus.FAILURE; // Sequence도 실패 반환
                case NodeStatus.RUNNING: // 자식 노드가 실행 중이면
                    currentNodeIndex = i; // 현재 실행 중인 자식 노드의 인덱스 저장 (다음 프레임에 여기부터 다시 시작)
                    return NodeStatus.RUNNING; // Sequence도 실행 중 반환
                case NodeStatus.SUCCESS: // 자식 노드가 성공하면
                    // 다음 자식으로 넘어감 (반복문의 다음 이터레이션에서 children[i+1]이 실행됨)
                    break;
            }
        }

        // 모든 자식이 SUCCESS를 반환했으면 (반복문이 끝까지 실행된 경우)
        currentNodeIndex = 0; // 다음 전체 평가를 위해 인덱스 초기화
        return NodeStatus.SUCCESS; // Sequence도 성공 반환
    }
}
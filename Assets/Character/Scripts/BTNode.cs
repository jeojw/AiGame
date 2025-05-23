// File: BTNode.cs (BTNode.cs 파일)
using UnityEngine;

// 노드 상태 열거형
public enum NodeStatus
{
    RUNNING, // 실행 중
    SUCCESS, // 성공
    FAILURE  // 실패
}

// 모든 노드의 추상 기본 클래스
public abstract class BTNode
{
    protected AgentBlackboard blackboard; // 에이전트 블랙보드 참조
    protected Transform agentTransform; // 에이전트 자신의 Transform 참조 (위치/방향)


    public AgentBlackboard Blackboard => blackboard;
    public Transform AgentTransform => agentTransform;


    public BTNode(AgentBlackboard agentBlackboard, Transform agentTransform)
    {
        this.blackboard = agentBlackboard;
        this.agentTransform = agentTransform;
    }

    // 노드의 로직을 실행하는 추상 메소드
    public abstract NodeStatus Tick();
}
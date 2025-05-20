// File: BTCompositeNode.cs (BTCompositeNode.cs 파일)
using System.Collections.Generic;
using UnityEngine;

// 자식 노드를 가지는 복합 노드의 기본 클래스 (예: Selector, Sequence)
public abstract class BTCompositeNode : BTNode
{
    protected List<BTNode> children = new List<BTNode>(); // 자식 노드 리스트

    public BTCompositeNode(AgentBlackboard blackboard, Transform agentTransform, List<BTNode> children) : base(blackboard, agentTransform)
    {
        this.children = children;
    }

    // 자식 노드 추가 메소드
    public void AddChild(BTNode child)
    {
        children.Add(child);
    }
}
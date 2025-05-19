// File: BTCompositeNode.cs (BTCompositeNode.cs ����)
using System.Collections.Generic;
using UnityEngine;

// �ڽ� ��带 ������ ���� ����� �⺻ Ŭ���� (��: Selector, Sequence)
public abstract class BTCompositeNode : BTNode
{
    protected List<BTNode> children = new List<BTNode>(); // �ڽ� ��� ����Ʈ

    public BTCompositeNode(AgentBlackboard blackboard, Transform agentTransform, List<BTNode> children) : base(blackboard, agentTransform)
    {
        this.children = children;
    }

    // �ڽ� ��� �߰� �޼ҵ�
    public void AddChild(BTNode child)
    {
        children.Add(child);
    }
}
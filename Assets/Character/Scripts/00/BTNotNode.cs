// File: BTNotNode.cs
// NotNode를 범용적으로 사용할 수 있도록 분리
using UnityEngine;
using System.Collections.Generic;

// 필요시 사용할 NotNode (또는 이를 피하도록 로직 재구성)
public class NotNode : BTConditionNode
{
    private BTConditionNode conditionToNegate; // 부정할 조건
    
    public NotNode(BTConditionNode condition) : base(condition.Blackboard, condition.AgentTransform)
    { // blackboard와 transform 전달
        this.conditionToNegate = condition;
    }

    public override bool CheckCondition()
    {
        return !conditionToNegate.Evaluate(); // 조건의 반대 결과 반환
    }
}

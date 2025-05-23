// File: Conditions.cs (Conditions.cs 파일)
using UnityEngine;

// 적이 공격 범위 내에 있는지 확인하는 조건 노드
public class IsEnemyInAttackRangeCondition : BTConditionNode
{
    private float attackRange; // 공격 범위
    public IsEnemyInAttackRangeCondition(AgentBlackboard blackboard, Transform agentTransform, float range) : base(blackboard, agentTransform)
    {
        this.attackRange = range;
    }
    protected override bool CheckCondition()
    {
        if (blackboard.enemyTransform == null) return false; // 적이 없으면 실패
        return blackboard.enemyDistance <= attackRange; // 적과의 거리가 공격 범위 이내이면 성공
    }
}

// 적이 시야에 보이는지 확인하는 조건 노드 (간단화된 버전)
public class IsEnemyVisibleCondition : BTConditionNode
{
    public IsEnemyVisibleCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    protected override bool CheckCondition()
    {
        // 기본적인 확인: enemyTransform이 할당되었는가?
        // 실제로는 시야 확보를 위해 Raycast 등을 사용해야 합니다.
        return blackboard.enemyTransform != null; // 적 Transform이 존재하면 성공
    }
}

// 체력이 낮은지 확인하는 조건 노드
public class IsHealthLowCondition : BTConditionNode
{
    private float healthThreshold; // 체력 기준치
    public IsHealthLowCondition(AgentBlackboard blackboard, Transform agentTransform, float threshold) : base(blackboard, agentTransform)
    {
        this.healthThreshold = threshold;
    }
    protected override bool CheckCondition()
    {
        return blackboard.currentHealth <= healthThreshold; // 현재 체력이 기준치 이하이면 성공
    }
}

// 특정 행동의 쿨타임이 준비되었는지 확인하는 조건 노드
public class IsCooldownReadyCondition : BTConditionNode
{
    private string actionKey; // 행동 키 (예: "Attack", "Defend")
    public IsCooldownReadyCondition(AgentBlackboard blackboard, Transform agentTransform, string key) : base(blackboard, agentTransform)
    {
        this.actionKey = key;
    }
    protected override bool CheckCondition()
    {
        return blackboard.IsActionReady(actionKey); // 해당 행동이 준비되었으면 성공
    }
}

// 적이 너무 가까이 있는지 확인하는 조건 노드
public class IsEnemyTooCloseCondition : BTConditionNode
{
    private float closeThreshold; // 근접 기준 거리
    public IsEnemyTooCloseCondition(AgentBlackboard blackboard, Transform agentTransform, float threshold) : base(blackboard, agentTransform)
    {
        this.closeThreshold = threshold;
    }
    protected override bool CheckCondition()
    {
        if (blackboard.enemyTransform == null) return false; // 적이 없으면 실패
        return blackboard.enemyDistance < closeThreshold; // 적과의 거리가 기준치 미만이면 성공
    }
}

// TODO: IsEnemyAttackingCondition 구현. 이는 더 복잡합니다.
// 적의 애니메이션 상태를 확인하거나, 예측 또는 외부 이벤트를 기반으로 할 수 있습니다.
// 현재는 플레이스홀더이거나 외부 이벤트에 의해 구동된다고 가정합니다.
// 적이 공격 중인지 확인하는 조건 노드
public class IsEnemyAttackingCondition : BTConditionNode
{
    private Animator enemyAnimator; // 적의 애니메이터를 저장할 변수

    public IsEnemyAttackingCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    protected override bool CheckCondition()
    {
        // 블랙보드에 적 정보가 없으면 당연히 공격 중이 아님
        if (blackboard.enemyTransform == null) return false;

        // 이전에 적의 Animator를 가져오지 않았다면 한번만 가져와서 저장 (매번 GetComponent하는 것을 방지)
        if (enemyAnimator == null)
        {
            enemyAnimator = blackboard.enemyTransform.GetComponent<Animator>();
        }

        // 적에게 Animator가 없으면 판단 불가
        if (enemyAnimator == null) return false;

        // 적 Animator의 첫 번째 레이어(기본값 0)의 현재 상태 정보 확인
        // "Attack" 이라는 태그를 가진 애니메이션 상태가 재생 중이면 true를 반환
        if (enemyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            Debug.Log("상태 감지: 적이 공격 중입니다!");
            return true;
        }

        return false;
    }
}
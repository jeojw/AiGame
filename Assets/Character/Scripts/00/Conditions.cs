// File: Conditions.cs (Conditions.cs ����)
using System.Runtime.Remoting.Messaging;
using UnityEngine;

// ���� ���� ���� ���� �ִ��� Ȯ���ϴ� ���� ���
public class IsEnemyInAttackRangeCondition : BTConditionNode
{
    private float attackRange; // ���� ����
    public IsEnemyInAttackRangeCondition(AgentBlackboard blackboard, Transform agentTransform, float range) : base(blackboard, agentTransform)
    {
        this.attackRange = range;
    }
    public override bool CheckCondition()
    {
        if (blackboard.enemyTransform == null) return false;
        return blackboard.enemyDistance <= attackRange;
    }
}

// ���� �þ߿� ���̴��� Ȯ���ϴ� ���� ��� (����ȭ�� ����)
public class IsEnemyVisibleCondition : BTConditionNode
{
    public IsEnemyVisibleCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override bool CheckCondition()
    {
        // �⺻���� Ȯ��: enemyTransform�� �Ҵ�Ǿ��°�?
        // �����δ� �þ� Ȯ���� ���� Raycast ���� ����ؾ� �մϴ�.
        return blackboard.enemyTransform != null; // �� Transform�� �����ϸ� ����
    }
}

// ü���� ������ Ȯ���ϴ� ���� ���
public class IsHealthLowCondition : BTConditionNode
{
    private float healthThreshold; // ü�� ����ġ
    public IsHealthLowCondition(AgentBlackboard blackboard, Transform agentTransform, float threshold) : base(blackboard, agentTransform)
    {
        this.healthThreshold = threshold;
    }
    public override bool CheckCondition()
    {
        return blackboard.currentHealth <= healthThreshold; // ���� ü���� ����ġ �����̸� ����
    }
}

// Ư�� �ൿ�� ��Ÿ���� �غ�Ǿ����� Ȯ���ϴ� ���� ���
public class IsCooldownReadyCondition : BTConditionNode
{
    private string actionKey; // �ൿ Ű (��: "Attack", "Defend")
    public IsCooldownReadyCondition(AgentBlackboard blackboard, Transform agentTransform, string key) : base(blackboard, agentTransform)
    {
        this.actionKey = key;
    }
    public override bool CheckCondition()
    {
        return blackboard.IsActionReady(actionKey); // �ش� �ൿ�� �غ�Ǿ����� ����
    }
}

public class IsNotCooldwonReadyCondition : BTConditionNode
{
    private string actionKey;
    public IsNotCooldwonReadyCondition(AgentBlackboard whiteboard, Transform agentTransform, string key) : base(whiteboard, agentTransform) { }

    public override bool CheckCondition()
    {
        return !blackboard.IsActionReady(actionKey);
    }
}
// ���� �ʹ� ������ �ִ��� Ȯ���ϴ� ���� ���
public class IsEnemyTooCloseCondition : BTConditionNode
{
    private float closeThreshold; // ���� ���� �Ÿ�
    public IsEnemyTooCloseCondition(AgentBlackboard blackboard, Transform agentTransform, float threshold) : base(blackboard, agentTransform)
    {
        this.closeThreshold = threshold;
    }
    public override bool CheckCondition()
    {
        if (blackboard.enemyTransform == null) return false; // ���� ������ ����
        return blackboard.enemyDistance < closeThreshold; // ������ �Ÿ��� ����ġ �̸��̸� ����
    }
}

// TODO: IsEnemyAttackingCondition ����. �̴� �� �����մϴ�.
// ���� �ִϸ��̼� ���¸� Ȯ���ϰų�, ���� �Ǵ� �ܺ� �̺�Ʈ�� ������� �� �� �ֽ��ϴ�.
// ����� �÷��̽�Ȧ���̰ų� �ܺ� �̺�Ʈ�� ���� �����ȴٰ� �����մϴ�.
// ���� ���� ������ Ȯ���ϴ� ���� ���
public class IsEnemyAttackingCondition : BTConditionNode
{
    private Animator enemyAnimator; // 적 애니메이터를 저장하는 변수

    public IsEnemyAttackingCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    public override bool CheckCondition()
    {
        if (blackboard.enemyTransform == null) return false;

        if (enemyAnimator == null)
        {
            enemyAnimator = blackboard.enemyTransform.GetComponent<Animator>();
            if (enemyAnimator == null)
            {
                Debug.LogWarning($"[IsEnemyAttackingCondition] {blackboard.enemyTransform.name}에 Animator 컴포넌트가 없습니다.");
                return false;
            }
        }

        // 적의 Animator가 "Attack" 태그를 가진 상태에 있는지 확인
        // GetCurrentAnimatorStateInfo(0)은 기본 레이어를 의미합니다.
        if (enemyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            // Debug.Log("적 공격: 적이 공격 애니메이션 상태입니다!"); // 디버그용
            // blackboard.isAttacking = true; // 이 부분은 AgentController의 FixedUpdate에서 처리하는 것이 더 일관적입니다.
            return true;
        }
        // else { blackboard.isAttacking = false; } // 마찬가지로 AgentController에서 처리

        return false;
    }
}

public class CanCounterAttackCondition : BTConditionNode
{
    public CanCounterAttackCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    public override bool CheckCondition()
    {
        return blackboard.canCounterAttack;
    }
}

public class DefendSuccessCondition : BTConditionNode
{
    public DefendSuccessCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override bool CheckCondition()
    {
        return blackboard.canCounterAttack;
    }
}

public class IsGetAttackCondition : BTConditionNode
{
    public IsGetAttackCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override bool CheckCondition()
    {
        return false;
    }
}

// [�߰�] �ݰ� �� ���� ���� ������ Ȯ���ϴ� ���� ���
public class IsEnemyAttackingDuringCounterAttackCondition : BTConditionNode
{
    private Animator enemyAnimator; // �� �ִϸ����� ����

    public IsEnemyAttackingDuringCounterAttackCondition(AgentBlackboard blackboard, Transform agentTransform)
        : base(blackboard, agentTransform) { }

    public override bool CheckCondition()
    {
        // ���� �ݰ� ���� �ƴ϶�� �� ������ ����
        if (!blackboard.isAttacking)
        {
            return false;
        }

        // ���� �������� ������ ����
        if (blackboard.enemyTransform == null)
        {
            return false;
        }

        // Animator ĳ��
        if (enemyAnimator == null)
        {
            enemyAnimator = blackboard.enemyTransform.GetComponent<Animator>();
            if (enemyAnimator == null)
            {
                Debug.LogWarning("�� �ִϸ����Ͱ� �����ϴ�.");
                return false;
            }
        }

        // ���� ���� ��
        var enemyAttackingCondition = new IsEnemyAttackingCondition(blackboard, agentTransform);
        if (enemyAttackingCondition.CheckCondition())
        {
            Debug.Log("�ݰ� �� ���� ���� ���Դϴ�!");
            return true;
        }

        return false;
    }
}

public class IsEnemyIdleForDurationCondition : BTConditionNode
{
    private float duration;

    public IsEnemyIdleForDurationCondition(AgentBlackboard blackboard, Transform agentTransform, float duration)
        : base(blackboard, agentTransform)
    {
        this.duration = duration;
    }

    public override bool CheckCondition()
    {
        // 적이 없으면 조건을 만족하지 않음
        if (blackboard.enemyTransform == null) return false;

        // 마지막 공격 시간으로부터 지정된 시간이 지났는지 확인
        // 만약 적이 한 번도 공격한 적이 없다면 lastEnemyAttackTime은 0이므로,
        // 게임 시작 후 5초가 지나면 이 조건은 참이 됩니다.
        return (Time.time - blackboard.lastEnemyAttackTime) > duration;
    }
}


// [새로 추가] 현재 방어 중이 아닌지 확인하는 조건 노드
public class IsNotDefendingCondition : BTConditionNode
{
    public IsNotDefendingCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override bool CheckCondition()
    {
        return !blackboard.isDefending; // 블랙보드의 isDefending 플래그 확인
    }
}

// [새로 추가] 현재 회피 중이 아닌지 확인하는 조건 노드
public class IsNotEvadingCondition : BTConditionNode
{
    public IsNotEvadingCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override bool CheckCondition()
    {
        return !blackboard.isEvading; // 블랙보드의 isEvading 플래그 확인
    }
}


// [새로 추가] 최근에 방어했는지 확인하는 조건 노드
public class WasRecentlyDefendedCondition : BTConditionNode
{
    public WasRecentlyDefendedCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override bool CheckCondition()
    {
        return blackboard.recentlyDefended;
    }
}
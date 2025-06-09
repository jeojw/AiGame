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
    private Animator enemyAnimator; // ���� �ִϸ����͸� ������ ����

    public IsEnemyAttackingCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    public override bool CheckCondition()
    {
        Debug.Log("checkCondition");
        // �������忡 �� ������ ������ �翬�� ���� ���� �ƴ�
        if (blackboard.enemyTransform == null) return false;

        // ������ ���� Animator�� �������� �ʾҴٸ� �ѹ��� �����ͼ� ���� (�Ź� GetComponent�ϴ� ���� ����)
        if (enemyAnimator == null)
        {
            enemyAnimator = blackboard.enemyTransform.GetComponent<Animator>();

            // �÷��̽�Ȧ��: ���� ���ӿ����� ���� �ִϸ��̼� ���¸� Ȯ���ϰų�,
            // �߻�ü�� ���ƿ�����, �Ǵ� ���� ���� �غ� ���� ������ ���� Ȯ���ؾ� �մϴ�.
            // �� ���������� ���� ������ ���� ��� 10% Ȯ���� true�� ��ȯ�ϵ��� ����ϴ�.
            if (blackboard.enemyTransform != null && blackboard.enemyDistance < 5f)
            {
                return Random.value < 0.1f; // ����: ���� ������ 10% Ȯ��
            }

            // ������ Animator�� ������ �Ǵ� �Ұ�
            if (enemyAnimator == null) return false;

            // �� Animator�� ù ��° ���̾�(�⺻�� 0)�� ���� ���� ���� Ȯ��
            // "Attack" �̶�� �±׸� ���� �ִϸ��̼� ���°� ��� ���̸� true�� ��ȯ
            if (enemyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                Debug.Log("enemy is attack1!!");
                return true;
            }

            return false;
        }

        if (enemyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            return true;
        }

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
        Debug.Log($"Block success!!!, {blackboard.canCounterAttack}");
        return blackboard.canCounterAttack;
    }
}

public class IsGetAttackCondition : BTConditionNode
{
    public IsGetAttackCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override bool CheckCondition()
    {
        return blackboard.isGetAttacked;
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

public class IsDeadCondition : BTConditionNode
{
    public IsDeadCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    public override bool CheckCondition()
    {
        return blackboard.isDead;
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
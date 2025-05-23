// File: Conditions.cs (Conditions.cs ����)
using UnityEngine;

// ���� ���� ���� ���� �ִ��� Ȯ���ϴ� ���� ���
public class IsEnemyInAttackRangeCondition : BTConditionNode
{
    private float attackRange; // ���� ����
    public IsEnemyInAttackRangeCondition(AgentBlackboard blackboard, Transform agentTransform, float range) : base(blackboard, agentTransform)
    {
        this.attackRange = range;
    }
    protected override bool CheckCondition()
    {
        if (blackboard.enemyTransform == null) return false; // ���� ������ ����
        return blackboard.enemyDistance <= attackRange; // ������ �Ÿ��� ���� ���� �̳��̸� ����
    }
}

// ���� �þ߿� ���̴��� Ȯ���ϴ� ���� ��� (����ȭ�� ����)
public class IsEnemyVisibleCondition : BTConditionNode
{
    public IsEnemyVisibleCondition(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    protected override bool CheckCondition()
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
    protected override bool CheckCondition()
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
    protected override bool CheckCondition()
    {
        return blackboard.IsActionReady(actionKey); // �ش� �ൿ�� �غ�Ǿ����� ����
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
    protected override bool CheckCondition()
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

    protected override bool CheckCondition()
    {
        // �����忡 �� ������ ������ �翬�� ���� ���� �ƴ�
        if (blackboard.enemyTransform == null) return false;

        // ������ ���� Animator�� �������� �ʾҴٸ� �ѹ��� �����ͼ� ���� (�Ź� GetComponent�ϴ� ���� ����)
        if (enemyAnimator == null)
        {
            enemyAnimator = blackboard.enemyTransform.GetComponent<Animator>();
        }

        // ������ Animator�� ������ �Ǵ� �Ұ�
        if (enemyAnimator == null) return false;

        // �� Animator�� ù ��° ���̾�(�⺻�� 0)�� ���� ���� ���� Ȯ��
        // "Attack" �̶�� �±׸� ���� �ִϸ��̼� ���°� ��� ���̸� true�� ��ȯ
        if (enemyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            Debug.Log("���� ����: ���� ���� ���Դϴ�!");
            return true;
        }

        return false;
    }
}
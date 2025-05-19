// File: Actions.cs (Actions.cs ����)
using UnityEngine;

// ������ �ٰ����� �ൿ ���
public class MoveTowardsEnemyAction : BTActionNode
{
    private float moveSpeed;        // �̵� �ӵ�
    private float stoppingDistance; // ���ߴ� �Ÿ� (�浹 �� ����)

    public MoveTowardsEnemyAction(AgentBlackboard blackboard, Transform agentTransform, float speed, float stopDist) : base(blackboard, agentTransform)
    {
        this.moveSpeed = speed;
        this.stoppingDistance = stopDist;
    }

    public override NodeStatus Tick()
    {
        if (blackboard.enemyTransform == null) return NodeStatus.FAILURE; // ���� ������ ����
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.MoveTowards(blackboard.enemyTransform.position, moveSpeed, stoppingDistance); // ��Ʈ�ѷ��� �̵� �޼ҵ� ȣ��
        }
        return NodeStatus.FAILURE;
    }
}

// ���� �����ϴ� �ൿ ���
public class AttackEnemyAction : BTActionNode
{
    public AttackEnemyAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.PerformAttack(); // ��Ʈ�ѷ��� ���� �޼ҵ� ȣ��
        }
        return NodeStatus.FAILURE;
    }
}

// ����ϴ� �ൿ ���
public class DefendAction : BTActionNode
{
    public DefendAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.PerformDefend(); // ��Ʈ�ѷ��� ��� �޼ҵ� ȣ��
        }
        return NodeStatus.FAILURE;
    }
}

// ȸ���ϴ� �ൿ ���
public class EvadeAction : BTActionNode
{
    public EvadeAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.PerformEvade(); // ��Ʈ�ѷ��� ȸ�� �޼ҵ� ȣ��
        }
        return NodeStatus.FAILURE;
    }
}

// �������� �ൿ ��� (����)
public class FleeAction : BTActionNode
{
    private float moveSpeed; // �̵� �ӵ�
    public FleeAction(AgentBlackboard blackboard, Transform agentTransform, float speed) : base(blackboard, agentTransform)
    {
        this.moveSpeed = speed;
    }
    public override NodeStatus Tick()
    {
        if (blackboard.enemyTransform == null) return NodeStatus.FAILURE; // ���� ������ ����
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            // 0f ���� �Ÿ��� ��� �̵����� �ǹ�
            return controller.MoveAwayFrom(blackboard.enemyTransform.position, moveSpeed, 0f);
        }
        return NodeStatus.FAILURE;
    }
}

// ����ϴ� �ൿ ���
public class IdleAction : BTActionNode
{
    public IdleAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.Idle(); // ��Ʈ�ѷ��� ��� �޼ҵ� ȣ��
        }
        return NodeStatus.FAILURE;
    }
}
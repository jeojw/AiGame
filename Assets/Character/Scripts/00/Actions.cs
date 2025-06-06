// File: Actions.cs (Actions.cs ����)
using UnityEngine;

// ������ �ٰ����� �ൿ ���
public class MoveTowardsEnemyAction : BTActionNode
{
    private float moveSpeed;        // �̵� �ӵ�
    private float stoppingDistance; // ���ߴ� �Ÿ� (�浹 �� ����)'

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

// [�߰�] �����Լ� �������� �ൿ ���(����X, ������ ���ġ)
public class MoveAwayFromEnemyAction : BTActionNode
{
    private float moveSpeed = 10f;             // �̵� �ӵ�
    private float desiredDistance = 1.8f;       // ���� �� ���� �̻� �Ÿ��� ������ ����
    private float stopBuffer = 0.1f;     // �̼� �Ÿ� ����

    public MoveAwayFromEnemyAction(AgentBlackboard blackboard, Transform agentTransform, float speed, float desiredDist) : base(blackboard, agentTransform)
    {
        this.moveSpeed = speed;
        this.desiredDistance = desiredDist;
    }

    public override NodeStatus Tick()
    {
        if (blackboard.enemyTransform == null) return NodeStatus.FAILURE;

        float currentDistance = Vector3.Distance(agentTransform.position, blackboard.enemyTransform.position);

        if (currentDistance > desiredDistance + stopBuffer)
            return NodeStatus.SUCCESS;

        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.MoveAwayFrom(blackboard.enemyTransform.position, moveSpeed, desiredDistance);
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

public class ChangeDefendToAttack : BTActionNode
{
    public ChangeDefendToAttack(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.PerformChangeDefendToAttack();
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

// �������� �ൿ ���
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

public class GetAttackAction : BTActionNode
{
    public GetAttackAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }

    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();

        if (controller != null)
        {
            return controller.GetAttack();
        }
        return NodeStatus.FAILURE;
    }
}
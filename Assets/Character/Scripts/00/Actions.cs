// File: Actions.cs (Actions.cs 파일)
using UnityEngine;

// 적에게 다가가는 행동 노드
public class MoveTowardsEnemyAction : BTActionNode
{
    private float moveSpeed;        // 이동 속도
    private float stoppingDistance; // 멈추는 거리 (충돌 전 멈춤)

    public MoveTowardsEnemyAction(AgentBlackboard blackboard, Transform agentTransform, float speed, float stopDist) : base(blackboard, agentTransform)
    {
        this.moveSpeed = speed;
        this.stoppingDistance = stopDist;
    }

    public override NodeStatus Tick()
    {
        if (blackboard.enemyTransform == null) return NodeStatus.FAILURE; // 적이 없으면 실패
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.MoveTowards(blackboard.enemyTransform.position, moveSpeed, stoppingDistance); // 컨트롤러의 이동 메소드 호출
        }
        return NodeStatus.FAILURE;
    }
}

// 적을 공격하는 행동 노드
public class AttackEnemyAction : BTActionNode
{
    public AttackEnemyAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.PerformAttack(); // 컨트롤러의 공격 메소드 호출
        }
        return NodeStatus.FAILURE;
    }
}

// 방어하는 행동 노드
public class DefendAction : BTActionNode
{
    public DefendAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.PerformDefend(); // 컨트롤러의 방어 메소드 호출
        }
        return NodeStatus.FAILURE;
    }
}

// 회피하는 행동 노드
public class EvadeAction : BTActionNode
{
    public EvadeAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.PerformEvade(); // 컨트롤러의 회피 메소드 호출
        }
        return NodeStatus.FAILURE;
    }
}

// 도망가는 행동 노드 (예시)
public class FleeAction : BTActionNode
{
    private float moveSpeed; // 이동 속도
    public FleeAction(AgentBlackboard blackboard, Transform agentTransform, float speed) : base(blackboard, agentTransform)
    {
        this.moveSpeed = speed;
    }
    public override NodeStatus Tick()
    {
        if (blackboard.enemyTransform == null) return NodeStatus.FAILURE; // 적이 없으면 실패
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            // 0f 정지 거리는 계속 이동함을 의미
            return controller.MoveAwayFrom(blackboard.enemyTransform.position, moveSpeed, 0f);
        }
        return NodeStatus.FAILURE;
    }
}

// 대기하는 행동 노드
public class IdleAction : BTActionNode
{
    public IdleAction(AgentBlackboard blackboard, Transform agentTransform) : base(blackboard, agentTransform) { }
    public override NodeStatus Tick()
    {
        AgentController controller = agentTransform.GetComponent<AgentController>();
        if (controller != null)
        {
            return controller.Idle(); // 컨트롤러의 대기 메소드 호출
        }
        return NodeStatus.FAILURE;
    }
}
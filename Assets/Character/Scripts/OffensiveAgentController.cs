// File: OffensiveAgentController.cs (OffensiveAgentController.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class OffensiveAgentController : AgentController
{
    public float offensiveAttackRange = 2.0f; // 공격형 에이전트의 공격 범위 (기본값과 다를 수 있음)
    public float repositionDistance = 4.0f;   // 선호하는 전투 거리 (재배치 기준)
    public float fleeHealthThreshold = 20f;   // 도망을 고려할 체력 기준치

    protected override void InitializeBehaviorTree()
    {
        // 공격형 에이전트의 행동 트리 정의
        // 전략: 공격 우선, 체력이 낮거나 적이 공격 중이면 회피, 필요시 재배치.

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // 1. 최우선 순위: 체력이 매우 낮고 회피가 준비되었으면 회피
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsHealthLowCondition(blackboard, transform, fleeHealthThreshold), // 체력이 도망 기준치 이하인가?
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY), // 회피 쿨타임이 준비되었는가?
                new EvadeAction(blackboard, transform) // 회피 행동 (또는 FleeAction)
            }),

            // 2. 적이 보이고, 범위 내에 있으며, 공격이 준비되었으면 공격
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // 적이 보이는가?
                new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange), // 적이 공격 범위 내에 있는가?
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // 공격 쿨타임이 준비되었는가?
                new AttackEnemyAction(blackboard, transform) // 공격 행동
            }),

            // 3. 재배치: 너무 멀면 적에게 다가가거나, 거리 유지
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // 적이 보이는가?
                // 이동 선택: 더 가까이 가거나, 너무 가까우면 약간 후퇴 (문서에는 명시되지 않았지만 공격에 유용)
                new BTSelector(blackboard, transform, new List<BTNode> {
                    new BTSequence(blackboard, transform, new List<BTNode> { // 공격 범위 밖이면 더 가까이 이동
                        //new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange)), // 사용자 정의 NotNode 또는 로직 재구성 필요
                        new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.9f) // 공격 범위 약간 안쪽까지 이동
                    }),
                    // 필요시 더 정교한 재배치 추가 (예: 좌우 이동, 특정 거리 유지).
                    // 현재는 공격 범위로 들어가는 것만 보장.
                }),
            }),

            // 4. 기본 행동: 적이 보이지만 다른 행동 조건이 충족되지 않으면 접근 (후순위)
            new BTSequence(blackboard, transform, new List<BTNode> {
                new IsEnemyVisibleCondition(blackboard, transform),
                new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.9f)
            }),

            // 5. 적이 없거나 다른 조건이 충족되지 않으면 대기
            new IdleAction(blackboard, transform)
        });
    }

    // 필요시 사용할 NotNode (또는 이를 피하도록 로직 재구성)
    /*
    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate; // 부정할 조건
        public NotNode(BTConditionNode condition) : base(condition.blackboard, condition.agentTransform)
        { // blackboard와 transform 전달
            this.conditionToNegate = condition;
        }
        protected override bool CheckCondition()
        {
            return !conditionToNegate.CheckCondition(); // 조건의 반대 결과 반환
        }
    }
    */
}
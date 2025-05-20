// File: DefensiveAgentController.cs (DefensiveAgentController.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class DefensiveAgentController : AgentController
{
    public float defensiveStanceRange = 7f;    // 방어/반격을 위한 최적 거리
    public float counterAttackHealthThreshold = 50f; // 체력이 일정 수준 이상일 때만 반격

    protected override void InitializeBehaviorTree()
    {
        // 수비형 에이전트의 행동 트리 정의
        // 전략: 방어/회피 우선, 반격 기회 모색, 체력 관리.

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // 1. 최우선 순위: 적이 공격 중이면 방어 또는 회피
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyAttackingCondition(blackboard, transform), // 견고한 구현 필요
                new BTSelector(blackboard, transform, new List<BTNode> // 체력이 낮고 회피 가능하면 회피, 아니면 방어
                {
                    new BTSequence(blackboard, transform, new List<BTNode> { // 체력이 낮으면 회피
                        new IsHealthLowCondition(blackboard, transform, lowHealthThreshold),
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                        new EvadeAction(blackboard, transform)
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // 방어가 준비되었으면 방어
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.DEFEND_COOLDOWN_KEY),
                        new DefendAction(blackboard, transform)
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // 방어가 준비 안됐지만 회피는 가능하면 회피 (후순위)
                        new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                        new EvadeAction(blackboard, transform)
                    })
                })
            }),

            // 2. 기회가 생기고 (예: 적이 공격 후 취약할 때) 안전하면 반격
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                // new CanCounterAttackCondition(blackboard, transform), // 특정 로직 필요 (예: 적이 회복 애니메이션 중)
                new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange), // 반격하려면 범위 내에 있어야 함
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // 공격 쿨타임 준비
                //new OffensiveAgentController.NotNode(new IsHealthLowCondition(blackboard, transform, counterAttackHealthThreshold)), // 너무 위험하면 반격하지 않음
                new AttackEnemyAction(blackboard, transform) // 공격 행동
            }),

            // 3. 방어적 위치 유지 / 체력 관리
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform),
                // 적이 너무 가까우면 약간 멀어지고, 너무 멀면 조심스럽게 접근하거나 유지
                new BTSelector(blackboard, transform, new List<BTNode> {
                    new BTSequence(blackboard, transform, new List<BTNode> { // 적이 너무 가까우면
                        new IsEnemyTooCloseCondition(blackboard, transform, closeRangeThreshold),
                        new FleeAction(blackboard, transform, 4f) // 약간 멀어지기
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // 적이 이상적인 방어 거리보다 멀면 조심스럽게 접근 또는 유지
                        //new OffensiveAgentController.NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, defensiveStanceRange)),
                        new MoveTowardsEnemyAction(blackboard, transform, 3f, defensiveStanceRange * 0.9f)
                    })
                })
            }),

            // 4. 일반적으로 체력이 낮고 회피가 준비되었으면 회피 (선제적 생존)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsHealthLowCondition(blackboard, transform, lowHealthThreshold + 10f), // 선제적 회피를 위해 약간 높은 기준치
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                new EvadeAction(blackboard, transform)
            }),

            // 5. 특정 위협이나 행동이 없으면 대기
            new IdleAction(blackboard, transform)
        });
    }
}
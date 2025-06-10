// File: OffensiveAgentController.cs
using System.Collections.Generic;
using UnityEngine;
// using static DefendSuccessCondition; // 이 부분은 실제 코드에 DefendSuccessCondition이 없으므로 제거하거나 필요한 경우에만 유지해야 합니다. (기존 주석 그대로 유지)

public class OffensiveAgentController : AgentController
{
    [SerializeField] private float offensiveAttackRange = 2.0f; // 공격형 에이전트의 공격 범위 (기본값과 다를 수 있음)
    [SerializeField] private float repositionDistance = 3.0f;   // 재정비 시 이동할 거리
    private float fleeHealthThreshold = 20f;   // 공격형 에이전트의 체력 임계치 (도주 또는 회피 판단 기준)

    // [추가] 공격 범위 허용치 (공격 행동의 유연성을 위해 추가)
    [SerializeField] private float attackRangeTolerance = 0.2f; // 공격 범위에 이 값만큼 추가 허용

    protected override void InitializeBehaviorTree()
    {
        // 공격형 에이전트의 행동 트리 구성
        // 목표: 공격 지향, 체력이 낮거나 적이 공격 중일 때 회피, 필요 시 재정비.

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // 1. (이전 주석 3) 적이 보이고, 공격 범위 내에 있으며, 공격 쿨타임이 준비되었을 때 공격
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // 적이 보이는가?
                // [수정] 공격 범위에 허용치 추가 적용
                new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange + attackRangeTolerance), // 적이 공격 범위 내에 있는가?
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // 공격 쿨타임이 준비되었는가?
                new AttackEnemyAction(blackboard, transform), // 적 공격 행동
            }),

            // [추가] 원래 있던 회피 로직 시작
            // 2. 체력이 낮거나 적이 공격 중일 때 회피
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // 적이 보이는가?
                new IsEnemyAttackingCondition(blackboard, transform), // 적이 공격 중인가?


                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY), // 회피 쿨타임이 준비되었는가?
                new EvadeAction(blackboard, transform) // 회피 행동
            }),
            // [추가] 원래 있던 회피 로직 끝

            // 3. (이전 주석 4) 적과의 거리 유지 (재정비 행동)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // 적이 보이는가?
                new BTSelector(blackboard, transform, new List<BTNode> // 둘 중 하나를 선택
                {
                    // 적이 공격 범위에 비해 너무 멀다면 적에게 다가가기
                    new BTSequence(blackboard, transform, new List<BTNode>
                    {
                        // [수정] 이동 목표 거리를 offensiveAttackRange * 0.8f로 설정
                        new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange * 0.8f)), // 적이 공격 범위의 80% 내에 있지 않은가?
                        new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.8f) // 에이전트가 공격 범위에 접근하도록 이동
                    }),

                    // 적이 너무 가까우면 뒤로 물러나기
                    new BTSequence(blackboard, transform, new List<BTNode>
                    {
                        new IsEnemyTooCloseCondition(blackboard, transform, 1.0f), // 적이 1.0f보다 가까운가?
                        new MoveAwayFromEnemyAction(blackboard, transform, 3f, 1.5f) // 적에게서 1.5f 거리까지 멀어지기
                    }),
                })
            }),

            // 4. (이전 주석 5) 기본 행동: 적이 보이면 접근 (fallback)
            new BTSequence(blackboard, transform, new List<BTNode> {
                new IsEnemyVisibleCondition(blackboard, transform), // 적이 보이는가?
                // [수정] 이동 목표 거리를 offensiveAttackRange * 0.8f로 설정
                new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.8f) // 적에게 공격 범위 내로 접근
            }),

            // 5. (이전 주석 6) 모든 다른 행동이 불가능할 경우 대기
            new IdleAction(blackboard, transform)
        });
    }
}
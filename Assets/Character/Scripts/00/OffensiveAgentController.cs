// File: OffensiveAgentController.cs (OffensiveAgentController.cs 파일)
using System.Collections.Generic;
using UnityEngine;
using static DefendSuccessCondition;

public class OffensiveAgentController : AgentController
{
    private float offensiveAttackRange = 2.0f; // 공격형 에이전트의 공격 범위 (기본값과 다를 수 있음)
    private float repositionDistance = 3.0f;   // 선호하는 전투 거리 (재배치 기준)
    private float fleeHealthThreshold = 20f;   // 도망을 고려할 체력 기준치

    // [추가] 공격 범위 허용 오차 (공격 조건의 관대함을 위한 변수)
    [SerializeField] private float attackRangeTolerance = 0.2f; // 공격 범위에 이 값만큼 추가 허용

    protected override void InitializeBehaviorTree()
    {
        
        // 공격형 에이전트의 행동 트리 정의
        // 전략: 공격 우선, 체력이 낮거나 적이 공격 중이면 회피, 필요시 재배치.

        rootNode = new BTSelector(blackboard, transform, new List<BTNode>
        {
            // [수정] 0. 피격 시의 대응을 최우선 순위로
            new BTSequence(blackboard, transform, new List<BTNode> {
                new IsGetAttackCondition(blackboard, transform),
                new GetAttackAction(blackboard, transform),
            }),

            // 3. 적이 보이고, 범위 내에 있으며, 공격이 준비되었으면 공격
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform), // 적이 보이는가?
                // [수정] 공격 범위에 허용 오차 적용
                new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange + attackRangeTolerance), // 적이 공격 범위 내에 있는가?
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // 공격 쿨타임이 준비되었는가?
                new AttackEnemyAction(blackboard, transform), // 공격 행동
            }),

            // 4. 적이 보일 경우 거리 조정 (재배치 포함)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform),
                new BTSelector(blackboard, transform, new List<BTNode>
                {
                    // 아직 공격 범위 밖이면 접근
                    new BTSequence(blackboard, transform, new List<BTNode>
                    {
                        // [수정] 이동 목표 거리를 offensiveAttackRange * 0.8f로 변경
                        new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, offensiveAttackRange * 0.8f)),
                        new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.8f) // 에이전트가 공격 범위에 더 가깝게 접근하도록 유도
                    }),

                    // 공격 범위 안이나 너무 가까우면 뒤로 살짝 이동
                    new BTSequence(blackboard, transform, new List<BTNode>
                    {
                        new IsEnemyTooCloseCondition(blackboard, transform, 1.0f),
                        new MoveAwayFromEnemyAction(blackboard, transform, 3f, 1.5f)
                    }),
                })
            }),

            // 5. 기본 행동: 적이 보이지만 다른 행동 조건이 충족되지 않으면 접근 (후순위)
            new BTSequence(blackboard, transform, new List<BTNode> {
                new IsEnemyVisibleCondition(blackboard, transform),
                // [수정] 이동 목표 거리를 offensiveAttackRange * 0.8f로 변경
                new MoveTowardsEnemyAction(blackboard, transform, 5f, offensiveAttackRange * 0.8f)
            }),

            // 6. 적이 없거나 다른 조건이 충족되지 않으면 대기
            new IdleAction(blackboard, transform)
        });
        
    }
}
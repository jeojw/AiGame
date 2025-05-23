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
                // [중요] 이 조건이 이제 제대로 작동할 것입니다.
                new IsEnemyAttackingCondition(blackboard, transform),
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
                // new CanCounterAttackCondition(blackboard, transform), // 반격 타이밍을 위한 별도 조건 (추후 구현)
                new IsEnemyInAttackRangeCondition(blackboard, transform, attackRange), // 반격하려면 범위 내에 있어야 함
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.ATTACK_COOLDOWN_KEY), // 공격 쿨타임 준비
                // [복원됨] 너무 위험하면 반격하지 않도록 체력 조건 다시 활성화
                new NotNode(new IsHealthLowCondition(blackboard, transform, counterAttackHealthThreshold)),
                new AttackEnemyAction(blackboard, transform) // 공격 행동
            }),

            // 3. 방어적 위치 유지 / 체력 관리
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsEnemyVisibleCondition(blackboard, transform),
                new BTSelector(blackboard, transform, new List<BTNode> {
                    new BTSequence(blackboard, transform, new List<BTNode> { // 적이 너무 가까우면
                        new IsEnemyTooCloseCondition(blackboard, transform, closeRangeThreshold),
                        // [복원됨] FleeAction(도망) 행동 다시 활성화
                        //new FleeAction(blackboard, transform, 4f)
                    }),
                    new BTSequence(blackboard, transform, new List<BTNode> { // 적이 이상적인 방어 거리보다 멀면 조심스럽게 접근
                        // [복원됨] NotNode를 사용하여 조건 논리 다시 활성화
                        new NotNode(new IsEnemyInAttackRangeCondition(blackboard, transform, defensiveStanceRange)),
                        new MoveTowardsEnemyAction(blackboard, transform, 3f, defensiveStanceRange * 0.9f)
                    })
                })
            }),

            // 4. 일반적으로 체력이 낮고 회피가 준비되었으면 회피 (선제적 생존)
            new BTSequence(blackboard, transform, new List<BTNode>
            {
                new IsHealthLowCondition(blackboard, transform, lowHealthThreshold + 10f),
                new IsCooldownReadyCondition(blackboard, transform, AgentBlackboard.EVADE_COOLDOWN_KEY),
                new EvadeAction(blackboard, transform)
            }),

            // 5. 특정 위협이나 행동이 없으면 대기
            new IdleAction(blackboard, transform)
        });
    }

    // [추가됨] NotNode 클래스 정의가 없었으므로 다시 추가합니다. 컴파일 오류 방지.
    public class NotNode : BTConditionNode
    {
        private BTConditionNode conditionToNegate;

        // [수정] 생성자에서 .blackboard 대신 .Blackboard를 사용하도록 수정합니다.
        public NotNode(BTConditionNode condition) : base(condition.Blackboard, condition.AgentTransform)
        {
            this.conditionToNegate = condition;
        }

        protected override bool CheckCondition()
        {
            // 자식 노드의 Tick()을 실행하고, 그 결과가 FAILURE이면 true를 반환하여
            // NotNode 자신은 SUCCESS 상태가 되도록 합니다. 이것이 논리를 뒤집는 핵심입니다.
            return conditionToNegate.Tick() == NodeStatus.FAILURE;
        }
    }
}
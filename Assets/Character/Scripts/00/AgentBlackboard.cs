// File: AgentBlackboard.cs
using UnityEngine;
using System.Collections.Generic;

public class AgentBlackboard
{
    // 에이전트 능력치 및 상태
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isInvincible = false;

    public bool canCounterAttack = false;        // 반격 가능 상태 플래그
    public float defenseInitiationTime = -1f;    // 방어 시작 시간 (유예 시간 판정용, -1f는 비활성)
    public float currentAttackDamageMultiplier = 1.0f; // 현재 공격에 적용될 데미지 배율

    // 적 정보
    public Transform enemyTransform;
    public float enemyDistance;
    public float enemyHealth;                     // 적의 체력 (AgentController에서 업데이트)

    // 행동 쿨타임 관련
    public Dictionary<string, float> actionCooldowns = new Dictionary<string, float>();
    public const string ATTACK_COOLDOWN_KEY = "Attack"; // 공격 쿨타임 키
    public const string DEFEND_COOLDOWN_KEY = "Defend"; // 방어 쿨타임 키
    public const string EVADE_COOLDOWN_KEY = "Evade";   // 회피 쿨타임 키

    public float attackCooldownDuration = 2.5f;  // 공격 쿨타임 지속 시간
    public float defendCooldownDuration = 2.5f;  // 방어 쿨타임 지속 시간
    public float evadeCooldownDuration = 5.0f;   // 회피 쿨타임 지속 시간

    public AgentBlackboard()
    {
        currentHealth = maxHealth; // 현재 체력을 최대 체력으로 초기화
    }

    // 적 정보 업데이트 메소드
    public void UpdateEnemyInfo(Transform enemy, float distance, float health)
    {
        this.enemyTransform = enemy;
        this.enemyDistance = distance;
        this.enemyHealth = health;
    }

    // 특정 행동이 사용 가능한지 (쿨타임이 지났는지) 확인하는 메소드
    public bool IsActionReady(string actionKey)
    {
        // 딕셔너리에 키가 없거나 (아직 한 번도 사용 안 함), 현재 시간이 기록된 쿨타임 종료 시간보다 크거나 같으면 사용 가능
        return !actionCooldowns.ContainsKey(actionKey) || Time.time >= actionCooldowns[actionKey];
    }

    // [수정] 특정 행동의 쿨타임을 설정하는 메소드 (if-else if 구문으로 명확화)
    public void SetActionCooldown(string actionKey)
    {
        float duration = 0f;

        if (actionKey == ATTACK_COOLDOWN_KEY)
        {
            duration = attackCooldownDuration;
        }
        else if (actionKey == DEFEND_COOLDOWN_KEY)
        {
            duration = defendCooldownDuration;
        }
        else if (actionKey == EVADE_COOLDOWN_KEY)
        {
            duration = evadeCooldownDuration;
        }
        // 다른 쿨타임 키가 추가될 경우 여기에 else if를 추가할 수 있습니다.

        // 유효한 쿨타임 지속 시간이 설정된 경우에만 쿨타임 기록
        if (duration > 0)
        {
            actionCooldowns[actionKey] = Time.time + duration;
        }
        else if (!string.IsNullOrEmpty(actionKey) && duration == 0)
        {
            // 쿨타임이 0인 행동은 즉시 사용 가능하도록 기존 키를 제거하거나 시간을 과거로 설정할 수 있으나,
            // 현재는 duration > 0 조건만 있으므로 쿨타임 0인 행동은 여기에 해당하지 않음.
            // 필요시 actionCooldowns.Remove(actionKey); 등을 고려.
        }
    }

    // 데미지를 받는 메소드 (체력 감소만 담당)
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        // 상세 로그 생성 기능은 AgentController로 이동되었습니다.
    }

    // 무적 상태 시작 메소드
    public void StartInvincibility(float duration) // duration 매개변수는 현재 사용되지 않으나, 향후 확장성을 위해 유지
    {
        isInvincible = true;
    }

    // 무적 상태 종료 메소드
    public void EndInvincibility()
    {
        isInvincible = false;
    }
}
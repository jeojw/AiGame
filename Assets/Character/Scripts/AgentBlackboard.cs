// File: AgentBlackboard.cs (AgentBlackboard.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class AgentBlackboard
{
    // 에이전트 능력치
    public float maxHealth = 100f; // 최대 체력
    public float currentHealth;    // 현재 체력
    public bool isInvincible = false; // 무적 상태 여부

    // 적 정보
    public Transform enemyTransform; // 적의 Transform
    public float enemyDistance;      // 적과의 거리
    public float enemyHealth;        // 적의 체력 (알 수 있다고 가정)

    // 쿨타임 (행동 이름, 종료 시간)
    public Dictionary<string, float> actionCooldowns = new Dictionary<string, float>();
    public const string ATTACK_COOLDOWN_KEY = "Attack"; // 공격 쿨타임 키
    public const string DEFEND_COOLDOWN_KEY = "Defend"; // 방어 쿨타임 키
    public const string EVADE_COOLDOWN_KEY = "Evade";   // 회피 쿨타임 키

    public float attackCooldownDuration = 2.5f; // 공격 쿨타임 지속 시간
    public float defendCooldownDuration = 2.5f; // 방어 쿨타임 지속 시간
    public float evadeCooldownDuration = 5.0f;  // 회피 쿨타임 지속 시간


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
        return !actionCooldowns.ContainsKey(actionKey) || Time.time >= actionCooldowns[actionKey];
    }

    // 특정 행동의 쿨타임을 설정하는 메소드
    public void SetActionCooldown(string actionKey)
    {
        float duration = 0f;
        if (actionKey == ATTACK_COOLDOWN_KEY) duration = attackCooldownDuration;
        else if (actionKey == DEFEND_COOLDOWN_KEY) duration = defendCooldownDuration;
        else if (actionKey == EVADE_COOLDOWN_KEY) duration = evadeCooldownDuration;

        if (duration > 0)
        {
            actionCooldowns[actionKey] = Time.time + duration;
        }
    }

    // 데미지를 받는 메소드
    public void TakeDamage(float amount)
    {
        if (!isInvincible) // 무적 상태가 아니라면
        {
            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
            Debug.Log($"에이전트가 {amount} 데미지를 받음, 현재 체력: {currentHealth}");
        }
        else
        {
            Debug.Log("에이전트가 무적 상태이므로 데미지를 받지 않음.");
        }
    }

    // 무적 상태 시작 메소드
    public void StartInvincibility(float duration)
    {
        isInvincible = true;
        // 실제로는 에이전트 컨트롤러에서 코루틴을 사용하여 무적 상태를 해제할 수 있습니다.
    }
    // 무적 상태 종료 메소드
    public void EndInvincibility()
    {
        isInvincible = false;
    }
}
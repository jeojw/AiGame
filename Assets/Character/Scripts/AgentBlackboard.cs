// File: AgentBlackboard.cs (AgentBlackboard.cs ����)
using UnityEngine;
using System.Collections.Generic;

public class AgentBlackboard
{
    
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isInvincible = false;

    public bool canCounterAttack = false;
    public float defenseInitiationTime = -1f;

    public float currentAttackDamageMultiplier = 1.0f;

    public Transform enemyTransform;
    public float enemyDistance;
    public float enemyHealth;
    public Dictionary<string, float> actionCooldowns = new Dictionary<string, float>();
    public const string ATTACK_COOLDOWN_KEY = "Attack";
    public const string DEFEND_COOLDOWN_KEY = "Defend";
    public const string EVADE_COOLDOWN_KEY = "Evade";
    public float attackCooldownDuration = 2.5f;
    public float defendCooldownDuration = 2.5f;
    public float evadeCooldownDuration = 5.0f;

    public AgentBlackboard()
    {
        currentHealth = maxHealth;
    }

    // ... UpdateEnemyInfo, IsActionReady, SetActionCooldown �޼ҵ�� �״�� ...
    public void UpdateEnemyInfo(Transform enemy, float distance, float health)
    {
        this.enemyTransform = enemy;
        this.enemyDistance = distance;
        this.enemyHealth = health;
    }

    public bool IsActionReady(string actionKey)
    {
        return !actionCooldowns.ContainsKey(actionKey) || Time.time >= actionCooldowns[actionKey];
    }

    public void SetActionCooldown(string actionKey)
    {
        float duration = actionKey switch
        {
            _ATTACK_COOLDOWN_KEY => attackCooldownDuration,
            _DEFEND_COOLDOWN_KEY => defendCooldownDuration,
            _EVADE_COOLDOWN_KEY => evadeCooldownDuration,
            _ => 0f
        };
        actionCooldowns[actionKey] = Time.time + duration;
    }

    // --- [����] TakeDamage �޼ҵ带 ������ �ܼ��� ���·� ���� ---
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        // �� �α� ���� ����� AgentController�� �̵��߽��ϴ�.
    }

    public void StartInvincibility(float duration)
    {
        isInvincible = true;
    }
    public void EndInvincibility()
    {
        isInvincible = false;
    }
}
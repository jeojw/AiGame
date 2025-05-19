// File: AgentBlackboard.cs (AgentBlackboard.cs ����)
using UnityEngine;
using System.Collections.Generic;

public class AgentBlackboard
{
    // ������Ʈ �ɷ�ġ
    public float maxHealth = 100f; // �ִ� ü��
    public float currentHealth;    // ���� ü��
    public bool isInvincible = false; // ���� ���� ����

    // �� ����
    public Transform enemyTransform; // ���� Transform
    public float enemyDistance;      // ������ �Ÿ�
    public float enemyHealth;        // ���� ü�� (�� �� �ִٰ� ����)

    // ��Ÿ�� (�ൿ �̸�, ���� �ð�)
    public Dictionary<string, float> actionCooldowns = new Dictionary<string, float>();
    public const string ATTACK_COOLDOWN_KEY = "Attack"; // ���� ��Ÿ�� Ű
    public const string DEFEND_COOLDOWN_KEY = "Defend"; // ��� ��Ÿ�� Ű
    public const string EVADE_COOLDOWN_KEY = "Evade";   // ȸ�� ��Ÿ�� Ű

    public float attackCooldownDuration = 2.5f; // ���� ��Ÿ�� ���� �ð�
    public float defendCooldownDuration = 2.5f; // ��� ��Ÿ�� ���� �ð�
    public float evadeCooldownDuration = 5.0f;  // ȸ�� ��Ÿ�� ���� �ð�


    public AgentBlackboard()
    {
        currentHealth = maxHealth; // ���� ü���� �ִ� ü������ �ʱ�ȭ
    }

    // �� ���� ������Ʈ �޼ҵ�
    public void UpdateEnemyInfo(Transform enemy, float distance, float health)
    {
        this.enemyTransform = enemy;
        this.enemyDistance = distance;
        this.enemyHealth = health;
    }

    // Ư�� �ൿ�� ��� �������� (��Ÿ���� ��������) Ȯ���ϴ� �޼ҵ�
    public bool IsActionReady(string actionKey)
    {
        return !actionCooldowns.ContainsKey(actionKey) || Time.time >= actionCooldowns[actionKey];
    }

    // Ư�� �ൿ�� ��Ÿ���� �����ϴ� �޼ҵ�
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

    // �������� �޴� �޼ҵ�
    public void TakeDamage(float amount)
    {
        if (!isInvincible) // ���� ���°� �ƴ϶��
        {
            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
            Debug.Log($"������Ʈ�� {amount} �������� ����, ���� ü��: {currentHealth}");
        }
        else
        {
            Debug.Log("������Ʈ�� ���� �����̹Ƿ� �������� ���� ����.");
        }
    }

    // ���� ���� ���� �޼ҵ�
    public void StartInvincibility(float duration)
    {
        isInvincible = true;
        // �����δ� ������Ʈ ��Ʈ�ѷ����� �ڷ�ƾ�� ����Ͽ� ���� ���¸� ������ �� �ֽ��ϴ�.
    }
    // ���� ���� ���� �޼ҵ�
    public void EndInvincibility()
    {
        isInvincible = false;
    }
}
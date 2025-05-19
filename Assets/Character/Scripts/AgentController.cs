// File: AgentController.cs (AgentController.cs ����)
using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    public Transform enemy; // �ν����Ϳ��� �Ҵ�
    public float detectionRadius = 20f; // �� ���� �ݰ�
    public float attackRange = 2f;      // ���� ����
    public float closeRangeThreshold = 5f; // "�ʹ� �����" �Ǵ� ���� �Ÿ�
    public float lowHealthThreshold = 30f; // ü�� ���� �Ǵ� ���� (���� �Ǵ� ���밪)

    protected AgentBlackboard blackboard; // ������ ����
    protected BTNode rootNode;            // �ൿ Ʈ���� ��Ʈ ���

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f; // ������ ���� ����
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.attackCooldownDuration = 2.5f; // ���� ��Ÿ��
        blackboard.defendCooldownDuration = 2.5f; // ��� ��Ÿ��
        blackboard.evadeCooldownDuration = 5f;    // ȸ�� ��Ÿ��
    }

    protected virtual void Start()
    {
        InitializeBehaviorTree(); // �ൿ Ʈ�� �ʱ�ȭ
    }

    // �ൿ Ʈ���� �ʱ�ȭ�ϴ� �߻� �޼ҵ� (�Ļ� Ŭ�������� ����)
    protected abstract void InitializeBehaviorTree();

    protected virtual void Update()
    {
        if (enemy == null) // ���� ���ٸ�
        {
            // ���������� ���� ã�ų� ��� ���� ����
            // ����� ���� �Ҵ�Ǿ��ų� ã�����ٰ� ����
            FindEnemy(); // ������ �� ã�� ����
        }

        // ������ ������Ʈ
        if (enemy != null)
        {
            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), 100f); // �� ü���� 100f�� ����, ���� ������ ��ü �ʿ�
        }
        else
        {
            blackboard.enemyTransform = null; // �� ����
        }


        if (rootNode != null) // ��Ʈ ��尡 �ִٸ�
        {
            rootNode.Tick(); // �ൿ Ʈ�� ����
        }

        // ����׿�
        // Debug.Log($"������Ʈ ü��: {blackboard.currentHealth}, ������ �Ÿ�: {blackboard.enemyDistance}");
    }

    void FindEnemy()
    {
        // ����: �±׷� ã��, �� ������ ��� ����
        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy"); // ���� "Enemy" �±׸� ������ �ִ��� Ȯ��
        if (enemyObject != null)
        {
            enemy = enemyObject.transform;
        }
    }

    // --- �ൿ �޼ҵ� (ActionNode���� ȣ���) ---
    public virtual NodeStatus MoveTowards(Vector3 targetPosition, float speed, float stopDistance)
    {
        if (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            transform.LookAt(targetPosition); // �⺻���� �ٶ󺸱�
            // �̵� �ִϸ��̼� ���
            Debug.Log("�ൿ: ��ǥ �������� �̵� ��");
            return NodeStatus.RUNNING; // ���� �̵� ��
        }
        return NodeStatus.SUCCESS; // ����
    }

    public virtual NodeStatus MoveAwayFrom(Vector3 targetPosition, float speed, float moveDistance)
    {
        Vector3 direction = (transform.position - targetPosition).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.LookAt(transform.position + direction); // �̵� �������� �ٶ󺸱�
        // �̵� �ִϸ��̼� ���
        Debug.Log("�ൿ: ��ǥ �������κ��� �־����� ��");
        // �� �ൿ�� �� ���� �̵� �� �������� �����ϰų� ���� �ð� ���� ����� �� �ֽ��ϴ�.
        // �ܼ�ȭ�� ����, �� ���� �̵� �� SUCCESS�� ��ȯ�ϵ��� �մϴ�.
        return NodeStatus.SUCCESS;
    }


    public virtual NodeStatus PerformAttack()
    {
        Debug.Log("�ൿ: ���� ����!");
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY); // ���� ��Ÿ�� ����
        // ���� �ִϸ��̼� ����
        // ���⿡ ������ ���� ���� (��: SphereCast, �ִϸ��̼� �̺�Ʈ)
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("�ൿ: ��� ����!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY); // ��� ��Ÿ�� ����
        blackboard.StartInvincibility(blackboard.defendCooldownDuration); // ���� ���� �ð� ���� ���� ���·� ����
        // ��� �ִϸ��̼� ����
        // ���� ���ӿ����� ���� �ð��� �� ª�ų� �ִϸ��̼� ���¿� ������ �� �ֽ��ϴ�.
        // ���� ���� ���� ��Ŀ������ �ʿ��մϴ�. �ܼ�ȭ�� ���� AgentController �Ǵ� �����忡�� ó���մϴ�.
        Invoke(nameof(StopDefendInvincibility), blackboard.defendCooldownDuration); // ��� ���� �ð� �� ���� ����
        return NodeStatus.SUCCESS;
    }
    private void StopDefendInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("��� ���� ���� ����.");
    }


    public virtual NodeStatus PerformEvade()
    {
        Debug.Log("�ൿ: ȸ�� ����!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY); // ȸ�� ��Ÿ�� ����
        blackboard.StartInvincibility(1.0f); // ȸ�Ǵ� ª�� �ð� ���� ���� ���� �ο� (��: 1��)
        Invoke(nameof(StopEvadeInvincibility), 1.0f); // ����: 1�ʰ� ����
        // ȸ�� �̵� ���� (��: Ư�� �������� ���� �뽬)
        // ����: �ڷ� �뽬
        transform.Translate(Vector3.back * 2f, Space.Self); // �ڽ��� �������� 2 ���� �뽬
        // ȸ�� �ִϸ��̼� ����
        return NodeStatus.SUCCESS;
    }
    private void StopEvadeInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("ȸ�� ���� ���� ����.");
    }

    public virtual NodeStatus Idle()
    {
        Debug.Log("�ൿ: ��� ��");
        // ��� �ִϸ��̼� ����
        return NodeStatus.SUCCESS;
    }

    // --- �浹/������ ó�� ---
    // �Ϲ������� �浹 ��ũ��Ʈ�� �߻�ü �ǰ� �� ȣ��˴ϴ�.
    public void HandleDamage(float damage)
    {
        blackboard.TakeDamage(damage);
        if (blackboard.currentHealth <= 0)
        {
            Die(); // ü���� 0 �����̸� ���� ó��
        }
    }

    protected virtual void Die()
    {
        Debug.Log(gameObject.name + "��(��) �׾����ϴ�.");
        // ���� �ִϸ��̼� ���, ��ũ��Ʈ ��Ȱ��ȭ ��
        Destroy(gameObject, 2f); // ����: 2�� �� ������Ʈ �ı�
    }
}
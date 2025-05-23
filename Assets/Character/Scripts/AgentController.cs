// File: AgentController.cs (AgentController.cs ����)
using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    public Transform enemy; // �ν����Ϳ��� �Ҵ�
    public float detectionRadius = 20f; // �� ���� �ݰ�
    public float attackRange = 2f;      // ���� ����
    public float closeRangeThreshold = 5f; // "�ʹ� �����" �Ǵ� ���� �Ÿ�
    public float lowHealthThreshold = 30f; // ü�� ���� �Ǵ� ���� (���� �Ǵ� ���밪)
    public float evadeDistance = 2.0f; // ȸ�� �� �̵��� �Ÿ�

    protected AgentBlackboard blackboard; // ������ ����
    protected BTNode rootNode;            // �ൿ Ʈ���� ��Ʈ ���

    private Animator animator; // Animator ���� ����
    private CharacterController characterController;

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f; // ������ ���� ���� [cite: 10]
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.attackCooldownDuration = 2.5f; // ���� ��Ÿ�� [cite: 10]
        blackboard.defendCooldownDuration = 2.5f; // ��� ��Ÿ�� [cite: 10]
        blackboard.evadeCooldownDuration = 5f;    // ȸ�� ��Ÿ�� [cite: 10]
        animator = GetComponent<Animator>(); // Animator ������Ʈ ��������
        characterController = GetComponent<CharacterController>(); // CharacterController ������Ʈ ��������
    }

    protected virtual void Start()
    {
        InitializeBehaviorTree(); // �ൿ Ʈ�� �ʱ�ȭ
    }

    // �ൿ Ʈ���� �ʱ�ȭ�ϴ� �߻� �޼ҵ� (�Ļ� Ŭ�������� ����)
    protected abstract void InitializeBehaviorTree();

    protected virtual void Update()
    {
        if (enemy == null)
        {
            FindEnemy(); // ���� ������ ã��
        }

        // ������ ������Ʈ
        if (enemy != null)
        {
            // [������] ���� ���� ü���� ���������� ����
            float enemyCurrentHealth = 100f; // �⺻��
            AgentController enemyController = enemy.GetComponent<AgentController>();
            if (enemyController != null)
            {
                enemyCurrentHealth = enemyController.blackboard.currentHealth;
            }
            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), enemyCurrentHealth);
        }
        else
        {
            blackboard.enemyTransform = null; // �� ����
        }

        if (rootNode != null) // ��Ʈ ��尡 �ִٸ�
        {
            rootNode.Tick(); // �ൿ Ʈ�� ����
        }
    }

    void FindEnemy()
    {
        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy"); // "Enemy" �±׷� �� ã��
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
            Vector3 direction = (targetPosition - transform.position).normalized;
            characterController.Move(direction * speed * Time.deltaTime); // CharacterController�� �̵�
            transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z)); // Y�� �����Ͽ� �ٶ󺸱�

            if (animator != null) animator.SetFloat("Speed", speed);
            Debug.Log("�ൿ: ��ǥ �������� �̵� �� (CharacterController)");
            return NodeStatus.RUNNING;
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus MoveAwayFrom(Vector3 targetPosition, float speed, float moveDistance)
    {
        // [������] CharacterController�� ����Ͽ� �浹�� �ν��ϸ� �̵��ϵ��� ����
        Vector3 direction = (transform.position - targetPosition).normalized;
        characterController.Move(direction * speed * Time.deltaTime);
        transform.LookAt(transform.position + direction); // �̵� �������� �ٶ󺸱�

        if (animator != null) animator.SetFloat("Speed", speed); // �־����� �����ӵ� �̵� �ִϸ��̼� ���

        Debug.Log("�ൿ: ��ǥ �������κ��� �־����� �� (CharacterController)");
        // �� �ൿ�� �� ���� �̵� �� �������� �����Ͽ�, BT�� ���� �Ǵ��� ������ �ϵ��� ��
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformAttack()
    {
        Debug.Log("�ൿ: ���� ����!");
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY); // ���� ��Ÿ�� ���� [cite: 10]
        transform.LookAt(new Vector3(enemy.position.x, transform.position.y, enemy.position.z)); // ���� �� ���� �ٶ󺸵��� ��

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }

        // ������ ó�� ������ �ִϸ��̼� �̺�Ʈ�� ���� �ý������� �����ϴ� ���� �� ��Ȯ������, ���⼭�� ��� �ߵ����� ����
        float attackDamage = 10f; // ���� ���ݷ�
        // SphereCast�� �̿��� ���� ���� ����
        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out RaycastHit hit, attackRange))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                AgentController enemyController = hit.collider.GetComponent<AgentController>();
                if (enemyController != null)
                {
                    Debug.Log(gameObject.name + "��(��) " + enemy.name + "��(��) �����Ͽ� " + attackDamage + " �������� �������ϴ�.");
                    enemyController.HandleDamage(attackDamage);
                }
            }
        }
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformDefend()
    {
        Debug.Log("�ൿ: ��� ����!");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY); // ��� ��Ÿ�� ���� [cite: 10]
        blackboard.StartInvincibility(blackboard.defendCooldownDuration); // ��� �ð� ���� ���� [cite: 10]

        if (animator != null)
        {
            animator.SetTrigger("IsDefending");
        }
        Invoke(nameof(StopDefendInvincibility), blackboard.defendCooldownDuration);
        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus PerformEvade()
    {
        Debug.Log("�ൿ: ȸ�� ����!");
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY); // ȸ�� ��Ÿ�� ���� [cite: 10]
        blackboard.StartInvincibility(1.0f); // ȸ�� �� ª�� �ð� ���� [cite: 10]
        Invoke(nameof(StopEvadeInvincibility), 1.0f);

        if (animator != null)
        {
            animator.SetTrigger("IsEvading");
        }



        return NodeStatus.SUCCESS;
    }

    public virtual NodeStatus Idle()
    {
        Debug.Log("�ൿ: ��� ��");
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
        return NodeStatus.SUCCESS;
    }

    private void StopDefendInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("��� ���� ���� ����.");
    }

    private void StopEvadeInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log("ȸ�� ���� ���� ����.");
    }

    // --- �浹/������ ó�� ---
    public void HandleDamage(float damage)
    {
        if (blackboard.isInvincible) // �� ȸ�� �߿��� ������ ��ȿȭ
        {
            Debug.Log(gameObject.name + "��(��) ������ ��ȿȭ�߽��ϴ�.");
            return;
        }

        blackboard.TakeDamage(damage);
        if (blackboard.currentHealth <= 0)
        {
            Die(); // ü���� 0 �����̸� ���� ó��
        }
    }

    protected virtual void Die()
    {
        Debug.Log(gameObject.name + "��(��) �׾����ϴ�.");
        if (animator != null)
        {
            animator.SetTrigger("Die"); // ���� �ִϸ��̼� ����
        }

        // ��ũ��Ʈ ��Ȱ��ȭ�Ͽ� �� �̻� �ൿ���� �ʵ��� ��
        this.enabled = false;

        // ���� �ð� �� ������Ʈ �ı�
        Destroy(gameObject, 3f);
    }
}
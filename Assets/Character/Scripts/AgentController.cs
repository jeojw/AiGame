// File: AgentController.cs
using UnityEngine;
using System.Collections;

public abstract class AgentController : MonoBehaviour
{
    // ... ��� ������ Awake, Start, Update �� �ٸ� �޼ҵ���� �״�� ...

    // --- [����] PerformAttack �޼ҵ� ---
    // ���� ���� �ִϸ��̼Ǹ� ���۽�Ű��, ���� ������ ������ �ִϸ��̼� �̺�Ʈ�� �ѱ�ϴ�.
    public virtual NodeStatus PerformAttack(float damageMultiplier = 1.0f)
    {
        Debug.Log("<color=blue>" + gameObject.name + " - PerformAttack: ���� �ִϸ��̼� ����! (����� ����: " + damageMultiplier + ")</color>");
        blackboard.SetActionCooldown(AgentBlackboard.ATTACK_COOLDOWN_KEY);
        blackboard.canCounterAttack = false; // ���� �� �ݰ� ��ȸ �ʱ�ȭ

        // [�߰�] �ִϸ��̼� �̺�Ʈ���� ����� ������ ������ �������忡 ����
        blackboard.currentAttackDamageMultiplier = damageMultiplier;

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }
        // ���� ������ ���� ����(SphereCast ��)�� �Ʒ� ActuallyDealDamage() �޼ҵ�� �̵��߽��ϴ�.
        return NodeStatus.SUCCESS;
    }

    // --- [�߰�] �ִϸ��̼� �̺�Ʈ���� ȣ��� ���� ������ ó�� �޼ҵ� ---
    public void ActuallyDealDamage() // �� �޼ҵ带 ���� �ִϸ��̼��� Ư�� �����ӿ� �̺�Ʈ�� �߰��ؾ� �մϴ�.
    {
        Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: �ִϸ��̼� �̺�Ʈ �߻�! ���� ���� ���� ����!</color>");

        // �������忡�� ���� ������ ������ ������ ������
        float finalDamage = this.attackDamage * blackboard.currentAttackDamageMultiplier;

        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out RaycastHit hit, attackRange))
        {
            Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: SphereCast ����! -> " + hit.collider.gameObject.name + "</color>");
            if (hit.collider.CompareTag("Enemy"))
            {
                AgentController enemyController = hit.collider.GetComponent<AgentController>();
                if (enemyController != null)
                {
                    Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: " + enemyController.gameObject.name + "���� HandleDamage ȣ��.</color>");
                    enemyController.HandleDamage(finalDamage, this);
                }
            }
        }
        else
        {
            Debug.Log("<color=red>" + gameObject.name + " - ActuallyDealDamage: SphereCast �꽺��.</color>");
        }
    }

    // (��ü �ڵ带 ���Ͻø� ���� �亯�� AgentController �ڵ忡�� �� �� �޼ҵ�� Blackboard ������ �����Ͻø� �˴ϴ�.)
    #region Unchanged_Methods_And_Variables
    public Transform enemy;
    public float detectionRadius = 20f;
    public float attackRange = 2f;
    public float closeRangeThreshold = 5f;
    public float lowHealthThreshold = 30f;
    public float evadeDistance = 2.5f;
    public float rotationSpeed = 10f;
    public float evadeDuration = 0.3f;
    public float attackDamage = 10f;
    public float defenseGracePeriod = 0.2f;
    protected AgentBlackboard blackboard;
    protected BTNode rootNode;
    private Animator animator;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private readonly float gravityValue = -9.81f;
    private Coroutine activeEvadeCoroutine = null;

    protected virtual void Awake()
    {
        blackboard = new AgentBlackboard();
        blackboard.maxHealth = 100f;
        blackboard.currentHealth = blackboard.maxHealth;
        blackboard.attackCooldownDuration = 2.5f;
        blackboard.defendCooldownDuration = 2.5f;
        blackboard.evadeCooldownDuration = 5f;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }
    protected virtual void Start()
    {
        InitializeBehaviorTree();
    }
    protected abstract void InitializeBehaviorTree();
    protected virtual void Update()
    {
        if (enemy == null) FindEnemy();
        bool isPerformingAction = false;
        if (animator != null)
        {
            var currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentStateInfo.IsTag("Attack") || currentStateInfo.IsTag("Defend") || currentStateInfo.IsTag("Evade"))
                isPerformingAction = true;
        }
        if (enemy != null)
        {
            float enemyCurrentHealth = 100f;
            AgentController enemyCtrl = enemy.GetComponent<AgentController>();
            if (enemyCtrl != null) enemyCurrentHealth = enemyCtrl.blackboard.currentHealth;
            blackboard.UpdateEnemyInfo(enemy, Vector3.Distance(transform.position, enemy.position), enemyCurrentHealth);
            if (!isPerformingAction) SmoothLookAtEnemy();
        }
        else
        {
            blackboard.enemyTransform = null;
        }
        if (!isPerformingAction)
        {
            if (characterController.isGrounded && playerVelocity.y < 0) playerVelocity.y = 0f;
            playerVelocity.y += gravityValue * Time.deltaTime;
            characterController.Move(playerVelocity * Time.deltaTime);
        }
        if (rootNode != null) rootNode.Tick();
    }
    void SmoothLookAtEnemy()
    {
        Vector3 direction = enemy.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    void FindEnemy()
    {
        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy");
        if (enemyObject != null)
        {
            enemy = enemyObject.transform;
        }
    }
    public virtual NodeStatus MoveTowards(Vector3 targetPosition, float speed, float stopDistance)
    {
        if (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0;
            direction.Normalize();
            characterController.Move(direction * speed * Time.deltaTime);
            if (animator != null) animator.SetFloat("Speed", speed);
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
        Vector3 direction = (transform.position - targetPosition);
        direction.y = 0;
        direction.Normalize();
        characterController.Move(direction * speed * Time.deltaTime);
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        if (animator != null) animator.SetFloat("Speed", speed);
        return NodeStatus.SUCCESS;
    }
    public virtual NodeStatus PerformDefend()
    {
        Debug.Log(gameObject.name + " - PerformDefend: ȣ���. ��� ���� �õ�.");
        blackboard.SetActionCooldown(AgentBlackboard.DEFEND_COOLDOWN_KEY);
        blackboard.StartInvincibility(blackboard.defendCooldownDuration);
        blackboard.defenseInitiationTime = Time.time;
        Debug.Log(gameObject.name + " - PerformDefend: isInvincible = " + blackboard.isInvincible + ", defenseInitiationTime = " + blackboard.defenseInitiationTime);
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
        blackboard.SetActionCooldown(AgentBlackboard.EVADE_COOLDOWN_KEY);
        blackboard.StartInvincibility(evadeDuration);
        Invoke(nameof(StopEvadeInvincibility), evadeDuration);
        if (animator != null)
        {
            animator.SetTrigger("IsEvading");
        }
        if (activeEvadeCoroutine != null)
        {
            StopCoroutine(activeEvadeCoroutine);
        }
        activeEvadeCoroutine = StartCoroutine(EvadeCoroutine());
        return NodeStatus.SUCCESS;
    }
    private IEnumerator EvadeCoroutine()
    {
        float randomDirection = Random.value > 0.5f ? 1f : -1f;
        Vector3 evadeStartDirection = transform.right * randomDirection;
        float elapsedTime = 0f;
        while (elapsedTime < evadeDuration)
        {
            Vector3 movement = evadeStartDirection * (evadeDistance / evadeDuration) * Time.deltaTime;
            characterController.Move(movement);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        activeEvadeCoroutine = null;
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
        blackboard.defenseInitiationTime = -1f;
        Debug.Log(gameObject.name + " ��� ���� ���� �� ���� �ð� ����.");
    }
    private void StopEvadeInvincibility()
    {
        blackboard.EndInvincibility();
        Debug.Log(gameObject.name + " ȸ�� ���� ���� ����.");
    }
    public void HandleDamage(float damage, AgentController attacker)
    {
        Debug.Log(gameObject.name + " - HandleDamage: ȣ���! ������: " + attacker.gameObject.name + ", isInvincible: " + blackboard.isInvincible);
        if (animator != null)
        {
            AnimatorStateInfo currentAnimState = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log(gameObject.name + " - HandleDamage: ���� �ִϸ��̼� �±� 'Defend' ����: " + currentAnimState.IsTag("Defend"));
        }
        float damageTaken = damage;
        if (blackboard.isInvincible)
        {
            bool inDefendAnim = animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Defend");
            bool inGracePeriod = blackboard.defenseInitiationTime > -0.5f && (Time.time - blackboard.defenseInitiationTime) < defenseGracePeriod;
            if (inDefendAnim || inGracePeriod)
            {
                Debug.Log("<color=green>" + gameObject.name + ": �ڡڡ� ��� ����! (�ִϸ��̼�: " + inDefendAnim + ", �����ð�: " + inGracePeriod + ") ī���� ��ȸ Ȱ��ȭ! �ڡڡ�" + "</color>");
                blackboard.canCounterAttack = true;
                blackboard.defenseInitiationTime = -1f;
                damageTaken = damage * 0.1f;
            }
            else if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Evade"))
            {
                Debug.Log(gameObject.name + ": ȸ�� ����! ������ ����.");
                damageTaken = 0f;
            }
            else
            {
                Debug.LogWarning(gameObject.name + ": �� �� ���� ���� ���¿��� �ǰ�. �ϴ� Ĩ������ ����.");
                damageTaken = damage * 0.1f;
            }
            blackboard.TakeDamage(damageTaken);
        }
        else
        {
            blackboard.TakeDamage(damageTaken);
        }
        string logMessage = string.Format("[������] {0} -> {1} | ��û������: {2}, ���� ���� ������: {3} | ���� ü��: ({0}) {4}/{5}, ({1}) {6}/{7}",
            attacker.gameObject.name,
            this.gameObject.name,
            damage.ToString("F1"),
            damageTaken.ToString("F1"),
            attacker.blackboard.currentHealth.ToString("F0"),
            attacker.blackboard.maxHealth,
            this.blackboard.currentHealth.ToString("F0"),
            this.blackboard.maxHealth
        );
        Debug.Log(logMessage);
        if (blackboard.currentHealth <= 0)
        {
            Die();
        }
    }
    protected virtual void Die()
    {
        Debug.Log(gameObject.name + "��(��) �׾����ϴ�.");
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        this.enabled = false;
        Destroy(gameObject, 3f);
    }
    #endregion
}
// File: AgentBlackboard.cs (AgentBlackboard.cs 파일)
using UnityEngine;
using System.Collections.Generic;

public class AgentBlackboard
{
    // 에이전트 능력치
    private float _maxHealth = 100f; // 최대 체력
    private float _currentHealth;    // 현재 체력
    private bool _isInvincible = false; // 무적 상태 여부
    private bool _isAttacking = false; // [추가] 공격 상태 여부
    private bool _isDefending = false;
    private bool _isEvading = false;
    private bool _isGetAttacked = false;
    private bool _canCounterAttack = false;
    private bool _isDead = false; // [추가]
    // [추가] 현재 공격에 적용될 데미지 배율을 저장하는 변수
    public float currentAttackMultiplier = 1.0f;

    // [추가] 적의 마지막 공격 시간을 기록할 변수
    public float lastEnemyAttackTime = 0f;

    public bool canCounterAttack
    {
        get {  return _canCounterAttack; }
        set { _canCounterAttack = value; }
    }
    public float maxHealth
    {
        get { return _maxHealth; }
        set { _maxHealth = value; }
    }
    public float currentHealth
    {
        get { return _currentHealth; }
        set { _currentHealth = value; }
    }
    public bool isInvincible
    {
        get { return _isInvincible; }
        set { _isInvincible = value; }
    }

    // 추가
    public bool isAttacking
    {
        get { return _isAttacking; }
        set { _isAttacking = value; }
    }

    public bool isDefending
    {
        get { return _isDefending; }
        set { _isDefending = value; }
    }

    public bool isEvading
    {
        get { return _isEvading; }
        set { _isEvading = value; }
    }
    
    public bool isGetAttacked
    {
        get { return _isGetAttacked; }
        set { _isGetAttacked = value; }
    }

    public bool isDead // [추가]
    {
        get { return _isDead; }
        set { _isDead = value; }
    }

    // 적 정보
    private Transform _enemyTransform; // 적의 Transform
    private float _enemyDistance;      // 적과의 거리
    private float _enemyHealth;        // 적의 체력 (알 수 있다고 가정)

    public Transform enemyTransform
    {
        get { return _enemyTransform; }
        set { _enemyTransform = value; }
    }
    public float enemyDistance
    {
        get { return _enemyDistance; }
        set { _enemyDistance = value; }
    }
    public float enemyHealth
    {
        get { return _enemyHealth; }
        set { _enemyHealth = value; }
    }

    // 쿨타임 (행동 이름, 종료 시간)
    private Dictionary<string, float> actionCooldowns = new Dictionary<string, float>();
    private const string _ATTACK_COOLDOWN_KEY = "Attack"; // 공격 쿨타임 키
    private const string _DEFEND_COOLDOWN_KEY = "Defend"; // 방어 쿨타임 키
    private const string _EVADE_COOLDOWN_KEY = "Evade";   // 회피 쿨타임 키

    public static string ATTACK_COOLDOWN_KEY 
    {
        get {  return _ATTACK_COOLDOWN_KEY; }
    }// 공격 쿨타임 키
    public static string DEFEND_COOLDOWN_KEY
    {
        get { return _DEFEND_COOLDOWN_KEY; }
    }// 공격 쿨타임 키
    public static string EVADE_COOLDOWN_KEY
    {
        get { return _EVADE_COOLDOWN_KEY; }
    }// 공격 쿨타임 키

    private float _attackCooldownDuration = 2.5f; // 공격 쿨타임 지속 시간
    private float _defendCooldownDuration = 2.5f; // 방어 쿨타임 지속 시간
    private float _evadeCooldownDuration = 5.0f;  // 회피 쿨타임 지속 시간

    public float attackCooldownDuration
    {
        get { return _attackCooldownDuration; }
    }
    public float defendCooldownDuration
    {
        get { return _defendCooldownDuration; }
    }
    public float evadeCooldownDuration
    {
        get { return _evadeCooldownDuration; }
    }


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
        float duration = actionKey switch
        {
            _ATTACK_COOLDOWN_KEY => attackCooldownDuration,
            _DEFEND_COOLDOWN_KEY => defendCooldownDuration,
            _EVADE_COOLDOWN_KEY => evadeCooldownDuration,
            _ => 0f
        };
        actionCooldowns[actionKey] = Time.time + duration;
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

    public void StartEvading(float duration)
    {
        isEvading = true;
    }
    public void EndEnvaing()
    {
        isEvading = false;
    }
}
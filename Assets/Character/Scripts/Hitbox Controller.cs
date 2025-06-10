using UnityEngine;
using UnityEngine.Rendering;

public class HitboxController : MonoBehaviour
{
    private Collider m_collider;
    private bool _isGetAttack = false;
    private bool _isBlocked = false;
    private AgentBlackboard agentBlackboard;
    public bool isGetAttack
    {
        get { return _isGetAttack; }
    }
    public bool isBlocked
    {
        get { return _isBlocked; }
    }

    private float invincibilityDuration = 1.0f;
    private float invincibilityStartTime;

    private float blockCoolTime = 1.0f;
    private float blockCoolStartTime;
    //private CapsuleCollider capsuleCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int thisLayer = gameObject.layer;
        if (thisLayer == LayerMask.NameToLayer("OffensiverSword") ||
            thisLayer == LayerMask.NameToLayer("DefensiverShield") ||
            thisLayer == LayerMask.NameToLayer("DefensiverSword"))
        {
            agentBlackboard = transform.root.GetComponent<AgentController>().blackboard;
        }
        else
        {
            agentBlackboard = GetComponent<AgentController>().blackboard;
        }
        m_collider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        int thisLayer = gameObject.layer;
        int otherLayer = other.gameObject.layer;

        if ((thisLayer == LayerMask.NameToLayer("OffensiverBody") && otherLayer == LayerMask.NameToLayer("DefensiverSword")) ||
        (thisLayer == LayerMask.NameToLayer("DefensiverBody") && otherLayer == LayerMask.NameToLayer("OffensiverSword")))
        {
            _isGetAttack = true;
            //체력도 표시하게 수정.
            Debug.Log($"[피격] {gameObject.name} 이(가) {other.gameObject.name} 에게 맞았습니다. 남은 체력: {agentBlackboard.currentHealth}");
        }

        // 방어 성공 시 _isBlocked를 true로 설정하고, canCounterAttack도 true로 설정
        if (thisLayer == LayerMask.NameToLayer("DefensiverShield") &&
            otherLayer == LayerMask.NameToLayer("OffensiverSword"))
        {
            _isBlocked = true;
            agentBlackboard.canCounterAttack = true; // 여기서만 true로 설정
            Debug.Log($"[막힘] {gameObject.name} 의 공격이 {other.gameObject.name} 에 막혔습니다. 카운터 어택 가능!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        agentBlackboard.isGetAttacked = _isGetAttack;
        //agentBlackboard.canCounterAttack = _isBlocked; // 이 줄을 원래대로 복원

        if (_isGetAttack)
        {
            invincibilityStartTime = Time.time;
        }

        if (_isBlocked)
        {
            blockCoolStartTime = Time.time;
        }

        if (Time.time - invincibilityStartTime > invincibilityDuration && invincibilityStartTime != 0)
        {
            _isGetAttack = false;
            invincibilityStartTime = 0;
        }

        if (Time.time - blockCoolStartTime > blockCoolTime && blockCoolStartTime != 0)
        {
            _isBlocked = false;
            blockCoolStartTime = 0;
        }
    }
}
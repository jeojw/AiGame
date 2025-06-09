using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class HitboxController : MonoBehaviour
{
    public event Action OnHitReceived;
    public event Action OnBlockReceived;

    private Collider m_collider;
    private bool _isGetAttack = false;
    private bool _isBlocked = false;
    private AgentBlackboard agentBlackboard;
    public bool isGetAttack
    {
        get { return _isGetAttack; }
        set { _isGetAttack = value; }
    }
    public bool isBlocked
    {
        get { return _isBlocked; }
        set { _isBlocked = value; }
    }

    private int thisLayer;
   //private CapsuleCollider capsuleCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisLayer = gameObject.layer;
        if (thisLayer == LayerMask.NameToLayer("OffensiverBody") ||
            thisLayer == LayerMask.NameToLayer("DefensiverBody"))
        {
            agentBlackboard = GetComponent<AgentController>().blackboard;
        }
        else
        {
            agentBlackboard = transform.root.GetComponent<AgentController>().blackboard;
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
            agentBlackboard.isGetAttacked = true;
            Debug.Log($"[피격] {gameObject.name}이(가) {other.gameObject.name}에게 맞음");

            OnHitReceived?.Invoke();
        }

        if (thisLayer == LayerMask.NameToLayer("DefensiverShield") &&
            otherLayer == LayerMask.NameToLayer("OffensiverSword"))
        {
            _isBlocked = true;
            agentBlackboard.canCounterAttack = true;

            Debug.Log($"[막힘] {gameObject.name} 의 공격이 {other.gameObject.name} 에 막혔습니다.");

            OnBlockReceived?.Invoke();

        }
    }

    public void ResetHitFlag()
    {
        _isGetAttack = false;
    }

    public void ResetBlockFlag()
    {
        _isBlocked = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (thisLayer == LayerMask.NameToLayer("DefensiverSword") ||
            thisLayer == LayerMask.NameToLayer("OffensiverSword"))
        {
            m_collider.enabled = agentBlackboard.isAttacking;
        }
        if (thisLayer == LayerMask.NameToLayer("DefensiverShield"))
        {
            if (agentBlackboard.owner.gameObject.CompareTag("Defensiver"))
            {
                m_collider.enabled = agentBlackboard.isDefending;
            }
                
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;

public class HitboxController : MonoBehaviour
{
    private Collider m_collider;
    private bool _isGetAttack = false;
    private bool _isBlocked = false;
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
   //private CapsuleCollider capsuleCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
            Debug.Log($"[피격] {gameObject.name} 이(가) {other.gameObject.name} 에게 맞았습니다.");
        }



        if (thisLayer == LayerMask.NameToLayer("OffensiverSword") &&
            otherLayer == LayerMask.NameToLayer("DefensiverShield"))
        {
            _isBlocked = true;
            Debug.Log($"[막힘] {gameObject.name} 의 공격이 {other.gameObject.name} 에 막혔습니다.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_isGetAttack)
        {
            invincibilityStartTime = Time.time;
        }

        if (Time.time - invincibilityStartTime > invincibilityDuration)
            _isGetAttack = false;
    }
}

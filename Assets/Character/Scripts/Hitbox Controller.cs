using UnityEngine;

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

    private void OnCollisionEnter(Collision collision)
    {
        int thisLayer = this.gameObject.layer;
        int otherLayer = collision.gameObject.layer;

        _isGetAttack =
            (thisLayer == LayerMask.NameToLayer("OffensiverBody") && otherLayer == LayerMask.NameToLayer("DefensiverSword")) ||
            (thisLayer == LayerMask.NameToLayer("DefensiverBody") && otherLayer == LayerMask.NameToLayer("OffensiverSword"));

        _isBlocked =
            thisLayer == LayerMask.NameToLayer("OffensiverSword") && otherLayer == LayerMask.NameToLayer("DefensiverShield");

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

        m_collider.enabled = !_isGetAttack;

        Debug.Log(_isGetAttack);
    }
}

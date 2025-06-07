// Scripts/Hitbox Controller.cs

using UnityEngine;
using System.Collections.Generic;

public class HitboxController : MonoBehaviour
{
    private Collider m_collider;
    private AgentController myController;
    private AgentBlackboard agentBlackboard;

    private List<Collider> alreadyHit = new List<Collider>();

    private bool _isGetAttackTriggered = false;
    private float invincibilityDuration = 0.5f;
    private float invincibilityEndTime;

    private bool _isBlockedTriggered = false;
    private float blockCoolTime = 0.5f;
    private float blockEndTime;

    private bool wasAttackingLastFrame = false;


    void Start()
    {
        myController = transform.root.GetComponent<AgentController>();
        if (myController == null)
        {
            Debug.LogError("HitboxController: AgentController를 찾을 수 없습니다. 스크립트가 올바른 계층 구조에 있는지 확인하세요.", this);
            this.enabled = false;
            return;
        }
        agentBlackboard = myController.blackboard;
        m_collider = GetComponent<Collider>();

        if (m_collider != null && !m_collider.isTrigger)
        {
            Debug.LogWarning($"HitboxController: 콜라이더 {m_collider.name}의 isTrigger가 false입니다. OnTriggerEnter 이벤트가 발생하지 않을 수 있습니다.", m_collider);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (myController == null || agentBlackboard == null)
        {
            Debug.LogWarning("HitboxController: myController 또는 agentBlackboard가 초기화되지 않았습니다.");
            return;
        }

        AgentController otherController = other.GetComponentInParent<AgentController>();

        // 1. 자기 자신을 때리는 경우 무시 (가장 우선 순위)
        if (otherController == null || otherController == myController)
        {
            return;
        }

        Debug.Log($"[OnTriggerEnter] 충돌 발생! 내 오브젝트: {gameObject.name} (태그: {gameObject.tag}), 충돌한 오브젝트: {other.gameObject.name} (태그: {other.gameObject.tag})");

        // --- A: 내가 '공격자'인 경우의 로직 (데미지 적용 담당) ---
        // 내가 'Sword' 태그를 가지고 있고, 현재 공격 중이며, 상대방이 'Sword' 태그가 아닌 경우
        if (gameObject.CompareTag("Sword") && agentBlackboard.isAttacking && !other.CompareTag("Sword"))
        {
            // [중복 피격 방지] 이미 이번 공격에서 맞췄던 대상이면 무시
            if (alreadyHit.Contains(other))
            {
                Debug.Log($"[공격자] {other.gameObject.name} (중복 피격 방지 - 이미 맞춘 대상)", other.gameObject);
                return; // 이미 때린 대상이면 더 이상 처리하지 않음
            }

            // --- 칩 데미지 / 일반 데미지 분기 ---
            // 상대방이 방어 중인지 확인 (상대방이 'Shield' 태그이고, 방어 중일 때)
            if (other.CompareTag("Shield") && otherController.blackboard.isDefending)
            {
                // **방어 성공 시 칩 데미지는 여기서 공격자가 적용합니다.**
                float attackDamage = 10f;
                float finalDamage = attackDamage * agentBlackboard.currentAttackMultiplier;
                float chipDamage = finalDamage * 0.3f; // 칩 데미지 계산

                Debug.Log($"[공격자] {otherController.name}이(가) 방어를 막았지만, 칩 데미지 {chipDamage:F2}를 입었습니다!");
                otherController.HandleDamage(chipDamage); // <--- 방어자에게 칩 데미지 적용!

                otherController.blackboard.canCounterAttack = true; // 방어자에게 반격 가능 플래그 설정
                alreadyHit.Add(other); // 이번 공격에서 이 대상을 때린 것으로 기록
            }
            else
            {
                // 일반 데미지 처리 (상대방이 방어 중이 아니거나 방패가 아닌 부위에 맞았을 때)
                float attackDamage = 10f;
                float finalDamage = attackDamage * agentBlackboard.currentAttackMultiplier;
                Debug.Log($"[공격자] {myController.name}이(가) {otherController.name}에게 데미지 {finalDamage:F2} 적용!");
                otherController.HandleDamage(finalDamage); // <--- 피격자에게 일반 데미지 적용!

                otherController.blackboard.isGetAttacked = true; // 피격자에게 피격 플래그 설정
                alreadyHit.Add(other); // 이번 공격에서 이 대상을 때린 것으로 기록
            }
        }

        // --- B: 내가 '피격자' 또는 '방어자'인 경우의 로직 (상태 플래그 설정만 담당) ---

        // 2. 내가 방패이고, 상대방이 공격 중인 무기에 맞았을 때 (방어 성공 감지)
        // 이 로직은 '방어하는 나'의 입장에서 자신의 방어 성공 상태 플래그를 설정합니다.
        if (gameObject.CompareTag("Shield") && agentBlackboard.isDefending && other.CompareTag("Sword") && otherController.blackboard.isAttacking)
        {
            // 방어에 성공했다는 로컬 플래그 설정 (Update에서 Blackboard로 전달)
            if (!_isBlockedTriggered || Time.time >= blockEndTime)
            {
                _isBlockedTriggered = true;
                blockEndTime = Time.time + blockCoolTime;
                Debug.Log($"[{myController.name} - 방어자] 방어 성공 감지! _isBlockedTriggered = true.");
            }
            else
            {
                Debug.Log($"[{myController.name} - 방어자] 방어 성공 감지! 하지만 이미 블록 쿨타임 중이라 무시됨.");
            }
            // **여기서는 절대로 데미지를 직접 적용하지 않습니다. 칩 데미지는 공격자 로직에서 이미 처리했습니다.**
        }

        // 3. 내가 무기가 아니고, 상대방이 공격 중인 무기에 맞았을 때 (피격 감지)
        // 이 로직은 '맞는 나'의 입장에서 자신의 피격 상태 플래그를 설정하는 부분입니다.
        // 내가 Shield 태그이거나, 내가 방어 중일 때는 피격으로 간주하지 않습니다. (방어 성공 로직과 분리)
        if (!gameObject.CompareTag("Sword") && !gameObject.CompareTag("Shield") && other.CompareTag("Sword") && otherController.blackboard.isAttacking)
        {
            // 내가 현재 방어 중이 아닌 경우에만 피격 감지 및 플래그 설정
            // HandleDamage는 공격자가 이미 호출했습니다. 여기서는 피격 상태만 기록합니다.
            if (!agentBlackboard.isDefending)
            {
                // 중복 피격 무적 시간을 고려하여 플래그 설정
                if (!_isGetAttackTriggered || Time.time >= invincibilityEndTime)
                {
                    _isGetAttackTriggered = true;
                    invincibilityEndTime = Time.time + invincibilityDuration;
                    Debug.Log($"[{myController.name} - 피격자] 피격 감지! _isGetAttackTriggered = true.");
                }
                else
                {
                    Debug.Log($"[{myController.name} - 피격자] 피격 감지! 하지만 이미 무적 상태라 추가 피격 무시됨.");
                }
            }
            else
            {
                Debug.Log($"[{myController.name} - 피격자] 피격 감지! 하지만 방어 중이라 피격으로 간주하지 않음.");
            }
        }
    }

    void Update()
    {
        if (agentBlackboard == null) return;

        // Blackboard의 isGetAttacked 플래그 업데이트 (피격자 자신)
        agentBlackboard.isGetAttacked = _isGetAttackTriggered && (Time.time < invincibilityEndTime);

        // Blackboard의 canCounterAttack 플래그 업데이트 (방어자 자신)
        agentBlackboard.canCounterAttack = _isBlockedTriggered && (Time.time < blockEndTime);

        // --- agentBlackboard.isAttacking 상태 변화 감지 및 alreadyHit 리스트 초기화 ---
        // Blackboard의 isAttacking 플래그가 false -> true로 변했을 때 (공격 시작 시점)
        if (agentBlackboard.isAttacking && !wasAttackingLastFrame)
        {
            alreadyHit.Clear(); // 새로운 공격 사이클 시작 시 중복 피격 리스트 초기화
            Debug.Log($"[{myController.name}] 공격 시작 감지. alreadyHit 리스트 초기화.");
        }
        wasAttackingLastFrame = agentBlackboard.isAttacking; // 다음 프레임을 위해 현재 상태 저장


        // --- 로컬 플래그 (_isGetAttackTriggered, _isBlockedTriggered) 리셋 로직 ---
        // Blackboard에 플래그가 반영된 후, 일정 시간이 지나면 로컬 플래그를 false로 리셋하여
        // 다음 충돌 이벤트를 다시 받을 준비를 합니다.
        if (_isGetAttackTriggered && Time.time >= invincibilityEndTime)
        {
            _isGetAttackTriggered = false;
            invincibilityEndTime = 0;
            Debug.Log($"[{myController.name}] 피격 플래그 리셋.");
        }

        if (_isBlockedTriggered && Time.time >= blockEndTime)
        {
            _isBlockedTriggered = false;
            blockEndTime = 0;
            Debug.Log($"[{myController.name}] 방어 성공 플래그 리셋.");
        }
    }
}
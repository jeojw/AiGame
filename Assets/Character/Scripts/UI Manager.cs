// File: UI Manager.cs
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider OffensiverHP;
    [SerializeField] private Slider DefensiverHP;
    [SerializeField] private TextMeshProUGUI TimeText;

    [SerializeField] private GameObject Offensiver;
    [SerializeField] private GameObject Defensiver;

    private AgentBlackboard OffensiverStat;
    private AgentBlackboard DefensiverStat;

    private AgentController offensiverController;
    private AgentController defensiverController;


    private bool hasGameEndedAndSaved = false;

    void Start()
    {
        offensiverController = Offensiver.GetComponent<OffensiveAgentController>();
        OffensiverStat = offensiverController.blackboard;
        OffensiverStat.isDead = false;

        defensiverController = Defensiver.GetComponent<DefensiveAgentController>();
        DefensiverStat = defensiverController.blackboard;
        DefensiverStat.isDead = false;
    }

    void Update()
    {
        OffensiverHP.value = (OffensiverStat.currentHealth / OffensiverStat.maxHealth);
        DefensiverHP.value = (DefensiverStat.currentHealth / DefensiverStat.maxHealth);

        // 게임 종료 조건 확인 (에이전트가 죽었는지)
        if (OffensiverStat.isDead || DefensiverStat.isDead)
        {
            if (!hasGameEndedAndSaved)
            {
                // 게임 데이터 저장 로직 (기존과 동일)
                GameData data = new GameData();
                data.TimeText = TimeText.text;
                data.OffensiverHp = OffensiverHP.value;
                data.DefensiverHp = DefensiverHP.value;
                data.DefensvierScore = DefensiverStat.score;
                data.OffensiverScore = OffensiverStat.score;

                // 누가 이겼는지 판별하여 GameResult 설정 (영어로 변경)
                if (OffensiverStat.isDead && !DefensiverStat.isDead)
                {
                    data.GameResult = "Defender Wins"; // [수정] 방어자 승리 -> Defender Wins
                    data.DefensvierScore += data.DefensiverHp * 100f;
                    data.DefensvierScore += 100f;
                }
                else if (!OffensiverStat.isDead && DefensiverStat.isDead)
                {
                    data.GameResult = "Attacker Wins"; // [수정] 공격자 승리 -> Attacker Wins
                    data.OffensiverScore += data.OffensiverHp * 100f;
                    data.OffensiverScore += 100f;
                }
                else
                {
                    data.GameResult = "Draw"; // [수정] 무승부 -> Draw
                }

                string jsonString = JsonUtility.ToJson(data, true);
                string filePath = Path.Combine(Application.dataPath, "game_save.json");

                try
                {
                    File.WriteAllText(filePath, jsonString);
                    Debug.Log($"<color=green>게임 데이터가 성공적으로 저장되었습니다:</color> {filePath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"<color=red>게임 데이터 저장 중 오류 발생:</color> {e.Message}");
                }

                hasGameEndedAndSaved = true;

                Debug.Log("게임 종료! 에이전트가 죽은 상태로 유지됩니다.");

                // TimeText에 게임 결과 표시
                TimeText.text = data.GameResult;

                // 게임 정지 로직
                Time.timeScale = 0f;
            }
        }
        else // 게임이 진행 중일 때만 시간 업데이트
        {
            TimeText.text = Time.time.ToString("N1");
            hasGameEndedAndSaved = false;
        }
    }

    // InitializeAgent 메서드는 기존과 동일하게 유지 (여기서는 BT 에이전트를 위한 재사용)
    private void InitializeAgent(AgentController agentController, GameObject agentObject, Vector3 initialPosition)
    {
        if (agentController != null)
        {
            agentObject.transform.position = initialPosition + Vector3.up * 0.1f;
            agentObject.transform.rotation = Quaternion.identity;

            Rigidbody rb = agentObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Blackboard 상태 초기화
            agentController.blackboard.currentHealth = agentController.blackboard.maxHealth;
            agentController.blackboard.isDead = false;
            agentController.blackboard.isAttacking = false;
            agentController.blackboard.isDefending = false;
            agentController.blackboard.isEvading = false;
            agentController.blackboard.isInvincible = false;
            agentController.blackboard.isGetAttacked = false;
            agentController.blackboard.canCounterAttack = false;
            agentController.blackboard.canBeDefended = true;
            agentController.blackboard.recentlyDefended = false;
            agentController.blackboard.lastEnemyAttackTime = 0f;
            agentController.blackboard.score = 0;
            agentController.blackboard.attackCount = 0;
            agentController.blackboard.defendCount = 0;
            agentController.blackboard.counterAttackCount = 0;
            agentController.blackboard.evadeCount = 0;

            agentController.enabled = true;
            agentController.ResetAllFlags();
            Debug.Log($"{agentObject.name}이(가) 초기화되고 재활성화되었습니다.");
        }
    }

    public GameData LoadGameData()
    {
        string filePath = Path.Combine(Application.dataPath, "game_save.json");

        if (File.Exists(filePath))
        {
            try
            {
                string jsonFromFile = File.ReadAllText(filePath);
                GameData loadedData = JsonUtility.FromJson<GameData>(jsonFromFile);
                Debug.Log($"<color=green>게임 데이터가 성공적으로 로드되었습니다.</color>");
                return loadedData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<color=red>게임 데이터 로드 중 오류 발생:</color> {e.Message}");
                return null;
            }
        }
        else
        {
            Debug.LogWarning($"<color=orange>저장된 게임 파일이 없습니다:</color> {filePath}");
            return null;
        }
    }
}
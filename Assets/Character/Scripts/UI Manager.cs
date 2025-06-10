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
                if (OffensiverStat.isDead && !DefensiverStat.isDead)
                {
                    data.GameResult = "방어자 승리";
                    data.DefensvierScore += data.DefensiverHp * 100f;
                    data.DefensvierScore += 100f;
                }
                else if (!OffensiverStat.isDead && DefensiverStat.isDead)
                {
                    data.GameResult = "공격자 승리";
                    data.OffensiverScore += data.OffensiverHp * 100f;
                    data.OffensiverScore += 100f;
                }
                else
                {
                    data.GameResult = "무승부";
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

                // --- [수정] ML-Agents 학습 환경에 맞게 에이전트 초기화 ---
                // 여기에 바로 초기화 로직을 두는 대신, 에이전트의 EndEpisode() (RL 에이전트의 경우)
                // 또는 외부 리셋 메커니즘에 의존해야 합니다.
                // BT 에이전트의 경우, 직접 EndEpisode을 호출하는 것이 아니라,
                // RL 에이전트가 EndEpisode을 호출할 때 해당 에이전트도 함께 초기화되도록 연결해야 합니다.

                // 임시적으로 이 곳에서 BT 에이전트를 초기화하지만,
                // ML-Agents 환경에서는 에피소드 종료 시 모든 에이전트가 재시작되는 것이 더 적합합니다.
                Debug.Log("게임 종료! 에이전트를 초기화합니다. (Time.timeScale 조정 안 함)");

                // BT Offensive 에이전트 초기화
                //InitializeAgent(offensiverController, Offensiver, new Vector3(0, 0, -5));
                // RL Defensive 에이전트는 EndEpisode()이 호출되면 자동으로 재시작되므로
                // 여기서 별도로 InitializeAgent를 호출하지 않습니다.
                // 만약 RL Defensive 에이전트도 여기서 초기화해야 한다면,
                // RLDefensiveAgent.cs에서 EndEpisode() 대신 이 UIManager 로직을 따르도록 해야 합니다.

                // Time.timeScale = 0f; // ★★★ 이 줄을 제거하거나 주석 처리합니다! ★★★
            }
            // 게임이 종료된 후에는 UI 업데이트(시간 텍스트)를 멈춥니다.
            TimeText.text = "Game Over";
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
            agentObject.transform.position = initialPosition + Vector3.up * 0.1f; // 0.1f는 예시, 콜라이더 크기에 맞게 조정
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

            // AgentController 스크립트 활성화
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
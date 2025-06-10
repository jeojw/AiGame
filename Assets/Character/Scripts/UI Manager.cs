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

    private AgentController offensiverController; // Offensiver의 AgentController 참조
    private AgentController defensiverController; // Defensiver의 AgentController 참조


    private bool hasGameEndedAndSaved = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Offensive Agent의 AgentController를 가져옵니다.
        offensiverController = Offensiver.GetComponent<OffensiveAgentController>();
        OffensiverStat = offensiverController.blackboard;
        OffensiverStat.isDead = false;

        // Defensive Agent의 AgentController를 가져옵니다.
        defensiverController = Defensiver.GetComponent<DefensiveAgentController>();
        DefensiverStat = defensiverController.blackboard;
        DefensiverStat.isDead = false;
    }

    // Update is called once per frame
    void Update()
    {
        OffensiverHP.value = (OffensiverStat.currentHealth / OffensiverStat.maxHealth);
        DefensiverHP.value = (DefensiverStat.currentHealth / DefensiverStat.maxHealth);

        if (!(OffensiverStat.isDead || DefensiverStat.isDead))
        {
            // 게임이 끝나지 않았다면 시간 업데이트
            TimeText.text = Time.time.ToString("N1");
            hasGameEndedAndSaved = false; // 게임이 다시 시작되거나 진행 중일 때 초기화
        }
        else // 게임이 종료되었다면 (누군가 죽었다면)
        {
            // 게임 종료 후 저장이 아직 되지 않았다면
            if (!hasGameEndedAndSaved)
            {
                // 1. 저장할 GameData 객체 생성 및 데이터 할당
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

                // 2. GameData 객체를 JSON 문자열로 변환
                string jsonString = JsonUtility.ToJson(data, true);

                // 3. 데이터를 저장할 파일 경로 설정
                string filePath = Path.Combine(Application.dataPath, "game_save.json");

                // 4. JSON 문자열을 파일에 쓰기 (기존 파일이 있다면 덮어씁니다)
                try
                {
                    File.WriteAllText(filePath, jsonString);
                    Debug.Log($"<color=green>게임 데이터가 성공적으로 저장되었습니다:</color> {filePath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"<color=red>게임 데이터 저장 중 오류 발생:</color> {e.Message}");
                }

                // 저장이 완료되었음을 표시하여 이 블록이 다시 실행되지 않도록 합니다.
                hasGameEndedAndSaved = true;

                // 에이전트 초기화 및 재활성화 로직
                Debug.Log("게임 종료! 에이전트를 초기화합니다.");

                // Offensive Agent 초기화 (공격자가 죽었을 때 움직이도록)
                // Offensive Agent의 초기 위치를 정확히 지정해야 합니다. (예: new Vector3(0, 0, -5))
                InitializeAgent(offensiverController, Offensiver, new Vector3(0, 0, -5));

                // Defensive Agent 초기화 (필요하다면, 방어자가 죽었을 때도 초기화)
                // Defensive Agent의 초기 위치를 정확히 지정해야 합니다. (예: new Vector3(0, 0, 5))
                InitializeAgent(defensiverController, Defensiver, new Vector3(0, 0, 5));

                Time.timeScale = 0f; // 게임 일시 정지 (선택 사항이며, 수동 재시작을 위해 유용)
            }
        }
    }

    // 에이전트 초기화 헬퍼 메서드
    private void InitializeAgent(AgentController agentController, GameObject agentObject, Vector3 initialPosition)
    {
        if (agentController != null)
        {
            // 에이전트의 위치와 회전을 초기화합니다.
            agentObject.transform.position = initialPosition;
            agentObject.transform.rotation = Quaternion.identity; // 기본 회전 (변경 가능)

            // Rigidbody가 있다면 속도를 리셋합니다.
            Rigidbody rb = agentObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 블랙보드 상태 초기화
            agentController.blackboard.currentHealth = agentController.blackboard.maxHealth;
            agentController.blackboard.isDead = false;
            agentController.blackboard.isAttacking = false;
            agentController.blackboard.isDefending = false;
            agentController.blackboard.isEvading = false;
            agentController.blackboard.isInvincible = false;
            agentController.blackboard.isGetAttacked = false;
            agentController.blackboard.canCounterAttack = false;
            agentController.blackboard.recentlyDefended = false;
            agentController.blackboard.lastEnemyAttackTime = 0f;
            agentController.blackboard.score = 0; // 점수도 초기화 (필요시)
            agentController.blackboard.attackCount = 0; // 스탯 초기화 (필요시)
            agentController.blackboard.defendCount = 0;
            agentController.blackboard.counterAttackCount = 0;
            agentController.blackboard.evadeCount = 0;

            // AgentController 스크립트 활성화
            agentController.enabled = true;
            agentController.ResetAllFlags(); // AgentController 내부 플래그도 리셋
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
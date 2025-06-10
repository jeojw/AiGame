using System.Collections.Generic;
using System.ComponentModel;
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

    private bool hasGameEndedAndSaved = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OffensiverStat = Offensiver.GetComponent<OffensiveAgentController>().blackboard;
        OffensiverStat.isDead = false;
        DefensiverStat = Defensiver.GetComponent<DefensiveAgentController>().blackboard;
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
                // true는 가독성 좋게 포맷팅 (들여쓰기) 해줍니다.
                string jsonString = JsonUtility.ToJson(data, true);

                // 3. 데이터를 저장할 파일 경로 설정
                // Path.Combine을 사용하여 운영체제에 맞는 경로 구분자를 자동으로 처리합니다.
                // Application.persistentDataPath는 앱이 제거되기 전까지 유지되는 안전한 저장 공간입니다.
                string filePath = Path.Combine(Application.dataPath, "game_save.json"); // 파일 확장자 .json 추가

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

                TimeText.text = 0.ToString();

                // 저장이 완료되었음을 표시하여 이 블록이 다시 실행되지 않도록 합니다.
                hasGameEndedAndSaved = true;

                // 이 스크립트의 Update() 함수가 더 이상 호출되지 않도록 비활성화
                // (선택 사항: 게임 오버 UI를 띄우거나 씬을 전환하는 등의 후속 처리를 할 수 있습니다.)
            }
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

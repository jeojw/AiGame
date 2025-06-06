using TMPro;
using UnityEngine;
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OffensiverStat = Offensiver.GetComponent<OffensiveAgentController>().blackboard;
        DefensiverStat = Defensiver.GetComponent<DefensiveAgentController>().blackboard;
    }

    // Update is called once per frame
    void Update()
    {
        OffensiverHP.value = (OffensiverStat.currentHealth / OffensiverStat.maxHealth);
        DefensiverHP.value = (DefensiverStat.currentHealth / DefensiverStat.maxHealth);
        TimeText.text = Time.time.ToString("N1");   
    }
}

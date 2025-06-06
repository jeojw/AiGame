using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 directionalVector;
    private Vector3 normalVector;
    private Vector3 targetPos;
    private Quaternion targetRotation;

    [SerializeField] private Transform Offensiver;
    [SerializeField] private Transform Defensiver;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // --- [수정 시작] ---
        // Offensiver 또는 Defensiver 오브젝트가 파괴되었는지 먼저 확인합니다.
        // 파괴된 Unity 오브젝트는 '==' 비교 시 null처럼 동작합니다.
        if (Offensiver == null || Defensiver == null)
        {
            // 둘 중 하나라도 파괴되었다면, 더 이상 카메라를 업데이트하지 않고 메소드를 종료합니다.
            return;
        }
        // --- [수정 끝] ---

        targetPos = (Offensiver.position + Defensiver.position) / 2;
        directionalVector = Offensiver.position - Defensiver.position;
        normalVector = Vector3.Cross(directionalVector.normalized, Vector3.up).normalized;

        mainCamera.transform.position = targetPos + normalVector * 7f + new Vector3(0, 2f, 0);
        mainCamera.transform.LookAt(targetPos);
    }
}

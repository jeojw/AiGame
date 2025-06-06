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
        targetPos = (Offensiver.position + Defensiver.position) / 2;
        directionalVector = Offensiver.position - Defensiver.position;
        normalVector = Vector3.Cross(directionalVector.normalized, Vector3.up).normalized;

        mainCamera.transform.position = targetPos + normalVector * 7f + new Vector3(0, 2f, 0);
        mainCamera.transform.LookAt(targetPos);
    }
}

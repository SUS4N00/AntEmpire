using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform targetGroupTransform;
    [SerializeField] private GameObject cameraGame;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 10f;

    private Vector2 moveDirection;
    private CinemachineCamera cinemachineCamera;
    private Camera mainCamera;
    private Vector3 velocity = Vector3.zero; // Thêm biến velocity cho SmoothDamp

    void Start()
    {
        cinemachineCamera = cameraGame.GetComponent<CinemachineCamera>();
        if (cinemachineCamera == null)
        {
            Debug.LogError("CinemachineCamera component is missing on cameraGame.");
        }
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null) return;

        HandleMouseEdgeMovement();
    }
    void LateUpdate()
    {
        if (Time.timeScale == 0f) return;
        HandleCameraSwitch();
        HandleZoom();

        Vector2 moveDirection = Vector2.zero;
        // Xác định hướng di chuyển dựa trên input hoặc vị trí chuột sát viền
        // Chỉ di chuyển khi chuột ở sát viền (1 pixel)
        if (Input.mousePosition.x <= 1) moveDirection.x = -1;
        else if (Input.mousePosition.x >= Screen.width - 1) moveDirection.x = 1;

        if (Input.mousePosition.y <= 1) moveDirection.y = -1;
        else if (Input.mousePosition.y >= Screen.height - 1) moveDirection.y = 1;

        if (moveDirection != Vector2.zero)
        {
            Vector3 targetPos = transform.position + new Vector3(moveDirection.x, moveDirection.y, 0f) * moveSpeed;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.1f); // 0.1f là hệ số mượt
        }
    }

    private void HandleMouseEdgeMovement()
    {
        moveDirection = Vector2.zero;
        Vector2 mousePos = Input.mousePosition;
        float width = Screen.width;
        float height = Screen.height;

        // Chỉ di chuyển khi chuột ở sát viền (1 pixel)
        if (mousePos.x <= 1) moveDirection.x = -1;
        else if (mousePos.x >= width - 1) moveDirection.x = 1;

        if (mousePos.y <= 1) moveDirection.y = -1;
        else if (mousePos.y >= height - 1) moveDirection.y = 1;
    }

    private void HandleCameraSwitch()
    {
        if (Input.GetKeyUp(KeyCode.Space)) SetCameraOnMouse();

        if (Input.GetKeyDown(KeyCode.Space) &&
            targetGroupTransform.GetComponent<CinemachineTargetGroup>().Targets.Count > 0)
        {
            SetCameraOnTarget();
        }
    }

    private void SetCameraOnMouse()
    {
        if (cinemachineCamera != null) cinemachineCamera.Follow = null;
    }

    private void SetCameraOnTarget()
    {
        if (cinemachineCamera != null && targetGroupTransform != null)
        {
            cinemachineCamera.Follow = targetGroupTransform;
        }
    }

    private void HandleZoom()
    {
        if (Time.timeScale == 0f) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f && cinemachineCamera != null)
        {
            var lens = cinemachineCamera.Lens;
            if (lens.Orthographic)
            {
                lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
            }
            else
            {
                lens.FieldOfView = Mathf.Clamp(lens.FieldOfView - scroll * zoomSpeed, minZoom, maxZoom);
            }
            cinemachineCamera.Lens = lens;
        }
    }
}
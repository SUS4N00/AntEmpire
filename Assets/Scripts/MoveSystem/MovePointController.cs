using UnityEngine;
using System.Collections.Generic;
using Pathfinding;
using Unity.Cinemachine;

public class MovePointController : MonoBehaviour
{
    public static MovePointController instance;
    public float formationSpacing = 1.5f;
    public float selectRange;
    private float doubleClickThreshold = 0.3f;
    private List<Vector2> targetPositions = new List<Vector2>();
    private List<GameObject> selectedAnts = new List<GameObject>();
    [SerializeField] private RectTransform selectionBox;
    [SerializeField] private LayerMask antLayerMask;
    [SerializeField] private LayerMask interactiveObjectLayerMask;
    [SerializeField] private CinemachineTargetGroup targetGroup;
    
    private Vector2 startMousePos;
    private bool isBoxSelecting = false;
    private float lastClickTime = 0f;

    // Lưu group theo phím số 1-9
    private Dictionary<int, List<GameObject>> antGroups = new Dictionary<int, List<GameObject>>();

    void Awake() => instance = this;

    void Update()
    {
        HandleSelectionInput();
        HandleMovementInput();
        HandleGroupHotkeyInput();
    }

    void HandleSelectionInput()
    {
        // Bắt đầu bôi đen
        if (Input.GetMouseButtonDown(0))
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            startMousePos = Input.mousePosition;
            isBoxSelecting = true;
            selectionBox.gameObject.SetActive(true);

            // Kiểm tra click đơn
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider == null || ((1 << hit.collider.gameObject.layer) & antLayerMask) == 0)
            {
                ClearSelection();
            }
            else if (((1 << hit.collider.gameObject.layer) & antLayerMask) != 0)
            {
                GameObject AntTmp = hit.collider.gameObject;
                if (AntTmp.GetComponent<AntAI>().GetSelectedStatus() && timeSinceLastClick <= doubleClickThreshold)
                {   // nếu đã được chọn thì sẽ chọn những con kiến khác có cùng layer với nó trong khoảng cách
                    Collider2D[] nearbyAnts = Physics2D.OverlapCircleAll(
                        AntTmp.transform.position, // tâm hình tròn tìm kiếm
                        selectRange,                              // bán kính
                        1 << AntTmp.layer                    // lớp cần tìm
                    );
                    foreach (Collider2D antCollider in nearbyAnts)
                    {
                        if (((1 << antCollider.gameObject.layer) & antLayerMask) != 0 && !selectedAnts.Contains(antCollider.gameObject))
                        {
                            AddAntToSelection(antCollider.gameObject);
                        }
                    }
                }
                else
                {
                    ClearSelection();
                    AddAntToSelection(hit.collider.gameObject);
                }
            }
        }

        // Đang bôi đen
        if (isBoxSelecting && Input.GetMouseButton(0))
        {
            UpdateSelectionBox();
        }

        // Kết thúc bôi đen
        if (isBoxSelecting && Input.GetMouseButtonUp(0))
        {
            SelectAntsInBox();
            selectionBox.gameObject.SetActive(false);
            isBoxSelecting = false;
        }
        selectedAnts.RemoveAll(ant => ant == null);
    }

    // Chuot phai de nhan lenh di chuyen
    void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(1) && selectedAnts.Count > 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if(hit.collider == null || ((1 << hit.collider.gameObject.layer) & interactiveObjectLayerMask) == 0){
                Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                CreateFormation(clickPos, selectedAnts.Count);
                ResetAllStatusForSelectedAnt();
                MoveAntsToTargets();
            }else if(((1 << hit.collider.gameObject.layer) & interactiveObjectLayerMask) != 0){
                foreach (var ant in selectedAnts)
                {
                    
                    ant.GetComponent<AntAI>().SetTarget(hit.collider.gameObject);
                    ant.GetComponent<AntAI>().SetInteractStatus(true);
                }
            }else{
                ResetAllStatusForSelectedAnt();
            }
        }
    }

    void HandleGroupHotkeyInput()
    {
        // Chỉ xử lý khi có phím số hoặc shift được nhấn
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        for (int i = 1; i <= 9; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + (i - 1));
            if (shift && Input.GetKeyDown(key))
            {
                if (selectedAnts.Count > 0)
                {
                    antGroups[i] = new List<GameObject>(selectedAnts);
                    Debug.Log($"[MovePointController] Saved group {i} ({selectedAnts.Count} ants)");
                }
            }
            else if (!shift && Input.GetKeyDown(key))
            {
                if (antGroups.ContainsKey(i))
                {
                    ClearSelection();
                    antGroups[i].RemoveAll(a => a == null);
                    if (antGroups[i].Count == 0)
                    {
                        antGroups.Remove(i);
                        Debug.Log($"[MovePointController] Group {i} is empty and removed");
                    }
                    else
                    {
                        foreach (var ant in antGroups[i])
                        {
                            AddAntToSelection(ant);
                        }
                        Debug.Log($"[MovePointController] Loaded group {i} ({antGroups[i].Count} ants)");
                    }
                }
            }
        }
    }

    // tao doi hinh quanh trung tam
    void CreateFormation(Vector2 center, int unitCount)
    {
        targetPositions.Clear();
        int rows = Mathf.CeilToInt(Mathf.Sqrt(unitCount));

        for (int i = 0; i < unitCount; i++)
        {
            int row = i / rows;
            int col = i % rows;
            
            Vector2 offset = new Vector2(
                (col - rows / 2) * formationSpacing,
                (row - rows / 2) * formationSpacing
            );
            targetPositions.Add(center + offset);
        }
    }

    // Gan diem dich moi cho kien
    void MoveAntsToTargets()
    {
        for (int i = 0; i < selectedAnts.Count; i++)
        {
            if (i < targetPositions.Count)
            {
                GameObject ant = selectedAnts[i];
                if (ant != null)
                {
                    AIDestinationSetter antAI = ant.GetComponent<AIDestinationSetter>();
                    if (antAI != null)
                    {
                        if (antAI.target == null)
                        {
                            antAI.target = new GameObject("AntTarget_" + i).transform;
                            antAI.target.position = targetPositions[i];
                            antAI.target.tag = "Temp";
                        }
                        else if (antAI.target.tag == "Temp")
                        {
                            antAI.target.position = targetPositions[i];
                        }
                        else
                        {
                            antAI.target = new GameObject("AntTarget_" + i).transform;
                            antAI.target.position = targetPositions[i];
                            antAI.target.tag = "Temp";
                        }
                    }
                }
            }
        }
    }

    void UpdateSelectionBox()
    {
        Canvas canvas = selectionBox.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 startLocalPos, currentLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, startMousePos, uiCamera, out startLocalPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, uiCamera, out currentLocalPos);

        Vector2 size = currentLocalPos - startLocalPos;

        selectionBox.anchoredPosition = startLocalPos + new Vector2(
            (size.x < 0) ? size.x : 0,
            (size.y < 0) ? size.y : 0
        );
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    // Thêm kiến vào danh sách chọn
    void SelectAntsInBox()
    {
        // Lấy vị trí chuột bắt đầu và kết thúc theo tọa độ màn hình
        Vector2 minScreenPos = new Vector2(
            Mathf.Min(startMousePos.x, Input.mousePosition.x),
            Mathf.Min(startMousePos.y, Input.mousePosition.y)
        );
        Vector2 maxScreenPos = new Vector2(
            Mathf.Max(startMousePos.x, Input.mousePosition.x),
            Mathf.Max(startMousePos.y, Input.mousePosition.y)
        );

        // Chuyển đổi tọa độ màn hình sang tọa độ thế giới
        Vector2 worldMin = Camera.main.ScreenToWorldPoint(minScreenPos);
        Vector2 worldMax = Camera.main.ScreenToWorldPoint(maxScreenPos);

        // Kiểm tra va chạm trong vùng chọn
        Collider2D[] allAnts = Physics2D.OverlapAreaAll(worldMin, worldMax, antLayerMask);

        foreach (Collider2D antCollider in allAnts)
        {
            if (((1 << antCollider.gameObject.layer) & antLayerMask) != 0 && !selectedAnts.Contains(antCollider.gameObject))
            {
                AddAntToSelection(antCollider.gameObject);
            }
        }
    }

    void AddAntToSelection(GameObject ant)
    {
        var antAI = ant.GetComponent<AntAI>();
        if (antAI != null)
        {
            antAI.Select();
            selectedAnts.Add(ant);
        }
    }

    void ClearSelection()
    {
        foreach (GameObject ant in selectedAnts)
        {
            if (ant != null)
            {
                var antAI = ant.GetComponent<AntAI>();
                if (antAI != null) antAI.Deselect();
            }
        }
        selectedAnts.Clear();
    }

    void ResetAllStatusForSelectedAnt(){
        foreach (var ant in selectedAnts)
        {
            ant.GetComponent<AntAI>().ResetAllStatus();
        }
    }
}
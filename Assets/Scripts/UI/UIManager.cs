using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public Tilemap tilemap;
    public ResourceBar resourceBar;
    public RectTransform commandBarRect;
    public Transform buildButtonPanel; // Panel chứa các nút build (gán trong Inspector)
    public GameObject buildButtonPrefab; // Prefab nút build (Button có Image+Text)
    public Transform spawnButtonPanel; // Panel chứa các nút spawn (gán trong Inspector)
    public GameObject spawnButtonPrefab; // Prefab nút spawn (Button có Image+Text)
    public List<BuildingType> allBuildingTypes; // Kéo các BuildingType vào đây trong Inspector
    public GameObject infoPanel; // Panel hiển thị thông tin (gán trong Inspector)
    public TextMeshProUGUI infoText; // Text hiển thị thông tin (gán trong Inspector)
    public GameObject waveInfoPanel; // Panel hiển thị thông tin wave (gán trong Inspector)
    public TextMeshProUGUI waveInfoText; // Text hiển thị thông tin wave (gán trong Inspector)

    // Hiển thị GameOverPanel với kết quả (victorious/defeated)
    [Header("Game Over UI")]
    public GameObject gameOverPanel; // Gán panel trong Inspector
    public TextMeshProUGUI gameResultText; // Gán TMP text trong Inspector

    private List<Vector3Int> selectedBuildingCells = new List<Vector3Int>();
    private GameObject ghostBuildObject = null;
    private bool isPlacingBuildGhost = false;
    private BuildingInstance selectedBuilding = null;
    private List<GameObject> currentSpawnButtons = new List<GameObject>();
    private List<GameObject> currentBuildButtons = new List<GameObject>();
    private BuildingType currentGhostBuildingType = null;

    // Thêm singleton cho UIManager để dễ gọi từ WaveManager
    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowAllBuildButtons();
    }

    void ShowAllBuildButtons()
    {
        foreach (var btn in currentBuildButtons) Destroy(btn);
        currentBuildButtons.Clear();

        foreach (var buildingType in allBuildingTypes)
        {
            GameObject btnObj = Instantiate(buildButtonPrefab, buildButtonPanel);
            Button btn = btnObj.GetComponent<Button>();
            Image img = btnObj.GetComponentInChildren<Image>();
            Text txt = btnObj.GetComponentInChildren<Text>();

            if (img != null && buildingType.icon != null) img.sprite = buildingType.icon;
            if (txt != null) txt.text = buildingType.buildingName;

            btn.onClick.AddListener(() => OnBuildButtonClick(buildingType));

            var btnUI = btnObj.GetComponent<SpawnButtonUI>();
            if (btnUI != null)
            {
                string info = $"<b>{buildingType.buildingName}</b>\n" +
                            $"<color=#966F33>Đất:</color> {buildingType.buildCost[0]}\n" +
                            $"<color=#E5B100>Mật:</color> {buildingType.buildCost[1]}\n" +
                            $"<color=#228B22>Lá:</color> {buildingType.buildCost[2]}\n" +
                            $"<color=#CCCCCC>Vỏ:</color> {buildingType.buildCost[3]}";

                btnUI.SetupBuildButton(buildingType.buildCost, info);

                // Thêm EventTrigger runtime
                EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
                if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                // Hover: PointerEnter
                var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((eventData) => UIManager.Instance.ShowInfoPanel(info));
                trigger.triggers.Add(entryEnter);

                // Hover out: PointerExit
                var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((eventData) => UIManager.Instance.HideInfoPanel());
                trigger.triggers.Add(entryExit);
            }

            currentBuildButtons.Add(btnObj);
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Canvas canvas = commandBarRect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas.worldCamera;
            bool isInCommandBar = RectTransformUtility.RectangleContainsScreenPoint(commandBarRect, Input.mousePosition, uiCamera);
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

            // Hiển thị info panel nếu nhấn vào unit hoặc building
            if (!isInCommandBar && hit != null)
            {
                var stats = hit.GetComponent<UnitStats>();
                var building = hit.GetComponent<BuildingInstance>();
                if (stats != null)
                {
                    ShowInfoPanel(GetUnitInfo(stats));
                }
                else if (building != null && building.buildingType != null)
                {
                    ShowInfoPanel(GetBuildingInfo(building.buildingType));
                }
                else
                {
                    HideInfoPanel();
                }
            }
            else if (!isInCommandBar)
            {
                HideInfoPanel();
            }

            if (!isInCommandBar && hit != null)
            {
                var building = hit.GetComponent<BuildingInstance>();
                if (building != null && building.buildingType != null)
                {
                    selectedBuilding = building;
                    selectedBuildingCells.Clear();
                    Bounds bounds = hit.bounds;
                    Vector3 min = bounds.min;
                    Vector3 max = bounds.max;
                    for (int x = Mathf.FloorToInt(min.x); x < Mathf.CeilToInt(max.x); x++)
                    {
                        for (int y = Mathf.FloorToInt(min.y); y < Mathf.CeilToInt(max.y); y++)
                        {
                            Vector3 worldPos = new Vector3(x + 0.5f, y + 0.5f, 0);
                            if (hit.OverlapPoint(worldPos))
                            {
                                Vector3Int cell = tilemap.WorldToCell(worldPos);
                                if (!selectedBuildingCells.Contains(cell))
                                    selectedBuildingCells.Add(cell);
                            }
                        }
                    }
                    ShowSpawnButtonsForBuilding(selectedBuilding.buildingType);
                }
                else
                {
                    selectedBuilding = null;
                    HideAllSpawnButtons();
                }
            }
            else if (!isInCommandBar)
            {
                selectedBuilding = null;
                HideAllSpawnButtons();
            }
        }

        // Ghost build placement logic
        if (isPlacingBuildGhost && ghostBuildObject != null)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = tilemap.WorldToCell(mouseWorldPos);
            Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);
            cellCenter.z = 0; // Đảm bảo ghost cũng ở z = 0
            ghostBuildObject.transform.position = cellCenter;
            ghostBuildObject.SetActive(true);

            if (Input.GetMouseButtonDown(0)) // Chuột trái
            {
                var ghostCollider = ghostBuildObject.GetComponent<Collider2D>();
                if (ghostCollider != null) ghostCollider.enabled = true;
                ContactFilter2D filter = new ContactFilter2D();
                filter.useTriggers = false;
                Collider2D[] results = new Collider2D[10];
                int hitCount = 0;
                if (ghostCollider != null)
                    hitCount = ghostCollider.Overlap(filter, results);
                bool hasBlockingCollider = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (results[i] != null && results[i].gameObject != ghostBuildObject)
                    {
                        var dist = Physics2D.Distance(ghostCollider, results[i]);
                        if (dist.isOverlapped && dist.distance < -0.3f)
                        {
                            hasBlockingCollider = true;
                            break;
                        }
                    }
                }
                if (ghostCollider != null) ghostCollider.enabled = false;
                if (!hasBlockingCollider)
                {
                    var buildingType = currentGhostBuildingType;
                    if (buildingType == null)
                    {
                        Debug.LogError("Prefab chưa gán BuildingType!");
                        Destroy(ghostBuildObject);
                        ghostBuildObject = null;
                        isPlacingBuildGhost = false;
                        return;
                    }
                    if (!CheckAndConsumeResources(buildingType.buildCost))
                    {
                        Debug.Log("Không đủ tài nguyên để xây tòa nhà!");
                        Destroy(ghostBuildObject);
                        ghostBuildObject = null;
                        isPlacingBuildGhost = false;
                        return;
                    }
                    Vector3 buildPos = cellCenter;
                    buildPos.z = 0;
                    Instantiate(buildingType.buildingPrefab, buildPos, Quaternion.identity);
                    Debug.Log($"Build placed at {cellPos}");
                }
                else
                {
                    Debug.Log("Ô này đã bị chiếm quá nhiều, không thể đặt tòa nhà!");
                }
                Destroy(ghostBuildObject);
                ghostBuildObject = null;
                isPlacingBuildGhost = false;
                currentGhostBuildingType = null;
            }
            else if (Input.GetMouseButtonDown(1)) // Chuột phải
            {
                Destroy(ghostBuildObject);
                ghostBuildObject = null;
                isPlacingBuildGhost = false;
                Debug.Log("Đã hủy ghost build object");
            }
        }

        // Cập nhật real-time UI cho các nút spawn
        if (selectedBuilding != null && selectedBuilding.buildingType != null && selectedBuilding.SpawnQueue != null)
        {
            foreach (var unitData in selectedBuilding.buildingType.spawnableUnits)
            {
                if (spawnButtonUIs.TryGetValue(unitData, out var btnUI))
                {
                    // Đếm số lượng unit này trong hàng chờ
                    int count = 0;
                    float progress = 0f;
                    var queue = selectedBuilding.SpawnQueue;
                    for (int i = 0; i < queue.Count; i++)
                    {
                        if (queue[i].unitData == unitData)
                        {
                            count++;
                            if (i == 0) progress = 1f - (queue[i].timeLeft / Mathf.Max(0.01f, unitData.spawnTime));
                        }
                    }
                    btnUI.UpdateSpawnProgress(count > 0 ? progress : 0f, count);
                }
            }
        }
    }

    // Lưu reference SpawnButtonUI cho từng nút spawn
    private Dictionary<UnitSpawnData, SpawnButtonUI> spawnButtonUIs = new Dictionary<UnitSpawnData, SpawnButtonUI>();

    private void ShowSpawnButtonsForBuilding(BuildingType buildingType)
    {
        HideAllSpawnButtons();
        spawnButtonUIs.Clear();
        if (buildingType == null || buildingType.spawnableUnits == null) return;
        foreach (var unitData in buildingType.spawnableUnits)
        {
            GameObject btnObj = Instantiate(spawnButtonPrefab, spawnButtonPanel);
            Button btn = btnObj.GetComponent<Button>();
            Image img = btnObj.GetComponentInChildren<Image>();
            Text txt = btnObj.GetComponentInChildren<Text>();
            if (img != null && unitData.icon != null) img.sprite = unitData.icon;
            if (txt != null) txt.text = unitData.unitName;
            btn.onClick.AddListener(() => OnSpawnUnitClick(unitData, buildingType));
            var btnUI = btnObj.GetComponent<SpawnButtonUI>();
            if (btnUI != null)
            {
                string info = GetSpawnableUnitInfo(unitData);
                btnUI.SetupSpawnButton(false, 0f, 0, unitData.resourceCost, info);

                // Thêm EventTrigger runtime
                EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
                if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((eventData) => UIManager.Instance.ShowInfoPanel(info));
                trigger.triggers.Add(entryEnter);

                var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((eventData) => UIManager.Instance.HideInfoPanel());
                trigger.triggers.Add(entryExit);
            }
            spawnButtonUIs[unitData] = btnUI;
            currentSpawnButtons.Add(btnObj);
        }
    }

    private void HideAllSpawnButtons()
    {
        foreach (var btn in currentSpawnButtons)
        {
            var btnUI = btn.GetComponent<SpawnButtonUI>();
            if (btnUI != null) btnUI.SetupSpawnButton(false, 0f, 0);
            Destroy(btn);
        }
        currentSpawnButtons.Clear();
        spawnButtonUIs.Clear();
    }

    private void OnSpawnUnitClick(UnitSpawnData unitData, BuildingType buildingType)
    {
        if (selectedBuilding == null) return;
        // Thêm vào hàng chờ spawn của building thay vì spawn ngay
        selectedBuilding.EnqueueSpawn(unitData);
        Debug.Log($"Đã thêm {unitData.unitName} vào hàng chờ spawn của {selectedBuilding.name}");
    }

    public void OnBuildButtonClick(BuildingType buildingType)
    {
        if (ghostBuildObject != null) Destroy(ghostBuildObject);
        ghostBuildObject = Instantiate(buildingType.buildingPrefab);
        ghostBuildObject.name = "GhostBuildObject";
        var sr = ghostBuildObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.5f;
            sr.color = c;
        }
        else
        {
            Debug.LogWarning("Prefab không có SpriteRenderer, không thể làm mờ ghost!");
        }
        var ghostCollider = ghostBuildObject.GetComponent<Collider2D>();
        if (ghostCollider != null) ghostCollider.enabled = false;
        ghostBuildObject.SetActive(true);
        isPlacingBuildGhost = true;
        currentGhostBuildingType = buildingType;
        Debug.Log($"Đã tạo ghost build object cho {buildingType.buildingName}");
    }

    // Cho phép BuildingInstance gọi kiểm tra tài nguyên
    public bool CheckAndConsumeResources(int[] needed)
    {
        int[] currentResources = { resourceBar.dirt, resourceBar.honey, resourceBar.leaf, resourceBar.shell };
        for (int i = 0; i < 4; i++)
        {
            if (currentResources[i] < needed[i])
                return false;
        }
        resourceBar.SetDirt(resourceBar.dirt - needed[0]);
        resourceBar.SetHoney(resourceBar.honey - needed[1]);
        resourceBar.SetLeaf(resourceBar.leaf - needed[2]);
        resourceBar.SetShell(resourceBar.shell - needed[3]);
        resourceBar.UpdateAllResources();
        return true;
    }

    // Trả về vị trí spawn gần nhất quanh building (mặc định là vị trí building)
    public Vector3 GetNearestSpawnPointForBuilding(BuildingInstance building)
    {
        // Nếu có selectedBuildingCells thì ưu tiên, nếu không trả về vị trí building
        if (building == null) return Vector3.zero;
        var bounds = building.GetComponent<Collider2D>()?.bounds;
        if (bounds != null)
            return bounds.Value.center;
        return building.transform.position;
    }

    // Trả về vị trí spawn trống gần nhất quanh building (chuẩn hóa: lấy cell gốc từ transform, sinh cell xung quanh theo layer)
    public Vector3 FindNearestSpawnPointForBuilding(BuildingInstance building)
    {
        if (building == null || tilemap == null) return building != null ? building.transform.position : Vector3.zero;
        Vector3Int centerCell = tilemap.WorldToCell(building.transform.position);
        int maxRadius = 5; // số ô tối đa kiểm tra xung quanh
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            // Quét theo vòng ngoài (layer) của hình vuông quanh centerCell
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue; // chỉ lấy viền ngoài
                    Vector3Int cell = new Vector3Int(centerCell.x + dx, centerCell.y + dy, centerCell.z);
                    Vector3 worldPos = tilemap.GetCellCenterWorld(cell);
                    Collider2D col = Physics2D.OverlapPoint(worldPos);
                    if (col == null)
                    {
                        return worldPos;
                    }
                }
            }
        }
        // Nếu không tìm được ô trống, trả về vị trí building
        return building.transform.position;
    }

    public void ShowInfoPanel(string info)
    {
        if (infoPanel != null && infoText != null)
        {
            infoPanel.SetActive(true);
            infoText.text = info;
        }
    }
    public void HideInfoPanel()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
    }
    private string GetUnitInfo(UnitStats stats)
    {
        // Lấy tên prefab gốc nếu có, nếu không thì loại bỏ (Clone) khỏi gameObject.name
        string displayName = stats.gameObject.name.Replace("(Clone)", "").Trim();
        return $"<b>{displayName}</b>\n" +
            $"Máu: {stats.MaxHealth}       \t" +
            $"Sát thương: {stats.Damage}\n" +
            $"Tốc chạy: {stats.MoveSpeed}\t" +
            $"Tốc đánh: {stats.AttackSpeed}\n" +
            $"Giáp: {stats.Armor}       \t" +
            $"Tầm đánh: {stats.AttackRange}\n" +
            $"Hút máu: {stats.LifeSteal}\t" +
            $"Hồi máu: {stats.HealthRegen}";
    }
    private string GetBuildingInfo(BuildingType buildingType)
    {
        string displayName = buildingType.buildingName != null && buildingType.buildingName != "" ? buildingType.buildingName : buildingType.name.Replace("(Clone)", "");
        string cost = $"Đất: {buildingType.buildCost[0]}, Mật: {buildingType.buildCost[1]}, Lá: {buildingType.buildCost[2]}, Vỏ: {buildingType.buildCost[3]}";
        string spawn = (buildingType.spawnableUnits != null && buildingType.spawnableUnits.Count > 0)
            ? "\nSpawn: " + string.Join(", ", buildingType.spawnableUnits.ConvertAll(u => u != null ? u.unitName : ""))
            : "\nKhông spawn được unit";
        return $"<b>{displayName}</b>\n{cost}{spawn}";
    }

    public void EnableBuildAndSpawnPanels(bool enable)
    {
        if (buildButtonPanel != null)
            buildButtonPanel.gameObject.SetActive(enable);
        if (spawnButtonPanel != null)
            spawnButtonPanel.gameObject.SetActive(enable);
    }

    public void SetBuildAndSpawnButtonsInteractable(bool interactable)
    {
        if (buildButtonPanel != null)
        {
            foreach (var btn in buildButtonPanel.GetComponentsInChildren<Button>(true))
                btn.interactable = interactable;
        }
        if (spawnButtonPanel != null)
        {
            foreach (var btn in spawnButtonPanel.GetComponentsInChildren<Button>(true))
                btn.interactable = interactable;
        }
    }

    // Lấy toàn bộ unit có thể spawn từ tất cả BuildingType
    public List<UnitSpawnData> GetAllSpawnableUnits()
    {
        HashSet<UnitSpawnData> unitSet = new HashSet<UnitSpawnData>();
        foreach (var building in allBuildingTypes)
        {
            if (building != null && building.spawnableUnits != null)
            {
                foreach (var unit in building.spawnableUnits)
                {
                    if (unit != null) unitSet.Add(unit);
                }
            }
        }
        return new List<UnitSpawnData>(unitSet);
    }

    // Hiển thị panel thông tin wave, truyền vào chuỗi info (dùng trong WaveManager)
    public void ShowWaveInfoPanel(string info)
    {
        if (waveInfoPanel != null && waveInfoText != null)
        {
            waveInfoPanel.SetActive(true);
            waveInfoText.text = info;
        }
    }

    // Ẩn panel thông tin wave
    public void HideWaveInfoPanel()
    {
        if (waveInfoPanel != null)
            waveInfoPanel.SetActive(false);
    }

    public static bool IsTimeFrozen = false;

    private string GetSpawnableUnitInfo(UnitSpawnData unitData)
    {
        if (unitData == null || unitData.resourceCost == null || unitData.resourceCost.Length < 4)
            return "";

        return $"<b>{unitData.unitName}</b>\n" +
            $"<color=#966F33>Đất:</color> {unitData.resourceCost[0]}\n" +
            $"<color=#E5B100>Mật:</color> {unitData.resourceCost[1]}\n" +
            $"<color=#228B22>Lá:</color> {unitData.resourceCost[2]}\n" +
            $"<color=#CCCCCC>Vỏ:</color> {unitData.resourceCost[3]}";
    }

    // Hiển thị GameOverPanel với kết quả (victorious/defeated)
    public void ShowGameOverPanel(string result)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        if (gameResultText != null)
        {
            if (result == "victorious")
                gameResultText.text = "Victory!";
            else if (result == "defeated")
                gameResultText.text = "Defeated!";
            else
                gameResultText.text = result;
        }
    }

}

[System.Serializable]
public class SpawnQueueEntry {
    public UnitSpawnData unitData;
    public float timeLeft;
    public SpawnQueueEntry(UnitSpawnData data) {
        unitData = data;
        timeLeft = data.spawnTime;
    }
    
}

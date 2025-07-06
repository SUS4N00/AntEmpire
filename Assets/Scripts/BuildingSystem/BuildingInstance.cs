using UnityEngine;
using System.Collections.Generic;

public class BuildingInstance : MonoBehaviour
{
    public BuildingType buildingType;
    private List<SpawnQueueEntry> spawnQueue = new List<SpawnQueueEntry>();
    public IReadOnlyList<SpawnQueueEntry> SpawnQueue => spawnQueue;
    private UIManager uiManager;

    void Awake() {
        uiManager = UIManager.Instance;
    }

    void Start() {
        // Đăng ký công trình với WaveManager
        if (WaveManager.Instance != null)
            WaveManager.Instance.RegisterBuilding();
    }

    void Update() {
        if (spawnQueue.Count > 0) {
            var entry = spawnQueue[0];
            entry.timeLeft -= Time.deltaTime;
            if (entry.timeLeft <= 0f) {
                // Đủ thời gian, kiểm tra tài nguyên
                if (uiManager != null && uiManager.CheckAndConsumeResources(entry.unitData.resourceCost)) {
                    // Tìm ô spawn gần nhất quanh building
                    Vector3 spawnPos = transform.position;
                    if (uiManager != null) spawnPos = uiManager.FindNearestSpawnPointForBuilding(this);
                    Instantiate(entry.unitData.unitPrefab, spawnPos, Quaternion.identity);
                    Debug.Log($"Spawned {entry.unitData.unitName} from queue at {spawnPos}");
                    spawnQueue.RemoveAt(0);
                } else {
                    // Không đủ tài nguyên, hủy toàn bộ hàng chờ
                    spawnQueue.Clear();
                    Debug.LogWarning("Không đủ tài nguyên, hủy toàn bộ hàng chờ spawn!");
                }
            }
        }
    }

    public void EnqueueSpawn(UnitSpawnData unitData) {
        spawnQueue.Add(new SpawnQueueEntry(unitData));
    }

    private void OnDestroy() {
        spawnQueue.Clear(); // Khi building bị phá hủy, hủy hàng chờ
        // Báo cho WaveManager khi công trình bị phá
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnBuildingDestroyed();
    }
}

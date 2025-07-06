using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Thêm using cho SceneManager

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyRewardEntry
    {
        public GameObject enemyPrefab;
        public int shellReward;
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<EnemyRewardEntry> normalEnemies; // Đổi từ List<GameObject> sang List<EnemyRewardEntry>
        public List<GameObject> bossEnemies;
        public int subWaveCount = 3;
        public int enemiesPerSubWave = 5;
        public float timeBetweenSubWaves = 5f;
    }

    public List<Wave> waves;
    public Transform[] spawnPoints;
    public EnemySpawner enemySpawner;
    public RewardSystem rewardSystem;
    public Text clockText;
    public RectTransform clockHand;

    private int currentWaveIndex = 0;
    private int currentSubWave = 0;
    private float timeLeft = 0f;
    private bool isClockRunning = false;
    private int nightCount = 0;

    public float dayDuration = 60f;
    public float nightDuration = 60f;

    public event System.Action OnAllWavesCompleted;
    // Danh sách phần thưởng dùng chung cho cả reward đầu đêm và boss chết
    [HideInInspector]
    [SerializeField] private List<RewardData> allRewards; // Tự động load, không thêm thủ công

    private float phaseStartTime = 0f;
    private bool isNight = false;

    // Số lượng công trình còn lại
    private int buildingCount = 0;
    private bool gameEnded = false;

    public static WaveManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        // Tự động load tất cả RewardData trong Resources/Rewards
        allRewards = new List<RewardData>(Resources.LoadAll<RewardData>("Rewards"));
        Debug.Log($"[WaveManager] Loaded {allRewards.Count} rewards from Resources/Rewards.");

        if (enemySpawner == null) Debug.LogError("EnemySpawner is not assigned!");
        if (rewardSystem == null) Debug.LogError("RewardSystem is not assigned!");
        if (spawnPoints == null || spawnPoints.Length == 0) Debug.LogError("No spawn points assigned!");
        if (rewardSystem != null)
        {
            rewardSystem.OnRewardSelected += OnRewardSelected;
        }
        if (enemySpawner != null)
        {
            enemySpawner.OnBossDead += OnBossDead;
        }
        Debug.Log($"[WaveManager] Game Start - Day {nightCount + 1} begins. Time: {dayDuration}s");
        StartCoroutine(DayNightCycle());
        timeLeft = dayDuration;
        isClockRunning = true;
        phaseStartTime = Time.time;
        isNight = false;
    }

    void OnEnable()
    {
        if (rewardSystem != null)
        {
            rewardSystem.OnRewardSelected += ResumeTime;
        }
    }

    void OnDisable()
    {
        if (rewardSystem != null)
        {
            rewardSystem.OnRewardSelected -= ResumeTime;
        }
    }

    void ResumeTime(int index)
    {
        Time.timeScale = 1f;
    }

    void OnRewardSelected(int index)
    {
        Debug.Log($"[WaveManager] Player selected reward {index}");
        Debug.Log($"Upgrade {index} applied!");
    }

    void OnBossDead(GameObject boss)
    {
        // Random 3 reward-unit combo khi boss chết
        if (rewardSystem != null)
        {
            Time.timeScale = 0f;
            rewardSystem.ShowRewardSelectionPanel();
        }
    }

    void StartWave()
    {
        if (currentWaveIndex < waves.Count)
        {
            Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} started.");
            currentSubWave = 0;
            StartCoroutine(SpawnSubWaves());
        }
        else
        {
            Debug.Log("All waves completed!");
            OnAllWavesCompleted?.Invoke();
        }
    }

    IEnumerator SpawnSubWaves()
    {
        Wave wave = waves[currentWaveIndex];
        while (currentSubWave < wave.subWaveCount)
        {
            Debug.Log($"[WaveManager] SubWave {currentSubWave + 1}/{wave.subWaveCount} of Wave {currentWaveIndex + 1} spawning.");
            if (wave.normalEnemies != null && wave.normalEnemies.Count > 0)
            {
                for (int i = 0; i < wave.enemiesPerSubWave; i++)
                {
                    var entry = wave.normalEnemies[Random.Range(0, wave.normalEnemies.Count)];
                    GameObject prefab = entry.enemyPrefab;
                    int shellReward = entry.shellReward;
                    Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    enemySpawner.SpawnEnemyWithShellReward(prefab, spawnPoint.position, currentWaveIndex, false, shellReward); // Truyền shellReward
                }
            }
            else
            {
                Debug.LogWarning($"Wave {currentWaveIndex + 1} has no normal enemies!");
            }
            if (currentSubWave == wave.subWaveCount - 1 && wave.bossEnemies != null && wave.bossEnemies.Count > 0)
            {
                Debug.Log($"[WaveManager] Boss(es) spawning for Wave {currentWaveIndex + 1}.");
                foreach (var boss in wave.bossEnemies)
                {
                    Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    enemySpawner.SpawnEnemyWithShellReward(boss, spawnPoint.position, currentWaveIndex, true, 0); // isBoss = true, shellReward = 0
                }
            }
            else if (currentSubWave == wave.subWaveCount - 1)
            {
                Debug.LogWarning($"Wave {currentWaveIndex + 1} has no boss enemies!");
            }
            currentSubWave++;
            yield return new WaitForSeconds(wave.timeBetweenSubWaves);
        }
        Debug.Log($"[WaveManager] All subwaves of Wave {currentWaveIndex + 1} spawned. Waiting for clear.");
        StartCoroutine(WaitForWaveClear());
    }

    IEnumerator WaitForWaveClear()
    {
        int waitingWaveIndex = currentWaveIndex;
        while (enemySpawner.GetTotalAliveEnemies() > 0)
        {
            yield return new WaitForSeconds(1f);
        }
        Debug.Log($"[WaveManager] Wave {waitingWaveIndex + 1} cleared.");
        currentWaveIndex++;
        // Kiểm tra thắng khi đã qua wave cuối và không còn quái
        if (currentWaveIndex >= waves.Count && enemySpawner.GetTotalAliveEnemies() == 0 && !gameEnded)
        {
            Victory();
        }
    }

    void Update()
    {
        if (isClockRunning)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0) timeLeft = 0;
            if (clockHand != null)
            {
                float duration = isNight ? nightDuration : dayDuration;
                float elapsed = Mathf.Clamp(Time.time - phaseStartTime, 0, duration);
                float percent = Mathf.Clamp01(1f - (elapsed / duration));
                float startAngle = isNight ? 360f : 180f;
                float endAngle = isNight ? 180f : 0f;
                float angle = Mathf.Lerp(startAngle, endAngle, 1f - percent);
                clockHand.localRotation = Quaternion.Euler(0, 0, angle);
            }
            if (clockText != null)
            {
                int min = Mathf.FloorToInt(timeLeft / 60f);
                int sec = Mathf.FloorToInt(timeLeft % 60f);
                clockText.text = (isNight ? "Night: " : "Day: ") + min.ToString("00") + ":" + sec.ToString("00");
            }
        }
    }

    IEnumerator DayNightCycle()
    {
        while (true)
        {
            Debug.Log($"[WaveManager] Day {nightCount + 1} begins. Time: {dayDuration}s");
            timeLeft = dayDuration;
            isClockRunning = true;
            isNight = false;
            phaseStartTime = Time.time;
            // Hiển thị thông tin wave sắp tới vào đầu sáng
            ShowNextWaveInfoPanel();
            yield return new WaitForSeconds(dayDuration);
            nightCount++;
            Debug.Log($"[WaveManager] Night {nightCount} begins. Time: {nightDuration}s");
            timeLeft = nightDuration;
            isClockRunning = true;
            isNight = true;
            phaseStartTime = Time.time;
            // Random 3 phần thưởng nhỏ đầu đêm từ allRewards
            Time.timeScale = 0f;
            rewardSystem.ShowRewardSelectionPanel();
            bool rewardChosen = false;
            rewardSystem.OnRewardSelected += (i) => { rewardChosen = true; };
            while (!rewardChosen) yield return null;
            rewardSystem.OnRewardSelected -= (i) => { rewardChosen = true; };
            Time.timeScale = 1f;
            Debug.Log($"[WaveManager] Player selected small reward, starting wave {nightCount}");
            timeLeft = nightDuration;
            isClockRunning = true;
            if (nightCount <= waves.Count)
            {
                currentWaveIndex = nightCount - 1;
                StartWave();
            }
            yield return new WaitForSeconds(nightDuration);
            Debug.Log($"[WaveManager] Night {nightCount} ended.");
            // Không hiện bossReward nữa, đã xử lý khi boss chết
        }
    }

    // Hàm tạo chuỗi thông tin wave tiếp theo và gọi UIManager
    void ShowNextWaveInfoPanel()
    {
        if (currentWaveIndex < waves.Count && UIManager.Instance != null)
        {
            var wave = waves[currentWaveIndex];
            string info = $"<b>Wave {currentWaveIndex + 1}: {wave.waveName}</b>\n";
            info += $"Subwave: <b>{wave.subWaveCount}</b>\n";
            info += $"Enemy: ";
            if (wave.normalEnemies != null && wave.normalEnemies.Count > 0)
            {
                var names = new List<string>();
                foreach (var e in wave.normalEnemies)
                    if (e != null && e.enemyPrefab != null) names.Add(e.enemyPrefab.name.Replace("(Clone)", ""));
                info += string.Join(", ", names) + $" (x{wave.enemiesPerSubWave} each subwave)\n";
            }
            else info += "None\n";
            info += $"Boss: ";
            if (wave.bossEnemies != null && wave.bossEnemies.Count > 0)
            {
                var names = new List<string>();
                foreach (var b in wave.bossEnemies)
                    if (b != null) names.Add(b.name.Replace("(Clone)", ""));
                info += string.Join(", ", names) + "\n";
            }
            else info += "None\n";
            UIManager.Instance.ShowWaveInfoPanel(info);
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWaveInfoPanel("<b>Complete!</b>");
        }
    }

    public void RegisterBuilding() { buildingCount++; }
    public void OnBuildingDestroyed() {
        buildingCount = Mathf.Max(0, buildingCount - 1);
        if (buildingCount == 0 && !gameEnded) GameOver();
    }

    private void GameOver()
    {
        gameEnded = true;
        Debug.Log("Game Over! All buildings destroyed.");
        Time.timeScale = 1f;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverPanel("defeated");
        }
        else
        {
            Debug.LogWarning("UIManager.Instance is null, cannot show GameOverPanel.");
        }
    }

    private void Victory()
    {
        gameEnded = true;
        Debug.Log("Victory! All waves cleared and all enemies defeated.");
        Time.timeScale = 1f;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverPanel("victorious");
        }
        else
        {
            Debug.LogWarning("UIManager.Instance is null, cannot show GameOverPanel.");
        }
    }
}

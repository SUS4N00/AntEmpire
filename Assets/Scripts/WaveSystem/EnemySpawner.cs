using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> aliveEnemies = new List<GameObject>();
    public Dictionary<int, List<GameObject>> waveEnemies = new Dictionary<int, List<GameObject>>();
    public int EnemyAliveCount => aliveEnemies.Count;
    public System.Action<GameObject> OnBossDead;

    // Hàm mới: Spawn enemy với shellReward
    public void SpawnEnemyWithShellReward(GameObject prefab, Vector3 position, int waveIndex, bool isBoss, int shellReward)
    {
        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        aliveEnemies.Add(enemy);
        if (isBoss)
        {
            Debug.Log($"[EnemySpawner] Boss spawned: {enemy.name}");
        }
        if (waveIndex >= 0)
        {
            if (!waveEnemies.ContainsKey(waveIndex))
                waveEnemies[waveIndex] = new List<GameObject>();
            waveEnemies[waveIndex].Add(enemy);
        }
        var health = enemy.GetComponent("UnitHealthSystem") as MonoBehaviour;
        if (health == null)
            health = enemy.GetComponent("UnitHealthSystam") as MonoBehaviour;
        if (health != null)
        {
            var evt = health.GetType().GetEvent("OnDead");
            if (evt != null)
            {
                System.Action handler = () => {
                    aliveEnemies.Remove(enemy);
                    if (waveIndex >= 0 && waveEnemies.ContainsKey(waveIndex))
                        waveEnemies[waveIndex].Remove(enemy);
                    if (!isBoss && shellReward > 0 && UIManager.Instance != null)
                    {
                        UIManager.Instance.resourceBar.SetShell(UIManager.Instance.resourceBar.shell + shellReward);
                        UIManager.Instance.resourceBar.UpdateAllResources();
                    }
                    if (isBoss && OnBossDead != null)
                    {
                        Debug.Log($"[EnemySpawner] Boss dead: {enemy.name}");
                        OnBossDead.Invoke(enemy);
                    }
                };
                evt.AddEventHandler(health, handler);
            }
        }
        else
        {
            Debug.LogWarning($"[EnemySpawner] Enemy {enemy.name} does not have UnitHealthSystem/UnitHealthSystam component!");
        }
    }

    public int GetAliveCountOfWave(int waveIndex)
    {
        if (waveEnemies.ContainsKey(waveIndex))
            return waveEnemies[waveIndex].Count;
        return 0;
    }

    // Trả về tổng số quái còn sống trên bản đồ
    public int GetTotalAliveEnemies()
    {
        return aliveEnemies.Count;
    }
}


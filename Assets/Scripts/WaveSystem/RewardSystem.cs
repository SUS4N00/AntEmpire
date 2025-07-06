using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardSystem : MonoBehaviour
{
    public GameObject rewardPanel;
    public Button[] rewardButtons;
    public Action<int> OnRewardSelected;
    public RewardManager rewardManager; // Kéo RewardManager vào đây
    public UIManager uiManager; // Kéo UIManager vào đây trong Inspector
    private List<RewardUnitCombo> currentCombos = new List<RewardUnitCombo>();
    public TextMeshProUGUI[] rewardDescriptionFields; // Gán từng TextMeshProUGUI cho từng nút reward

    void Start()
    {
        rewardPanel.SetActive(false);
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            int idx = i;
            rewardButtons[i].onClick.AddListener(() => SelectReward(idx));
        }
    }

    public void ShowRewardOptions(RewardUnitCombo[] combos)
    {
        currentCombos.Clear();
        currentCombos.AddRange(combos);
        rewardPanel.SetActive(true);
        if (uiManager != null) uiManager.SetBuildAndSpawnButtonsInteractable(false); // Disable chỉ các nút build/spawn
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            if (i < combos.Length)
            {
                var txt = rewardButtons[i].GetComponentInChildren<Text>();
                if (txt != null) txt.text = combos[i].reward.rewardName + "\n<color=yellow>" + combos[i].unit.unitName + "</color>";
                var img = rewardButtons[i].GetComponentInChildren<Image>();
                if (img != null && combos[i].reward.icon != null) img.sprite = combos[i].reward.icon;
                if (rewardDescriptionFields != null && i < rewardDescriptionFields.Length && rewardDescriptionFields[i] != null)
                    rewardDescriptionFields[i].text = combos[i].reward.description + "<b><color=red> áp dụng với: " + combos[i].unit.unitName + "</color></b>";
                rewardButtons[i].gameObject.SetActive(true);
            }
            else
            {
                rewardButtons[i].gameObject.SetActive(false);
                if (rewardDescriptionFields != null && i < rewardDescriptionFields.Length && rewardDescriptionFields[i] != null)
                    rewardDescriptionFields[i].text = "";
            }
        }
    }

    void SelectReward(int index)
    {
        rewardPanel.SetActive(false);
        if (uiManager != null) uiManager.SetBuildAndSpawnButtonsInteractable(true); // Enable lại các nút build/spawn
        if (index < currentCombos.Count && rewardManager != null)
        {
            rewardManager.ApplyRewardToUnit(currentCombos[index].reward, currentCombos[index].unit);
        }
        OnRewardSelected?.Invoke(index);
        Debug.Log($"Reward {index} selected");
    }

    // Hàm random 3 combo reward-unit
    public void ShowRandomRewardCombos(List<RewardData> allRewards, List<UnitSpawnData> allUnits)
    {
        var rewardPool = new List<RewardData>(allRewards);
        var combos = new List<RewardUnitCombo>();
        int count = Mathf.Min(3, rewardPool.Count);
        var playerUnits = GameObject.FindGameObjectsWithTag("PlayerUnit");
        int maxTries = 20; // Ngăn vòng lặp vô hạn nếu pool nhỏ
        int tries = 0;
        while (combos.Count < count && tries < maxTries)
        {
            if (rewardPool.Count == 0 || allUnits.Count == 0) break;
            int rewardIdx = UnityEngine.Random.Range(0, rewardPool.Count);
            var reward = rewardPool[rewardIdx];
            List<UnitSpawnData> validUnits = new List<UnitSpawnData>(allUnits);
            var unit = validUnits[UnityEngine.Random.Range(0, validUnits.Count)];
            // Kiểm tra combo đã tồn tại chưa
            bool exists = combos.Exists(c => c.reward == reward && c.unit == unit);
            if (!exists)
            {
                combos.Add(new RewardUnitCombo(reward, unit));
            }
            tries++;
        }
        ShowRewardOptions(combos.ToArray());
    }

    public void ShowRewardSelectionPanel()
    {
        if (rewardManager != null && uiManager != null)
        {
            var allUnits = uiManager.GetAllSpawnableUnits();
            ShowRandomRewardCombos(rewardManager.allRewards, allUnits);
        }
    }
}

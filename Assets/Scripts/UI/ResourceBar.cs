using UnityEngine;
using TMPro;

public class ResourceBar : MonoBehaviour
{
    public TextMeshProUGUI dirtText;
    public TextMeshProUGUI honeyText;
    public TextMeshProUGUI leafText;
    public TextMeshProUGUI shellText;
    public TextMeshProUGUI stageText;
    public GameObject menuPanel; // Kéo panel menu vào đây trong Inspector

    // Giá trị tài nguyên (có thể lấy từ GameManager hoặc script khác)
    public int dirt = 1000;
    public int honey = 1000;
    public int leaf = 1000;
    public int shell = 1000;
    public int stage = 1;

    void Start()
    {
        UpdateDirtText();
        UpdateHoneyText();
        UpdateLeafText();
        UpdateShellText();
        UpdateStageText();
    }

    public void UpdateDirtText()
    {
        dirtText.text = dirt.ToString();
    }

    public void UpdateHoneyText()
    {
        honeyText.text = honey.ToString();
    }

    public void UpdateLeafText()
    {
        leafText.text = leaf.ToString();
    }

    public void UpdateShellText()
    {
        shellText.text = shell.ToString();
    }

    public void UpdateStageText()
    {
        stageText.text = $"Stage {stage}";
    }
    // Hàm này có thể gọi khi tài nguyên thay đổi
    public void SetDirt(int newDirt)
    {
        dirt = newDirt;
    }

    public void SetHoney(int newHoney)
    {
        honey = newHoney;
    }

    public void SetLeaf(int newLeaf)
    {
        leaf = newLeaf;
    }

    public void SetShell(int newShell)
    {
        shell = newShell;
    }

    public void SetStage(int newStage)
    {
        stage = newStage;
    }

    // Hàm này có thể gán vào Unity Button để cập nhật tất cả tài nguyên
    [ContextMenu("Update All Resources")]
    public void UpdateAllResources()
    {
        UpdateDirtText();
        UpdateHoneyText();
        UpdateLeafText();
        UpdateShellText();
        UpdateStageText();
    }

    // Hàm này sẽ được gọi khi bấm vào nút menu
    public void OnMenuButtonClicked()
    {
        Time.timeScale = 0f;
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void OnResumeButtonClicked()
    {
        Time.timeScale = 1f;
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void OnBackToMenuButtonClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

}
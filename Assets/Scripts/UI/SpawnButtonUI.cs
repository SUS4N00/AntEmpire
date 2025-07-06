using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SpawnButtonUI : MonoBehaviour
{
    public Image overlayImage;
    public TextMeshProUGUI queueCountText;

    private bool isSpawnButton = false;
    private int[] resourceCost;
    private string infoText;

    public void SetupSpawnButton(bool showOverlay, float progress, int count, int[] resourceCost = null, string infoText = null)
    {
        isSpawnButton = true;
        this.resourceCost = resourceCost;
        this.infoText = infoText;

        if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(showOverlay);
            overlayImage.fillAmount = progress;
        }

        if (queueCountText != null)
        {
            queueCountText.gameObject.SetActive(true);
            queueCountText.text = count > 0 ? count.ToString() : "";
        }
    }

    public void SetupBuildButton(int[] resourceCost = null, string infoText = null)
    {
        isSpawnButton = false;
        this.resourceCost = resourceCost;
        this.infoText = infoText;

        if (overlayImage != null) overlayImage.gameObject.SetActive(false);
        if (queueCountText != null) queueCountText.gameObject.SetActive(false);
    }

    public void UpdateSpawnProgress(float progress, int count)
    {
        if (!isSpawnButton) return;
        if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(count > 0);
            overlayImage.fillAmount = progress;
        }
        if (queueCountText != null)
            queueCountText.text = count > 0 ? count.ToString() : "";
    }
}

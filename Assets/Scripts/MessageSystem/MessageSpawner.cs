using UnityEngine;

public class MessageSpawner : MonoBehaviour
{
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private Transform messageSpawnPoint;
    [SerializeField] private Color messageColor = Color.red;

    public void SpawnMessage(string message)
    {
        var messageObject = Instantiate(messagePrefab, messageSpawnPoint.position, Quaternion.identity);
        var inGameMessage = messageObject.GetComponent<IInGameMessage>();
        inGameMessage.SetMessage(message, messageColor);
    }

    public void SpawnMessage(string message, Color color)
    {
        var messageObject = Instantiate(messagePrefab, messageSpawnPoint.position, Quaternion.identity);
        var inGameMessage = messageObject.GetComponent<IInGameMessage>();
        inGameMessage.SetMessage(message, color);
    }
}

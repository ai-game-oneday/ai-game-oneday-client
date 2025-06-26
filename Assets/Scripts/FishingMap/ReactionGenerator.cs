using System.Collections;
using Network.ApiClient;
using Network.Models;
using TMPro;
using UnityEngine;

public class ReactionGenerator : MonoBehaviour
{
  [SerializeField] private GameManager gameManager;
  [SerializeField] private AIServerConfig _cfg;
  [SerializeField] private Transform spawnPoint;
  [SerializeField] private TextMeshProUGUI reactionText;
  private Vector2 uiOffset = Vector2.zero;
  public Camera targetCamera;

  private ApiClient _apiClient;

  private void Awake()
  {
    _apiClient = new(_cfg);
    targetCamera = Camera.main;
  }

  public void GenerateReaction()
  {
    StartCoroutine(GeneratingReaction());
  }

  private IEnumerator GeneratingReaction()
  {
    ReactionRequest payload = new()
    {
      location = gameManager.Location,
      human = gameManager.Human,
      boat = gameManager.Boat,
      fish = gameManager.Fish,
      size = gameManager.Size,
    };

    ReactionResponse response = null;
    yield return _apiClient.PostJson(
      _cfg.GenerateReactionPath,
      payload,
      onOk => { response = JsonUtility.FromJson<ReactionResponse>(onOk); },
      onError => { Debug.LogError(onError); });

    Debug.Log(response.reaction);

    reactionText.text = response.reaction;
    Vector3 screenPosition = targetCamera.WorldToScreenPoint(spawnPoint.position);
    Vector2 uiPosition = new Vector2(screenPosition.x, screenPosition.y) + uiOffset;
    reactionText.rectTransform.position = uiPosition;

    reactionText.gameObject.SetActive(true);

    yield return new WaitForSeconds(5f);

    reactionText.gameObject.SetActive(false);
  }
}

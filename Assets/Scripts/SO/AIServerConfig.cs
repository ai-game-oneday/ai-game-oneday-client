using UnityEngine;

[CreateAssetMenu(fileName = "ServerConfig", menuName = "Scriptable Objects/ServerConfig")]
public class AIServerConfig : ScriptableObject
{
  [Header("Server URL")]
  [SerializeField] private string url;
  [HideInInspector] public string Url => url;

  [Header("Server API Key")]
  [SerializeField] private string apiKey;
  [HideInInspector] public string ApiKey => apiKey;

  [Space(10)]
  [Header("API Endpoints")]
  [SerializeField] private string generateFishPath;
  [HideInInspector] public string GenerateFishPath => generateFishPath;
  [SerializeField] private string generateHumanPath;
  [HideInInspector] public string GenerateHumanPath => generateHumanPath;
  [SerializeField] private string generateBoatPath;
  [HideInInspector] public string GenerateBoatPath => generateBoatPath;
  [SerializeField] private string generateBackgroundPath;
  [HideInInspector] public string GenerateBackgroundPath => generateBackgroundPath;
  [SerializeField] private string generateReactionPath;
  [HideInInspector] public string GenerateReactionPath => generateReactionPath;
  [SerializeField] private string generateImagePath;
  [HideInInspector] public string GenerateImagePath => generateImagePath;
  public string Full(string path) => Url + path;
}

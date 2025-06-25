using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Network.ApiClient
{
  public class ApiClient
  {
    private readonly AIServerConfig _config;

    public ApiClient(AIServerConfig config)
    {
      _config = config != null ? config : throw new ArgumentNullException(nameof(config));
    }

    private string URL(string path) => _config.Full(path);

    public IEnumerator PostJson<TReq>(string path, TReq payload, Action<string> onOk, Action<string> onError)
    {
      string json = JsonUtility.ToJson(payload);
      byte[] body = Encoding.UTF8.GetBytes(json);

      using var req = new UnityWebRequest(URL(path), UnityWebRequest.kHttpVerbPOST);
      req.uploadHandler = new UploadHandlerRaw(body);
      req.downloadHandler = new DownloadHandlerBuffer();
      req.SetRequestHeader("Content-Type", "application/json");
      req.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");

      yield return req.SendWebRequest();

      if (req.result != UnityWebRequest.Result.Success)
        onError?.Invoke(req.error);
      else
        onOk?.Invoke(req.downloadHandler.text);
    }
  }
}
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Network.Models;
using Network.ApiClient;
using UnityEngine.InputSystem;

public class ImageSpriteGenerator : MonoBehaviour
{
  [SerializeField] private GameManager gameManager;
  [SerializeField] private ReactionGenerator reactionGenerator;

  [Header("UI References")]
  public TMP_InputField promptInput;
  public Text statusText;
  public Fish fish;
  public Human human;

  [Header("Sprite Settings")]
  public SpriteRenderer fishRenderer;
  public GameObject fishMask;
  public SpriteRenderer humanRenderer;
  public GameObject humanMask;
  public SpriteRenderer boatRenderer;
  public GameObject boatMask;
  public SpriteRenderer backgroundRenderer;
  public GameObject bgMask;

  public float pixelsPerUnit = 16f;

  [Header("Server Settings")]
  [SerializeField] private AIServerConfig _serverCfg;

  [Header("Sounds")]
  [SerializeField] private AudioClip clickUI;
  [SerializeField] private AudioClip submitUI;
  [SerializeField] private AudioClip speedUpFirst;
  [SerializeField] private AudioClip speedUpLoop;
  [SerializeField] private AudioClip changeSkinSFX;
  [SerializeField] private AudioClip fishingSFX;

  private ApiClient _apiClient;

  private Vector2Int small = new(64, 64);
  private Vector2Int medium = new(128, 128);
  private Vector2Int large = new(256, 256);

  private string currentSelected = "fish";

  void Start()
  {
    _apiClient = new(_serverCfg);

    // testRenderer가 없으면 경고
    if (fishRenderer == null)
    {
      Debug.LogWarning("testRenderer가 할당되지 않았습니다. Inspector에서 SpriteRenderer를 할당해주세요.");
    }

    UpdateStatus("이미지 생성 준비 완료");
  }

  public void SetFish()
  {
    currentSelected = "fish";
    MaskAll();
    fishMask.SetActive(true);
    AudioManager.I.PlaySFX(clickUI);
  }
  public void SetHuman()
  {
    currentSelected = "human";
    MaskAll();
    humanMask.SetActive(true);
    AudioManager.I.PlaySFX(clickUI);
  }
  public void SetBoat()
  {
    currentSelected = "boat";
    MaskAll();
    boatMask.SetActive(true);
    AudioManager.I.PlaySFX(clickUI);
  }
  public void SetBg()
  {
    currentSelected = "bg";
    MaskAll();
    bgMask.SetActive(true);
    AudioManager.I.PlaySFX(clickUI);
  }

  private void MaskAll()
  {
    fishMask.SetActive(false);
    humanMask.SetActive(false);
    boatMask.SetActive(false);
    bgMask.SetActive(false);
  }

  public void OnSubmit(InputAction.CallbackContext context)
  {
    if (string.IsNullOrEmpty(promptInput.text)) return;

    if (context.performed)
      GenerateImage();
  }

  public void GenerateImage()
  {
    if (promptInput == null || string.IsNullOrEmpty(promptInput.text))
    {
      UpdateStatus("프롬프트를 입력해주세요.");
      return;
    }

    if (fishRenderer == null)
    {
      UpdateStatus("testRenderer가 할당되지 않았습니다.");
      return;
    }

    AudioManager.I.PlaySFX(submitUI);
    if (currentSelected == "fish")
      StartCoroutine(GenerateFishCoroutine(promptInput.text));
    else if (currentSelected == "human")
      StartCoroutine(GenerateHumanCoroutine(promptInput.text));
    else if (currentSelected == "boat")
      StartCoroutine(GenerateBoatCoroutine(promptInput.text));
    else if (currentSelected == "bg")
      StartCoroutine(GenerateBgCoroutine(promptInput.text));
  }

  private IEnumerator GenerateFishCoroutine(string prompt)
  {
    if (gameManager.IsFishing) yield break;

    promptInput.text = string.Empty;
    UpdateStatus("이미지 생성 중...");
    fish.ReleaseFish();
    gameManager.IsFishing = true;

    float sizeRand = UnityEngine.Random.Range(0f, 1f);
    Vector2Int size = Vector2Int.zero;
    if (sizeRand < 0.5f)
    {
      size = small;
      gameManager.Size = "small";
    }
    else if (sizeRand < 0.8f)
    {
      size = medium;
      gameManager.Size = "medium";
      human.StartLightShaking();
    }
    else
    {
      size = large;
      gameManager.Size = "large";
      human.StartHeavyShaking();
    }

    // 요청 데이터 준비
    ImageRequest payload = new()
    {
      prompt = prompt,
      width = size.x,
      height = size.y,
      num_images = 1
    };

    AudioManager.I.PlaySFXSequence(speedUpFirst, speedUpLoop);

    ImageResponse response = null;
    yield return _apiClient.PostJson(
      _serverCfg.GenerateFishPath,
      payload,
      onOk => { response = JsonUtility.FromJson<ImageResponse>(onOk); },
      onError => { Debug.LogError(onError); });

    Texture2D texture = null;
    if (!string.IsNullOrEmpty(response.base64_image))
    {
      // Base64를 Texture2D로 변환
      Utils.Base64ToTexture2D(
        response.base64_image,
        result => texture = result,
        error => DestroyImmediate(error)
      );

      if (texture != null)
      {
        // 기존 SpriteRenderer에 Sprite 할당
        Utils.UpdateSpriteRenderer(fishRenderer, texture, prompt, sprite => DestroyImmediate(sprite));
        UpdateStatus($"스프라이트 업데이트 완료! ({texture.width}x{texture.height})");
      }
      else
      {
        UpdateStatus("이미지 변환 실패");
      }
    }
    else
    {
      UpdateStatus("서버 응답에 이미지가 없습니다.");
    }

    AudioManager.I.StopAllSFXSequences();

    gameManager.IsFishing = false;
    gameManager.Fish = prompt;
    human.StopShaking();
    fish.Init();
    AudioManager.I.PlaySFX(fishingSFX);
    reactionGenerator.GenerateReaction();
  }

  private IEnumerator GenerateHumanCoroutine(string prompt)
  {
    promptInput.text = string.Empty;

    UpdateStatus("이미지 생성 중...");

    // 요청 데이터 준비
    ImageRequest payload = new()
    {
      prompt = prompt,
    };

    ImageResponse response = null;
    yield return _apiClient.PostJson(
      _serverCfg.GenerateHumanPath,
      payload,
      onOk => { response = JsonUtility.FromJson<ImageResponse>(onOk); },
      onError => { Debug.LogError(onError); });

    Texture2D texture = null;
    if (!string.IsNullOrEmpty(response.base64_image))
    {
      // Base64를 Texture2D로 변환
      Utils.Base64ToTexture2D(
        response.base64_image,
        result => texture = result,
        error => DestroyImmediate(error)
      );

      if (texture != null)
      {
        // 기존 SpriteRenderer에 Sprite 할당
        Utils.UpdateSpriteRenderer(humanRenderer, texture, prompt, sprite => DestroyImmediate(sprite));
        UpdateStatus($"스프라이트 업데이트 완료! ({texture.width}x{texture.height})");
      }
      else
      {
        UpdateStatus("이미지 변환 실패");
      }
    }
    else
    {
      UpdateStatus("서버 응답에 이미지가 없습니다.");
    }

    AudioManager.I.PlaySFX(changeSkinSFX);

    gameManager.Human = prompt;
  }

  private IEnumerator GenerateBoatCoroutine(string prompt)
  {
    promptInput.text = string.Empty;

    UpdateStatus("이미지 생성 중...");

    // 요청 데이터 준비
    ImageRequest payload = new()
    {
      prompt = prompt,
    };

    ImageResponse response = null;
    yield return _apiClient.PostJson(
      _serverCfg.GenerateBoatPath,
      payload,
      onOk => { response = JsonUtility.FromJson<ImageResponse>(onOk); },
      onError => { Debug.LogError(onError); });

    Texture2D texture = null;
    if (!string.IsNullOrEmpty(response.base64_image))
    {
      // Base64를 Texture2D로 변환
      Utils.Base64ToTexture2D(
        response.base64_image,
        result => texture = result,
        error => DestroyImmediate(error)
      );

      if (texture != null)
      {
        // 기존 SpriteRenderer에 Sprite 할당
        Utils.UpdateSpriteRenderer(boatRenderer, texture, prompt, sprite => DestroyImmediate(sprite));
        UpdateStatus($"스프라이트 업데이트 완료! ({texture.width}x{texture.height})");
      }
      else
      {
        UpdateStatus("이미지 변환 실패");
      }
    }
    else
    {
      UpdateStatus("서버 응답에 이미지가 없습니다.");
    }

    AudioManager.I.PlaySFX(changeSkinSFX);

    gameManager.Boat = prompt;
  }

  private IEnumerator GenerateBgCoroutine(string prompt)
  {
    promptInput.text = string.Empty;

    UpdateStatus("이미지 생성 중...");

    // 요청 데이터 준비
    ImageRequest payload = new()
    {
      prompt = prompt,
    };

    ImageResponse response = null;
    yield return _apiClient.PostJson(
      _serverCfg.GenerateBackgroundPath,
      payload,
      onOk => { response = JsonUtility.FromJson<ImageResponse>(onOk); },
      onError => { Debug.LogError(onError); });

    Texture2D texture = null;
    if (!string.IsNullOrEmpty(response.base64_image))
    {
      // Base64를 Texture2D로 변환
      Utils.Base64ToTexture2D(
        response.base64_image,
        result => texture = result,
        error => DestroyImmediate(error)
      );

      if (texture != null)
      {
        // 기존 SpriteRenderer에 Sprite 할당
        Utils.UpdateSpriteRenderer(backgroundRenderer, texture, prompt, sprite => DestroyImmediate(sprite));
        UpdateStatus($"스프라이트 업데이트 완료! ({texture.width}x{texture.height})");
      }
      else
      {
        UpdateStatus("이미지 변환 실패");
      }
    }
    else
    {
      UpdateStatus("서버 응답에 이미지가 없습니다.");
    }

    AudioManager.I.PlaySFX(changeSkinSFX);

    gameManager.Location = prompt;
  }

  private void UpdateStatus(string message)
  {
    if (statusText != null)
    {
      statusText.text = message;
    }
    Debug.Log(message);
  }

  // 테스트용 메서드들 - Inspector에서 호출 가능
  [ContextMenu("Test Generate")]
  public void TestGenerate()
  {
    if (promptInput != null)
    {
      promptInput.text = "A beautiful landscape with mountains";
    }
    GenerateImage();
  }

  [ContextMenu("Clear Sprite")]
  public void ClearSprite()
  {
    if (fishRenderer != null && fishRenderer.sprite != null)
    {
      // 동적으로 생성된 스프라이트라면 정리
      if (fishRenderer.sprite.name.Contains("GeneratedImage"))
      {
        DestroyImmediate(fishRenderer.sprite);
      }
      fishRenderer.sprite = null;
      UpdateStatus("스프라이트가 제거되었습니다.");
    }
  }

  [ContextMenu("Get TestRenderer Info")]
  public void GetTestRendererInfo()
  {
    if (fishRenderer != null)
    {
      Debug.Log($"TestRenderer: {fishRenderer.name}");
      Debug.Log($"Current Sprite: {(fishRenderer.sprite != null ? fishRenderer.sprite.name : "None")}");
      Debug.Log($"Position: {fishRenderer.transform.position}");
      Debug.Log($"Scale: {fishRenderer.transform.localScale}");
    }
    else
    {
      Debug.LogWarning("testRenderer가 할당되지 않았습니다.");
    }
  }
}

// 스프라이트 메타데이터를 저장하는 컴포넌트 (기존과 동일)
[Serializable]
public class SpriteMetadata : MonoBehaviour
{
  [Header("Generated Image Info")]
  [TextArea(2, 4)]
  public string prompt;
  public string creationTime;
  public Vector2Int imageSize;

  void Start()
  {
    // 컴포넌트가 추가될 때 정보 로그
    Debug.Log($"Sprite Metadata - Prompt: '{prompt}', Size: {imageSize}, Created: {creationTime}");
  }
}
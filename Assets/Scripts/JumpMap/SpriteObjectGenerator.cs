using System;
using System.Collections;
using Network.ApiClient;
using Network.Models;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpriteObjectGenerator : MonoBehaviour
{
  [Header("UI References")]
  public TMP_InputField promptInput;
  public TMP_Text statusText;

  [Header("Sprite Settings")]
  public Transform spawnPoint1;
  public Transform spawnPoint2;
  public float spriteScale = 1.0f;
  public bool destroyPreviousSprite = true;
  public float pixelsPerUnit = 32f;

  [Header("Sounds")]
  [SerializeField] private AudioClip submitUI;
  [SerializeField] private AudioClip dropSFX;

  [Header("Server Settings")]
  [SerializeField] private AIServerConfig _serverCfg;

  private ApiClient _apiClient;
  private Vector2Int small = new(64, 64);
  private Vector2Int medium = new(128, 128);
  private Vector2Int large = new(256, 256);

  private bool isGenerating = false;
  private GameObject currentSpriteObject;
  private Rigidbody2D currentObjectRigid;

  private void Awake()
  {
    _apiClient = new(_serverCfg);
  }

  public void OnSubmit(InputAction.CallbackContext context)
  {
    if (isGenerating) return;
    if (context.performed == false) return;

    GenerateImage();
  }

  public void OnStopObject(InputAction.CallbackContext context)
  {
    if (context.performed == false) return;
    if (promptInput.isFocused) return;
    if (currentObjectRigid == null) return;

    currentObjectRigid.bodyType = RigidbodyType2D.Static;
  }

  private void GenerateImage()
  {
    if (isGenerating)
    {
      UpdateStatus("이미지 생성 중입니다. 잠시 기다려주세요.");
      return;
    }

    if (promptInput == null || string.IsNullOrEmpty(promptInput.text))
    {
      UpdateStatus("프롬프트를 입력해주세요.");
      return;
    }

    AudioManager.I.PlaySFX(submitUI);
    StartCoroutine(GenerateImageCoroutine(promptInput.text));
  }

  private IEnumerator GenerateImageCoroutine(string prompt)
  {
    isGenerating = true;
    promptInput.text = string.Empty;
    promptInput.DeactivateInputField();
    WebGLInputHelper.DeactivateInputField(promptInput);

    UpdateStatus("이미지 생성 중...");

    // 요청 데이터 준비
    ImageRequest payload = new()
    {
      prompt = prompt,
      width = small.x,
      height = small.y,
      num_images = 1
    };
    ImageResponse response = null;

    yield return _apiClient.PostJson(
      _serverCfg.GenerateImagePath,
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

      Transform spawnPoint = UnityEngine.Random.Range(0f, 1f) < 0.5 ? spawnPoint1 : spawnPoint2;
      if (texture != null)
      {
        // 기존 SpriteRenderer에 Sprite 할당
        Utils.CreateSprite(
          texture,
          prompt,
          spawnPoint,
          (spriteObejct) => currentSpriteObject = spriteObejct
        );
        UpdateStatus($"스프라이트 생성 완료! ({texture.width}x{texture.height})");
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

    // Rigidbody2D 추가 + physics
    Rigidbody2D rigid = currentSpriteObject.AddComponent<Rigidbody2D>();
    currentObjectRigid = rigid;
    float randomXForce = UnityEngine.Random.Range(-10f, 10f);
    float randomYForce = UnityEngine.Random.Range(-3f, 0f);
    Vector2 randForce = new(randomXForce, randomYForce);
    rigid.AddForce(randForce, ForceMode2D.Impulse);
    AudioManager.I.PlaySFX(dropSFX);

    isGenerating = false;
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

  [ContextMenu("Clear Current Sprite")]
  public void ClearCurrentSprite()
  {
    if (currentSpriteObject != null)
    {
      DestroyImmediate(currentSpriteObject);
      UpdateStatus("스프라이트가 제거되었습니다.");
    }
  }
}

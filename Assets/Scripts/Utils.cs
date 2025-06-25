using UnityEngine;
using System;

public static class Utils
{
  public static void UpdateSpriteRenderer(SpriteRenderer renderer, Texture2D texture, string prompt, Action<Sprite> OnExists)
  {
    // 이전 스프라이트가 있다면 정리
    if (renderer.sprite != null)
    {
      // 동적으로 생성된 스프라이트라면 메모리 정리
      if (renderer.sprite.name.Contains("GeneratedImage"))
      {
        OnExists?.Invoke(renderer.sprite);
      }
    }

    // Sprite 생성
    Sprite newSprite = Sprite.Create(
      texture,
      new Rect(0, 0, texture.width, texture.height),
      new Vector2(0.5f, 0.5f), // 중심점
      16f
  );

    // 스프라이트 이름 설정
    newSprite.name = $"GeneratedImage_{DateTime.Now:HHmmss}";

    // SpriteRenderer에 할당
    renderer.sprite = newSprite;

    // 메타데이터 저장 (renderer 오브젝트에)
    SpriteMetadata metadata = renderer.GetComponent<SpriteMetadata>();
    if (metadata == null)
    {
      metadata = renderer.gameObject.AddComponent<SpriteMetadata>();
    }

    metadata.prompt = prompt;
    metadata.creationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    metadata.imageSize = new Vector2Int(texture.width, texture.height);

    Debug.Log($"SpriteRenderer 업데이트됨: {newSprite.name}");
    Debug.Log($"프롬프트: '{prompt}', 크기: {texture.width}x{texture.height}");
  }

  public static void CreateSprite(Texture2D texture, string prompt, Transform spawnPoint, Action<GameObject> OnOk, float spriteScale = 1f)
  {
    // Sprite 생성
    Sprite sprite = Sprite.Create(
        texture,
        new Rect(0, 0, texture.width, texture.height),
        new Vector2(0.5f, 0.5f), // 중심점 (0.5, 0.5는 정중앙)
        32f // pixels per unit (작을수록 큰 스프라이트)
    );

    // GameObject 생성
    GameObject spriteObject = new GameObject($"GeneratedImage_{DateTime.Now:HHmmss}");

    // 위치 및 크기 설정
    spriteObject.transform.position = spawnPoint.position;
    spriteObject.transform.localScale = Vector3.one * spriteScale;

    // SpriteRenderer 추가
    SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
    spriteRenderer.sprite = sprite;

    // 정렬 레이어 설정 (옵션)
    spriteRenderer.sortingOrder = 1;

    // Collider 추가
    spriteRenderer.GenerateCollider();

    // 메타데이터 저장을 위한 컴포넌트 추가
    SpriteMetadata metadata = spriteObject.AddComponent<SpriteMetadata>();
    metadata.prompt = prompt;
    metadata.creationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    metadata.imageSize = new Vector2Int(texture.width, texture.height);

    // 현재 스프라이트 오브젝트 참조 저장
    OnOk?.Invoke(spriteObject);

    Debug.Log($"스프라이트 생성됨: {spriteObject.name} at {spriteObject.transform.position}");
    Debug.Log($"프롬프트: '{prompt}', 크기: {texture.width}x{texture.height}");
  }

  public static void Base64ToTexture2D(string base64String, Action<Texture2D> OnOk, Action<Texture2D> OnError)
  {
    try
    {
      // Base64 문자열을 바이트 배열로 변환
      byte[] imageData = Convert.FromBase64String(base64String);

      // Compression을 None으로 설정한 텍스처 생성
      Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

      // 압축 설정을 None으로 변경
      texture.Apply(false, false);

      // 이미지 데이터 로드
      if (texture.LoadImage(imageData))
      {
        // 텍스처 설정 최적화
        texture.filterMode = FilterMode.Point; // 픽셀 아트 스타일용, 필요에 따라 변경
        texture.wrapMode = TextureWrapMode.Clamp;

        // 변경사항 적용
        texture.Apply();

        OnOk?.Invoke(texture);
      }
      else
      {
        Debug.LogError("이미지 로드 실패");
        OnError?.Invoke(texture);
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Base64 변환 오류: {e.Message}");
    }
  }
}

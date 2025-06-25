using UnityEngine;

/// <summary>
/// 픽셀아트 전용 콜라이더 생성기
/// 픽셀 단위의 정확한 윤곽선 추출에 특화
/// </summary>
public static class SpriteColliderGenerator
{

  /// <summary>
  /// 픽셀아트 스프라이트에서 콜라이더를 생성합니다
  /// </summary>
  public static bool GenerateCollider(SpriteRenderer spriteRenderer)
  {
    if (spriteRenderer?.sprite == null)
    {
      Debug.LogWarning("SpriteRenderer or Sprite is null!");
      return false;
    }

    Sprite sprite = spriteRenderer.sprite;
    Texture2D texture = sprite.texture;
    if (!texture.isReadable)
    {
      texture = CreateReadableTexture(texture);
    }
    Color32[] pixels = texture.GetPixels32();
    foreach (var item in pixels)
    {
      Debug.Log(item.a);
    }

    Vector2[] points = new Vector2[pixels.Length];
    int pointCount = 0;
    for (int y = 0; y < texture.height; y++)
    {
      for (int x = 0; x < texture.width; x++)
      {
        int index = y * texture.width + x;
        if (pixels[index].a > 127) // 투명하지 않은 픽셀
        {
          points[pointCount] = new Vector2((x - 32f) / 32f, (y - 32f) / 32f);
          pointCount++;
        }
      }
    }
    PolygonCollider2D collider = spriteRenderer.gameObject.AddComponent<PolygonCollider2D>();

    // PolygonCollider2D에 점 설정
    collider.SetPath(0, points);

    return true;
  }

  private static Texture2D CreateReadableTexture(Texture2D source)
  {
    RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height);
    Graphics.Blit(source, renderTexture);

    RenderTexture previous = RenderTexture.active;
    RenderTexture.active = renderTexture;

    Texture2D readable = new Texture2D(source.width, source.height);
    readable.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
    readable.Apply();

    RenderTexture.active = previous;
    RenderTexture.ReleaseTemporary(renderTexture);

    return readable;
  }
}

/// <summary>
/// 픽셀아트 콜라이더 생성을 위한 확장 메서드
/// </summary>
public static class SpriteRendererExtensions
{
  /// <summary>
  /// 픽셀아트 콜라이더 생성
  /// </summary>
  public static bool GenerateCollider(this SpriteRenderer spriteRenderer)
  {
    return SpriteColliderGenerator.GenerateCollider(spriteRenderer);
  }
}
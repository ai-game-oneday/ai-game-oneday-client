using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 픽셀아트 전용 콜라이더 생성기
/// 픽셀 단위의 정확한 윤곽선 추출에 특화
/// </summary>
public static class PixelArtColliderGenerator
{
  [System.Serializable]
  public class PixelArtSettings
  {
    [Header("Basic Settings")]
    public bool usePolygonCollider = true;
    public int alphaThreshold = 200; // 픽셀아트용 높은 임계값

    [Header("Precision")]
    public float simplificationLevel = 0.01f; // 매우 정밀
    public bool enableCornerOptimization = true;
    public bool fillHoles = true; // 작은 구멍 메우기

    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showPixelGrid = false;

    // 프리셋들
    public static PixelArtSettings Precise()
    {
      return new PixelArtSettings
      {
        usePolygonCollider = true,
        alphaThreshold = 220,
        simplificationLevel = 0.005f,
        enableCornerOptimization = true,
        fillHoles = true,
        enableDebugLogs = false
      };
    }

    public static PixelArtSettings Fast()
    {
      return new PixelArtSettings
      {
        usePolygonCollider = true,
        alphaThreshold = 180,
        simplificationLevel = 0.02f,
        enableCornerOptimization = false,
        fillHoles = false,
        enableDebugLogs = false
      };
    }

    public static PixelArtSettings Debug()
    {
      return new PixelArtSettings
      {
        usePolygonCollider = true,
        alphaThreshold = 200,
        simplificationLevel = 0.01f,
        enableCornerOptimization = true,
        fillHoles = true,
        enableDebugLogs = true,
        showPixelGrid = true
      };
    }
  }

  /// <summary>
  /// 픽셀아트 스프라이트에서 콜라이더를 생성합니다
  /// </summary>
  public static bool GenerateCollider(SpriteRenderer spriteRenderer, PixelArtSettings settings = null)
  {
    if (spriteRenderer?.sprite == null)
    {
      Debug.LogWarning("SpriteRenderer or Sprite is null!");
      return false;
    }

    if (settings == null)
      settings = PixelArtSettings.Precise();

    // 기존 콜라이더 제거
    RemoveExistingColliders(spriteRenderer.gameObject);

    // 픽셀아트 윤곽선 추출
    var contourPoints = ExtractPixelArtContour(spriteRenderer, settings);

    if (contourPoints.Count < 3)
    {
      if (settings.enableDebugLogs)
        Debug.LogWarning($"Not enough points for {spriteRenderer.name}: {contourPoints.Count}");

      // 폴백: 간단한 박스 콜라이더
      return CreateFallbackCollider(spriteRenderer.gameObject, spriteRenderer.sprite, settings);
    }

    // 콜라이더 생성
    CreateCollider(spriteRenderer.gameObject, contourPoints, settings);

    if (settings.enableDebugLogs)
      Debug.Log($"Generated collider for {spriteRenderer.name} with {contourPoints.Count} points");

    return true;
  }

  /// <summary>
  /// 기본 설정으로 콜라이더 생성
  /// </summary>
  public static bool GenerateCollider(SpriteRenderer spriteRenderer)
  {
    return GenerateCollider(spriteRenderer, null);
  }

  #region Pixel Art Contour Extraction

  private static List<Vector2> ExtractPixelArtContour(SpriteRenderer spriteRenderer, PixelArtSettings settings)
  {
    Sprite sprite = spriteRenderer.sprite;

    // 픽셀 데이터 가져오기
    var pixelData = GetSpritePixelData(sprite);
    if (pixelData == null) return new List<Vector2>();

    if (settings.enableDebugLogs)
    {
      Debug.Log($"=== Processing {sprite.name} ===");
      Debug.Log($"Pixel data: {pixelData.width}x{pixelData.height} pixels");
      Debug.Log($"Start position: ({pixelData.startX}, {pixelData.startY})");
      Debug.Log($"Alpha threshold: {settings.alphaThreshold}");
      Debug.Log($"Sprite rect: {sprite.textureRect}");
      Debug.Log($"Sprite pivot: {sprite.pivot}");
    }

    // 픽셀 마스크 생성 (디버깅 정보 추가)
    bool[,] pixelMask = CreatePixelMask(pixelData, settings.alphaThreshold, settings.enableDebugLogs);

    // 작은 구멍 메우기
    if (settings.fillHoles)
    {
      pixelMask = FillSmallHoles(pixelMask, pixelData.width, pixelData.height);
    }

    // 외곽 윤곽선 추출
    var edgePixels = FindEdgePixels(pixelMask, pixelData.width, pixelData.height);

    if (settings.enableDebugLogs)
      Debug.Log($"Found {edgePixels.Count} edge pixels");

    if (edgePixels.Count == 0)
    {
      Debug.LogWarning("No edge pixels found! Check your alpha threshold.");
      return new List<Vector2>();
    }

    // 윤곽선 점들을 순서대로 연결
    var orderedPoints = OrderEdgePixels(edgePixels, pixelData.width, pixelData.height);

    if (settings.enableDebugLogs)
      Debug.Log($"Ordered {orderedPoints.Count} edge pixels");

    // 코너 최적화
    if (settings.enableCornerOptimization)
    {
      orderedPoints = OptimizeCorners(orderedPoints);
      if (settings.enableDebugLogs)
        Debug.Log($"After corner optimization: {orderedPoints.Count} points");
    }

    // 단순화
    if (settings.simplificationLevel > 0)
    {
      orderedPoints = SimplifyContour(orderedPoints, settings.simplificationLevel);
      if (settings.enableDebugLogs)
        Debug.Log($"After simplification: {orderedPoints.Count} points");
    }

    // 로컬 좌표로 변환
    var localPoints = ConvertToLocalCoordinates(orderedPoints, sprite, pixelData.width, pixelData.height);

    if (settings.enableDebugLogs)
      Debug.Log($"Final contour: {localPoints.Count} points");

    return localPoints;
  }

  private class PixelData
  {
    public Color32[] pixels;
    public int width;
    public int height;
    public int startX;
    public int startY;
  }

  private static PixelData GetSpritePixelData(Sprite sprite)
  {
    Texture2D texture = sprite.texture;
    if (!texture.isReadable)
    {
      texture = CreateReadableTexture(texture);
    }

    var spriteRect = sprite.textureRect;
    int startX = Mathf.RoundToInt(spriteRect.x);
    int startY = Mathf.RoundToInt(spriteRect.y);
    int width = Mathf.RoundToInt(spriteRect.width);
    int height = Mathf.RoundToInt(spriteRect.height);

    try
    {
      Color32[] pixels = texture.GetPixels32();

      return new PixelData
      {
        pixels = pixels,
        width = width,
        height = height,
        startX = startX,
        startY = startY
      };
    }
    catch
    {
      Debug.LogError($"Failed to read pixels from {sprite.name}");
      return null;
    }
    finally
    {
      if (texture != sprite.texture)
      {
        Object.DestroyImmediate(texture);
      }
    }
  }

  private static bool[,] CreatePixelMask(PixelData pixelData, int threshold, bool enableDebug = false)
  {
    bool[,] mask = new bool[pixelData.width, pixelData.height];
    int validPixels = 0;
    int totalPixels = pixelData.width * pixelData.height;

    for (int y = 0; y < pixelData.height; y++)
    {
      for (int x = 0; x < pixelData.width; x++)
      {
        int index = y * pixelData.width + x;
        if (index < pixelData.pixels.Length)
        {
          bool isValid = pixelData.pixels[index].a > threshold;
          mask[x, y] = isValid;
          if (isValid) validPixels++;
        }
      }
    }

    if (enableDebug)
    {
      Debug.Log($"Pixel mask created: {validPixels}/{totalPixels} valid pixels ({(float)validPixels / totalPixels * 100:F1}%)");

      // 첫 번째와 마지막 줄의 픽셀 상태 확인
      string firstRow = "First row: ";
      string lastRow = "Last row: ";
      for (int x = 0; x < Mathf.Min(10, pixelData.width); x++)
      {
        firstRow += mask[x, 0] ? "■" : "□";
        lastRow += mask[x, pixelData.height - 1] ? "■" : "□";
      }
      Debug.Log(firstRow);
      Debug.Log(lastRow);
    }

    return mask;
  }

  private static bool[,] FillSmallHoles(bool[,] mask, int width, int height)
  {
    bool[,] result = (bool[,])mask.Clone();

    // 1픽셀 구멍 메우기
    for (int y = 1; y < height - 1; y++)
    {
      for (int x = 1; x < width - 1; x++)
      {
        if (!mask[x, y]) // 빈 픽셀인 경우
        {
          // 4방향이 모두 채워져 있으면 구멍을 메움
          if (mask[x - 1, y] && mask[x + 1, y] && mask[x, y - 1] && mask[x, y + 1])
          {
            result[x, y] = true;
          }
        }
      }
    }

    return result;
  }

  private static List<Vector2> FindEdgePixels(bool[,] mask, int width, int height)
  {
    List<Vector2> edgePixels = new List<Vector2>();

    for (int y = 0; y < height; y++)
    {
      for (int x = 0; x < width; x++)
      {
        if (mask[x, y] && IsEdgePixel(mask, x, y, width, height))
        {
          edgePixels.Add(new Vector2(x, y));
        }
      }
    }

    return edgePixels;
  }

  private static bool IsEdgePixel(bool[,] mask, int x, int y, int width, int height)
  {
    // 경계에 있거나 인접 4방향 중 하나라도 빈 곳이 있으면 경계 픽셀
    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
      return true;

    return !mask[x - 1, y] || !mask[x + 1, y] || !mask[x, y - 1] || !mask[x, y + 1];
  }

  private static List<Vector2> OrderEdgePixels(List<Vector2> edgePixels, int width, int height)
  {
    if (edgePixels.Count < 3) return edgePixels;

    // 가장 왼쪽 아래 점부터 시작
    Vector2 startPixel = edgePixels.OrderBy(p => p.y).ThenBy(p => p.x).First();

    List<Vector2> orderedPixels = new List<Vector2>();
    HashSet<Vector2> visited = new HashSet<Vector2>();

    Vector2 current = startPixel;
    orderedPixels.Add(current);
    visited.Add(current);

    // 시계방향으로 윤곽선 추적
    while (orderedPixels.Count < edgePixels.Count)
    {
      Vector2 next = FindNextEdgePixel(current, edgePixels, visited);
      if (next == Vector2.zero) break;

      orderedPixels.Add(next);
      visited.Add(next);
      current = next;
    }

    return orderedPixels;
  }

  private static Vector2 FindNextEdgePixel(Vector2 current, List<Vector2> edgePixels, HashSet<Vector2> visited)
  {
    // 8방향 우선순위 (시계방향)
    Vector2[] directions = {
            new Vector2(1, 0),   // 오른쪽
            new Vector2(1, -1),  // 오른쪽 아래
            new Vector2(0, -1),  // 아래
            new Vector2(-1, -1), // 왼쪽 아래
            new Vector2(-1, 0),  // 왼쪽
            new Vector2(-1, 1),  // 왼쪽 위
            new Vector2(0, 1),   // 위
            new Vector2(1, 1)    // 오른쪽 위
        };

    foreach (var dir in directions)
    {
      Vector2 candidate = current + dir;

      if (edgePixels.Contains(candidate) && !visited.Contains(candidate))
      {
        return candidate;
      }
    }

    return Vector2.zero;
  }

  private static List<Vector2> OptimizeCorners(List<Vector2> points)
  {
    if (points.Count < 4) return points;

    List<Vector2> optimized = new List<Vector2>();

    for (int i = 0; i < points.Count; i++)
    {
      Vector2 prev = points[(i - 1 + points.Count) % points.Count];
      Vector2 curr = points[i];
      Vector2 next = points[(i + 1) % points.Count];

      // 직선상에 있지 않은 점만 유지 (코너 점)
      if (!IsCollinear(prev, curr, next))
      {
        optimized.Add(curr);
      }
    }

    return optimized.Count >= 3 ? optimized : points;
  }

  private static bool IsCollinear(Vector2 a, Vector2 b, Vector2 c)
  {
    // 외적을 이용한 직선 판정 (허용 오차 포함)
    float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    return Mathf.Abs(cross) < 0.1f;
  }

  private static List<Vector2> SimplifyContour(List<Vector2> points, float tolerance)
  {
    if (points.Count < 3) return points;
    return DouglasPeucker(points, 0, points.Count - 1, tolerance);
  }

  private static List<Vector2> DouglasPeucker(List<Vector2> points, int start, int end, float tolerance)
  {
    float maxDistance = 0;
    int maxIndex = 0;

    for (int i = start + 1; i < end; i++)
    {
      float distance = PointToLineDistance(points[i], points[start], points[end]);
      if (distance > maxDistance)
      {
        maxDistance = distance;
        maxIndex = i;
      }
    }

    if (maxDistance > tolerance)
    {
      var left = DouglasPeucker(points, start, maxIndex, tolerance);
      var right = DouglasPeucker(points, maxIndex, end, tolerance);

      left.RemoveAt(left.Count - 1);
      left.AddRange(right);
      return left;
    }

    return new List<Vector2> { points[start], points[end] };
  }

  private static float PointToLineDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
  {
    Vector2 line = lineEnd - lineStart;
    Vector2 pointVec = point - lineStart;

    float lineLength = line.magnitude;
    if (lineLength == 0) return pointVec.magnitude;

    float projection = Vector2.Dot(pointVec, line) / lineLength;
    projection = Mathf.Clamp(projection, 0, lineLength);

    Vector2 closest = lineStart + line.normalized * projection;
    return Vector2.Distance(point, closest);
  }

  private static List<Vector2> ConvertToLocalCoordinates(List<Vector2> pixelPoints, Sprite sprite, int width, int height)
  {
    List<Vector2> localPoints = new List<Vector2>();

    // 스프라이트의 실제 정보
    Vector2 spriteSize = sprite.bounds.size;
    Vector2 pivot = sprite.pivot;
    Vector2 pixelsPerUnit = new Vector2(sprite.pixelsPerUnit, sprite.pixelsPerUnit);

    // 디버깅용 로그
    Debug.Log($"Sprite Info:");
    Debug.Log($"- Bounds Size: {spriteSize}");
    Debug.Log($"- Pivot: {pivot}");
    Debug.Log($"- Pixels Per Unit: {sprite.pixelsPerUnit}");
    Debug.Log($"- Texture Rect: {sprite.textureRect}");
    Debug.Log($"- Texture Size: {width}x{height}");

    foreach (var pixelPos in pixelPoints)
    {
      // 방법 1: 직접 픽셀을 Unity 유닛으로 변환
      Vector2 localPos = new Vector2(
          (pixelPos.x - pivot.x) / sprite.pixelsPerUnit,
          (pixelPos.y - pivot.y) / sprite.pixelsPerUnit
      );

      localPoints.Add(localPos);
    }

    // 결과 점들의 범위 로깅
    if (localPoints.Count > 0)
    {
      float minX = localPoints.Min(p => p.x);
      float maxX = localPoints.Max(p => p.x);
      float minY = localPoints.Min(p => p.y);
      float maxY = localPoints.Max(p => p.y);
      Debug.Log($"Generated collider bounds: ({minX:F3}, {minY:F3}) to ({maxX:F3}, {maxY:F3})");
      Debug.Log($"Expected sprite bounds: ({-spriteSize.x * 0.5f:F3}, {-spriteSize.y * 0.5f:F3}) to ({spriteSize.x * 0.5f:F3}, {spriteSize.y * 0.5f:F3})");
    }

    return localPoints;
  }

  #endregion

  #region Collider Creation

  private static void CreateCollider(GameObject gameObject, List<Vector2> points, PixelArtSettings settings)
  {
    if (settings.usePolygonCollider)
    {
      CreatePixelArtPolygonCollider(gameObject, points);
    }
    else
    {
      CreatePixelArtEdgeCollider(gameObject, points);
    }
  }

  private static void CreatePixelArtPolygonCollider(GameObject gameObject, List<Vector2> points)
  {
    // 시계 반대 방향 확인
    if (IsClockwise(points))
    {
      points.Reverse();
    }

    PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();
    collider.points = points.ToArray();
  }

  private static void CreatePixelArtEdgeCollider(GameObject gameObject, List<Vector2> points)
  {
    EdgeCollider2D collider = gameObject.AddComponent<EdgeCollider2D>();

    // 닫힌 루프 생성
    Vector2[] edgePoints = new Vector2[points.Count + 1];
    for (int i = 0; i < points.Count; i++)
    {
      edgePoints[i] = points[i];
    }
    edgePoints[points.Count] = points[0];

    collider.points = edgePoints;
  }

  private static bool IsClockwise(List<Vector2> points)
  {
    float sum = 0;
    for (int i = 0; i < points.Count; i++)
    {
      Vector2 current = points[i];
      Vector2 next = points[(i + 1) % points.Count];
      sum += (next.x - current.x) * (next.y + current.y);
    }
    return sum > 0;
  }

  private static bool CreateFallbackCollider(GameObject gameObject, Sprite sprite, PixelArtSettings settings)
  {
    Debug.Log($"Creating fallback box collider for {gameObject.name}");

    if (settings.usePolygonCollider)
    {
      // 박스 형태 PolygonCollider2D
      PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();
      Vector2 size = sprite.bounds.size;

      Vector2[] boxPoints = {
                new Vector2(-size.x * 0.5f, -size.y * 0.5f),
                new Vector2(size.x * 0.5f, -size.y * 0.5f),
                new Vector2(size.x * 0.5f, size.y * 0.5f),
                new Vector2(-size.x * 0.5f, size.y * 0.5f)
            };

      collider.points = boxPoints;
    }
    else
    {
      // BoxCollider2D
      BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
      collider.size = sprite.bounds.size;
    }

    return true;
  }

  #endregion

  #region Debug and Test Methods

  /// <summary>
  /// 스프라이트 정보를 자세히 분석합니다
  /// </summary>
  public static void AnalyzeSprite(SpriteRenderer spriteRenderer)
  {
    if (spriteRenderer?.sprite == null)
    {
      Debug.LogError("SpriteRenderer or Sprite is null!");
      return;
    }

    Sprite sprite = spriteRenderer.sprite;

    Debug.Log($"=== SPRITE ANALYSIS: {sprite.name} ===");
    Debug.Log($"Texture: {sprite.texture.name} ({sprite.texture.width}x{sprite.texture.height})");
    Debug.Log($"Texture Rect: {sprite.textureRect}");
    Debug.Log($"Bounds: {sprite.bounds}");
    Debug.Log($"Pivot: {sprite.pivot}");
    Debug.Log($"Pixels Per Unit: {sprite.pixelsPerUnit}");
    Debug.Log($"Readable: {sprite.texture.isReadable}");

    // 픽셀 데이터 분석
    var pixelData = GetSpritePixelData(sprite);
    if (pixelData != null)
    {
      Debug.Log($"Extracted pixels: {pixelData.width}x{pixelData.height}");

      // 알파 값 분포 확인
      var alphaStats = AnalyzeAlphaDistribution(pixelData.pixels);
      Debug.Log($"Alpha distribution: Min={alphaStats.min}, Max={alphaStats.max}, Avg={alphaStats.average:F1}");
      Debug.Log($"Transparent pixels: {alphaStats.transparentCount}/{pixelData.pixels.Length}");
      Debug.Log($"Semi-transparent pixels: {alphaStats.semiTransparentCount}/{pixelData.pixels.Length}");
      Debug.Log($"Opaque pixels: {alphaStats.opaqueCount}/{pixelData.pixels.Length}");
    }
  }

  /// <summary>
  /// 여러 임계값으로 테스트해서 최적값을 찾습니다
  /// </summary>
  public static void TestThresholds(SpriteRenderer spriteRenderer, int[] thresholds = null)
  {
    if (thresholds == null)
      thresholds = new int[] { 0, 50, 100, 150, 200, 250 };

    Debug.Log($"=== TESTING THRESHOLDS FOR {spriteRenderer.sprite.name} ===");

    foreach (int threshold in thresholds)
    {
      var settings = new PixelArtSettings
      {
        alphaThreshold = threshold,
        enableDebugLogs = false,
        enableCornerOptimization = false,
        simplificationLevel = 0
      };

      var contour = ExtractPixelArtContour(spriteRenderer, settings);
      Debug.Log($"Threshold {threshold}: {contour.Count} contour points");
    }
  }

  private struct AlphaStats
  {
    public int min, max;
    public float average;
    public int transparentCount;
    public int semiTransparentCount;
    public int opaqueCount;
  }

  private static AlphaStats AnalyzeAlphaDistribution(Color32[] pixels)
  {
    var stats = new AlphaStats();
    stats.min = 255;
    stats.max = 0;

    int alphaSum = 0;

    foreach (var pixel in pixels)
    {
      int alpha = pixel.a;
      alphaSum += alpha;

      if (alpha < stats.min) stats.min = alpha;
      if (alpha > stats.max) stats.max = alpha;

      if (alpha == 0) stats.transparentCount++;
      else if (alpha < 255) stats.semiTransparentCount++;
      else stats.opaqueCount++;
    }

    stats.average = (float)alphaSum / pixels.Length;
    return stats;
  }

  #endregion

  #region Utility Methods

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

  private static void RemoveExistingColliders(GameObject gameObject)
  {
    var colliders = gameObject.GetComponents<Collider2D>();
    for (int i = colliders.Length - 1; i >= 0; i--)
    {
      if (Application.isPlaying)
        Object.Destroy(colliders[i]);
      else
        Object.DestroyImmediate(colliders[i]);
    }
  }

  #endregion
}

/// <summary>
/// 픽셀아트 콜라이더 생성을 위한 확장 메서드
/// </summary>
public static class PixelArtExtensions
{
  /// <summary>
  /// 픽셀아트 콜라이더 생성
  /// </summary>
  public static bool GeneratePixelArtCollider(this SpriteRenderer spriteRenderer, PixelArtColliderGenerator.PixelArtSettings settings = null)
  {
    return PixelArtColliderGenerator.GenerateCollider(spriteRenderer, settings);
  }
}
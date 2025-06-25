using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MaintainAspectRatio : MonoBehaviour
{
  // 고정할 목표 비율
  private const float targetAspect = 16f / 9f;

  Camera cam;
  int lastWidth, lastHeight;

  void Awake()
  {
    cam = GetComponent<Camera>();
  }

  void Start()
  {
    // 초기 한 번 적용
    ApplyAspect();
  }

  void Update()
  {
    // 화면 크기가 변경되었으면 다시 계산
    if (Screen.width != lastWidth || Screen.height != lastHeight)
    {
      ApplyAspect();
    }
  }

  void ApplyAspect()
  {
    lastWidth = Screen.width;
    lastHeight = Screen.height;

    float windowAspect = (float)lastWidth / lastHeight;
    float scaleHeight = windowAspect / targetAspect;

    if (scaleHeight < 1.0f)
    {
      // 레터박스 (위·아래 검은 띠)
      cam.rect = new Rect(
          0,
          (1.0f - scaleHeight) / 2.0f,
          1.0f,
          scaleHeight
      );
    }
    else
    {
      // 필러박스 (좌·우 검은 띠)
      float scaleWidth = 1.0f / scaleHeight;
      cam.rect = new Rect(
          (1.0f - scaleWidth) / 2.0f,
          0,
          scaleWidth,
          1.0f
      );
    }
  }
}

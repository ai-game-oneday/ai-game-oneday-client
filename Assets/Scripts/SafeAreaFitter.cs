using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
  [SerializeField] private RectTransform canvas;

  private RectTransform t;

  private void Awake()
  {
    t = GetComponent<RectTransform>();
  }
  private void LateUpdate()
  {

    if (canvas.sizeDelta.y < canvas.sizeDelta.x)
    {
      float safeArea = canvas.sizeDelta.y;

      // float safeAreaHeight = safeArea;
      // float rectHeight = t.sizeDelta.y;
      // float ratio = safeAreaHeight / rectHeight;

      t.sizeDelta = new Vector2(safeArea * (16f / 9f), safeArea);
    }
    else
    {
      float safeArea = canvas.sizeDelta.x;

      // float safeAreaHeight = safeArea;
      // float rectHeight = t.sizeDelta.y;
      // float ratio = safeAreaHeight / rectHeight;

      t.sizeDelta = new Vector2(safeArea, safeArea * (9f / 16f));
    }
  }
}

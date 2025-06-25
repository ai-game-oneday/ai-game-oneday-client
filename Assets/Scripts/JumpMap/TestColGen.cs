using UnityEngine;

public class TestColGen : MonoBehaviour
{
  [SerializeField] private SpriteRenderer spriteRenderer;

  private void Start()
  {
    spriteRenderer.GenerateCollider();
  }
}

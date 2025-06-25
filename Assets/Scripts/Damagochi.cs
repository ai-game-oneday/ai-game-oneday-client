using System.Collections;
using UnityEngine;

public class Damagochi : MonoBehaviour
{
  [SerializeField] private GameManager gameManager;
  [SerializeField] private SpriteRenderer human;
  [SerializeField] private SpriteRenderer boat;
  [SerializeField] private float minInterval = 10f;
  [SerializeField] private float maxInterval = 20f;

  private float currentInterval;
  private float deltaInterval = 0f;
  private bool isMoving = false;
  private float speed = 0.5f;

  private Vector2 destination = Vector2.zero;

  private void Start()
  {
    SetInterval();
  }

  public void StopMove()
  {
    SetInterval();
    isMoving = false;
    human.flipX = 0f > transform.position.x;
    boat.flipX = 0f > transform.position.x;
  }

  private void SetInterval()
  {
    currentInterval = UnityEngine.Random.Range(minInterval, maxInterval);
  }

  private void Update()
  {
    if (isMoving || gameManager.IsFishing) return;

    if (deltaInterval < currentInterval)
    {
      deltaInterval += Time.deltaTime;
      return;
    }

    deltaInterval = 0f;
    float rand = UnityEngine.Random.Range(0f, 1f);
    float destX = 4.5f;
    if (rand < 0.5)
    {
      destX = -4.5f;
    }
    destination.x = destX;
    human.flipX = destination.x > transform.position.x;
    boat.flipX = destination.x > transform.position.x;

    isMoving = true;
    StartCoroutine(Moving());
  }

  private IEnumerator Moving()
  {
    while (isMoving && !gameManager.IsFishing)
    {
      transform.position = Vector2.MoveTowards(transform.position, destination, Time.deltaTime * speed);

      if (Mathf.Abs(transform.position.x - destination.x) < 0.1f)
      {
        StopMove();
        yield break;
      }

      yield return null;
    }

    StopMove();
  }
}

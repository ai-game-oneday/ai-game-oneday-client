using System.Collections;
using UnityEngine;

public class Fish : MonoBehaviour
{
  [Header("Caught Animation Settings")]
  [SerializeField] private float jumpHeight = 4.0f;           // 튀어오르는 높이
  [SerializeField] private float jumpDuration = 0.8f;        // 튀어오르는 시간
  [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

  [Header("Dangling Animation Settings")]
  [SerializeField] private float swingAngle = 15.0f;          // 좌우로 흔들리는 각도
  [SerializeField] private float swingSpeed = 2.0f;          // 흔들리는 속도
  [SerializeField] private float bobHeight = 0.3f;           // 위아래로 움직이는 높이
  [SerializeField] private float bobSpeed = 0.5f;            // 위아래 움직임 속도

  [Header("Physics Settings")]
  [SerializeField] private float damping = 0.98f;            // 감쇠 계수
  [SerializeField] private float rotationDamping = 0.95f;    // 회전 감쇠

  private Vector3 originalPosition;
  private Vector3 caughtPosition;
  private Quaternion originalRotation;
  private bool isCaught = false;
  private float timeOffset;

  // 애니메이션 변수
  private float swingTime = 0f;
  private float currentSwingIntensity = 1f;
  private float currentBobIntensity = 1f;

  void Start()
  {
    originalPosition = transform.position;
    originalRotation = transform.rotation;
    timeOffset = Mathf.PI;
  }

  void Update()
  {
    if (isCaught)
    {
      UpdateDanglingMotion();
    }
  }

  /// <summary>
  /// 물고기가 낚싯대에 걸렸을 때 호출되는 함수
  /// </summary>
  public void Init()
  {
    if (!isCaught)
    {
      StartCoroutine(CaughtSequence());
    }
  }

  /// <summary>
  /// 물고기가 잡힌 후의 전체 시퀀스
  /// </summary>
  private IEnumerator CaughtSequence()
  {
    isCaught = true;

    // 1단계: 튀어오르기
    yield return StartCoroutine(JumpUp());

    // 2단계: 매달린 위치 설정
    SetCaughtPosition();

    // 3단계: 대롱대롱 매달리기 시작
    StartDanglingMotion();
  }

  /// <summary>
  /// 물고기가 튀어오르는 애니메이션
  /// </summary>
  private IEnumerator JumpUp()
  {
    Vector3 startPos = transform.position;
    Vector3 targetPos = startPos + Vector3.up * jumpHeight;

    float elapsedTime = 0f;

    // 튀어오르는 동안 약간의 회전도 추가
    Quaternion startRot = transform.rotation;
    Quaternion targetRot = startRot * Quaternion.Euler(0, 0, Random.Range(-30f, 30f));

    while (elapsedTime < jumpDuration)
    {
      elapsedTime += Time.deltaTime;
      float progress = elapsedTime / jumpDuration;

      // 점프 커브 적용
      float curveValue = jumpCurve.Evaluate(progress);

      // 위치 보간
      Vector3 currentPos = Vector3.Lerp(startPos, targetPos, curveValue);

      // 포물선 효과 추가 (더 자연스러운 점프)
      float parabolaHeight = 4f * jumpHeight * progress * (1f - progress);
      currentPos.y = startPos.y + parabolaHeight;

      transform.position = currentPos;

      // 회전 보간
      transform.rotation = Quaternion.Lerp(startRot, targetRot, progress);

      yield return null;
    }
  }

  /// <summary>
  /// 매달린 위치 설정
  /// </summary>
  private void SetCaughtPosition()
  {
    caughtPosition = transform.position;
    swingTime = 0f;
    currentSwingIntensity = 1f;
    currentBobIntensity = 1f;
  }

  /// <summary>
  /// 대롱대롱 매달리는 움직임 시작
  /// </summary>
  private void StartDanglingMotion()
  {
    // 초기 강한 움직임에서 점진적으로 약해짐
    currentSwingIntensity = 1f;
    currentBobIntensity = 1f;
  }

  /// <summary>
  /// 매 프레임 호출되는 대롱대롱 움직임 업데이트
  /// </summary>
  private void UpdateDanglingMotion()
  {
    swingTime += Time.deltaTime;

    // 감쇠 적용 (시간이 지날수록 움직임이 약해짐)
    currentSwingIntensity *= damping;
    currentBobIntensity *= damping;

    // 좌우 흔들림 (진자 운동)
    float swingX = Mathf.Sin(swingTime * swingSpeed + timeOffset) * swingAngle * currentSwingIntensity;

    // 위아래 움직임 (살짝 통통 튀는 느낌)
    float bobY = Mathf.Sin(swingTime * bobSpeed + timeOffset) * bobHeight * currentBobIntensity;

    // 회전 적용 (물고기가 좌우로 기울어짐)
    float rotationZ = swingX * 0.5f; // 좌우 움직임에 따른 기울어짐

    // 위치 업데이트
    Vector3 newPosition = caughtPosition;
    newPosition.x += swingX * 0.01f; // 실제 위치도 살짝 움직임
    newPosition.y += bobY;

    transform.position = newPosition;

    // 회전 업데이트 (원래 회전에 흔들림 추가)
    Quaternion swingRotation = Quaternion.Euler(0, 0, rotationZ);
    transform.rotation = originalRotation * swingRotation;

    // 회전 감쇠
    transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, (1f - rotationDamping) * Time.deltaTime);
  }

  /// <summary>
  /// 물고기를 원래 상태로 되돌리기 (릴리즈)
  /// </summary>
  [ContextMenu("Release Fish")]
  public void ReleaseFish()
  {
    if (isCaught)
    {
      StartCoroutine(ReleaseSequence());
    }
  }

  /// <summary>
  /// 물고기 릴리즈 시퀀스
  /// </summary>
  private IEnumerator ReleaseSequence()
  {
    isCaught = false;

    float releaseTime = 1f;
    float elapsedTime = 0f;

    Vector3 startPos = transform.position;
    Quaternion startRot = transform.rotation;

    while (elapsedTime < releaseTime)
    {
      elapsedTime += Time.deltaTime;
      float progress = elapsedTime / releaseTime;

      // 원래 위치와 회전으로 부드럽게 복귀
      transform.position = Vector3.Lerp(startPos, originalPosition, progress);
      transform.rotation = Quaternion.Lerp(startRot, originalRotation, progress);

      yield return null;
    }

    transform.position = originalPosition;
    transform.rotation = originalRotation;
  }

  /// <summary>
  /// 흔들림 강도를 외부에서 설정할 수 있는 함수
  /// </summary>
  public void SetSwingIntensity(float intensity)
  {
    currentSwingIntensity = Mathf.Clamp01(intensity);
    currentBobIntensity = Mathf.Clamp01(intensity);
  }

  /// <summary>
  /// 현재 잡힌 상태인지 확인
  /// </summary>
  public bool IsCaught()
  {
    return isCaught;
  }

  // 테스트용 메서드들
  [ContextMenu("Test Init")]
  private void TestInit()
  {
    Init();
  }

  [ContextMenu("Reset Position")]
  private void ResetPosition()
  {
    isCaught = false;
    transform.position = originalPosition;
    transform.rotation = originalRotation;
  }
}
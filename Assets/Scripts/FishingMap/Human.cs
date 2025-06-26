using System.Collections;
using UnityEngine;

public class Human : MonoBehaviour
{
  [Header("Shaking Animation Settings")]
  [SerializeField] private float lightShakeIntensity = 0.05f;     // 약한 떨림 강도
  [SerializeField] private float heavyShakeIntensity = 0.15f;     // 강한 떨림 강도
  [SerializeField] private float lightShakeSpeed = 8.0f;          // 약한 떨림 속도
  [SerializeField] private float heavyShakeSpeed = 15.0f;         // 강한 떨림 속도

  [Header("Shake Pattern Settings")]
  [SerializeField] private AnimationCurve shakeIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.3f);
  [SerializeField] private bool useRandomShake = true;            // 랜덤 떨림 사용 여부

  private Vector3 originalPosition;
  private Quaternion originalRotation;
  private bool isShaking = false;
  private Coroutine currentShakeCoroutine;

  // 떨림 상태 열거형
  public enum ShakeIntensity
  {
    Light,  // 가벼운 떨림 (집중할 때)
    Heavy   // 강한 떨림 (힘을 많이 줄 때)
  }

  void Start()
  {
    SetOriginalPosition();
  }

  private void SetOriginalPosition()
  {
    originalPosition = transform.localPosition;
    originalRotation = transform.localRotation;
  }

  /// <summary>
  /// 가벼운 떨림 - 물고기를 기다리거나 집중할 때
  /// </summary>
  public void StartLightShaking()
  {
    StartShaking(ShakeIntensity.Light);
  }

  /// <summary>
  /// 강한 떨림 - 큰 물고기와 힘겨루기 할 때
  /// </summary>
  public void StartHeavyShaking()
  {
    StartShaking(ShakeIntensity.Heavy);
  }

  /// <summary>
  /// 떨림 시작 (통합 함수) - StopShaking 호출까지 계속 반복
  /// </summary>
  public void StartShaking(ShakeIntensity intensity)
  {
    // 이미 떨고 있다면 중지하고 새로 시작
    if (isShaking)
    {
      StopShaking();
    }

    SetOriginalPosition();
    switch (intensity)
    {
      case ShakeIntensity.Light:
        currentShakeCoroutine = StartCoroutine(ContinuousLightShakeRoutine());
        break;
      case ShakeIntensity.Heavy:
        currentShakeCoroutine = StartCoroutine(ContinuousHeavyShakeRoutine());
        break;
    }
  }

  /// <summary>
  /// 가벼운 떨림 루틴 - 일정 주기로 계속 반복
  /// </summary>
  private IEnumerator ContinuousLightShakeRoutine()
  {
    isShaking = true;

    while (isShaking)
    {
      // 한 번의 떨림 수행
      yield return StartCoroutine(SingleLightShakePattern());

      // 다음 떨림까지 잠시 휴식
      yield return new WaitForSeconds(0.2f);
    }
  }

  /// <summary>
  /// 강한 떨림 루틴 - 일정 주기로 계속 반복
  /// </summary>
  private IEnumerator ContinuousHeavyShakeRoutine()
  {
    isShaking = true;

    while (isShaking)
    {
      // 한 번의 떨림 수행
      yield return StartCoroutine(SingleHeavyShakePattern());

      // 다음 떨림까지 잠시 휴식
      yield return new WaitForSeconds(0.1f);
    }
  }

  /// <summary>
  /// 단일 가벼운 떨림 패턴
  /// </summary>
  private IEnumerator SingleLightShakePattern()
  {
    float patternDuration = 0.8f;
    float elapsedTime = 0f;

    while (elapsedTime < patternDuration && isShaking)
    {
      elapsedTime += Time.deltaTime;
      float progress = elapsedTime / patternDuration;

      // 강도 커브 적용 (시작과 끝에서 약해짐)
      float currentIntensity = shakeIntensityCurve.Evaluate(progress) * lightShakeIntensity;

      // 부드러운 사인파 기반 떨림
      float shakeX = Mathf.Sin(Time.time * lightShakeSpeed) * currentIntensity;
      float shakeY = Mathf.Sin(Time.time * lightShakeSpeed * 1.2f) * currentIntensity * 0.5f;

      // 약간의 랜덤 노이즈 추가
      if (useRandomShake)
      {
        shakeX += Random.Range(-currentIntensity * 0.3f, currentIntensity * 0.3f);
        shakeY += Random.Range(-currentIntensity * 0.2f, currentIntensity * 0.2f);
      }

      // 위치 적용
      Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0);
      transform.localPosition = originalPosition + shakeOffset;

      // 미세한 회전 떨림
      float rotationShake = Mathf.Sin(Time.time * lightShakeSpeed * 0.8f) * currentIntensity * 10f;
      transform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationShake);

      yield return null;
    }
  }

  /// <summary>
  /// 단일 강한 떨림 패턴
  /// </summary>
  private IEnumerator SingleHeavyShakePattern()
  {
    float patternDuration = 0.6f;
    float elapsedTime = 0f;
    float burstStartTime = Random.Range(0.1f, 0.3f); // 랜덤한 시점에 버스트

    while (elapsedTime < patternDuration && isShaking)
    {
      elapsedTime += Time.deltaTime;
      float progress = elapsedTime / patternDuration;

      // 강도 커브 적용
      float baseIntensity = shakeIntensityCurve.Evaluate(progress) * heavyShakeIntensity;

      // 버스트 패턴 (특정 시점에서 더 강하게)
      float burstMultiplier = 1f;
      if (elapsedTime > burstStartTime && elapsedTime < burstStartTime + 0.15f)
      {
        float burstProgress = (elapsedTime - burstStartTime) / 0.15f;
        burstMultiplier = 1f + Mathf.Sin(burstProgress * Mathf.PI) * 1.5f;
      }

      float currentIntensity = baseIntensity * burstMultiplier;

      // 불규칙한 고주파 떨림
      float shakeX = Mathf.Sin(Time.time * heavyShakeSpeed) * currentIntensity;
      float shakeY = Mathf.Cos(Time.time * heavyShakeSpeed * 1.3f) * currentIntensity;

      // 강한 랜덤 노이즈
      shakeX += Random.Range(-currentIntensity * 0.6f, currentIntensity * 0.6f);
      shakeY += Random.Range(-currentIntensity * 0.4f, currentIntensity * 0.4f);

      // 미세한 Z축 움직임도 추가
      float shakeZ = Random.Range(-currentIntensity * 0.2f, currentIntensity * 0.2f);

      // 위치 적용
      Vector3 shakeOffset = new Vector3(shakeX, shakeY, shakeZ);
      transform.localPosition = originalPosition + shakeOffset;

      // 강한 회전 떨림
      float rotationX = Random.Range(-currentIntensity * 15f, currentIntensity * 15f);
      float rotationY = Random.Range(-currentIntensity * 10f, currentIntensity * 10f);
      float rotationZ = Random.Range(-currentIntensity * 20f, currentIntensity * 20f);

      Quaternion shakeRotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
      transform.localRotation = originalRotation * shakeRotation;

      yield return null;
    }
  }

  /// <summary>
  /// 떨림 중지
  /// </summary>
  public void StopShaking()
  {
    isShaking = false; // 먼저 플래그를 false로 설정하여 루프 중단

    if (currentShakeCoroutine != null)
    {
      StopCoroutine(currentShakeCoroutine);
      currentShakeCoroutine = null;
    }

    // 원래 위치로 복귀
    StartCoroutine(ReturnToOriginalPosition());
  }

  /// <summary>
  /// 원래 위치로 부드럽게 복귀
  /// </summary>
  private IEnumerator ReturnToOriginalPosition()
  {
    float returnDuration = 0.3f;
    float elapsedTime = 0f;

    Vector3 startPos = transform.localPosition;
    Quaternion startRot = transform.localRotation;

    while (elapsedTime < returnDuration)
    {
      elapsedTime += Time.deltaTime;
      float progress = elapsedTime / returnDuration;

      // 부드러운 복귀 (EaseOut 커브)
      float smoothProgress = 1f - (1f - progress) * (1f - progress);

      transform.localPosition = Vector3.Lerp(startPos, originalPosition, smoothProgress);
      transform.localRotation = Quaternion.Lerp(startRot, originalRotation, smoothProgress);

      yield return null;
    }

    transform.localPosition = originalPosition;
    transform.localRotation = originalRotation;
  }

  /// <summary>
  /// 현재 떨림 상태 확인
  /// </summary>
  public bool IsShaking()
  {
    return isShaking;
  }

  /// <summary>
  /// 원래 위치 재설정 (위치가 바뀌었을 때 사용)
  /// </summary>
  public void UpdateOriginalPosition()
  {
    if (!isShaking)
    {
      originalPosition = transform.localPosition;
      originalRotation = transform.localRotation;
    }
  }

  // 테스트용 메서드들
  [ContextMenu("Test Light Shake")]
  private void TestLightShake()
  {
    StartLightShaking();
  }

  [ContextMenu("Test Heavy Shake")]
  private void TestHeavyShake()
  {
    StartHeavyShaking();
  }

  [ContextMenu("Stop All Shaking")]
  private void TestStopShaking()
  {
    StopShaking();
  }
}
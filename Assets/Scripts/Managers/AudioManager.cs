using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
  public static AudioManager I { get; private set; }

  [Header("Mixer & Groups")]
  [SerializeField] private AudioMixer mixer;                    // MasterMixer 에셋
  [SerializeField] private AudioMixerGroup bgmGroup;            // BGM 그룹
  [SerializeField] private AudioMixerGroup sfxGroup;            // SFX 그룹

  [Header("Sources")]
  [SerializeField] private AudioSource bgmSource;               // 루프 재생용 BGM AudioSource

  // 효과음 재생 풀
  private List<AudioSource> sfxSources = new();
  private int sfxPoolSize = 10;
  private float sfxMinInterval = 0.1f;
  private Dictionary<AudioClip, float> lastPlayTime = new Dictionary<AudioClip, float>();
  private Dictionary<string, AudioSource> loopingSources = new Dictionary<string, AudioSource>();
  private Dictionary<string, Coroutine> sequenceCoroutines = new Dictionary<string, Coroutine>();


  void Awake()
  {
    // 1) 싱글톤 설정
    if (I != null && I != this) { Destroy(gameObject); return; }
    I = this;
    DontDestroyOnLoad(gameObject);

    // 2) AudioSource 세팅
    bgmSource.outputAudioMixerGroup = bgmGroup;
    bgmSource.loop = true;
    bgmSource.volume = 0.5f;

    // 3) SFX 풀 초기화
    for (int i = 0; i < sfxPoolSize; i++)
    {
      var src = gameObject.AddComponent<AudioSource>();
      src.outputAudioMixerGroup = sfxGroup;
      src.playOnAwake = false;
      sfxSources.Add(src);
    }
  }

  // SFX용 비어있는 AudioSource 반환
  private AudioSource GetAvailableSfxSource()
  {
    foreach (var src in sfxSources)
      if (!src.isPlaying)
        return src;
    // 모두 사용 중이면 풀 확장
    var extra = gameObject.AddComponent<AudioSource>();
    extra.outputAudioMixerGroup = sfxGroup;
    extra.playOnAwake = false;
    sfxSources.Add(extra);
    return extra;
  }

  /// <summary> BGM 재생 (페이드 가능) </summary>
  public void PlayBGM(AudioClip clip, float fadeDuration = 1f)
  {
    StopAllCoroutines();
    StartCoroutine(FadeBgmRoutine(clip, fadeDuration));
  }

  private IEnumerator<WaitForSeconds> FadeBgmRoutine(AudioClip newClip, float duration)
  {
    // 현재 볼륨 페이드아웃
    float t = 0f;
    mixer.GetFloat("BGMVolume", out float startVol);
    while (t < duration)
    {
      t += Time.deltaTime;
      mixer.SetFloat("BGMVolume", Mathf.Lerp(startVol, -80f, t / duration));
      yield return new WaitForSeconds(0.01f);
    }
    // 클립 교체
    bgmSource.clip = newClip;
    bgmSource.Play();
    // 볼륨 페이드인
    t = 0f;
    while (t < duration)
    {
      t += Time.deltaTime;
      mixer.SetFloat("BGMVolume", Mathf.Lerp(-80f, 0f, t / duration));
      yield return new WaitForSeconds(0.01f);
    }
  }

  /// <summary> 효과음 재생 </summary>
  public void PlaySFX(AudioClip clip, float volume = 1f)
  {
    if (clip == null)
      return;

    float now = Time.time;
    if (lastPlayTime.TryGetValue(clip, out float lastTime))
    {
      // 마지막 재생 이후 너무 짧다면 스킵
      if (now - lastTime < sfxMinInterval)
        return;
    }

    // 재생 허용: 타임스탬프 갱신
    lastPlayTime[clip] = now;

    var source = GetAvailableSfxSource();
    source.volume = volume;
    source.clip = clip;
    source.Play();
  }

  /// <summary> Mixer Snapshot 전환 </summary>
  public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float time)
  {
    snapshot.TransitionTo(time);
  }

  public void StopSFX(AudioClip clip)
  {
    if (clip == null)
      return;

    // sfxSources 풀에서 해당 클립을 재생 중인 AudioSource들을 찾아서 정지
    foreach (var source in sfxSources)
    {
      if (source.isPlaying && source.clip == clip)
      {
        source.Stop();
        source.clip = null; // 클립 참조 정리
      }
    }

    // lastPlayTime 딕셔너리에서도 해당 클립의 타임스탬프 제거 (선택사항)
    if (lastPlayTime.ContainsKey(clip))
    {
      lastPlayTime.Remove(clip);
    }
  }

  public void StopAllSFX()
  {
    foreach (var source in sfxSources)
    {
      StopSFX(source.clip);
    }
  }

  /// <summary>
  /// SFX A 재생 후 완료되면 SFX B를 루프로 재생
  /// </summary>
  /// <param name="clipA">처음 재생할 클립</param>
  /// <param name="clipB">루프로 재생할 클립</param>
  /// <param name="volumeA">클립 A의 볼륨</param>
  /// <param name="volumeB">클립 B의 볼륨</param>
  /// <param name="sequenceId">시퀀스 식별자 (중복 방지용)</param>
  /// <returns>시퀀스를 제어할 수 있는 코루틴</returns>
  public Coroutine PlaySFXSequence(AudioClip clipA, AudioClip clipB, float volumeA = 1f, float volumeB = 1f, string sequenceId = "default")
  {
    if (clipA == null || clipB == null)
    {
      Debug.LogWarning("AudioManager: PlaySFXSequence - 클립이 null입니다.");
      return null;
    }

    // 기존 같은 ID의 시퀀스가 있다면 정지
    StopSFXSequence(sequenceId);

    // 새 시퀀스 시작
    var coroutine = StartCoroutine(SFXSequenceRoutine(clipA, clipB, volumeA, volumeB, sequenceId));
    sequenceCoroutines[sequenceId] = coroutine;

    return coroutine;
  }

  /// <summary>
  /// SFX 시퀀스 실행 코루틴
  /// </summary>
  private IEnumerator SFXSequenceRoutine(AudioClip clipA, AudioClip clipB, float volumeA = 1f, float volumeB = 1f, string sequenceId = "sequence")
  {
    // 1단계: 클립 A 재생
    var sourceA = GetAvailableSfxSource();
    sourceA.volume = volumeA;
    sourceA.clip = clipA;
    sourceA.loop = false;
    sourceA.Play();

    Debug.Log($"AudioManager: 시퀀스 '{sequenceId}' - 클립 A '{clipA.name}' 재생 시작");

    // 클립 A가 끝날 때까지 대기
    yield return new WaitForSeconds(clipA.length);

    // 클립 A 정리
    sourceA.Stop();
    sourceA.clip = null;

    Debug.Log($"AudioManager: 시퀀스 '{sequenceId}' - 클립 A 완료, 클립 B '{clipB.name}' 루프 시작");

    // 2단계: 클립 B를 루프로 재생
    var sourceB = GetAvailableSfxSource();
    sourceB.volume = volumeB;
    sourceB.clip = clipB;
    sourceB.loop = true;
    sourceB.Play();

    // 루핑 소스 딕셔너리에 등록 (나중에 정지할 수 있도록)
    loopingSources[sequenceId] = sourceB;

    Debug.Log($"AudioManager: 시퀀스 '{sequenceId}' 완료 - 클립 B가 루프 재생 중");

    // 코루틴 딕셔너리에서 제거 (정상 완료)
    if (sequenceCoroutines.ContainsKey(sequenceId))
    {
      sequenceCoroutines.Remove(sequenceId);
    }
  }

  /// <summary>
  /// 특정 시퀀스 정지 (루프 중인 클립 B도 정지)
  /// </summary>
  /// <param name="sequenceId">정지할 시퀀스 ID</param>
  public void StopSFXSequence(string sequenceId)
  {
    // 1. 실행 중인 코루틴 정지
    if (sequenceCoroutines.TryGetValue(sequenceId, out Coroutine coroutine))
    {
      if (coroutine != null)
      {
        StopCoroutine(coroutine);
      }
      sequenceCoroutines.Remove(sequenceId);
    }

    // 2. 루핑 중인 AudioSource 정지
    if (loopingSources.TryGetValue(sequenceId, out AudioSource loopingSource))
    {
      if (loopingSource != null && loopingSource.isPlaying)
      {
        loopingSource.Stop();
        loopingSource.clip = null;
        loopingSource.loop = false;
      }
      loopingSources.Remove(sequenceId);
    }

    Debug.Log($"AudioManager: 시퀀스 '{sequenceId}' 정지됨");
  }

  /// <summary>
  /// 모든 SFX 시퀀스 정지
  /// </summary>
  public void StopAllSFXSequences()
  {
    // 모든 시퀀스 정지
    var sequenceIds = new List<string>(sequenceCoroutines.Keys);
    foreach (string id in sequenceIds)
    {
      StopSFXSequence(id);
    }

    var sequenceloopIds = new List<string>(loopingSources.Keys);
    foreach (string id in sequenceloopIds)
    {
      StopSFXSequence(id);
    }

    Debug.Log("AudioManager: 모든 SFX 시퀀스 정지됨");
  }

  /// <summary>
  /// 특정 시퀀스가 실행 중인지 확인
  /// </summary>
  /// <param name="sequenceId">확인할 시퀀스 ID</param>
  /// <returns>실행 중이면 true</returns>
  public bool IsSequenceRunning(string sequenceId)
  {
    return sequenceCoroutines.ContainsKey(sequenceId) || loopingSources.ContainsKey(sequenceId);
  }
}
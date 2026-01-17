using System;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Unity Ads를 "앱 전체에서 1개만" 관리하는 매니저.
/// - DontDestroyOnLoad(DDO)로 씬이 바뀌어도 유지
/// - Initialize(초기화) -> Load(광고 준비) -> Show(광고 표시) -> Complete(완료/실패) -> 다시 Load
/// - 외부(씬 UI / EditorWindow)는 Instance로 접근해서 Load/Show만 호출하면 되게 만든 구조
/// </summary>
public sealed class AdsManager : MonoBehaviour,
    IUnityAdsInitializationListener, // Initialize 결과 콜백을 받기 위한 인터페이스
    IUnityAdsLoadListener,           // Load 결과 콜백을 받기 위한 인터페이스
    IUnityAdsShowListener            // Show(표시) 과정/결과 콜백을 받기 위한 인터페이스
{
    /// <summary>
    /// 어디서든 AdsManager.Instance로 접근하기 위한 싱글톤 인스턴스.
    /// - Awake에서 최초 1개만 살아남게 설정함.
    /// </summary>
    public static AdsManager Instance { get; private set; }

    // ====== Unity Dashboard에서 발급되는 "게임(프로젝트) ID" ======
    [Header("Game IDs")]
    [SerializeField] private string androidGameId; // Android용 Game ID (Unity Dashboard에서 복사)
    [SerializeField] private string iosGameId;     // iOS용 Game ID (Unity Dashboard에서 복사)

    // ====== Unity Dashboard에서 만든 "광고 슬롯(Ad Unit/Placement) 이름" ======
    [Header("Rewarded Ad Unit IDs")]
    [SerializeField] private string rewardedAndroidAdUnitId = "Rewarded_Android"; // Android Rewarded 슬롯 이름
    [SerializeField] private string rewardediOSAdUnitId = "Rewarded_iOS";         // iOS Rewarded 슬롯 이름

    [Header("Settings")]
    [SerializeField] private bool testMode = true;
    // - true: 테스트 광고(개발 중)
    // - false: 실제 광고(출시용)

    [SerializeField] private bool autoLoadRewardedAfterInit = true;
    // - true: Initialize가 성공하면 자동으로 Rewarded를 미리 Load(준비) 해 둠
    // - false: 외부에서 LoadRewarded()를 직접 호출해야 Load 함

    // ====== 현재 플랫폼에 맞게 ResolveIds()에서 최종 선택되는 값 ======
    private string gameId;           // 실제 Initialize에 쓸 GameId(현재 플랫폼용)
    private string rewardedAdUnitId; // 실제 Load/Show에 쓸 AdUnitId(현재 플랫폼용)

    // ====== 현재 상태 플래그 ======
    private bool rewardedLoaded; // Rewarded 광고가 "로드 완료(표시 가능)" 상태인지
    private bool initDone;       // Initialize가 완료되었는지(성공/이미 초기화 포함)

    // ====== ShowRewarded()로 들어온 콜백들을 임시로 저장 ======
    // 광고가 끝났을 때(Complete/Fail) 호출해야 해서 잠깐 들고 있는 변수들
    private Action pendingReward; // "끝까지 봤으면" 실행할 보상 처리 콜백
    private Action pendingFail;   // 로드/표시 실패 또는 스킵 등 보상 조건 불충족 시 실행할 콜백
    private Action pendingClosed; // 광고가 닫혔을 때(성공/실패 상관없이) 실행할 콜백

    /// <summary>
    /// Unity가 오브젝트를 생성할 때 자동으로 1회 호출.
    /// 언제 호출되는가?
    /// - 씬에 AdsManager가 존재하면 플레이 시작 시점에 자동 호출
    ///
    /// 여기서 하는 일:
    /// 1) 싱글톤 보장(중복이면 파괴)
    /// 2) DDO 설정(씬 이동해도 유지)
    /// 3) 플랫폼별 ID 선택(ResolveIds)
    /// 4) 광고 초기화(InitializeAds)
    /// </summary>
    private void Awake()
    {
        // 이미 인스턴스가 있으면, 새로 생긴 것은 제거(중복 방지)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveIds();
        InitializeAds();
    }

    /// <summary>
    /// 현재 빌드 플랫폼에 따라 사용할 GameId / Rewarded AdUnitId를 결정.
    /// 언제 호출되는가?
    /// - Awake에서 1회 호출(일반적)
    ///
    /// 왜 필요한가?
    /// - Android/iOS는 Dashboard에서 발급되는 ID가 다름
    /// - 잘못된 ID를 쓰면 Initialize/Load/Show가 실패할 수 있음
    /// </summary>
    private void ResolveIds()
    {
#if UNITY_IOS
        gameId = iosGameId;
        rewardedAdUnitId = rewardediOSAdUnitId;
#elif UNITY_ANDROID
        gameId = androidGameId;
        rewardedAdUnitId = rewardedAndroidAdUnitId;
#else
        // Editor/기타 플랫폼에서는 보통 Android 값을 넣어 테스트하는 경우가 많음
        gameId = androidGameId;
        rewardedAdUnitId = rewardedAndroidAdUnitId;
#endif
    }

    /// <summary>
    /// Unity Ads SDK 초기화.
    /// 인자 없음.
    ///
    /// 언제 호출하는가?
    /// - 보통 Awake에서 1회 호출(현재 코드)
    /// - 앱 실행 중 다시 호출할 필요는 거의 없음
    ///
    /// 성공/실패 결과는 아래 콜백에서 받음:
    /// - OnInitializationComplete()
    /// - OnInitializationFailed(...)
    /// </summary>
    public void InitializeAds()
    {
        // 이미 초기화가 끝났거나, Unity Ads가 이미 초기화된 상태면 재초기화하지 않음
        if (initDone || Advertisement.isInitialized)
        {
            initDone = true;
            return;
        }

        // GameId가 비어있으면 무조건 실패하므로 즉시 중단
        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogError("[AdsManager] GameId가 비어 있습니다. Inspector에 Android/iOS GameId를 입력하세요.");
            return;
        }

        // 현재 플랫폼에서 Unity Ads가 지원되지 않는다면 호출해도 의미가 없음
        if (!Advertisement.isSupported)
        {
            Debug.LogWarning("[AdsManager] 현재 플랫폼에서 Unity Ads가 지원되지 않습니다.");
            return;
        }

        // 실제 초기화 호출
        // testMode: 테스트 광고 여부
        // this: 초기화 성공/실패 콜백 받을 리스너
        Advertisement.Initialize(gameId, testMode, this);
    }

    /// <summary>
    /// Rewarded 광고를 "미리 준비(로드)"하는 함수.
    /// 인자 없음.
    ///
    /// 언제 호출하는가?
    /// - Initialize 성공 후: 자동 로드(autoLoadRewardedAfterInit = true)면 자동 호출됨
    /// - 또는 광고를 한번 본 다음, 다음 시청을 위해 다시 로드할 때(ShowComplete에서 재호출)
    ///
    /// 성공/실패 결과는 아래 콜백에서 받음:
    /// - OnUnityAdsAdLoaded(adUnitId)
    /// - OnUnityAdsFailedToLoad(adUnitId, error, message)
    /// </summary>
    public void LoadRewarded()
    {
        // 초기화가 안 끝났으면 로드가 실패할 가능성이 크므로 방지
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[AdsManager] Initialize가 끝나기 전에 LoadRewarded를 호출했습니다.");
            return;
        }

        // AdUnitId(슬롯 이름)가 비어있으면 로드 대상이 없어서 실패
        if (string.IsNullOrEmpty(rewardedAdUnitId))
        {
            Debug.LogError("[AdsManager] Rewarded AdUnitId가 비어 있습니다.");
            return;
        }

        // 로드를 시작하므로 "아직 로드됨" 상태를 false로
        rewardedLoaded = false;

        // 실제 로드 요청
        // this: 로드 성공/실패 콜백 받을 리스너
        Advertisement.Load(rewardedAdUnitId, this);
    }

    /// <summary>
    /// 현재 Rewarded 광고를 "지금 바로 Show 가능한 상태인지" 확인하는 함수.
    /// 반환값:
    /// - true  : Initialize 완료 + Load 완료(ready)
    /// - false : 아직 준비 안 됨
    ///
    /// 언제 쓰는가?
    /// - UI 버튼 interactable 여부 결정할 때
    /// - EditorWindow에서 "지금 쇼 가능한지" 상태 표시할 때
    /// </summary>
    public bool CanShowRewarded()
    {
        return Advertisement.isInitialized && rewardedLoaded;
    }

    /// <summary>
    /// Rewarded 광고를 실제로 표시하는 함수(외부에서 호출하는 메인 API).
    ///
    /// 인자 설명:
    /// - onReward : 사용자가 광고를 "끝까지 시청(COMPLETED)"했을 때 실행할 함수(보상 지급 로직)
    /// - onFail   : 로드/표시 실패, 또는 스킵 등으로 보상 조건을 만족하지 못했을 때 실행할 함수(선택)
    /// - onClosed : 광고가 닫힐 때 실행할 함수(성공/실패 상관없이). UI 복구용으로 주로 사용(선택)
    ///
    /// 언제 호출하는가?
    /// - "광고 보고 보상 받기" 버튼을 눌렀을 때
    /// - EditorWindow 디버그 버튼으로 강제 시청 테스트할 때
    ///
    /// 내부 동작:
    /// 1) 콜백을 pending 변수에 저장(광고가 끝나면 호출해야 하므로)
    /// 2) 초기화/로드 상태를 체크
    /// 3) 준비 안 됐으면 실패 처리 + Load 재시도
    /// 4) 준비 됐으면 Advertisement.Show(...) 호출
    /// </summary>
    public void ShowRewarded(Action onReward, Action onFail = null, Action onClosed = null)
    {
        // 광고가 끝났을 때 호출할 콜백들을 저장
        pendingReward = onReward;
        pendingFail = onFail;
        pendingClosed = onClosed;

        // 초기화가 안 되어 있으면 Show 불가 -> 실패 처리
        if (!Advertisement.isInitialized)
        {
            pendingFail?.Invoke();
            ClearPending();
            return;
        }

        // 로드가 안 되어 있으면 Show 불가 -> 실패 처리 후 Load를 걸어 다음 기회 준비
        if (!rewardedLoaded)
        {
            pendingFail?.Invoke();
            ClearPending();
            LoadRewarded();
            return;
        }

        // 지금부터 Show를 시도하므로 "로드됨" 플래그는 false로 내려둠(중복 Show 방지)
        rewardedLoaded = false;

        // 실제 광고 표시(재생) 호출
        Advertisement.Show(rewardedAdUnitId, this);
    }

    /// <summary>
    /// pending 콜백들 제거.
    /// 언제 호출되는가?
    /// - Show가 실패/완료로 끝나서 더 이상 콜백을 들고 있을 필요가 없을 때
    /// </summary>
    private void ClearPending()
    {
        pendingReward = null;
        pendingFail = null;
        pendingClosed = null;
    }

    // ===================== Initialization callbacks =====================

    /// <summary>
    /// Initialize 성공 시 Unity Ads SDK가 자동으로 호출하는 콜백.
    /// 인자 없음.
    ///
    /// 여기서 하는 일:
    /// - initDone = true로 상태 업데이트
    /// - 설정에 따라 Rewarded 광고를 자동으로 Load해서 미리 준비
    /// </summary>
    public void OnInitializationComplete()
    {
        initDone = true;

        if (autoLoadRewardedAfterInit)
            LoadRewarded();
    }

    /// <summary>
    /// Initialize 실패 시 Unity Ads SDK가 자동으로 호출하는 콜백.
    ///
    /// 인자:
    /// - error   : 실패 종류(열거형)
    /// - message : 상세 메시지(문자열)
    ///
    /// 여기서는 로그만 찍고 끝냈지만,
    /// 실제 서비스에서는 재시도 타이머를 두거나, UI에 "광고 사용 불가" 표시를 할 수 있음.
    /// </summary>
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[AdsManager] Init Failed: {error} - {message}");
    }

    // ===================== Load callbacks =====================

    /// <summary>
    /// Load 성공 시 Unity Ads SDK가 자동으로 호출하는 콜백.
    ///
    /// 인자:
    /// - adUnitId : 로드가 완료된 광고 슬롯 이름
    ///
    /// 여기서 하는 일:
    /// - 내가 관리하는 rewardedAdUnitId가 로드되었으면 rewardedLoaded = true로 표시
    /// </summary>
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId == rewardedAdUnitId)
            rewardedLoaded = true;
    }

    /// <summary>
    /// Load 실패 시 Unity Ads SDK가 자동으로 호출하는 콜백.
    ///
    /// 인자:
    /// - adUnitId : 로드를 시도한 광고 슬롯 이름
    /// - error    : 실패 원인(열거형)
    /// - message  : 상세 메시지(문자열)
    ///
    /// 여기서 하는 일:
    /// - 로드 실패했으니 rewardedLoaded는 false 유지
    /// - 로그 출력
    /// </summary>
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId == rewardedAdUnitId)
            rewardedLoaded = false;

        Debug.LogError($"[AdsManager] Load Failed: {adUnitId} / {error} - {message}");
    }

    // ===================== Show callbacks =====================

    /// <summary>
    /// 광고 표시가 시작될 때 Unity Ads SDK가 자동으로 호출하는 콜백.
    /// 인자:
    /// - adUnitId : 표시를 시작한 광고 슬롯 이름
    ///
    /// 보통 여기서 하는 일(선택):
    /// - 게임 입력 막기, 일시정지, 사운드 조절 등
    /// </summary>
    public void OnUnityAdsShowStart(string adUnitId) { }

    /// <summary>
    /// 광고 클릭 시 Unity Ads SDK가 자동으로 호출하는 콜백.
    /// 인자:
    /// - adUnitId : 클릭된 광고 슬롯 이름
    ///
    /// 보상과는 무관하고 로그/분석 용도.
    /// </summary>
    public void OnUnityAdsShowClick(string adUnitId) { }

    /// <summary>
    /// Show(표시) 자체가 실패했을 때 Unity Ads SDK가 자동으로 호출하는 콜백.
    ///
    /// 인자:
    /// - adUnitId : 표시를 시도한 광고 슬롯 이름
    /// - error    : 실패 원인(열거형)
    /// - message  : 상세 메시지(문자열)
    ///
    /// 여기서 하는 일:
    /// - pendingFail 호출(외부에서 넘긴 실패 처리)
    /// - pendingClosed 호출(닫힘 처리)
    /// - 콜백 정리
    /// - 다음 시도를 위해 LoadRewarded()로 재로드
    /// </summary>
    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[AdsManager] Show Failed: {adUnitId} / {error} - {message}");
        pendingFail?.Invoke();
        pendingClosed?.Invoke();
        ClearPending();
        LoadRewarded();
    }

    /// <summary>
    /// 광고가 종료되었을 때 Unity Ads SDK가 자동으로 호출하는 콜백(가장 중요).
    ///
    /// 인자:
    /// - adUnitId              : 종료된 광고 슬롯 이름
    /// - showCompletionState   : 완료 상태
    ///   - COMPLETED : 끝까지 시청(보상 지급 조건)
    ///   - SKIPPED   : 중간 종료/스킵(보상 지급하면 안 됨)
    ///   - UNKNOWN   : 알 수 없음(보상 지급 보수적으로 금지 권장)
    ///
    /// 여기서 하는 일:
    /// - COMPLETED면 pendingReward 호출(보상 지급)
    /// - 그 외는 pendingFail 호출(보상 없음)
    /// - pendingClosed 호출(광고 닫힘 후 UI 복구 등)
    /// - 콜백 정리
    /// - 다음 광고를 위해 LoadRewarded()로 재로드
    /// </summary>
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId == rewardedAdUnitId && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            pendingReward?.Invoke();
        }
        else
        {
            pendingFail?.Invoke();
        }

        pendingClosed?.Invoke();
        ClearPending();
        LoadRewarded();
    }
}

using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD.
///  - 좌측 하단 : 체력 바(반투명) + 숫자
///  - 우측 하단 : 탄약(현재/여분) + 투척물 개수
///  - 상단 중앙 : 라운드 번호 + 승수 표시(점)
///  - 화면 중앙 : 라운드 결과 배너
///  - 화면 전체 : 체력이 낮을 때 붉은 비네트
///
/// 로컬 플레이어는 씬 로드보다 늦게 스폰되므로, 잡힐 때까지 매 프레임 찾습니다.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("체력 (좌측 하단)")]
    public Image hpFill;
    public Image hpFillGhost;          // 실제 체력보다 천천히 따라오는 잔상 바 (피격이 눈에 띄게)
    public TMP_Text hpText;

    [Header("탄약 (우측 하단)")]
    public TMP_Text ammoCurrentText;
    public TMP_Text ammoReserveText;

    [Header("투척물 (우측 하단)")]
    public TMP_Text fragCountText;
    public TMP_Text smokeCountText;
    public CanvasGroup fragGroup;      // 다 쓰면 흐리게
    public CanvasGroup smokeGroup;

    [Header("라운드 (상단 중앙)")]
    public TMP_Text roundLabel;
    [Tooltip("내 승수 표시용 점. 왼쪽부터 채워집니다. 3선승이면 3개.")]
    public Image[] myPips;
    [Tooltip("상대 승수 표시용 점.")]
    public Image[] opponentPips;

    [Header("배너 (중앙)")]
    public TMP_Text bannerText;

    [Header("피격 비네트")]
    public Image lowHealthVignette;

    [Header("색")]
    public Color healthyColor = new Color(0.55f, 0.92f, 0.65f, 0.80f);
    public Color hurtColor = new Color(0.98f, 0.78f, 0.30f, 0.85f);
    public Color criticalColor = new Color(0.95f, 0.32f, 0.32f, 0.92f);
    public Color pipOnMine = new Color(0.45f, 0.85f, 1f, 1f);
    public Color pipOnTheirs = new Color(1f, 0.42f, 0.42f, 1f);
    public Color pipOff = new Color(1f, 1f, 1f, 0.18f);

    private PlayerHealth localHealth;
    private PlayerFire localFire;
    private NetGrenadeLauncher localGrenades;

    private float bannerHideTime;
    private float ghostValue = 1f;

    void OnEnable() { RoundManager.BannerRequested += ShowBanner; }
    void OnDisable() { RoundManager.BannerRequested -= ShowBanner; }

    void Start()
    {
        if (bannerText != null) bannerText.text = "";
        if (lowHealthVignette != null)
            lowHealthVignette.color = new Color(0.7f, 0.05f, 0.05f, 0f);
    }

    void Update()
    {
        AcquireLocalPlayer();
        UpdateHealth();
        UpdateAmmo();
        UpdateGrenades();
        UpdateRound();
        UpdateBanner();
    }

    private void AcquireLocalPlayer()
    {
        if (localHealth != null && localFire != null && localGrenades != null) return;

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsClient) return;

        var playerObject = nm.LocalClient?.PlayerObject;
        if (playerObject == null) return;

        localHealth = playerObject.GetComponent<PlayerHealth>();
        localFire = playerObject.GetComponentInChildren<PlayerFire>(true);
        localGrenades = playerObject.GetComponent<NetGrenadeLauncher>();
    }

    private void UpdateHealth()
    {
        if (localHealth == null) return;

        float t = localHealth.Normalized;

        if (hpFill != null)
        {
            hpFill.fillAmount = t;
            hpFill.color = t > 0.5f ? healthyColor
                         : t > 0.25f ? hurtColor
                         : criticalColor;
        }

        // 잔상 바는 실제 체력을 천천히 따라옵니다. 얼마나 깎였는지 한눈에 보입니다.
        if (hpFillGhost != null)
        {
            if (t > ghostValue) ghostValue = t;                       // 회복은 즉시
            else ghostValue = Mathf.MoveTowards(ghostValue, t, Time.deltaTime * 0.55f);
            hpFillGhost.fillAmount = ghostValue;
        }

        if (hpText != null)
            hpText.text = Mathf.CeilToInt(localHealth.Hp.Value).ToString();

        if (lowHealthVignette != null)
        {
            // 체력 35% 아래부터 서서히 붉어지고, 낮을수록 맥박이 빨라집니다.
            float danger = Mathf.Clamp01((0.35f - t) / 0.35f);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * (4f + danger * 6f));
            var c = lowHealthVignette.color;
            c.a = danger * 0.42f * pulse;
            lowHealthVignette.color = c;
        }
    }

    private void UpdateAmmo()
    {
        if (localFire == null) return;

        if (ammoCurrentText != null)
        {
            ammoCurrentText.text = localFire.isReloading ? "--" : localFire.currentAmmo.ToString();

            // 탄창이 얼마 안 남으면 빨갛게
            bool low = !localFire.isReloading &&
                       localFire.currentAmmo <= Mathf.Max(1, localFire.magazineSize / 4);
            ammoCurrentText.color = low ? new Color(1f, 0.45f, 0.40f) : Color.white;
        }

        if (ammoReserveText != null)
            ammoReserveText.text = localFire.totalAmmo.ToString();
    }

    private void UpdateGrenades()
    {
        if (localGrenades == null) return;

        int frag = localGrenades.FragLeft.Value;
        int smoke = localGrenades.SmokeLeft.Value;

        if (fragCountText != null) fragCountText.text = frag.ToString();
        if (smokeCountText != null) smokeCountText.text = smoke.ToString();

        if (fragGroup != null) fragGroup.alpha = frag > 0 ? 1f : 0.28f;
        if (smokeGroup != null) smokeGroup.alpha = smoke > 0 ? 1f : 0.28f;
    }

    private void UpdateRound()
    {
        var round = RoundManager.Instance;

        int mine = round != null ? round.MyScore : 0;
        int theirs = round != null ? round.OpponentScore : 0;
        int number = round != null ? round.RoundNumber.Value : 0;

        if (roundLabel != null)
            roundLabel.text = number > 0 ? "ROUND " + number : "대기 중";

        SetPips(myPips, mine, pipOnMine);
        SetPips(opponentPips, theirs, pipOnTheirs);
    }

    private void SetPips(Image[] pips, int count, Color onColor)
    {
        if (pips == null) return;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            pips[i].color = i < count ? onColor : pipOff;
        }
    }

    private void UpdateBanner()
    {
        if (bannerText == null) return;
        if (bannerText.text.Length == 0) return;

        if (Time.time >= bannerHideTime) bannerText.text = "";
    }

    private void ShowBanner(string message, float duration)
    {
        if (bannerText == null) return;
        bannerText.text = message;
        bannerHideTime = Time.time + duration;
    }
}

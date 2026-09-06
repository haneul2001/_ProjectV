using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 매치메이킹 씬의 UI를 MatchSession에 연결합니다.
/// 버튼/텍스트 참조는 에디터에서 자동으로 연결해두었습니다.
/// </summary>
public class MatchmakingUI : MonoBehaviour
{
    [Header("버튼")]
    public Button quickMatchButton;
    public Button createRoomButton;
    public Button joinCodeButton;
    public Button leaveButton;
    public Button quitButton;

    [Header("입력 / 표시")]
    public TMP_InputField codeInput;
    public TMP_Text statusText;
    public TMP_Text playerCountText;
    public TMP_Text joinCodeText;

    void Start()
    {
        // 로비에서는 커서가 보여야 합니다.
        // (게임 씬의 PlayerFire가 커서를 잠그고 나오기 때문에 여기서 반드시 풀어줍니다)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (quickMatchButton != null)
            quickMatchButton.onClick.AddListener(() => MatchSession.Instance.QuickMatch());

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(() => MatchSession.Instance.CreateRoom());

        if (joinCodeButton != null)
            joinCodeButton.onClick.AddListener(() =>
                MatchSession.Instance.JoinByCode(codeInput != null ? codeInput.text : ""));

        if (leaveButton != null)
            leaveButton.onClick.AddListener(() => MatchSession.Instance.Leave());
        if (quitButton != null)
            quitButton.onClick.AddListener(() => MatchSession.Instance.QuitGame());


        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        var session = MatchSession.Instance;
        if (session == null) return;

        bool inSession = session.Session != null;

        if (statusText != null)
            statusText.text = session.StatusMessage;

        if (playerCountText != null)
            playerCountText.text = inSession
                ? $"{session.PlayerCount} / {MatchSession.MaxPlayers}"
                : "- / " + MatchSession.MaxPlayers;

        if (joinCodeText != null)
        {
            string code = session.JoinCode;
            joinCodeText.text = string.IsNullOrEmpty(code) ? "" : "참가 코드:  " + code;
        }

        // 세션에 들어가 있거나 통신 중이면 진입 버튼들을 잠급니다.
        bool canEnter = !inSession && !session.Busy;
        SetInteractable(quickMatchButton, canEnter);
        SetInteractable(createRoomButton, canEnter);
        SetInteractable(joinCodeButton, canEnter);
        SetInteractable(leaveButton, inSession && !session.Busy);

        if (codeInput != null) codeInput.interactable = canEnter;
    }

    private static void SetInteractable(Button b, bool value)
    {
        if (b != null) b.interactable = value;
    }
}

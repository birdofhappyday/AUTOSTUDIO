using PhotonWrapper;
using SkywardRay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PCObserverLobbyUI : BootUIBase
{
    static public PCObserverLobbyUI PCObserverLobbyPage { get; internal set; } = null;

    public enum PCUIState
    {
        None = -1, //초기상태
        Lobby,
        Multi,
    }

    #region UIPage Inspector UI Class, Variable

    [System.Serializable]
    class UIPair : InspectorEnumPair<PCUIState, UIPage>
    {
        UIPair(PCUIState key, UIPage value) : base(key, value) { }
    }

    [SerializeField, HideInInspector]
    UIPair[] m_UIPages;
    #endregion


    UIPage GetPage(PCUIState key)
    {
        var page = m_UIPages.First(item => item.Key == key);
        if (page == null) throw new System.NullReferenceException($"UI State {key} is not exist.");
        return page.value;
    }

    // BootUIBase.state 를 덮어쓰기한다. int형을 가독의 편의를 위해 VRUIState로 캐스팅 하는 과정.
    new PCUIState state
    {
        get => (PCUIState)base.state;
        set
        {
            // 상태 전환이 일어나면 UIPage를 전환한다.
            if (state != value)
            {
                var prv = GetPage(state);
                var next = GetPage(value);
                if (prv != null)
                {
                    prv.Deactive(next);
                }
                else if (next != null)
                {
                    next.Active();
                }
                base.state = (int)value;
            }
        }
    }

    // UIPage에서 상태 전환을 하기위한 인터페이스.
    public override void SetState(System.Enum state)
    {
        this.state = (PCUIState)state;
    }

    public override UIPage GetCurrentPage()
    {
        return GetPage(state);
    }

    private void Awake()
    {
        PCObserverLobbyPage = this;
        PublicUI.Instance.pcObserverCamera.JoinLobby();
        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, true);

        foreach (var page in m_UIPages)
        {
            if (page.value != null)
            {
                page.value.Init();
                page.value.gameObject.SetActive(false);
            }
        }

        SetState(PCUIState.Lobby);

        NetworkManager.Instance.JoinLobby();

        ARP_NetMain.Instance.onConnectedToMaster += OnConnectedToMaster;
        ARP_NetMain.Instance.onLeftRoom += OnLeftRoom;
    }

    private void OnDestroy()
    {
        if (ARP_NetMain.Instance != null)
        {
            ARP_NetMain.Instance.onConnectedToMaster -= OnConnectedToMaster;
            ARP_NetMain.Instance.onLeftRoom -= OnLeftRoom;
        }
    }

    // 마스터 서버에 재 접속 시 호출
    void OnConnectedToMaster()
    {
        Debug.Log("LobbyUI.cs OnConnectedToMaster()");

        NetworkManager.Instance.JoinLobby();
    }

    // 공유하기 종료
    void OnLeftRoom()
    {
        Managed.XRHubSceneManager.Instance.SceneChange(SceneState.LOBBY, DataManager.STAGE_NAME);
        SetState(PCUIState.Lobby);
    }
}

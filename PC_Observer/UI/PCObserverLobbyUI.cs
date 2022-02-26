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
        None = -1, //�ʱ����
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

    // BootUIBase.state �� ������Ѵ�. int���� ������ ���Ǹ� ���� VRUIState�� ĳ���� �ϴ� ����.
    new PCUIState state
    {
        get => (PCUIState)base.state;
        set
        {
            // ���� ��ȯ�� �Ͼ�� UIPage�� ��ȯ�Ѵ�.
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

    // UIPage���� ���� ��ȯ�� �ϱ����� �������̽�.
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

    // ������ ������ �� ���� �� ȣ��
    void OnConnectedToMaster()
    {
        Debug.Log("LobbyUI.cs OnConnectedToMaster()");

        NetworkManager.Instance.JoinLobby();
    }

    // �����ϱ� ����
    void OnLeftRoom()
    {
        Managed.XRHubSceneManager.Instance.SceneChange(SceneState.LOBBY, DataManager.STAGE_NAME);
        SetState(PCUIState.Lobby);
    }
}

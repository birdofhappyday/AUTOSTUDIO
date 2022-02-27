
using Photon.Pun;
using PhotonWrapper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 1. 작성자 : 김성민
/// 
/// 2. 작성일자 : 2021.03.17
/// 
/// 3. 설명
/// 
///  - 로비 씬에서 사용되는 UI와 네트워크 콜백
///    
///     -- 방 만들기, 대기방, 기본 창, 저장소
///  
///  
/// </summary>
/// 
public class LobbyUI_RealConnect : ARPFunction.MonoBeHaviourSingleton<LobbyUI_RealConnect>
{
    [SerializeField]
    private LobbyState eLobbyState = LobbyState.SINGLE;
    [SerializeField]
    private Text nickName;
    [SerializeField]
    private GameObject Main;
    public GameObject lobbyUI;
    public StageUI_RealConnect stageUI;

    public ARP_NetRoom currentRoom;
    [HideInInspector]
    public GameObject CurrentOpenPopup;

    public RealConnectWaitRoom realConnectWaitRoom;
    public RealConnectStroge realConnectStroge;

    [SerializeField]
    private GameObject roomListParent;
    [SerializeField]
    private GameObject roomObj;

    public CreateRoom_RealConnect realConnectCreateRoom;

    public GameObject videoLoadingObj;

    private RealConnectRoomCategory realConnectRoomCategory;

    [Header("VR")]
    [SerializeField]
    private KeyBoardMng keyBoard;

    [Header("Mobile")]
    public GameObject underMenu;
    public GameObject underMenuToggle;
    public GameObject topMenu;
    public List<GameObject> topMenuToggleParent;
    public List<Toggle> underMenuToggleList;
    public ToggleGroup topMenuToggleGroup;

    [SerializeField]
    private GameObject watchingRoom;
    [SerializeField]
    private GameObject messageToggle;
    [SerializeField]
    private GameObject emptyRoomMessage;
    [SerializeField]
    private GameObject messageListObj;
    [SerializeField]
    private GameObject addButton;

    // 싱글상태와 멀티 상태에 따른 부모 오브젝트 세팅.
    public LobbyState ELobbyState
    {
        get => eLobbyState;
        set
        {
            eLobbyState = value;

            lobbyUI.SetActive(eLobbyState == LobbyState.SINGLE);
            stageUI.gameObject.SetActive(eLobbyState != LobbyState.SINGLE);
#if OCULUS
#else
            PublicUI.Instance.RealConnectInit();
#endif
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
        {
#if OCULUS
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
#else
            GyroCamera.instance.GyroModeOnOff(true);
#endif

            DontDestroyOnLoad(this);

            Initialized();

            ARP_NetMain.Instance.onRoomListUpdate += OnRoomListUpdate;
            ARP_NetMain.Instance.onLeftRoom += OnLeftRoom;
            ARP_NetMain.Instance.onConnectedToMaster += OnConnectedToMaster;
            ARP_NetMain.Instance.onJoinedRoom += JoinedRoom;
            ARP_NetMain.Instance.onPlayerEnteredRoom += OnPlayerEnteredRoom;
            ARP_NetMain.Instance.onPlayerLeftRoom += OnPlayerLeftRoom;

            NetworkManager.Instance.JoinLobby();

#if OUCLUS
            keyBoard.gameObject.SetActive(false);
#endif
            nickName.text = $"{ARP_NetMain.Instance.NickName}님 안녕하세요.";

            PublicUI.Instance.LoadingObjSet(false);
        }
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        if (ARP_NetMain.Instance != null)
        {
            ARP_NetMain.Instance.onLeftRoom -= OnLeftRoom;
            ARP_NetMain.Instance.onRoomListUpdate -= OnRoomListUpdate;
            ARP_NetMain.Instance.onConnectedToMaster -= OnConnectedToMaster;
            ARP_NetMain.Instance.onJoinedRoom -= JoinedRoom;
            ARP_NetMain.Instance.onPlayerLeftRoom -= OnPlayerLeftRoom;
            ARP_NetMain.Instance.onPlayerEnteredRoom -= OnPlayerEnteredRoom;
        }
    }

    public void CurrentPopupOff()
    {
        if (CurrentOpenPopup != null)
            CurrentOpenPopup.GetComponent<PopupBase>().Close();
    }

    public void CurrentRoomRegister(ARP_NetRoom _room)
    {
        currentRoom = _room;
    }

    // 로비 초기화. 싱글상태의 메인창으로 돌린다.
    public void Initialized(bool videoClear = true)
    {
        Main.SetActive(true);
        CurrentPopupOff();
        if (videoClear)
            VideoManager.Instance.SetVideoInfo(null);

#if OCULUS

#else
        watchingRoom.SetActive(true);
        underMenu.SetActive(true);
        underMenuToggle.SetActive(true);
        topMenu.SetActive(false);

        underMenuToggleList[0].isOn = true;
#endif
    }

    // 로비 초기화. 싱글상태의 메인창으로 돌린다.
    public void OnClickInitialized(bool isOn)
    {
        if (isOn)
        {
            Main.SetActive(isOn);
            CurrentPopupOff();
            if (isOn)
                VideoManager.Instance.SetVideoInfo(null);

#if OCULUS

#else
            watchingRoom.SetActive(isOn);
            underMenu.SetActive(isOn);
            underMenuToggle.SetActive(isOn);
            topMenu.SetActive(!isOn);

#endif
        }
    }

    public void KeyboardInput(InputField _inf)
    {
        keyBoard.gameObject.SetActive(true);

        Vector3 pos = new Vector3(0, -150.0f, 0);
        keyBoard.SetKeyBoard(_inf, pos);
    }

    public void KeyActiveFalse()
    {
        keyBoard.gameObject.SetActive(false);
    }

    // 대기방 팝업 오픈
    public void WaitRoomOpen()
    {
        CurrentPopupOff();
        realConnectWaitRoom.Open();
#if OCULUS
        Main.SetActive(false);
#else
        if (!topMenu.gameObject.activeSelf && PhotonNetwork.IsMasterClient)
            topMenu.gameObject.SetActive(true);

        watchingRoom.SetActive(false);
        underMenuToggle.SetActive(false);
#endif
    }

    public void WaitRoomOpen(bool isOn)
    {
        if (isOn)
        {
            if (CurrentOpenPopup == null)
            {
                realConnectWaitRoom.Open();
                topMenuToggleGroup.allowSwitchOff = false;
            }
        }
        else
            realConnectWaitRoom.Close();
    }

    // 방 정렬
    public void SetRoomArray()
    {
        if (this.realConnectRoomCategory != RealConnectRoomCategory.ALL)
        {
            foreach (Transform room in roomListParent.transform)
            {
                //[김성민] 리얼커넥트인지 확인해서 분류
                if (room.GetComponent<Room>().netRoom.netRoom.isVideoPlayer)
                    room.gameObject.SetActive(room.GetComponent<Room>().realConnectRoomCategory == this.realConnectRoomCategory);
            }
        }
        else
        {
            foreach (Transform room in roomListParent.transform)
            {
                //[김성민] 리얼커넥트인지 확인해서 분류
                if (room.GetComponent<Room>().netRoom.netRoom.isVideoPlayer)
                    room.gameObject.SetActive(true);
            }
        }
    }

    public void OnClickRoomCateGory(int category)
    {
        this.realConnectRoomCategory = (RealConnectRoomCategory)category;

        SetRoomArray();
    }

    #region NetworkCallBack

    // 룸리스트를 받아와서 갱신해준다.
    void OnRoomListUpdate(List<ARP_NetRoom> roomList)
    {
        // rooms를 UI에 출력
        Debug.Log("OnRoomListUpdate: " + Newtonsoft.Json.JsonConvert.SerializeObject(roomList));

        // 룸 목록 초기화
        foreach (Transform t in roomListParent.transform)
        {
            if (t.name != "RoomCreate")
                Destroy(t.gameObject);
        }

        // 룸 리스트 구축
        foreach (ARP_NetRoom room in roomList)
        {
            // 룸 Object 생성
            GameObject _roomObj = Instantiate(roomObj, roomListParent.transform);

            // 룸 클래스 없으면 생성
            Room _room = _roomObj.GetComponent<Room>();

#if (REALCONNECT)
            _roomObj.SetActive(room.netRoom.isVideoPlayer);
#else
            _roomObj.SetActive(!room.netRoom.isVideoPlayer);
#endif

            if (_room == null)
            {
                _room = new GameObject(typeof(Room).Name, typeof(Room)).GetComponent<Room>();
            }

            // 룸 초기화
            _room.Initialize(room);

            SetRoomArray();
        }
#if REALCONNECT
#if OCULUS
#else
        emptyRoomMessage.SetActive(roomList.Count == 0);
#endif
#else
#endif
    }

    // 방을 떠났을 경우 씬을 체인지하고 로비로 돌아간다.
    void OnLeftRoom()
    {
        currentRoom = null;
        if (PropertyManager.Instance != null)
            PropertyManager.Instance.SetSettingProperties(new ExitGames.Client.Photon.Hashtable());

        //[김성민] vr은 현재 버튼에서 직접 신전환을 하고 있다.
#if OCULUS
#else
        if (Managed.XRHubSceneManager.Instance != null)
        {
            Managed.XRHubSceneManager.Instance.SceneloadComplete += () => Initialized();
            Managed.XRHubSceneManager.Instance.SceneChange(SceneState.LOBBY);
        }
#endif

        VoiceManager.Instance.StopRecord();
        VoiceManager.Instance.EndVoiceNetwork();
    }

    // 멀티방에서 나갔을 때 로비상태로 진입한다.
    void OnConnectedToMaster()
    {
        NetworkManager.Instance.JoinLobby();
    }

    // 멀티방 입장해 성공했을때 방장과 방원의 세팅을 해준다.
    void JoinedRoom(bool isSuccess, string msg, GameObject playerObj)
    {
        if (isSuccess)
        {
            ARP_NetRoom CurrentRoom = new ARP_NetRoom(NetworkManager.Instance.GetCurrentRoom(), true);
            CurrentRoomRegister(CurrentRoom);
#if OCULUS
            Main.SetActive(false);
#else
            watchingRoom.SetActive(false);
            underMenuToggle.SetActive(false);
#endif
            if (PhotonNetwork.IsMasterClient)
            {
                MasterAddRoomProperty();
            }

            PublicUI.Instance.LoadingObjSet(false);
        }
    }

    // 다른 플레이어가 나갔을 경우 방장과 방원의 상태 처리
    public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            stageUI.realConnectVoicePopup.SetRoomVoicePlayerList(otherPlayer, false, true);
            realConnectWaitRoom.SetRoomWaitPlayerList(otherPlayer, false, true);
        }

        // otherPlayer.UserId 쓰면 됨.
        ///해당룸 생성 아이디를 비교해서 기존에 방을 만든 유저가 나가면 방을 깬다.
        if (otherPlayer.UserId.Equals(currentRoom.GetRoomResource().user_id))
        {
            VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.STOP;
            ARP_NetMain.Instance.LeaveRoom();

            Managed.XRHubSceneManager.Instance.SceneloadComplete += () => Initialized();
            Managed.XRHubSceneManager.Instance.SceneChange(SceneState.LOBBY);

            return;
        }

        realConnectWaitRoom.SetPlayerCount();
    }

    // 다른 플레이어가 들어왔을 경우 방장과 방원의 상태 처리
    public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            stageUI.realConnectVoicePopup.SetRoomVoicePlayerList(newPlayer, false);
            realConnectWaitRoom.SetRoomWaitPlayerList(newPlayer, false);
        }

        realConnectWaitRoom.SetPlayerCount();
    }

    // 방을 만들때 처음에 방장이 룸프로퍼티로 정보를 보내주는 부분.
    // 기본적인 정보를 다 보내준다. VIDEO_PROPERTY_KEY중 VIDEOSTART빼고 모든 정보를 보낸다.
    void MasterAddRoomProperty()
    {
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.INFO, VideoManager.Instance.GetRealConnectVideoInfo());
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.MUTE, VideoManager.Instance.GetVideoMute().ToString());
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.ISPLAYING, VideoManager.Instance.VideoIsPlaying);
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.PLAYTIME, 0.ToString());
        if (realConnectCreateRoom.pointerToggle != null)
            PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.POINTERSETTING, realConnectCreateRoom.pointerToggle.isOn.ToString());
        realConnectWaitRoom.ClearRoomWaitPlayer();
        realConnectWaitRoom.SetRoomWaitPlayerList(PhotonNetwork.LocalPlayer, false);
        stageUI.realConnectVoicePopup.ClearRoomVoicePlayerList();
        stageUI.realConnectVoicePopup.SetRoomVoicePlayerList(PhotonNetwork.LocalPlayer, true);
    }
    #endregion

    #region Button

    // 방만들기 버튼 클릭
    public void OnClickCreateRoom()
    {
        CurrentPopupOff();
#if OCULUS
        Main.SetActive(false);
#else
        watchingRoom.SetActive(false);
        underMenuToggle.SetActive(false);
#endif
        stageUI.realConnectMessagePopup.LobbyInit();
        realConnectCreateRoom.Open();
        realConnectCreateRoom.LobbySetting();
    }

    // 위에 있는 백 화살표 클릭
    public void OnClickBackButton()
    {
        PublicUI.Instance.MessagePopupOpen("앱을 종료하시겠습니까?",
            () => { Application.Quit(); });
    }

    public void OnClickStrogeButton()
    {
        Main.SetActive(false);
        realConnectStroge.Open();
        realConnectStroge.LobbySetting();
    }

    public void OnClickStrogeButton(bool isOn)
    {
        watchingRoom.SetActive(!isOn);
        //underMenuToggle.SetActive(!isOn);

        realConnectStroge.Open(isOn);
        if (isOn)
            realConnectStroge.LobbySetting();
    }


    //[김성민] 콘텐츠선택과 대기룸 왔다갔다 할때 선택완료 버튼과 message버튼 켰다끄기
    public void OnChangeTopMenuButton(bool waitRoom)
    {
        realConnectCreateRoom.startToggle.gameObject.SetActive(waitRoom);
        messageToggle.gameObject.SetActive(!waitRoom);
        if (VideoManager.Instance.VideoIsPlaying == VIDEO_ISPLAYING.PLAY)
            VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.STOP;
    }

    public void OnClickMessageToggle(bool isOn)
    {
        messageListObj.SetActive(isOn);
        addButton.SetActive(isOn);
    }

    IEnumerator keyboardCall(InputField mesage)
    {
        var key = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "입력하세요");
        while (key.status != TouchScreenKeyboard.Status.Done && key.status != TouchScreenKeyboard.Status.Canceled && key.status != TouchScreenKeyboard.Status.LostFocus)
        {
            yield return null;
        }

        string value = key.text;

        if (string.IsNullOrEmpty(key.text))
            value = "입력하지 않았습니다.";

        mesage.text = value;
    }

    public void CorKeyboardCall(InputField mesage)
    {
        StartCoroutine(keyboardCall(mesage));
    }

    #endregion

}
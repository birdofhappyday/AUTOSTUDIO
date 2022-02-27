using Evereal.VRVideoPlayer;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 1. 작성자 : 김성민
/// 
/// 2. 작성일자 : 2021.03.17
/// 
/// 3. 설명
/// 
///  - stage 씬에서 사용되는 UI와 네트워크 콜백, 사용 키보드 관련.
///  
///    -- 보이스 팝업, 비디오 재생, 음소거 기능
///    
///  - 추가 예정
///  
///    -- 강조, 메시지
///  
/// </summary>

// [김성민] 트리거 선택을 체크위한 팝업 타입
public enum StagePopupType
{
    NONE,
    MESSAGE,
    EMPHASIS,
}

public class StageUI_RealConnect : MonoBehaviour
{
    [SerializeField]
    private Toggle playToggle;
    [SerializeField]
    private Button stopButton;
    [SerializeField]
    private Toggle messageToggle;
    [SerializeField]
    private Text videoTime;
    [SerializeField]
    private Text videoLenght;
    [SerializeField]
    private Toggle pointerToggle;
    [SerializeField]
    private Toggle micToggle;
    [SerializeField]
    private Toggle emphasisToggle;


    const float videoStartTime = 4f;

    public RealConnectVoicePopup realConnectVoicePopup;
    public RealConnectMessagePopup realConnectMessagePopup;
    public EmphasisPopup emphasisPopup;

    public Slider voluemSlider;
    public Slider videoSlider;

    public Toggle volumeToggle;

    //[김성민] 트리거 때문에 있는 현재 스테이지 타입. 이거에 따라서 VR 트리거가 작동된다.
    public StagePopupType stagePopupType = StagePopupType.NONE;
    [HideInInspector]
    public RealConnectAvatar realConnectAvatar;
    //[김성민] 포인터 세팅을 위해서 만든 멀티플레이어 아바타 리스트를 저장해놓은 부분.
    [HideInInspector]
    public List<RealConnectAvatar> realConnectAvatarList = new List<RealConnectAvatar>();
    //[김성민] 멀티에서 방장 눈
    [HideInInspector]
    public GameObject masterEye;

    [Header("Mobile")]
    public GameObject makeObj;


    private Coroutine _startCoroutine;
    // 싱글에서 멀티방으로 전환했을때 이 오브젝트를 켜준다.
    // 이 오브젝트는 stage씬에 오면 켜지는 상태가 되어서 방장 상태에 따라서 처음에 토글 활성화 여부를 결정한다.
    private void OnEnable()
    {
        voluemSlider.Set(VideoManager.Instance.GetVideoVolume());

        if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.MULTI)
            ToggleInit(PhotonNetwork.IsMasterClient);
        else if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.STORGE)
            StorageToggleInit();

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            _startCoroutine = StartCoroutine(VideoStart());
    }

    private void OnDisable()
    {
        if (_startCoroutine != null)
        {
            StopCoroutine(_startCoroutine);
            _startCoroutine = null;
        }
    }

    IEnumerator VideoStart()
    {
        yield return new WaitForSeconds(videoStartTime);
        playToggle.isOn = true;
        _startCoroutine = null;
    }

    // 토글 초기화
    private void ToggleInit(bool interactable)
    {
        playToggle.interactable = interactable;
        voluemSlider.interactable = interactable;
        stopButton.interactable = interactable;
        videoSlider.interactable = interactable;
        volumeToggle.interactable = interactable;
        messageToggle.interactable = interactable;
        pointerToggle.interactable = interactable;
        emphasisToggle.interactable = true;
        micToggle.interactable = true;
        if (PhotonNetwork.InRoom)
            pointerToggle.SetIsOnWithoutNotify(Convert.ToBoolean(PhotonNetwork.CurrentRoom.CustomProperties[VIDEO_PROPERTY_KEY.POINTERSETTING.ToString()]));
    }

    // 저장소 토글 초기화
    private void StorageToggleInit()
    {
        playToggle.interactable = true;
        voluemSlider.interactable = true;
        stopButton.interactable = true;
        videoSlider.interactable = true;
        volumeToggle.interactable = true;
        emphasisToggle.interactable = false;
        micToggle.interactable = false;
        messageToggle.interactable = false;
        pointerToggle.interactable = false;
        playToggle.SetIsOnWithoutNotify(true);
        playToggle.targetGraphic.gameObject.SetActive(false);
    }

    // Play 버튼 클릭.
    public void OnClickPlayPause(bool isOn)
    {
        playToggle.targetGraphic.gameObject.SetActive(!isOn);

        if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.MULTI)
        {
            if (isOn)
                OnClickPlayVideo();
            else
                OnClickPauseVideo();
        }
        else if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.STORGE)
        {
            if (isOn)
                VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.PLAY;
            else
                VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.PAUSE;
        }
    }

    // 비디오 재생. 서버 시간과 플레이 타임, 플레이 상태를 모두 보내준다.
    // 방장만 작동할 수 있으며
    // 방원은 비디오 재생에 서버시간, 플레이 타임 변수를 통해 재생 시간을 계산해서 먼저 보내주어서 세팅하고 재생하도록 순서를 잡았다.
    public void OnClickPlayVideo()
    {
        if (UpdateFlag) UpdateFlag = false;
        isTimeMoving = false;
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.TIMESTAMP, PhotonNetwork.Time.ToString());
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.PLAYTIME, VideoManager.Instance.GetVideoTime().ToString());
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.ISPLAYING, VIDEO_ISPLAYING.PLAY);
    }

    // 스탑 버튼 클릭
    // 비디오 정지, 플레이 상태와, 플레이 타임을 보내준다.
    // 정지에는 시간계산이 따로 없기에 세팅 순서는 관계가 없다.
    public void OnClickStopVideo()
    {
        if (UpdateFlag) UpdateFlag = false;

        playToggle.SetIsOnWithoutNotify(false);
        playToggle.targetGraphic.gameObject.SetActive(true);

        if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.MULTI)
        {
            PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.ISPLAYING, VIDEO_ISPLAYING.STOP);
            PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.PLAYTIME, 0.ToString());
        }
        else
        {
            VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.STOP;
        }
    }

    // 비디오 일시정지, 플레이 상태와 플레이 타임을 보내준다.
    // 일시정지에는 시간을 플레이 타임을 보내주지만 시작할때 다시 시간 세팅 부분이 있기때문에 순서는 크게 상관은 없다.
    // 다만 같이 보고 있는 경우 같은 화면으로 정지하게 세팅 순서가 되어있다.
    public void OnClickPauseVideo()
    {
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.ISPLAYING, VIDEO_ISPLAYING.PAUSE);
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.PLAYTIME, VideoManager.Instance.GetVideoTime().ToString());
    }

    // 멀티방을 나간다. stageUI에 나가기 버튼에 붙어있다.
    // 기존에 재생되던 영상을 정지상태로 돌리고 로비로 나간다.
    // 방장은 rpc를 싸서 전부다 대기방으로 돌리고 방원은 그냥 로비로 돌아온다.
    public void LeaveRoom()
    {
        playToggle.isOn = false;

        if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.MULTI)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                LobbyUI_RealConnect.Instance.realConnectWaitRoom.SetRoomWaitPlayerAllSetting(false);
                LobbyUI_RealConnect.Instance.stageUI.realConnectAvatar.photonSerializeView.photonView.RPC("RPC_SetReceive_MasterExitGameRoom", RpcTarget.All);

                realConnectMessagePopup.ReadStageString();
            }
            else
            {
                VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.STOP;
                ARP_NetMain.Instance.LeaveRoom();
                Managed.XRHubSceneManager.Instance.SceneloadComplete += () => LobbyUI_RealConnect.Instance.Initialized();

                Managed.XRHubSceneManager.Instance.SceneChange(SceneState.LOBBY);
            }
        }
        else if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.STORGE)
        {
            VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.STOP;
            LobbyUI_RealConnect.Instance.ELobbyState = LobbyState.SINGLE;
            LobbyUI_RealConnect.Instance.Initialized(false);
#if OCULUS
            LobbyUI_RealConnect.Instance.OnClickStrogeButton();
#else
            //LobbyUI_RealConnect.Instance.OnClickStrogeButton(true);
            LobbyUI_RealConnect.Instance.underMenuToggleList[1].isOn = true;
#endif
        }

        volumeToggle.SetIsOnWithoutNotify(false);
        volumeToggle.targetGraphic.gameObject.SetActive(true);
        VideoManager.Instance.VideoMute(true);
        voluemSlider.SetValueWithoutNotify(1f);
        VideoManager.Instance.SetVideoVolume(1f);
    }

    // 방장이 stage에서 방을 나가지 않고 벗어났을 경우 방원들과 전부다 대기방으로 돌아온다.
    public void MasterExitRoonWaitRoomOpen()
    {
        VideoManager.Instance.VideoIsPlaying = VIDEO_ISPLAYING.STOP;

        this.gameObject.SetActive(false);

        Managed.XRHubSceneManager.Instance.SceneloadComplete += () => LobbyUI_RealConnect.Instance.Initialized(false);
        Managed.XRHubSceneManager.Instance.SceneloadComplete += () => LobbyUI_RealConnect.Instance.WaitRoomOpen();

#if OCULUS

#else

#endif

        Managed.XRHubSceneManager.Instance.SceneChange(SceneState.LOBBY);
    }

    // 비디오 음소거 설정 (VideoPlayer의 소리 설정)
    public void SetMute(Toggle toggle)
    {
        toggle.targetGraphic.gameObject.SetActive(!toggle.isOn);

        if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.MULTI)
        {
            if (PhotonNetwork.IsMasterClient)
                voluemSlider.interactable = !toggle.isOn;

            PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.MUTE, toggle.isOn.ToString());
        }
        else
            VideoManager.Instance.VideoMute(!toggle.isOn);
    }

    // 비디오 볼륨 설정 (VideoPlayer의 소리 설정)
    public void SetMute(float value)
    {
        if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.MULTI)
        {
            if (PhotonNetwork.IsMasterClient && value == 0f)
                volumeToggle.isOn = true;
            else
                volumeToggle.isOn = false;

            PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.VOL, value.ToString());
        }
        else if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.STORGE)
        {
            VideoManager.Instance.SetVideoVolume(value);
        }
        //VideoManager.Instance.SetVideoVolume(value);
    }

    // 포인트 버튼 클릭.
    public void SetPointerSetting(bool isOn)
    {
        PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.POINTERSETTING, isOn.ToString());
    }

    // 시간 이동 중
    bool isTimeMoving = false;
    // 비디오 시간 조절시 비디오 타임을 설정해주는 함수
    public void SetTimeSync(float time)
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            isTimeMoving = true;

        VideoManager.Instance.SetVideoTime(time);
        seekTime = time;
        Debug.Log($"[StageUI_Realconnect] SetTimeSync(float time) : {time}");
    }

    // 방장이 마지막으로 움직인 시간을 저장하기 위한 변수(가장 마지막에 변화된 시간으로 값을 보내준다.)
    float seekTime = 0;
    // 비디오 시간 조절을 마쳤을때 방원에게 옮긴 시간을 날려준다.
    public void SendTimeSync()
    {
        if (LobbyUI_RealConnect.Instance.ELobbyState == LobbyState.MULTI)
        {
            PropertyManager.Instance.AddRoomVideoPlayModeProperty(VIDEO_PROPERTY_KEY.PLAYTIME, seekTime.ToString());
            Debug.Log($"[StageUI_Realconnect] MasterVideoStart : {seekTime}");
            isTimeMoving = false;
        }
    }

    // 비디오 슬라이드 업데이트를 할지 안 할지 판단하는 변수
    bool UpdateFlag { get; set; } = false;
    // 비디오 플레이어의 시간을 가져와서 슬라이드 바에 표시해준다.

#if OCULUS
    InputDevice rightDevice;
    InputDevice leftDevice;
    bool isRightTriggerButtonAction = false;
#endif

    public void CameraResetOnClick()
    {
        if (GyroCamera.instance != null)
            GyroCamera.instance.ResetOnClick();
    }

    // 생성하는 object[] 구분 처리 추가
    public object[] SetCreateData(string objName = "")
    {
        object[] data = null;

        switch (stagePopupType)
        {
            case StagePopupType.MESSAGE:
                data = new object[]
               {
                    realConnectMessagePopup.SelectMessage
               };

                break;

            case StagePopupType.EMPHASIS:

                data = new object[]
              {
                  objName,
                  emphasisPopup.expressionIndex++
              };

                break;
        }

        return data;
    }

    public void CreateStageUI()
    {
        string _createObject = string.Empty;
        Vector3 _pos = Vector3.zero;
        Quaternion _quaternion = Quaternion.identity;

        switch (stagePopupType)
        {
            case StagePopupType.MESSAGE:
                {
                    _createObject = realConnectMessagePopup.messageObj.name;
#if OCULUS
                    _pos = VRcamera.instance.currentMakeObj.transform.position;
                    _quaternion = VRcamera.instance.currentMakeObj.transform.rotation;
#else
                    _pos = LobbyUI_RealConnect.Instance.stageUI.makeObj.transform.position;
                    _quaternion = LobbyUI_RealConnect.Instance.stageUI.makeObj.transform.rotation;
#endif
                }
                break;

            case StagePopupType.EMPHASIS:
                {
                    _createObject = emphasisPopup.selectEmphasis.name;
#if OCULUS
                    _pos = VRcamera.instance.currentMakeObj.transform.position;
                    _quaternion = VRcamera.instance.currentMakeObj.transform.rotation;
#else
                    _pos = LobbyUI_RealConnect.Instance.stageUI.makeObj.transform.position;
                    _quaternion = LobbyUI_RealConnect.Instance.stageUI.makeObj.transform.rotation;
#endif
                }
                break;

        }

        GameObject destoryObject;

#if OCULUS
        destoryObject = VRcamera.instance.currentMakeObj;
#else
        destoryObject = LobbyUI_RealConnect.Instance.stageUI.makeObj;
#endif

        Destroy(destoryObject);

        if (PhotonNetwork.IsMasterClient)
        {
            if (stagePopupType == StagePopupType.MESSAGE)
                ARP_NetMain.Instance.CreateSyncObject(_createObject, _pos, _quaternion, 0, SetCreateData());
            else
                ARP_NetMain.Instance.CreateSyncObject("RealConnect_WrapperObj", _pos, _quaternion, 0, SetCreateData(_createObject));
        }
        else
        {
            if (realConnectAvatar != null && realConnectAvatar.photonSerializeView != null)
                realConnectAvatar.photonSerializeView.photonView.RPC("RPC_SetReceive_GuestRequestMakeExpression", RpcTarget.MasterClient, _createObject, _pos, _quaternion.eulerAngles);
        }

        stagePopupType = StagePopupType.NONE;


    }

    //[김성민] 비디오 슬라이드 바 움직임과 VR 트리거 체크를 해서 움직인다.
    private void Update()
    {
        if (!isTimeMoving)
        {
            if (UpdateFlag)
            {
                float now = (float)VideoManager.Instance.GetVideoTime();
                // 현재 이동한 값과 실제 비디오 값의 차이가 1초 이상 나면서 업데이트를 잠시 막는다.(이동한 값의 반영 시간 동안 잠시 업데이트를 막음)
                if (Mathf.Abs(seekTime - now) > 1f)
                    return;
                else
                    UpdateFlag = false;
            }

            float max = (float)VideoManager.Instance.GetVideoLength();
            videoSlider.maxValue = max;
            float current = (float)VideoManager.Instance.GetVideoTime();
            videoSlider.SetValueWithoutNotify(current);
            videoLenght.text = $"{(int)max / 60}:{(int)max % 60}";
            videoTime.text = $"{(int)current / 60}:{(int)current % 60}";
        }
        else
        {
            UpdateFlag = true;
            float current = seekTime;
            float max = (float)VideoManager.Instance.GetVideoLength();
            videoSlider.maxValue = max;
            videoLenght.text = $"{(int)max / 60}:{(int)max % 60}";
            videoTime.text = $"{(int)current / 60}:{(int)current % 60}";
        }

        // 트리거 체크
        if (stagePopupType != StagePopupType.NONE)
        {
#if OCULUS
            ///오른손이 현재 연결되 있는지 체크해서 기기 연결을 한다.
            if (!rightDevice.isValid)
            {
                rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }

            ///왼손이 현재 연결되 있는지 체크해서 기기 연결을 한다.
            if (!leftDevice.isValid)
            {
                leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            }

            //Trigger 클릭시 작동하는 함수
            if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction)
            {
                CreateStageUI();
            }
#else
            if (Input.touchCount != 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
            {
                CreateStageUI();
            }
#endif
        }
    }
}

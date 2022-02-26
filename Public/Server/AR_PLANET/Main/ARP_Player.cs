using ExitGames.Client.Photon;
using Photon.Pun;
// using ExitGames.Client.Photon.Voice;
//using Photon.Voice.PUN;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Photon.Realtime;
using PhotonWrapper;
using LightShaft.Scripts;

// 룸 플레이어 객체
// RPC 등을 여기서 구현한다.
// 반드시 플레이어 프리팹에 추가되어 있어야 한다.
// NetRoomPlayer.prefab 참고
public class ARP_Player : MonoBehaviourPunCallbacks
{
    // 포톤 뷰
    public ARPPhotonSerializeView photonSerializeView = null;

    // [임시]
    //public PhotonVoiceView photonVoiceView;

    // public PlayerUI playerUI;

    Transform _cameraPosition = null;

    // 빌보드로 표시할 유저 정보 UI
    // 추후 추가 스크립트 필요할듯 (분리 필요)
    GameObject _playerInfo = null;
    TMPro.TextMeshPro _playerName = null;

    // 빌보드기능 처리
    AimConstraint _AimConstraint = null;

    Vector3 _vMoveEndPos = Vector3.zero;
    Vector3 _vMoveEndRot = Vector3.zero;

    public GameObject playerNeck;
    public GameObject playerRightHand;
    public GameObject playerLeftHand;
    public List<SkinnedMeshRenderer> skinnedMeshRenderers;

    public bool settingEnd = false;

    public GameObject avartInfoUI;

    // [철진] 아바타 추가
    public string currentPlayerAvatar;

    // [철진] 아바타 추가
    private AsyncOperationHandle currentAvatarOperationHandle;

    public GameObject model;

    public void Start()
    {
        ObjectManager.Instance.ARPPlayerAdd(this);

        if (photonSerializeView == null)
            photonSerializeView = GetComponent<ARPPhotonSerializeView>();

        // [임시]
        //photonVoiceView = gameObject.GetComponent<PhotonVoiceView>();

        string _avatar = "";
        if (photonSerializeView.photonView.IsMine)
        {
#if PC_OBSERVER
            _avatar = "PC_OBSERVER_AVATAR";
#else
            if (string.IsNullOrEmpty(DataManager.AVATAR_NAME))
                DataManager.AVATAR_NAME = "A01_Avatar1";
            _avatar = DataManager.AVATAR_NAME;
#endif
            //[김성민] PlayerCostomPropertyAdd
            PropertyManager.Instance.PlayerCostomPropertyAdd(PLAYER_PROPERTY_KEY.AVATAR, _avatar);

            //[김성민] 자기 아바타는 로컬로 자기가 생성.(프로퍼티 매니저에서는 리스트에서 찾아내는데 현재 리스트에 추가를 생성후로 옮겨서 자기것을 생성안해서 로컬로 변경)
            //SetPlayerAvatar(_avatar);
        }
        else
        {
            //[김성민] 다른 유저일시 처음에 리스트에 추가되기 전에 정보가 와서 타이밍이 어긋나도 아바타 생성을 진행하기 위한 준비.
            if (photonSerializeView.photonView.Owner.CustomProperties.TryGetValue(PLAYER_PROPERTY_KEY.AVATAR.ToString(), out object avatarName))
            {
                SetPlayerAvatar(avatarName.ToString());
            }
        }
    }

    private void OnDestroy()
    {
        if (ObjectManager.Instance != null)
            ObjectManager.Instance.ARPPlayerRemove(this);
    }


    // [철진] 아바타 추가
    public void SetPlayerAvatar(string _avatarName)
    {
        // 방장일 때
        if (photonSerializeView.photonView.Owner.UserId.Equals(LobbyUI.Instance.currentRoom.GetRoomResource().user_id))
        {
            string _s = string.Format("{0}_{1}", _avatarName, "Admin");

            Debug.Log("[ 철진 / Avatar / 방장 ] ARP_Player Avatar Set : " + _s);

            if (currentPlayerAvatar != _s)
                currentPlayerAvatar = _s;
        }
        else
        {
            Debug.Log("[ 철진 / Avatar / 방원 ] ARP_Player Avatar Set : " + _avatarName);

            if (currentPlayerAvatar != _avatarName)
                currentPlayerAvatar = _avatarName;
        }
        Debug.Log("[ 철진 / Avatar / 방원 ] ARP_Player Avatar Addressable Load : " + currentPlayerAvatar);

        Addressables.InstantiateAsync(currentPlayerAvatar, this.transform).Completed += (AsyncOperationHandle<GameObject> obj) => { AvatarMakeEnd(obj.Result); };
    }

    private bool init = false;

    public void AvatarMakeEnd(GameObject avart)
    {
        Debug.Log("[ 철진 / Avatar ] ARP_Player Avatar 생성 완료");

        model = avart;

        avart.transform.localPosition = Vector3.zero;
        avart.transform.localRotation = Quaternion.identity;
        AvartInfomation avartInfomation = avart.GetComponent<AvartInfomation>();

        if (avartInfomation != null)
        {
            this.playerNeck = avartInfomation.playerNeck;
            this.playerLeftHand = avartInfomation.leftHand;
            this.playerRightHand = avartInfomation.rightHand;

            if (skinnedMeshRenderers.Count != 0)
            {
                skinnedMeshRenderers.Clear();
            }

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in avartInfomation.skinnedMeshRenderers)
                skinnedMeshRenderers.Add(skinnedMeshRenderer);

            photonSerializeView.AddSerializeObjects(this.gameObject, true, true);
            photonSerializeView.AddSerializeObjects(playerNeck, false, true);

            if (this.playerLeftHand != null)
            {
                photonSerializeView.AddSerializeObjects(this.playerLeftHand, true, true);
                photonSerializeView.AddSerializeObjects(this.playerRightHand, true, true);
            }
        }

        // 방장이면 룸 생성 로그
        if (PhotonNetwork.IsMasterClient && !init)
        {
            if (photonSerializeView.photonView.IsMine)
            {
                ARP_WebProc.Instance.AddLogRoomCreate(ARP_NetMain.Instance.GetUserID(), LobbyUI.Instance.currentRoom.roomName);

                // 방장이면 스테이지 정보 등록
                PropertyManager.Instance.AddRoomPlayModeProperty(ROOM_PROPERTY_KEY.STAGE, DataManager.STAGE_NAME);
            }
        }
        else if (!init)
        {
            string room_id = LobbyUI.Instance.currentRoom.GetProperty(ARP_NetRoom.KEY_Room_Id);

            if (string.IsNullOrEmpty(room_id))
                Debug.Log("룸 ID 오류");
            else
                ARP_WebProc.Instance.AddLogRoomJoin(ARP_NetMain.Instance.GetUserID(), LobbyUI.Instance.currentRoom.roomName, room_id);
        }
        init = true;

        if (PublicUI.Instance.auto)
        {
            // 룸 입장 시 타이틀 출력
            LobbyUI.Instance.playerList.RoomTitleObject.gameObject.SetActive(true);
            LobbyUI.Instance.playerList.t_RoomTitle.text = LobbyUI.Instance.currentRoom.roomName;
        }
        if (!PublicUI.Instance.auto)
        {
            // 플레이어 카운트 
            LobbyUI.Instance.playerList.Open();
        }
        ArpPlayerSetting();
    }

    public void ArpPlayerSetting()
    {
        // 카메라 오브젝트 참조
        if (_cameraPosition == null)
        {
            _cameraPosition = Camera.main.transform;
        }
        // [임시]
        //photonVoiceView = gameObject.GetComponent<PhotonVoiceView>();

        if (photonSerializeView.photonView == null)
            Debug.LogError("PhtonView is NULL!!");
        // [철진] Multu Scene 추가 시작
        // 포톤뷰가 있다면,
        else
        {
            // 나인지 체크
            //if (photonSerializeView.photonView.IsMine)
            //{
            //    // 방장이 아니라면,
            //    if (!PhotonNetwork.IsMasterClient)
            //    {
            //        // 방장에게 스테이지 이름 요청
            //        ARP_NetMain.Instance.Send_SelectStageName();
            //    }
            //}
        }

        // [철진] Multu Scene 추가 끝
        if (_playerInfo == null)
        {
            //AddressableManager.Instance.ObjectLoad("Avatar Info UI", Vector3.zero, Quaternion.identity, this.gameObject.transform, (obj) => SettingPlayerinfo(obj));
            Addressables.InstantiateAsync("Avatar Info UI", this.transform).Completed += (AsyncOperationHandle<GameObject> obj) =>
            {
                obj.Result.transform.localPosition = Vector3.zero;
                obj.Result.transform.localRotation = Quaternion.identity;
                SettingPlayerinfo(obj.Result);
            };
        }
        else
        {
            SettingPlayerinfo(_playerInfo.gameObject);
        }

        ////[김성민] 공유 모드시 아바타 리스트에 추가
        //ObjectManager.Instance.ARPPlayerAdd(this);
    }

    public void SettingPlayerinfo(GameObject avatarInfo)
    {
        avartInfoUI = avatarInfo;
        avatarInfo.transform.localPosition = new Vector3(0, 0.3f, 0);
        avatarInfo.transform.localRotation = Quaternion.identity;
        _playerInfo = avatarInfo;
        _playerName = _playerInfo.transform.Find("Name").GetComponent<TMPro.TextMeshPro>();

        // 초기화
        Init();
        PlayerInfoUISet();

        // 플레이어 라면
        if (photonSerializeView.photonView.IsMine)
        {
            // 내 아바타 끄기
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
                skinnedMeshRenderer.enabled = false;

            _playerInfo.SetActive(false);

            //[김성민] 카메라에 오디오 리스너가 붙어있다.
#if !OCULUS && !VIVE && !HOLOLENS2
            this.gameObject.AddComponent<AudioListener>();
#endif
            ///[김성민]현재 방의 상태 요청
            ARP_NetMain.Instance.Call_CurrentPlayMode();
        }
        // 아닐 경우
        else
        {
            // 목이 꺼진 아바타는 PC옵져버가 사용하는 아바타로 인포 UI를 끈다.

            if (!this.playerNeck.activeSelf)
            {
                _playerInfo.SetActive(false);
            }
        }

        _playerName.text = photonSerializeView.photonView.Owner.NickName;

#if (AUTOSTUDIO && PC_OBSERVER && !settingEnd)
        // [김성민] 아바타 생성이 끝난 후 카메라 세팅
        PCObserverCamera.instance.PlayerFirstPersonCameraSet();
#endif

        settingEnd = true;
    }

    void PlayerInfoUISet()
    {

        ConstraintSource _conSource = new ConstraintSource();
        // 바라볼 카메라 설정
        if (_cameraPosition != null)
        {
            _conSource.sourceTransform = _cameraPosition;
            _conSource.weight = 1;
        }

        if (_AimConstraint == null && _playerInfo != null)
            _AimConstraint = _playerInfo.transform.GetComponent<AimConstraint>();

        _AimConstraint.AddSource(_conSource);

        if (!_AimConstraint.constraintActive)
            _AimConstraint.constraintActive = true;
    }

    // 플레이어 일때
    void Init()
    {
        ///[김성민]
        ///룸에서 플래그 오브젝트 기준이다.

        Transform flagObj = null;

        flagObj = ObjectManager.Instance.FlagObject.transform;

#if OCULUS || VIVE
        if (!PublicUI.Instance.auto)
            flagObj = ObjectManager.Instance.HubFlagObject.transform;
#endif

        if (ObjectManager.Instance.FlagObject != null)
            this.transform.parent = flagObj;
    }

    void Update()
    {
        if (!settingEnd)
        {
            return;
        }

        ///[김성민]
        ///캐릭터 움직임을 관장한다.
        ///기본적으로 받는 값은 기기값으로 추정. 그것을 x,y,z축으로 나누었다.
        ///가정은 태블릿의 카메라 방향을 기준으로 잡았다.
        ///x축은 카메라 방향으로 회전, y축은 카메라를 옆으로 회전, z축은 카메라를 세우는 방향이다.
        ///캐릭터의 경우
        ///x축은 사용하지 않고, y축은 몸을 회전(옆으로), z축은 목을 회전(위,아래)
        ///이 경우 y축은 맞지만 목에는 카메라의 x축의 값을 대입하였다.

        if (_cameraPosition != null && photonSerializeView.photonView.IsMine)
        {
            ///[김성민] 아바타 위치의 경우에는 플래그 오브젝트 아래에 있다. 그래서 카메라에서 플래그를 뺀 좌표를 로컬 포지션으로 넣어준다.(카메라의 부모를 플래그로 설정한 효과)
            this.transform.localPosition = _cameraPosition.position - ObjectManager.Instance.FlagObject.transform.position;

            Vector3 characterRotation = _cameraPosition.rotation.eulerAngles;
            characterRotation.x = 0;
            characterRotation.z = 0;
            this.transform.rotation = Quaternion.Euler(characterRotation);

            Vector3 neckRotation = _cameraPosition.localRotation.eulerAngles;

            ///[김성민]
            ///180을 빼는 이유는 기기의 x축의 값이 0부터 360도로 마이너스의 값이 없다.
            ///그래서 180이후는 뒤이기에 값을 빼주었다.
            if (neckRotation.x > 180)
            {
                neckRotation.x -= 360;
            }

            neckRotation.y = 0;
            neckRotation.z = Mathf.Clamp(neckRotation.x, -60, 90);          ///[김성민] 목의 각도를 제한.
            neckRotation.x = 0;
            if (playerNeck != null)
                playerNeck.transform.localRotation = Quaternion.Euler(neckRotation);

#if OCULUS || VIVE
            //[김성민] 손 임시 테스트
            //if (this.playerRightHand != null)
            //{
            //    model.GetComponent<AvartInfomation>().HandUpdate(VRcamera.instance.xrLeftRayInteractor.transform.rotation, VRcamera.instance.xrLeftRayInteractor.transform.position, VRcamera.instance.xrRightRayInteractor.transform.rotation, VRcamera.instance.xrRightRayInteractor.transform.position);
            //}
#endif
        }
    }

    //[김성민] 분해모드 정보
    public void RPC_SetObjectPosition(Vector3 PositionValue, int SelectNum, int assembleNum)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceivePosition", RpcTarget.Others, PositionValue.x, PositionValue.y, PositionValue.z, SelectNum, assembleNum);
    }

    [PunRPC]
    public void RPC_SetReceivePosition(float PositionValue_x, float PositionValue_y, float PositionValue_z, int SelectNum, int assembleNum, PhotonMessageInfo info)
    {
        if (assembleNum == -9)
        {
            //ObjectManager.Instance.AllObjectList[SelectNum].GetComponent<ObjectMove>().Move(new Vector3(PositionValue_x, PositionValue_y, PositionValue_z));

            // 220222 / KCJ /Test
            ObjectManager.Instance.AllObjectInfo[SelectNum].obj.GetComponent<ObjectMove>().Move(new Vector3(PositionValue_x, PositionValue_y, PositionValue_z));
        }
        else
        {
            //ObjectManager.Instance.AllObjectList[SelectNum].GetComponent<WrapperObject>().objectData.assemblyObjects[assembleNum].GetComponent<ObjectMove>().Move(new Vector3(PositionValue_x, PositionValue_y, PositionValue_z));

            // 220222 / KCJ /Test
            ObjectManager.Instance.AllObjectInfo[SelectNum].obj.GetComponent<WrapperObject>().objectData.assemblyObjects[assembleNum].GetComponent<ObjectMove>().Move(new Vector3(PositionValue_x, PositionValue_y, PositionValue_z));
        }
    }

    public void RPC_PanelVolume(float Volume, int SelectNum)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceivePanelVolume", RpcTarget.Others, Volume, SelectNum);
    }

    [PunRPC]
    public void RPC_SetReceivePanelVolume(float Volume, int SelectNum, PhotonMessageInfo info)
    {
        //ObjectManager.Instance.AllObjectList[SelectNum].GetComponentInChildren<YoutubePlayer>().videoPlayer.GetComponent<AudioSource>().volume = Volume;
        //ObjectManager.Instance.AllObjectList[SelectNum].GetComponentInChildren<YoutubePlayer>().videoPlayer.GetComponent<AudioSource>().volume = Volume;
        //ObjectManager.Instance.AllObjectList[SelectNum].GetComponentInChildren<YoutubePlayer>().audioPlayer.SetDirectAudioVolume(0, Volume);

        // 220222 / KCJ /Test
        ObjectManager.Instance.AllObjectInfo[SelectNum].obj.GetComponentInChildren<YoutubePlayer>().videoPlayer.GetComponent<AudioSource>().volume = Volume;
        ObjectManager.Instance.AllObjectInfo[SelectNum].obj.GetComponentInChildren<YoutubePlayer>().audioPlayer.SetDirectAudioVolume(0, Volume);
    }

    public void RPC_PlayOrPause(int IsPause, int SelectNum)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_PlayOrPause", RpcTarget.All, IsPause, SelectNum);
    }

    [PunRPC]
    public void RPC_SetReceive_PlayOrPause(int IsPause, int SelectNum, PhotonMessageInfo info)
    {
        // YoutubePlayer youtubePlayer = ObjectManager.Instance.AllObjectList[SelectNum].GetComponentInChildren<YoutubePlayer>();
        // 220222 / KCJ /Test
        YoutubePlayer youtubePlayer = ObjectManager.Instance.AllObjectInfo[SelectNum].obj.GetComponentInChildren<YoutubePlayer>();
        if (youtubePlayer != null)
        {
            if (IsPause == 1)
            {
                string s = youtubePlayer.youtubeUrl;
                youtubePlayer.Play(s);
            }
            else
            if (IsPause == 2)
            {
                youtubePlayer.Play();
            }
            else
            if (IsPause == 3)
            {
                youtubePlayer.Pause();
            }
            else
            if (IsPause == 4)
                youtubePlayer.Stop();
        }
    }

    public void RPC_AniPlayOrPause(int IsPause, int selectNum)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_AniPlayOrPause", RpcTarget.All, IsPause, selectNum);
    }

    [PunRPC]
    public void RPC_SetReceive_AniPlayOrPause(int IsPause, int selectNum, PhotonMessageInfo info)
    {
        //if (ObjectManager.Instance.AllObjectList[selectNum] != null)

        // 220222 / KCJ /Test
        if (ObjectManager.Instance.AllObjectInfo[selectNum] != null)
        {
            //ObjectAnimationListInfo objectAnimationListInfo = ObjectManager.Instance.AllObjectList[selectNum].GetComponent<ObjectAnimationListInfo>();

            // 220222 / KCJ /Test
            ObjectAnimationListInfo objectAnimationListInfo = ObjectManager.Instance.AllObjectInfo[selectNum].obj.GetComponent<ObjectAnimationListInfo>();

            if (objectAnimationListInfo.animation != null)
            {
                if (IsPause == 1)
                {
                    objectAnimationListInfo.AnimationPlay();
                }
                else if (IsPause == 0)
                {
                    objectAnimationListInfo.AnimationPause();
                }
                else if (IsPause == 2)
                {
                    objectAnimationListInfo.AnimationStop();
                }
            }
        }
    }

    public void RPC_AniChangeNumber(int aniNum, int selectNum)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_AniChangeNumber", RpcTarget.All, aniNum, selectNum);
    }

    [PunRPC]
    public void RPC_SetReceive_AniChangeNumber(int aniNum, int selectNum, PhotonMessageInfo info)
    {
        //if (ObjectManager.Instance.AllObjectList[(selectNum)] != null)

        // 220222 / KCJ /Test
        if (ObjectManager.Instance.AllObjectInfo[(selectNum)] != null)
        {
            // 220222 / KCJ /Test
            // ObjectAnimationListInfo objectAnimationListInfo = ObjectManager.Instance.AllObjectList[(selectNum)].GetComponent<ObjectAnimationListInfo>();

            ObjectAnimationListInfo objectAnimationListInfo = ObjectManager.Instance.AllObjectInfo[(selectNum)].obj.GetComponent<ObjectAnimationListInfo>();

            objectAnimationListInfo.AnimationChange(aniNum);
        }
    }

    public void RPC_SelectObject(int select, string modelName)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_SelectObject", RpcTarget.Others, select, modelName);
    }

    [PunRPC]
    public void RPC_SetReceive_SelectObject(int select, string modelName, PhotonMessageInfo info)
    {
        ObjectManager.Instance.currentSelectObjectNum = select;
        if (select != -1)
        {
            //ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectList[select];
            // 220222 / KCJ /Test
            ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectInfo[select].obj;
            LobbyUI.Instance.SelectModelName(modelName);
        }
        else
        {
            ObjectManager.Instance.currentSelectObject = null;
            LobbyUI.Instance.SelectModelName(string.Empty);
        }
    }

    public void RPC_CallCurrentSelectModelNumber()
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_CallCurrentSelectModelNumber", RpcTarget.MasterClient);
    }

    [PunRPC]
    public void RPC_SetReceive_CallCurrentSelectModelNumber(PhotonMessageInfo info)
    {
        if (ObjectManager.Instance.currentSelectObject != null)
            RPC_SelectObject(ObjectManager.Instance.currentSelectObjectNum, ObjectManager.Instance.currentSelectObject.name);
    }

    //[김성민] 방에 있을때 방장이 플레이 모드 들어갔을때 보내는 rpc
    public void RPC_PlayModeInit(PlayType _e)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_PlayModeInit", RpcTarget.Others, _e);
    }

    [PunRPC]
    public void RPC_SetReceive_PlayModeInit(PlayType _e, PhotonMessageInfo info)
    {
        switch (_e)
        {
            case PlayType.None:
                {
                    if (LobbyUI.Instance.objectEdit.eCurrentEditType == PlayType.Scenario)
                    {
                        LobbyUI.Instance.objectEdit.scenarioPopup.Close();
                    }

                    else if (LobbyUI.Instance.objectEdit.eCurrentEditType == PlayType.Assembly)
                    {
                        LobbyUI.Instance.objectEdit.assemblyPopup.GuestInit();
                    }

                    LobbyUI.Instance.objectEdit.eCurrentEditType = PlayType.None;
                }
                break;

            case PlayType.Assembly:
                {
                    LobbyUI.Instance.objectEdit.eCurrentEditType = PlayType.Assembly;

                    //foreach (GameObject _o in ObjectManager.Instance.AllObjectList.Values)
                    // 220222 / KCJ /Test
                    foreach (ObjectInfo _i in ObjectManager.Instance.AllObjectInfo.Values)
                    {
                        GameObject _o = _i.obj;

                        if (_o != ObjectManager.Instance.currentSelectObject)
                        {
                            _o.gameObject.SetActive(false);
                        }
                        else
                        {
                            _o.gameObject.SetActive(true);

                            if (_o.GetComponent<WrapperObject>() != null)
                                _o.GetComponent<WrapperObject>().objectAnimation.AnimationSetting(false);
                        }
                    }
                }
                break;
            case PlayType.Scenario:
                {
                    LobbyUI.Instance.objectEdit.eCurrentEditType = PlayType.Scenario;

                    // foreach (GameObject wrapperObject in ObjectManager.Instance.AllObjectList.Values)

                    // 220222 / KCJ /Test
                    foreach (ObjectInfo _i in ObjectManager.Instance.AllObjectInfo.Values)
                    {
                        //wrapperObject.SetActive(false);

                        GameObject _o = _i.obj;
                        _o.SetActive(false);
                    }

                    LobbyUI.Instance.objectEdit.scenarioPopup.Open();
#if OCULUS || VIVE
                    LobbyUI.Instance.FrontToggles.SetActive(false);
#endif

                    LobbyUI.Instance.objectEdit.scenarioPopup.gameObject.SetActive(true);
                    LobbyUI.Instance.objectEdit.scenarioPopup.ControlUISet(false);
                    LobbyUI.Instance.objectEdit.scenarioPopup.ListUI.SetActive(false);
                    if (LobbyUI.Instance.objectEdit.scenarioPopup.ScenarioDatas == null)
                    {
                        LobbyUI.Instance.objectEdit.scenarioPopup.ScenarioDatas = new List<ScenarioData>();
                    }

                    if (LobbyUI.Instance.objectEdit.scenarioPopup.ScenarioDatas.Count != 0)
                    {
                        LobbyUI.Instance.objectEdit.scenarioPopup.ScenarioDatas.Clear();
                    }
                }
                break;
        }
    }

    public void RPC_CallCurrentPlayMode()
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_CallCurrentPlayMode", RpcTarget.MasterClient, ARP_NetMain.Instance.userID);
    }

    [PunRPC]
    public void RPC_SetReceive_CallCurrentPlayMode(string requestUserid, PhotonMessageInfo info)
    {
        int currentEditObjectNum = 0;
        PlayType playType = LobbyUI.Instance.objectEdit.eCurrentEditType;

        switch (playType)
        {
            case PlayType.Assembly:
                currentEditObjectNum = LobbyUI.Instance.objectEdit.assemblyPopup.modelIndex;
                break;

            case PlayType.Scenario:
                currentEditObjectNum = LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex;
                break;
        }

        //RPC_IntrusionPlayMode(requestUserid, LobbyUI.Instance.objectEdit.eCurrentEditType, ObjectManager.Instance.AllObjectList.Count, currentEditObjectNum);
        // 220222 / KCJ /Test
        RPC_IntrusionPlayMode(requestUserid, LobbyUI.Instance.objectEdit.eCurrentEditType, ObjectManager.Instance.AllObjectInfo.Count, currentEditObjectNum);
    }

    // [김성민] 난입시 플레이모드 설정
    public void RPC_IntrusionPlayMode(string requsetUserId, PlayType playType, int objectCount, int currentEditObject)
    {
        if (photonSerializeView == null)
            photonSerializeView = GetComponent<ARPPhotonSerializeView>();

        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_IntrusionPlayMode", RpcTarget.Others, requsetUserId, playType, objectCount, currentEditObject);
    }

    [PunRPC]
    public void RPC_SetReceive_IntrusionPlayMode(string requestUserid, PlayType playType, int objectCount, int objectNum, PhotonMessageInfo info)
    {
        if (ARP_NetMain.Instance.userID.Equals(requestUserid))
            StartCoroutine(WaitPlayModeInit(playType, objectCount, objectNum, info));

    }

    IEnumerator WaitPlayModeInit(PlayType playType, int objectCount, int objectNum, PhotonMessageInfo info)
    {
        ///[김성민]모든 오브젝트가 소환되기 까지 기다린다.
        ///지나가도 모든 오브젝트가 로드된 상황은 아니다.
        while (true)
        {
            // if (ObjectManager.Instance.AllObjectList.Count == objectCount)
            // 220222 / KCJ /Test
            if (ObjectManager.Instance.AllObjectInfo.Count == objectCount)
            {
                break;
            }
            yield return new WaitForSeconds(1f);
        }

        LobbyUI.Instance.objectEdit.eCurrentEditType = playType;

        switch (playType)
        {
            case PlayType.Assembly:
                {
                    ObjectManager.Instance.PlayModeObjSetting(objectNum);
                    Rpc_CallAssembly();
                }
                break;
            case PlayType.Scenario:
                {
                    ObjectManager.Instance.PlayModeObjSetting(objectNum);
                    Rpc_CallScenarioInfo();
                }
                break;

            case PlayType.None:
                {
                    RPC_CallCurrentSelectModelNumber();
                }
                break;
        }
    }

    //[김성민] 시나리오 팝업 정보 요청
    public void Rpc_CallScenarioInfo()
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_CallScenarioInfo", RpcTarget.MasterClient);
    }

    [PunRPC]
    public void RPC_SetReceive_CallScenarioInfo(PhotonMessageInfo info)
    {
        RPC_ScenarioInfo(LobbyUI.Instance.objectEdit.scenarioPopup.ScenarioIndexNum, LobbyUI.Instance.objectEdit.scenarioPopup.IndexNum, LobbyUI.Instance.objectEdit.scenarioPopup.play, false, LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex);
    }

    //[김성민] 분해오브젝트 정보 요청
    public void Rpc_CallAssembly()
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_CallAssembly", RpcTarget.MasterClient, ARP_NetMain.Instance.userID);
    }

    [PunRPC]
    public void RPC_SetReceive_CallAssembly(string requestUserId, PhotonMessageInfo info)
    {
        List<object> eventData = new List<object>();
        if (LobbyUI.Instance.objectEdit.eCurrentEditType == PlayType.Assembly)
        {
            //WrapperObject _w = ObjectManager.Instance.AllObjectList[LobbyUI.Instance.objectEdit.assemblyPopup.modelIndex].GetComponent<WrapperObject>();
            // 220222 / KCJ /Test
            WrapperObject _w = ObjectManager.Instance.AllObjectInfo[LobbyUI.Instance.objectEdit.assemblyPopup.modelIndex].obj.GetComponent<WrapperObject>();

            for (int i = 0; i < _w.objectData.assemblyObjects.Count; ++i)
            {
                eventData.Add(_w.objectData.assemblyObjects[i].transform.localPosition);
            }

            photonSerializeView.photonView.RPC("RPC_SetReceive_SendAssemblyInfo", RpcTarget.Others, eventData, LobbyUI.Instance.objectEdit.assemblyPopup.modelIndex, requestUserId);
        }
    }

    [PunRPC]
    public void RPC_SetReceive_SendAssemblyInfo(object[] eventData, int modelIndex, string requestUserId, PhotonMessageInfo info)
    {
        if (ARP_NetMain.Instance.userID.Equals(requestUserId))
        {
            StartCoroutine(WaitAssemblyObjectCreate(eventData, modelIndex));
        }
    }

    IEnumerator WaitAssemblyObjectCreate(object[] eventData, int modelIndex)
    {
        while (true)
        {
            // if (ObjectManager.Instance.AllObjectList[modelIndex].GetComponent<WrapperObject>().objectData.objectState == ObjectState.COMPLETE)

            // 220222 / KCJ /Test
            if (ObjectManager.Instance.AllObjectInfo[modelIndex].obj.GetComponent<WrapperObject>().objectData.objectState == ObjectState.COMPLETE)
                break;

            yield return new WaitForSeconds(1f);
        }

        //if (ObjectManager.Instance.AllObjectList[modelIndex].GetComponent<ObjectAnimationListInfo>().animation.enabled)
        //  ObjectManager.Instance.AllObjectList[modelIndex].GetComponent<ObjectAnimationListInfo>().animation.enabled = true;

        // 220222 / KCJ /Test
        if (ObjectManager.Instance.AllObjectInfo[modelIndex].obj.GetComponent<ObjectAnimationListInfo>().animation.enabled)
            ObjectManager.Instance.AllObjectInfo[modelIndex].obj.GetComponent<ObjectAnimationListInfo>().animation.enabled = true;

        List<Vector3> assemblyObjList = new List<Vector3>();

        for (int i = 0; i < eventData.Length; ++i)
        {
            assemblyObjList.Add((Vector3)eventData[i]);
        }

        for (int i = 0; i < assemblyObjList.Count; ++i)
        {
            if (assemblyObjList[i] != Vector3.zero)
                //ObjectManager.Instance.AllObjectList[modelIndex].GetComponent<WrapperObject>().objectData.assemblyObjects[i].GetComponent<ObjectMove>().Move(assemblyObjList[i]);

                // 220222 / KCJ /Test
                ObjectManager.Instance.AllObjectInfo[modelIndex].obj.GetComponent<WrapperObject>().objectData.assemblyObjects[i].GetComponent<ObjectMove>().Move(assemblyObjList[i]);
        }
    }

    public void RPC_SetPlayerVoiceOnOff(string userId, bool isOn)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_SetPlayerVoiceOnOff", RpcTarget.Others, userId, isOn);
    }

    [PunRPC]
    public void RPC_SetReceive_SetPlayerVoiceOnOff(string userId, bool isOn, PhotonMessageInfo info)
    {
        if (userId.Equals(ARP_NetMain.Instance.userID))
        {
            PlayerUI playerUI = null;

            foreach (GameObject player in LobbyUI.Instance.playerList.PlayerUIPrefabs)
            {
                if (player.GetComponent<PlayerUI>().player.UserId.Equals(userId))
                {
                    playerUI = player.GetComponent<PlayerUI>();
                    break;
                }
            }

            if (!isOn)
            {
                // [임시]
                //PhotonVoiceNetwork.Instance.PrimaryRecorder.TransmitEnabled = false;
                VoiceManager.Instance.StopRecord();

                if (playerUI != null)
                {
                    playerUI.micState = false;
                    playerUI.MiceSetting(false);
                }

#if !PC_OBSERVER
                PublicUI.Instance.instanceMessage.InstanceMessage($"음성권한이 취소되었습니다.");
#endif
            }
            else
            {
                // [임시]
                //PhotonVoiceNetwork.Instance.PrimaryRecorder.TransmitEnabled = true;
                VoiceManager.Instance.StartRecord();
                if (playerUI != null)
                {
                    playerUI.micState = true;
                    playerUI.MiceSetting(true);
                }

#if !PC_OBSERVER
                PublicUI.Instance.instanceMessage.InstanceMessage($"음성권한이 부여되었습니다.");
#endif
            }
        }
    }

    public void RPC_SetPlayerObjectControllerOnOff(string userId, bool permission, string userNickName, bool notificationMessage, bool display)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_SetPlayerObjectControllerOnOff", RpcTarget.All, userId, permission, userNickName, notificationMessage, display);
    }

    [PunRPC]
    public void RPC_SetReceive_SetPlayerObjectControllerOnOff(string userId, bool permission, string userNickName, bool notificationMessage, bool display, PhotonMessageInfo info)
    {
        if (display)
        {
            if (notificationMessage)
                PublicUI.Instance.instanceMessage.InstanceMessage($"{userNickName}에게 발표자 권한이 부여되었습니다.");
            else
                PublicUI.Instance.instanceMessage.InstanceMessage($"{userNickName}의 발표자 권한이 취소되었습니다.");
        }

        if (userId.Equals(ARP_NetMain.Instance.userID))
        {
            PlayerUI playerUI = null;

            foreach (GameObject player in LobbyUI.Instance.playerList.PlayerUIPrefabs)
            {
                if (player.GetComponent<PlayerUI>().player.UserId.Equals(userId))
                {
                    playerUI = player.GetComponent<PlayerUI>();
                    break;
                }
            }

            if (permission)
            {
                ObjectManager.Instance.objectController = true;
                PublicUI.Instance.SettingInteractAlawysAndLockToggle(true);
                LobbyUI.Instance.__ShareToggle.interactable = true;
                if (ARP_NetMain.Instance.SyncObjects.Count != 0)
                {
                    PublicUI.Instance.SettingInteractDeleteToggle(true);
                }

                if (playerUI != null)
                {
                    playerUI.ObjectControllerSetting(true);
                }
                PublicUI.Instance.SettingModeToggle(true);
                if (!PublicUI.Instance.auto)
                {
                    PublicUI.Instance.SettingDrawingToggle(true);
                }
                LobbyUI.Instance.playerList.ObjectControllerId = string.Empty;
            }
            else
            {
                ObjectManager.Instance.objectController = false;
                PublicUI.Instance.AutoSettingInteractOffAllToggle(false);
                LobbyUI.Instance.__ShareToggle.interactable = true;
                PublicUI.Instance.SettingModeToggle(false);
                if (playerUI != null)
                {
                    playerUI.ObjectControllerSetting(false);
                }
            }
        }
        LobbyUI.Instance.playerList.Refresh();
    }

    public void RPC_SetPlayerInfoRequest(string infoUserId, string requestUserId)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_SetPlayerInfoRequest", RpcTarget.Others, infoUserId, requestUserId);
    }

    [PunRPC]
    public void RPC_SetReceive_SetPlayerInfoRequest(string infoUserId, string requestUserId, PhotonMessageInfo info)
    {
        if (infoUserId.Equals(ARP_NetMain.Instance.userID))
        {
            // [임시]
            RPC_SnedPlayerInfo(infoUserId, requestUserId, VoiceManager.Instance.recorder.recording, ObjectManager.Instance.objectController, string.Empty);
        }
    }

    public void RPC_SnedPlayerInfo(string infoUserId, string requestUserId, bool voice, bool objectController, string image)
    {
        if (photonSerializeView == null)
            photonSerializeView = GetComponent<ARPPhotonSerializeView>();

        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_SendPlayerInfo", RpcTarget.Others, infoUserId, requestUserId, voice, objectController);
    }

    [PunRPC]
    public void RPC_SetReceive_SendPlayerInfo(string infoUserId, string requestUserId, bool voice, bool objectController, PhotonMessageInfo info)
    {
        if (requestUserId.Equals(ARP_NetMain.Instance.userID))
        {
            foreach (GameObject player in LobbyUI.Instance.playerList.PlayerUIPrefabs)
            {
                if (player.GetComponent<PlayerUI>().player.UserId.Equals(infoUserId))
                {
                    PlayerUI playerUI = player.GetComponent<PlayerUI>();
                    playerUI.micState = voice;
                    playerUI.MiceSetting(voice);
                    playerUI.ObjectControllerSetting(objectController);
                    break;
                }
            }
        }
    }

    //[김성민] 시나리오 관련 정보를 보내는 함수.
    public void RPC_ScenarioInfo(int scenarioInex, int index, bool aniPlay, bool stop, int scenarioObjecrNum)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_SendScenarioInfo", RpcTarget.Others, scenarioInex, index, aniPlay, stop, scenarioObjecrNum);
    }

    [PunRPC]
    public void RPC_SetReceive_SendScenarioInfo(int scenarioInex, int index, bool aniPlay, bool stop, int scenarioObjectNum, PhotonMessageInfo info)
    {
        if (LobbyUI.Instance.objectEdit.eCurrentEditType == PlayType.Scenario)
        {
            StartCoroutine(WaitScenarioObjectCreate(scenarioInex, index, aniPlay, stop, scenarioObjectNum));
        }
    }

    // [김성민] 시나리오 오브젝트가 생성될 때까지 기다리는 함수.
    IEnumerator WaitScenarioObjectCreate(int scenarioInex, int index, bool aniPlay, bool stop, int scenarioObjectNum)
    {
        //ObjectManager.Instance.AllObjectList[scenarioObjectNum].SetActive(true);
        // 220222 / KCJ /Test
        ObjectManager.Instance.AllObjectInfo[scenarioObjectNum].obj.SetActive(true);

        while (true)
        {
            //if (ObjectManager.Instance.AllObjectList[scenarioObjectNum].GetComponent<WrapperObject>().objectData.objectState == ObjectState.COMPLETE)
            // 220222 / KCJ /Test
            if (ObjectManager.Instance.AllObjectInfo[scenarioObjectNum].obj.GetComponent<WrapperObject>().objectData.objectState == ObjectState.COMPLETE)
                break;

            yield return new WaitForSeconds(1f);
        }

        LobbyUI.Instance.objectEdit.scenarioPopup.ScenarioIndexNum = scenarioInex;
        LobbyUI.Instance.objectEdit.scenarioPopup.IndexNum = index;

        LobbyUI.Instance.objectEdit.scenarioPopup.RecevieInfoSetting(scenarioObjectNum);

        LobbyUI.Instance.objectEdit.scenarioPopup.ReceivePlay(aniPlay, stop);

        //if (!ObjectManager.Instance.AllObjectList[scenarioObjectNum].activeSelf)
        //  ObjectManager.Instance.AllObjectList[scenarioObjectNum].SetActive(true);

        // 220222 / KCJ /Test
        if (!ObjectManager.Instance.AllObjectInfo[scenarioObjectNum].obj.activeSelf)
            ObjectManager.Instance.AllObjectInfo[scenarioObjectNum].obj.SetActive(true);
    }

    // [김성민] 오브젝트 소환후 관련된 애니메이션 정보 요청 함수.
    public void RPC_CallAnimationState(int indexNum, string requestUserId)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_CallAnimationState", RpcTarget.MasterClient, indexNum, requestUserId);
    }

    [PunRPC]
    public void RPC_SetReceive_CallAnimationState(int indexNum, string requestUserId, PhotonMessageInfo info)
    {
        // if (ObjectManager.Instance.AllObjectList.TryGetValue(indexNum, out GameObject gameObject))
        // RPC_SetAnimaionState(indexNum, requestUserId, gameObject.GetComponent<ObjectAnimationListInfo>().currentNum, gameObject.GetComponent<ObjectAnimationListInfo>().isPlay);

        // 220222 / KCJ /Test
        if (ObjectManager.Instance.AllObjectInfo.TryGetValue(indexNum, out ObjectInfo gameObject))
            RPC_SetAnimaionState(indexNum, requestUserId, gameObject.obj.GetComponent<ObjectAnimationListInfo>().currentNum, gameObject.obj.GetComponent<ObjectAnimationListInfo>().isPlay);
    }


    public void RPC_SetAnimaionState(int indexNum, string requestUserId, int aniNum, bool isPlay)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_SetReceive_SetAnimationState", RpcTarget.Others, indexNum, requestUserId, aniNum, isPlay);
    }

    [PunRPC]
    public void RPC_SetReceive_SetAnimationState(int indexNum, string requestUserId, int aniNum, bool isPlay, PhotonMessageInfo info)
    {
        if (ARP_NetMain.Instance.userID.Equals(requestUserId))
        {
            //ObjectManager.Instance.AllObjectList[indexNum].GetComponent<ObjectAnimationListInfo>().isPlay = isPlay;
            //ObjectManager.Instance.AllObjectList[indexNum].GetComponent<ObjectAnimationListInfo>().AnimationChange(aniNum);

            // 220222 / KCJ /Test
            ObjectManager.Instance.AllObjectInfo[indexNum].obj.GetComponent<ObjectAnimationListInfo>().isPlay = isPlay;
            ObjectManager.Instance.AllObjectInfo[indexNum].obj.GetComponent<ObjectAnimationListInfo>().AnimationChange(aniNum);
        }
    }

    public void RPC_DrawingLineType(LineType lineType, List<List<Vector3>> m_PointList = null, List<LineInfo> m_LineInfo = null)
    {
        if (photonSerializeView.photonView != null)
        {
            List<object> eventData = null;
            if (m_PointList != null)
            {
                eventData = new List<object>();
                eventData.Add(m_PointList.Count);
                for (int i = 0; i < m_PointList.Count; ++i)
                {
                    eventData.Add(m_PointList[i].Count);
                    for (int j = 0; j < m_PointList[i].Count; ++j)
                    {
                        Quaternion rRot = Quaternion.Inverse(ObjectManager.Instance.FlagObject.transform.rotation);

#if OCULUS || VIVE
                        if (!PublicUI.Instance.auto)
                            rRot = Quaternion.Inverse(ObjectManager.Instance.HubFlagObject.transform.rotation);
#endif
                        eventData.Add(rRot * (m_PointList[i][j] - ObjectManager.Instance.FlagObject.transform.position));
                    }
                }
            }

            List<object> lineData = null;
            if (m_LineInfo != null)
            {
                lineData = new List<object>();
                lineData.Add(m_LineInfo.Count);
                for (int i = 0; i < m_LineInfo.Count; ++i)
                {
                    ///[김성민]
                    ///라인 정보가 추가되면 여기에 새로 값을 담아서 넘겨준다.
                    lineData.Add(m_LineInfo[i].width);
                    lineData.Add(new Vector3(m_LineInfo[i].color.r, m_LineInfo[i].color.g, m_LineInfo[i].color.b));
                }
            }
            photonSerializeView.photonView.RPC("RPC_SetReceive_DrawingLineType", RpcTarget.Others, lineType, eventData, lineData);
        }
    }

    [PunRPC]
    public void RPC_SetReceive_DrawingLineType(LineType lineType, object[] eventData, object[] infoList, PhotonMessageInfo info)
    {
        switch (lineType)
        {
            case LineType.CREATE:
                {
                    int num = 0;

                    List<List<Vector3>> pointList = new List<List<Vector3>>();

                    int count = (int)eventData[num++];
                    for (int i = 0; i < count; ++i)
                    {
                        List<Vector3> line = new List<Vector3>();
                        int len = (int)eventData[num++];
                        for (int j = 0; j < len; ++j)
                        {
                            Quaternion rot = ObjectManager.Instance.FlagObject.transform.rotation;
                            Vector3 flagPosition = ObjectManager.Instance.FlagObject.transform.position;

#if OCULUS || VIVE
                            if (!PublicUI.Instance.auto)
                            {
                                rot = Quaternion.Inverse(ObjectManager.Instance.HubFlagObject.transform.rotation);
                                flagPosition = ObjectManager.Instance.HubFlagObject.transform.position;
                            }
#endif

                            line.Add(rot * (Vector3)eventData[num++] + flagPosition);
                        }
                        pointList.Add(line);
                    }

                    num = 0;
                    count = (int)eventData[num++];
                    List<LineInfo> lineInfoList = new List<LineInfo>();
                    for (int i = 0; i < count; ++i)
                    {
                        ///[김성민]
                        ///라인 정보가 추가되면 여기서 값을 채워서 리스트에 넣어준다.
                        LineInfo lineInfo = new LineInfo();
                        lineInfo.width = (int)infoList[num++];
                        Vector3 colorVector = Vector3.zero;
                        colorVector = (Vector3)infoList[num++];
                        Color color = Color.green;
                        color.r = colorVector.x;
                        color.g = colorVector.y;
                        color.b = colorVector.z;
                        lineInfo.color = color;
                        lineInfoList.Add(lineInfo);
                    }
#if !PC_OBSERVER
#if !OCULUS && !VIVE
                    ARCamera.instance.drawObject.RecevieLine(pointList, lineInfoList);
#else
                    VRcamera.instance.drawObject.RecevieLine(pointList, lineInfoList);
#endif
#else
                    PCObserverCamera.instance.drawObject.RecevieLine(pointList, lineInfoList);
#endif
                }
                break;

            case LineType.REMOVE:
#if !PC_OBSERVER
#if !OCULUS && !VIVE
                ARCamera.instance.drawObject.Remove();
#else
                VRcamera.instance.drawObject.Remove();        
#endif
#else
                PCObserverCamera.instance.drawObject.Remove();
#endif
                break;

            case LineType.UNDO:
#if !PC_OBSERVER
#if !OCULUS && !VIVE
                ARCamera.instance.drawObject.Undo();
#else
                VRcamera.instance.drawObject.Undo();
#endif
#else
                PCObserverCamera.instance.drawObject.Undo();
#endif
                break;
        }
    }

    public void Send_CTModelSetRenderModeOnClick(int _renderMode)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_Send_CTModelSetRenderModeOnClick", RpcTarget.Others, _renderMode);
    }

    [PunRPC]
    public void RPC_Send_CTModelSetRenderModeOnClick(int _renderMode, PhotonMessageInfo info)
    {
        LobbyUI.Instance.objectEdit.cTPopup.SetRenderModeOnClick(_renderMode);
    }

    public void Send_CTModelSetVisibilityWindow(float _visibilityWindow)
    {
        if (photonSerializeView.photonView != null)
            photonSerializeView.photonView.RPC("RPC_Send_CTModelSetVisibilityWindow", RpcTarget.Others, _visibilityWindow);
    }

    [PunRPC]
    public void RPC_Send_CTModelSetVisibilityWindow(float _visibilityWindow, PhotonMessageInfo info)
    {
        LobbyUI.Instance.objectEdit.cTPopup.SetVisibilityWindow(_visibilityWindow);
    }
}

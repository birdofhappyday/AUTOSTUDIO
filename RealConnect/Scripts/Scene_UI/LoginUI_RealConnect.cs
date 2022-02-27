using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1. 작성자 : 김성민
/// 
/// 2. 작성일자 : 2021.03.17
/// 
/// 3. 설명
/// 
///  - 로그인 씬에서 사용되는 UI와 네트워크 콜백, 사용 키보드 관련.
///  
///    -- 회원가입, 로그인 관련 기능
/// 
///  - RealConnectLoginState 상태에 따라서 표시되는 UI의 형태가 바뀐다.
///  
/// </summary>

public enum RealConnectLoginState
{
    LOGIN,
    SIGNUP,
    SELECTGROUP,
    LOADING,
}

public class LoginUI_RealConnect : ARPFunction.MonoBeHaviourSingleton<LoginUI_RealConnect>
{
    private RealConnectLoginState realConnectLoginState;

    public GameObject loginWin;
    public GameObject singUpWin;
    public GameObject signUPInputWin;
    public GameObject selectGroupWin;

    [SerializeField]
    private Transform groupParent;
    [SerializeField]
    private GameObject group;
    [SerializeField]
    private InputField inf_ID;
    [SerializeField]
    private InputField inf_Password;
    [SerializeField]
    private InputField inf_SignID;
    [SerializeField]
    private InputField inf_SignPassword;
    [SerializeField]
    private InputField inf_ConfirmPassword;
    [SerializeField]
    private InputField inf_Nickname;
    [SerializeField]
    private KeyBoardMng keyboard;
    [SerializeField]
    private Text selectGroupUI;

    // 그룹 이름들
    private List<string> GroupNames;

    private string selectGroup;

    // 로그인 씬 상태에 따른 UI모습 변화
    public RealConnectLoginState RealConnectLoginState
    {
        get => realConnectLoginState;
        set
        {
            realConnectLoginState = value;

            switch (realConnectLoginState)
            {
                case RealConnectLoginState.LOGIN:
                    loginWin.SetActive(true);
                    singUpWin.SetActive(false);
                    break;
                case RealConnectLoginState.SIGNUP:
                    loginWin.SetActive(false);
                    singUpWin.SetActive(true);
                    signUPInputWin.SetActive(true);
                    selectGroupWin.SetActive(false);
                    break;
                case RealConnectLoginState.SELECTGROUP:
                    loginWin.SetActive(false);
                    singUpWin.SetActive(true);
                    signUPInputWin.SetActive(false);
                    selectGroupWin.SetActive(true);
                    break;
                case RealConnectLoginState.LOADING:
                    loginWin.SetActive(false);
                    singUpWin.SetActive(false);
                    break;
            }
        }
    }

    [System.Obsolete]
    private void Awake()
    {
        ARP_NetMain.Instance.onLogin += OnLogin;
        ARP_WebProc.Instance.onGetGroupNameList += OnGetGroupNameList;
        ARP_WebProc.Instance.onGetGroupInfo += OnGetGroupInfo;

        ////[김성민] 번들 정보 추가
        ARP_WebProc.Instance.onGetBundleIndexsByGroupIndex += OnGetBundleIndexsByGroupIndex;
        ARP_WebProc.Instance.onGetBundleByIndex += OnGetBundleByIndex;
#if OCULUS
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        keyboard.gameObject.SetActive(false);
#else
        GyroCamera.instance.GyroModeOnOff(true);
#endif

        inf_ID.text = DataManager.ID;
        inf_Password.text = DataManager.PASSWORD;

        RealConnectLoginState = RealConnectLoginState.LOGIN;

        
    }

    private void OnDestroy()
    {
        if (ARP_NetMain.Instance != null)
        {
            ARP_NetMain.Instance.onLogin -= OnLogin;
        }

        if (ARP_WebProc.Instance != null)
        {
            ARP_WebProc.Instance.onGetGroupNameList -= OnGetGroupNameList;
            ARP_WebProc.Instance.onGetGroupInfo -= OnGetGroupInfo;
        }
    }

#if OCULUS
    // 인풋 필드 클릭시 키보드 띄우기
    private void Update()
    {

        if (RealConnectLoginState == RealConnectLoginState.LOGIN)
        {
            if (inf_ID.isFocused)
            {
                KeyboardInput(inf_ID);
            }
            else if (inf_Password.isFocused)
            {
                KeyboardInput(inf_Password);
            }
        }
        else if (RealConnectLoginState == RealConnectLoginState.SIGNUP)
        {
            if (inf_ConfirmPassword.isFocused)
            {
                KeyboardInput(inf_ConfirmPassword);
            }
            else if (inf_Nickname.isFocused)
            {
                KeyboardInput(inf_Nickname);
            }
            else if (inf_SignID.isFocused)
            {
                KeyboardInput(inf_SignID);
            }
            else if (inf_SignPassword.isFocused)
            {
                KeyboardInput(inf_SignPassword);
            }
        }
}
#endif

#region NetworkCallBack

    // 로그인 콜백
    public void OnLogin(bool isSuccess, string msg)
    {
        if (isSuccess)
        {
            DataManager.ID = inf_ID.text;
            DataManager.PASSWORD = inf_Password.text;
            Debug.Log("[ LOGIN ] 로그인 성공, Group Name : " + ARP_NetMain.Instance.GroupName);
            ARP_WebProc.Instance.GetGroupInfoByName(ARP_NetMain.Instance.GroupName);
            RealConnectLoginState = RealConnectLoginState.LOADING;
        }
        else
        {
            RealConnectLoginState = RealConnectLoginState.LOGIN;
        }
    }

    // 그룹 정보 요청해서 세팅
    void OnGetGroupNameList(List<string> group_names)
    {
        // 에러 팝업 출력할 것.
        if (group_names == null)
            Debug.LogError("그룹 오류");

        if (GroupNames == null)
            GroupNames = new List<string>();
        else
            GroupNames.Clear();

        GroupNames.AddRange(group_names);

        foreach (Transform t in groupParent.transform)
        {
            Destroy(t.gameObject);
        }

        ToggleGroup toggleGroup = groupParent.GetComponent<ToggleGroup>();

        foreach (string s in GroupNames)
        {
            GameObject _groupObj = Instantiate(group, groupParent.transform);

            Group _g = _groupObj.GetComponent<Group>();

            if (_g == null)
            {
                _g = new GameObject(typeof(Group).Name, typeof(Group)).GetComponent<Group>();
            }

            Toggle _t = _groupObj.GetComponent<Toggle>();
            _t.group = toggleGroup;
            _t.onValueChanged.AddListener((isOn) =>
           {
               if (isOn)
                   selectGroup = _g.GroupName;
               else
                   selectGroup = "";

               selectGroupUI.text = selectGroup;
           });

            _g.Initialize(s);
        }
    }

    // 그룹 정보 콜백
    // 콜백후 비디오를 불러와서 정보 세팅을 해준다.
    void OnGetGroupInfo(GroupInfo _groupInfo)
    {
        ResourceManager.Instance.MyGroup = _groupInfo;

        Debug.Log("[LoginUI] My Group Name : " + _groupInfo.name);
        Debug.Log("[LoginUI] My Group Index : " + _groupInfo.index);
        Debug.Log("[LoginUI] My Group License Start Time : " + _groupInfo.license_start);
        Debug.Log("[LoginUI] My Group License End Time : " + _groupInfo.license_end);

        Debug.Log("[ LOGIN ] 로그인 성공, ID , Group Index : " + inf_ID.text + " / " + ResourceManager.Instance.MyGroup.index);
        ARP_WebProc.Instance.GetGroupResource(inf_ID.text, ResourceManager.Instance.MyGroup.index);

        //[김성민] 로컬 비디오 불러오기 이후에 스트리밍 비디오도 읽어온다.
        ResourceManager.Instance.LoadLocalVideo();
    }
    //[김성민] 번들이 모두 다운되었는지 개수를 체크하기 위한 변수
    int totalBundleCount;

    //[김성민] 서버에서 해당 그룹에 속해있는 번들 인덱스 번호를 받아와서 세팅.
    [System.Obsolete]
    void OnGetBundleIndexsByGroupIndex(List<int> bundle_indexs)
    {
        Debug.Log($"ksmksmksm OnGetBundleIndexsByGroupIndex");
        AddressableManager.Instance.ClearBundleInfo();

        totalBundleCount = bundle_indexs.Count;

        foreach (int index in bundle_indexs)
        {
            Debug.Log($"ksmksmksm bundle_indexs : {index}");
            ARP_WebProc.Instance.GetBundleByIndex(index);
        }

        if(totalBundleCount == 0)
            ResourceManager.Instance.ResourceProcess(DownloadProcess.LOGIN);
    }

    //[김성민] 서버에서 번들 인덱스로 정보를 받아서 세팅.
    [System.Obsolete]
    void OnGetBundleByIndex(ARP_WebProc.BundleInfo bundleInfo)
    {
        AddressableManager.Instance.AddBundleInfos((BundleType)bundleInfo.type_index, bundleInfo.name);

        if (totalBundleCount == AddressableManager.Instance.CountBundleDic())
            ResourceManager.Instance.ResourceProcess(DownloadProcess.LOGIN);
    }

    #endregion

    #region Button

    // 회원가입 버튼 클릭
    public void SignUpOnClick()
    {
        // 서버에 등록된 그룹의 이름들 요청
        ARP_WebProc.Instance.GetGroupNameList();

        RealConnectLoginState = RealConnectLoginState.SELECTGROUP;
    }

    // 그룹을 선택하고 다음 버튼 클릭
    public void GroupSelectNextOnClick()
    {
        if (!string.IsNullOrEmpty(selectGroup))
        {
            RealConnectLoginState = RealConnectLoginState.SIGNUP;
        }
    }

    // 로그인 버튼 클릭
    public void LoginOnClick()
    {
#if UNITY_EDITOR
        //[김성민] 테스트용 비번 자동입력
        //inf_Password.text = "digia";
#endif

        ARP_NetMain.Instance.LoginUsingPhotonSettings
            (
            inf_ID.text,
            inf_Password.text,
#if OCULUS
            PhotonWrapper.ePlatformType.VR,
#else
            PhotonWrapper.ePlatformType.AR,
#endif
            SystemInfo.deviceModel
            );

        Debug.Log("[ LoginUI ] Current Device Model : " + SystemInfo.deviceModel);
    }

    // 아이디 생성 버튼 클릭
    public void CreateAccountOnClick()
    {
        ARP_NetMain.Instance.SignUpUsingPhotonSettings
            (
            inf_SignID.text,
            inf_SignPassword.text == inf_ConfirmPassword.text ? inf_SignPassword.text : null,
            inf_Nickname.text,
            selectGroup,
#if OCULUS
            PhotonWrapper.ePlatformType.VR,
#else
            PhotonWrapper.ePlatformType.AR,
#endif
            SystemInfo.deviceModel
            );

        Debug.Log("[ LoginUI ] Current Device Model : " + SystemInfo.deviceModel);

        inf_ID.text = inf_SignID.text;
        RealConnectLoginState = RealConnectLoginState.LOGIN;
    }

    // 취소 클릭
    public void CancelOnClick()
    {
        RealConnectLoginState = RealConnectLoginState.LOGIN;
    }

#endregion

    private void KeyboardInput(InputField _inf)
    {
        if (keyboard.gameObject.activeSelf == true)
            return;
        keyboard.gameObject.SetActive(true);

        Vector3 pos = new Vector3(0, -150.0f, 0);
        keyboard.SetKeyBoard(_inf, pos);
    }

    public void InputFieldReset()
    {
        inf_ID.text = "";
        inf_Password.text = "";
        inf_SignID.text = "";
        inf_SignPassword.text = "";
        inf_ConfirmPassword.text = "";
        inf_Nickname.text = "";
    }
}

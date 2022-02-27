using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

//[김성민] 방장은 강조 오브젝트들이 뜨고 방원은 표현 오브젝트들이 뜬다.
public class EmphasisPopup : PopupBase
{
    public GameObject listParent;
    public AsyncOperationHandle<IList<GameObject>> loadHandle;

    public Emphasis selectEmphasis;

    public string Label;
    public Text title;

    public int limiteExperssionCount;

    [SerializeField]
    List<string> _emphasisList = new List<string>();


    [HideInInspector]
    public Emphasis currentEmphasis;
    [HideInInspector]
    public int expressionIndex = 0;
    [HideInInspector]
    public Dictionary<int, Emphasis> emphasisDic = new Dictionary<int, Emphasis>();

    bool _init = false;

    private EmphasisType _preEmphasisType;
    //[김성민] 방장은 강조 오브젝트가 나오게 방원은 표현 오브젝트들이 나온다.

    private string thumbleText = "_Thumbnail";

    public override void Open()
    {
        base.Open();

        EmphasisType _curEmphasisType;

        if (Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            title.text = "강조";
            Label = "Emphasis";
            _curEmphasisType = EmphasisType.EMPHASIS;
        }
        else
        {
            title.text = "표현";
            Label = "Expression";
            _curEmphasisType = EmphasisType.EXPRESSION;
        }

        if (!_init || _preEmphasisType != _curEmphasisType)
        {
            listParent.GetComponent<ToggleGroup>().allowSwitchOff = true;

            if (_emphasisList == null)
                _emphasisList = new List<string>();
            else if (_emphasisList.Count != 0)
                _emphasisList.Clear();

            foreach (Transform t in listParent.transform)
                Addressables.ReleaseInstance(t.gameObject);

            //[김성민] 서버에서 받아온 리스트에서 강조 리스트를 가져와서 만든다.
            if (_curEmphasisType == EmphasisType.EMPHASIS)
            {
                List<string> thumbleNameList = 
                    AddressableManager.Instance.GetBundleNameList(_curEmphasisType == EmphasisType.EMPHASIS ? BundleType.EMPHASIS : BundleType.EXPRESSION);

                foreach (string _emphasisName in thumbleNameList)
                {
                    if (AddressableManager.Instance.AddressableResourceExists($"{_emphasisName}{thumbleText}"))
                        _emphasisList.Add($"{_emphasisName}{thumbleText}");
                }

                SetList();
            }

            _init = true;
            _preEmphasisType = _curEmphasisType;
        }
    }

    public void SetList()
    {
        foreach (string _s in _emphasisList)
        {
            Addressables.InstantiateAsync(_s, listParent.transform);
        }
    }

    //[김성민] 선택완료 했을때 선 끝에 오브젝트들이 뜨고 타입을 EMPHASIS로 바뀌어서 StageUI_RealConnect Update에서 트리거 체크를 해서 실행한다.
    public override void OnClick()
    {
        base.Close();

        if (selectEmphasis == null)
            return;
#if OCULUS
        VRcamera _vc = VRcamera.instance;

        if (_vc == null)
            return;

        Emphasis _g = Instantiate(selectEmphasis, VRcamera.instance.emphasisPosition.transform.position, Quaternion.identity, RealConnectStageCanvas.Instance.transform);
        _vc.currentMakeObj = _g.gameObject;
#else
        Emphasis _g = Instantiate(selectEmphasis, Camera.main.transform.position + Camera.main.transform.forward * 50, Quaternion.identity, RealConnectStageCanvas.Instance.transform);
        LobbyUI_RealConnect.Instance.stageUI.makeObj = _g.gameObject;
#endif
        LobbyUI_RealConnect.Instance.stageUI.stagePopupType = StagePopupType.EMPHASIS;
    }

    //[김성민] 개수 제한에 맞추어 생성이 가능한지 판단한다.
    public bool MakeExperssionPossible()
    {
        return emphasisDic.Count <= limiteExperssionCount;
    }
}

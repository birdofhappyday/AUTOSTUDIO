using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//[김성민] 오브젝트가 강조인지, 표현인지 구분하기 위한 변수
public enum EmphasisType
{
    EXPRESSION,
    EMPHASIS,
}

//[김성민]
//강조와 표현 오브젝트에 같이 붙어 있어서 구분 변수를 두어 서로 다르게 처리하도록 되어 있다.
public class Emphasis : MonoBehaviourPun
{
    public bool Ready = false;

    public float lifeTime;
    public EmphasisType emphasisType;

    [HideInInspector]
    public int expressionIndex;

    [SerializeField]
    private Button button;

    public void Init(int index)
    {
        // 강조의 경우에 버튼 활성화와 현재 강조가 떠 있는지 체크하고 담아둔다.
        if (emphasisType == EmphasisType.EMPHASIS)
        {
            button.interactable = PhotonNetwork.IsMasterClient;
            if (PhotonNetwork.IsMasterClient)
            {
                if (LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.currentEmphasis != null && LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.currentEmphasis != this)
                    ARP_NetMain.Instance.DestorySyncObject(LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.currentEmphasis.transform.parent.gameObject);

                LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.currentEmphasis = this;
            }
        }

        //표현의 경우에 버튼 활성화와 표현 오브젝트를 관리하는 Dictionary에 담아둔다.
        else if (emphasisType == EmphasisType.EXPRESSION)
        {
            expressionIndex = index;

            if (PhotonNetwork.IsMasterClient)
            {
                LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.emphasisDic.Add(expressionIndex, this);
            }

            button.interactable = !PhotonNetwork.IsMasterClient;
            _lifeTimeStart = PhotonNetwork.IsMasterClient;

        }

        _isMaking = false;
    }

    // 버튼 클릭은 이미 방장인지 아닌지에 활성화 처리가 되어 있다.
    public void OnClick()
    {
        if (!_isMaking)
        {
            // 방장이 오브젝트 클릭시 삭제.
            if (emphasisType == EmphasisType.EMPHASIS)
            {
                ARP_NetMain.Instance.DestorySyncObject(this.transform.parent.gameObject);
            }

            //방원이 오브젝트 클릭시 방장한테 삭제 요청
            else if (emphasisType == EmphasisType.EXPRESSION)
            {
                LobbyUI_RealConnect.Instance.stageUI.realConnectAvatar.photonSerializeView.photonView.RPC("RPC_SetReceive_GuestRequestRemoveExpression", RpcTarget.MasterClient, expressionIndex);
            }
        }
    }

    // 방장이 생성하기 위한 위치 잡을때 강조가 회전하는 것을 막기위한 체크
    // 위치도 특정 지점을 따라다닌다.
    // 표현의 경우에 방장은 라이프 타임 체크가 있다.
    bool _isMaking = true;
    bool _lifeTimeStart = false;
    float _life = 0.0f;

    private void Update()
    {
        //[김성민] 임시로 오브젝트 만들었을때 위치를 맞춰준다.
        if (_isMaking)
        {
#if OCULUS
            var camPos = VRcamera.instance.CenterEyeAnchor.transform.position;
            this.transform.position = VRcamera.instance.emphasisPosition.transform.position;
#else
            var camPos = Camera.main.transform.position;
            this.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 50;
#endif
            //var direction = (transform.position - camPos).normalized;
            //var right = Vector3.Cross(direction, Vector3.up);
            //var look = Vector3.Cross(Vector3.up, right);
            //this.transform.rotation = Quaternion.LookRotation(look, Vector3.up);
            this.transform.LookAt(transform.position + (transform.position - Camera.main.transform.position));
        }

        if (emphasisType == EmphasisType.EMPHASIS)
        {

        }
        else if (emphasisType == EmphasisType.EXPRESSION)
        {
            if (_lifeTimeStart)
            {
                _life += Time.deltaTime;

                if (_life >= lifeTime)
                    ARP_NetMain.Instance.DestorySyncObject(this.transform.parent.gameObject);//DestroyImmediate(this.gameObject);
            }
        }
    }

    // 오브젝트들이 부서질때 담고 있는 부분에서 지워준다.
    private void OnDestroy()
    {
        if (LobbyUI_RealConnect.Instance != null && LobbyUI_RealConnect.Instance.stageUI != null)
        {
            if (emphasisType == EmphasisType.EMPHASIS)
            {
                if (LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.currentEmphasis == this)
                    LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.currentEmphasis = null;
            }
            else if (emphasisType == EmphasisType.EXPRESSION)
                LobbyUI_RealConnect.Instance.stageUI.emphasisPopup.emphasisDic.Remove(expressionIndex);
        }
    }
}

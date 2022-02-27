using Microsoft.MixedReality.Toolkit.UI;
using Photon.Pun;
using PhotonWrapper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.AR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using AxisFlags = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags;

public enum MoveType
{
    none = 0,
    rotation,
    scale,
    move,
    height,
    flag,
};

public enum InputType
{
    NONE,
    FLAG,
    MOVE,
    ROTATE,
    SCALE,
    HEIGHT,
    DELETE,
    ASSEMBLY,
    DRAWING,
}


public class InputManager : ARPFunction.MonoBeHaviourSingleton<InputManager>
{
#if !PC_OBSERVER
    public GameObject telePortLeg;
    public bool teleport;
    public IEnumerator updateCor = null;
    private InputType inputType = InputType.NONE;

    public float RotateSpeed = 1.0f;
    private RaycastHit raycastHit;

    [HideInInspector]
    public float scaleMinimum = 0.5f;
    [HideInInspector]
    public float panelScaleMinimum = 0.03f;
#if !OCULUS && !VIVE
    public float ZoomSpeed = 1.0f;
    public MoveType SelectMoveType = MoveType.none;
#else
    public XRRayInteractor xrRightRayInteractor;
    public LineRenderer rightLine;
    public XRRayInteractor xrLeftRayInteractor;
    public GameObject centerEyeAnchor;
    public bool IsSelectObj;

    private MoveType SelectMoveType = MoveType.none;
    public InputDevice rightDevice;
    public InputDevice leftDevice;
    private List<InputFeatureUsage> featureUsages = new List<InputFeatureUsage>();
    public bool isRightTriggerButtonAction = false;
    public bool isLeftTriggerButtonAction = false;

    private Vector3 ControllerPosBk = Vector3.zero;
    private Vector3 MoveBackUp = Vector3.zero;
    private Vector3 ScaleBackUp = Vector3.zero;
    private float yRotationValue = 0.0f;

#endif
    // Start is called before the first frame update
    void Start()
    {
#if HOLOLENS2
        SetInputType(InputType.FLAG);
#elif OCULUS || VIVE
        if (telePortLeg == null)
        {
            telePortLeg = GameObject.Find("VRteleport_leg");
        }
        rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        xrRightRayInteractor = VRcamera.instance.xrRightRayInteractor;
        rightLine = VRcamera.instance.rightLine;
        xrLeftRayInteractor = VRcamera.instance.xrLeftRayInteractor;
        centerEyeAnchor = VRcamera.instance.CenterEyeAnchor;

        SetInputType(InputType.FLAG);
#else
        ZoomSpeed = 0.1f;
#endif
    }

    public bool isSelect = true;
    float _distance;
    Transform _hitObj;
    Vector3 _hitPoint;
    int _hitColliderNum;

    /// [김성민]
    /// 클릭한 위치에 UI가 있는지 판단하기 위한 함수.
    /// AR체크를 위한 함수
    public bool IsPointerOverUIObject(Vector2 vector2)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(vector2.x, vector2.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        if (!ARCamera.instance.isAR)
        {
            foreach (var _rayCastResult in results)
            {
                if ((LayerMask.GetMask("UI") >> _rayCastResult.gameObject.layer) == 1)//LayerMask.GetMask("UI"))
                    return true;
            }

            return false;
        }
        else
            return results.Count > 0;
    }

    /// [김성민]
    /// 클릭한 위치에 UI가 있는지 판단하기 위한 함수.
    /// VR체크를 위한 함수
    public bool IsPointerOverUIObject()
    {
        TrackedDeviceEventData trackedDeviceEventData = new TrackedDeviceEventData(EventSystem.current);
        if (trackedDeviceEventData.selectedObject != null)
        {
            return true;
        }
        else
            return false;
    }

    // Update is called once per frame
    void Update()
    {
#if !OCULUS && !VIVE

#if !UNITY_EDITOR
        if (Input.touchCount != 0)
        {
            if (InputManager.Instance.IsPointerOverUIObject(new Vector2(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y)))
            {
                // ui touched
                //Debug.Log("[은지] ui Touched");
                return;
            }
        }
#elif HOLOLENS2

#else
        if (IsPointerOverUIObject(new Vector2(Input.mousePosition.x, Input.mousePosition.y)))
        {
            return;
        }

#endif
#else
        ///레이 캐스트 경로에 ui가 있을 경우에 작동을 막기 위해서 체크해서 지나간다.
        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction)
        {
            if(IsPointerOverUIObject())
            {
                return;
            }
        }

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

        ///텔레포트 이동 체크
        VrTeleportCheck();
#endif
        //[김성민] 편집권한이 없는 사람은 오브젝트 조작을 하면 안된다.
        if (!ObjectManager.Instance.objectController)
            return;

#if HOLOLENS2

        switch (inputType)
        {
            case InputType.FLAG:
                FlagSetting();
                break;
        }
#else
        //Debug.Log("[은지] Inputmanager.Update() : " + GetInputType);
        // [은지] nonAR일경우는 NonARInputManager에서 처리한다.
        if (ARCamera.instance == null || ARCamera.instance.isAR)
        {
            switch (inputType)
            {
                case InputType.NONE:
                    if (LobbyUI.Instance.objectEdit.eCurrentEditType != PlayType.Scenario)
                        SelectObject();
                    break;

                case InputType.FLAG:
                    FlagSetting();
                    break;

                case InputType.MOVE:
                    InputMove();
                    break;

                case InputType.HEIGHT:
                    InputHeight();
                    break;

                case InputType.SCALE:
                    InputScale();
                    break;

                case InputType.ROTATE:
                    InputRotate();
                    break;

                case InputType.DELETE:
                    DeleteObject();
                    break;

                case InputType.ASSEMBLY:
                    AssemblyInputUpdate();
                    break;

                case InputType.DRAWING:
#if !OCULUS && !VIVE
                    ARCamera.instance.drawObject.DrawProc();
#else
                    VRcamera.instance.drawObject.DrawProc();
#endif
                    break;
            }
        }

#endif
    }

    /// [김성민]
    /// InputManager에서 실행되는 함수를 결정짓는 분기.
    public void SetInputType(InputType inputType, bool flag = true)
    {
        // [은지] AR과 nonAR처리
#if !OCULUS && !VIVE
        if (ARCamera.instance == null || ARCamera.instance.isAR)
        {
            if (flag)
                this.inputType = inputType;
            else
            {
                this.inputType = InputType.NONE;
                editStart = false;
            }
        }
        else
        {
            if (flag)
                NonARInputManager.Instance.inputType = inputType;
            else
            {
                NonARInputManager.Instance.inputType = InputType.NONE;
            }
        }
#else
        if (flag)
            this.inputType = inputType;
        else
            this.inputType = InputType.NONE;

#endif
    }

    public InputType GetInputType { get { return inputType; } }

    public void HoloLens2ObjectEdit(GameObject obj)
    {
        Microsoft.MixedReality.Toolkit.UI.BoundsControl.BoundsControl bc = obj.GetComponent<Microsoft.MixedReality.Toolkit.UI.BoundsControl.BoundsControl>();
        MoveAxisConstraint moveAxisConstraint = obj.GetComponent<MoveAxisConstraint>();
        RotationAxisConstraint rotationAxisConstraint = obj.GetComponent<RotationAxisConstraint>();

        switch (inputType)
        {
            case InputType.NONE:
                SelectObject(obj.transform);
                bc.enabled = false;
                moveAxisConstraint.ConstraintOnMovement = AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
                rotationAxisConstraint.ConstraintOnRotation = AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
                break;

            case InputType.DELETE:
                ObjectManager.Instance.DeleteObject(obj);
                break;
        }

    }

#if !OCULUS && !VIVE
    bool editStart = false;
#endif

    TouchPhase InputEditCheck()
    {
#if !OCULUS && !VIVE
        if (Input.touchCount >= 1)
        {
            Touch touch = Input.GetTouch(0);
            if (!editStart)
            {
#if !UNITY_EDITOR
                Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 100));
#else
                Ray ray_Info = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
                if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity)) // 선택
                {
                    if (raycastHit.transform != null && raycastHit.transform.gameObject == ObjectManager.Instance.currentSelectObject.gameObject)
                    {
                        return TouchPhase.Began;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && editStart)
            {
                return TouchPhase.Moved;
            }
            else if (touch.phase == TouchPhase.Ended && editStart)
            {
                return TouchPhase.Ended;
            }
        }
#else

#endif
        return TouchPhase.Canceled;
    }

    void InputMoveSample()
    {
#if !OCULUS && !VIVE
        switch (InputEditCheck())
        {
            case TouchPhase.Began:
                editStart = true;

                GestureTransformationUtility.Placement desiredPlacement =
                GestureTransformationUtility.GetBestPlacementPosition(
                    ObjectManager.Instance.currentSelectObject.transform.parent.position, Input.GetTouch(0).position, ObjectManager.Instance.currentSelectObject.transform.parent.position.y, 0.03f,
                    10, GestureTransformationUtility.GestureTranslationMode.Horizontal, ARCamera.instance.arSessionOrigin);
                if (desiredPlacement.hasHoveringPosition && desiredPlacement.hasPlacementPosition)
                {
                    // If desired position is lower than current position, don't drop it until it's finished.
                    firstMovePosition = ObjectManager.Instance.currentSelectObject.transform.parent.InverseTransformPoint(desiredPlacement.hoveringPosition);
                    firstMovePosition -= ObjectManager.Instance.currentSelectObject.transform.localPosition;
                }
                break;
            case TouchPhase.Moved:
                ///[김성민]
                ///이동 방식은 ARTranslationInteractable에서 포지션을 구하는 방법을 가져와서 사용했다.
                Vector3 movePosition = Vector3.zero;

                desiredPlacement =
                GestureTransformationUtility.GetBestPlacementPosition(
                    ObjectManager.Instance.currentSelectObject.transform.parent.position, Input.GetTouch(0).position, ObjectManager.Instance.currentSelectObject.transform.parent.position.y, 0.03f,
                    10, GestureTransformationUtility.GestureTranslationMode.Horizontal, ARCamera.instance.arSessionOrigin);

                if (desiredPlacement.hasHoveringPosition && desiredPlacement.hasPlacementPosition)
                {
                    // If desired position is lower than current position, don't drop it until it's finished.
                    movePosition = ObjectManager.Instance.currentSelectObject.transform.parent.InverseTransformPoint(desiredPlacement.hoveringPosition);
                    movePosition -= firstMovePosition;
                    ObjectManager.Instance.currentSelectObject.transform.localPosition = Vector3.Lerp(
                    ObjectManager.Instance.currentSelectObject.transform.localPosition, movePosition, Time.deltaTime * 17f);
                }
                break;
            case TouchPhase.Ended:
                editStart = false;
                break;
        }
#else

#endif
    }

    void InputRotateSample()
    {
#if !OCULUS && !VIVE
        switch (InputEditCheck())
        {
            case TouchPhase.Began:
                editStart = true;
                break;
            case TouchPhase.Moved:
                ObjectManager.Instance.currentSelectObject.transform.Rotate(Vector3.up * -(RotateSpeed) * Time.deltaTime * Input.GetTouch(0).deltaPosition.x, Space.Self);
                break;
            case TouchPhase.Ended:
                editStart = false;
                break;
        }
#else

#endif
    }

    void InputScaleSample()
    {
#if !OCULUS && !VIVE
        switch (InputEditCheck())
        {
            case TouchPhase.Began:
                editStart = true;
                break;
            case TouchPhase.Moved:
                float value;
                value = (Input.GetTouch(0).deltaPosition.y * (1.0f * 0.002f));

                if (ObjectManager.Instance.currentSelectObject.transform.localScale.x + value <= scaleMinimum)
                    value = scaleMinimum - ObjectManager.Instance.currentSelectObject.transform.localScale.x;

                value = ObjectManager.Instance.currentSelectObject.transform.localScale.x + value;

                ObjectManager.Instance.currentSelectObject.transform.localScale = new Vector3(value, value, value);
                break;
            case TouchPhase.Ended:
                editStart = false;
                break;
        }
#else

#endif
    }

    void InputHeightSample()
    {
#if !OCULUS && !VIVE
        switch (InputEditCheck())
        {
            case TouchPhase.Began:
                editStart = true;
                break;
            case TouchPhase.Moved:
                ObjectManager.Instance.currentSelectObject.transform.localPosition = new Vector3(ObjectManager.Instance.currentSelectObject.transform.localPosition.x,
                    ObjectManager.Instance.currentSelectObject.transform.localPosition.y + ((Time.deltaTime * Input.GetTouch(0).deltaPosition.y) * 0.1f),
                    ObjectManager.Instance.currentSelectObject.transform.localPosition.z);
                break;
            case TouchPhase.Ended:
                editStart = false;
                break;
        }
#else

#endif
    }

    #region toggle method
    public void SetMove(bool flag)
    {
        EditMarkSetting(flag, EditMarkType.MOVE);
    }
    public void SetRotation(bool flag)
    {
        EditMarkSetting(flag, EditMarkType.ROTATE);
    }
    public void SetScale(bool flag)
    {
        EditMarkSetting(flag, EditMarkType.SCALE);
    }
    public void SetHeight(bool flag)
    {
        EditMarkSetting(flag, EditMarkType.HEIGHT);
    }

    public void SetScenarioMoveToggle(bool flag)
    {
        // ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectList[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].gameObject;
        // 220222 / KCJ /Test
        ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectInfo[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].obj.gameObject;
        EditMarkSetting(flag, EditMarkType.MOVE);
    }

    public void SetScenarioRotateToggle(bool flag)
    {
        //ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectList[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].gameObject;
        // 220222 / KCJ /Test
        ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectInfo[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].obj.gameObject;

        EditMarkSetting(flag, EditMarkType.ROTATE);
    }

    public void SetScenarioScaleToggle(bool flag)
    {
        //ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectList[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].gameObject;
        // 220222 / KCJ /Test
        ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectInfo[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].obj.gameObject;
        EditMarkSetting(flag, EditMarkType.SCALE);
    }

    public void SetScenarioHeightToggle(bool flag)
    {
        //ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectList[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].gameObject;
        // 220222 / KCJ /Test
        ObjectManager.Instance.currentSelectObject = ObjectManager.Instance.AllObjectInfo[LobbyUI.Instance.objectEdit.scenarioPopup.modelIndex].obj.gameObject;
        EditMarkSetting(flag, EditMarkType.HEIGHT);
    }
    #endregion

    #region Input
    Vector3 firstMovePosition = Vector3.zero;

    public void InputMove()
    {
#if HOLOLENS2

#elif !OCULUS && !VIVE
        if (Input.touchCount >= 1)
        {
            Touch touch = Input.GetTouch(0);
            if (!editStart)
            {
#if !UNITY_EDITOR
                Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 100));
#else
                Ray ray_Info = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
                if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity)) // 선택
                {
                    if (raycastHit.transform != null && raycastHit.transform.gameObject == ObjectManager.Instance.currentSelectObject.gameObject)
                    {
                        editStart = true;

                        GestureTransformationUtility.Placement desiredPlacement =
               GestureTransformationUtility.GetBestPlacementPosition(
                   ObjectManager.Instance.currentSelectObject.transform.parent.position, touch.position, ObjectManager.Instance.currentSelectObject.transform.parent.position.y, 0.03f,
                   10, GestureTransformationUtility.GestureTranslationMode.Horizontal, ARCamera.instance.arSessionOrigin);

                        if (desiredPlacement.hasHoveringPosition && desiredPlacement.hasPlacementPosition)
                        {
                            // If desired position is lower than current position, don't drop it until it's finished.
                            firstMovePosition = ObjectManager.Instance.currentSelectObject.transform.parent.InverseTransformPoint(desiredPlacement.hoveringPosition);
                            firstMovePosition -= ObjectManager.Instance.currentSelectObject.transform.localPosition;
                        }
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && editStart)
            {
                ///[김성민]
                ///이동 방식은 ARTranslationInteractable에서 포지션을 구하는 방법을 가져와서 사용했다.
                Vector3 movePosition = Vector3.zero;

                GestureTransformationUtility.Placement desiredPlacement =
                GestureTransformationUtility.GetBestPlacementPosition(
                    ObjectManager.Instance.currentSelectObject.transform.parent.position, touch.position, ObjectManager.Instance.currentSelectObject.transform.parent.position.y, 0.03f,
                    10, GestureTransformationUtility.GestureTranslationMode.Horizontal, ARCamera.instance.arSessionOrigin);

                if (desiredPlacement.hasHoveringPosition && desiredPlacement.hasPlacementPosition)
                {
                    // If desired position is lower than current position, don't drop it until it's finished.
                    movePosition = ObjectManager.Instance.currentSelectObject.transform.parent.InverseTransformPoint(desiredPlacement.hoveringPosition);
                    movePosition -= firstMovePosition;
                    ObjectManager.Instance.currentSelectObject.transform.localPosition = Vector3.Lerp(
                    ObjectManager.Instance.currentSelectObject.transform.localPosition, movePosition, Time.deltaTime * 17f);
                }
            }
            else if (touch.phase == TouchPhase.Ended && editStart)
            {
                editStart = false;
                touch.deltaPosition = Vector2.zero;
            }
        }
#else
        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction)
        {
            xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

            if (raycastHit.transform != null && raycastHit.transform.CompareTag("InGameObject") && !IsSelectObj)
            {
                if (raycastHit.transform == ObjectManager.Instance.currentSelectObject.transform)
                {
                    ControllerPosBk = rightLine.GetPosition(0);
                    MoveBackUp = ObjectManager.Instance.currentSelectObject.transform.position;
                    IsSelectObj = true;
                }
            }

            if (IsSelectObj)
            {
                float valuex = (rightLine.GetPosition(0).x - ControllerPosBk.x) * 30.0f;
                float valuez = (rightLine.GetPosition(0).z - ControllerPosBk.z) * 30.0f;
                ObjectManager.Instance.currentSelectObject.transform.position = new Vector3(MoveBackUp.x + valuex, MoveBackUp.y, MoveBackUp.z + valuez);
            }
        }
        else
        {
            IsSelectObj = false;
        }
#endif
    }

    public void InputRotate()
    {
#if !OCULUS && !VIVE
        if (Input.touchCount >= 1)
        {
            Touch touch = Input.GetTouch(0);
            if (!editStart)
            {
#if !UNITY_EDITOR
                Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 100));
#else
                Ray ray_Info = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
                if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity)) // 선택
                {
                    if (raycastHit.transform != null && raycastHit.transform.gameObject == ObjectManager.Instance.currentSelectObject.gameObject)
                    {
                        editStart = true;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && editStart)
            {
                ObjectManager.Instance.currentSelectObject.transform.Rotate(Vector3.up * -(RotateSpeed) * Time.deltaTime * touch.deltaPosition.x, Space.Self);
            }
            else if (touch.phase == TouchPhase.Ended && editStart)
            {
                editStart = false;
                touch.deltaPosition = Vector2.zero;
            }
        }
#else
        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction && !IsSelectObj)
        {
            xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

            if (raycastHit.transform != null && raycastHit.transform.CompareTag("InGameObject"))
            {
                if (raycastHit.transform == ObjectManager.Instance.currentSelectObject.transform)
                {
                    ControllerPosBk = Camera.main.transform.InverseTransformPoint(rightLine.GetPosition(0));
                    yRotationValue = ObjectManager.Instance.currentSelectObject.transform.localEulerAngles.y;
                    IsSelectObj = true;
                }
            }
        }

        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction && IsSelectObj)
        {
            float value = (Camera.main.transform.InverseTransformPoint(rightLine.GetPosition(0)).x - ControllerPosBk.x) * 250.0f;
            ObjectManager.Instance.currentSelectObject.transform.localEulerAngles = new Vector3(ObjectManager.Instance.currentSelectObject.transform.localEulerAngles.x, yRotationValue - value, ObjectManager.Instance.currentSelectObject.transform.localEulerAngles.z);
        }
        else
        {
            IsSelectObj = false;
        }
#endif
    }

    public void InputScale()
    {
#if !OCULUS && !VIVE
        if (Input.touchCount >= 1)
        {
            Touch touch = Input.GetTouch(0);
            if (!editStart)
            {
#if !UNITY_EDITOR
                Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 100));
#else
                Ray ray_Info = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
                if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity)) // 선택
                {
                    if (raycastHit.transform != null && raycastHit.transform.gameObject == ObjectManager.Instance.currentSelectObject.gameObject)
                    {
                        editStart = true;
                    }
                }
            }

            else if (touch.phase == TouchPhase.Moved && editStart)
            {
                float value;
                if (ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>() != null)
                    value = (touch.deltaPosition.y * (1.0f * 0.002f));
                else
                    value = (touch.deltaPosition.y * (1.0f * 0.002f) * 0.03f);

                if (ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>() != null)
                {
                    if (ObjectManager.Instance.currentSelectObject.transform.localScale.x + value <= scaleMinimum)
                        value = scaleMinimum - ObjectManager.Instance.currentSelectObject.transform.localScale.x;
                }
                else
                {
                    if (ObjectManager.Instance.currentSelectObject.transform.localScale.x + value <= panelScaleMinimum)
                        value = panelScaleMinimum - ObjectManager.Instance.currentSelectObject.transform.localScale.x;
                }
                value = ObjectManager.Instance.currentSelectObject.transform.localScale.x + value;

                ObjectManager.Instance.currentSelectObject.transform.localScale = new Vector3(value, value, value);
            }

            else if (touch.phase == TouchPhase.Ended && editStart)
            {
                editStart = false;
                touch.deltaPosition = Vector2.zero;
            }
        }
#else
        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction && !IsSelectObj)
        {
            xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

            if (raycastHit.transform != null && raycastHit.transform.CompareTag("InGameObject"))
            {
                if (raycastHit.transform == ObjectManager.Instance.currentSelectObject.transform)
                {
                    ControllerPosBk = rightLine.GetPosition(0);
                    ScaleBackUp = ObjectManager.Instance.currentSelectObject.transform.localScale;
                    IsSelectObj = true;
                }
            }
        }

        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction && IsSelectObj)
        {
            float value;

            if(ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>() != null)
                value = (rightLine.GetPosition(0).y - ControllerPosBk.y) * 10f;
            else
                value = (rightLine.GetPosition(0).y - ControllerPosBk.y) * 0.3f;

            if (ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>() != null)
            {
                if (ScaleBackUp.x + value <= scaleMinimum)
                    value = scaleMinimum - ScaleBackUp.x;
            }
            else
            {
                if (ScaleBackUp.x + value <= panelScaleMinimum)
                    value = panelScaleMinimum - ScaleBackUp.x;
            }
            value = ScaleBackUp.x + value;
            ObjectManager.Instance.currentSelectObject.transform.localScale = new Vector3(value, value, value);
        }
        else
        {
            IsSelectObj = false;
        }
#endif
    }

    public void InputHeight()
    {
#if !OCULUS && !VIVE
        if (Input.touchCount >= 1)
        {
            Touch touch = Input.GetTouch(0);
            if (!editStart)
            {
#if !UNITY_EDITOR
                Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 100));
#else
                Ray ray_Info = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
                if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity)) // 선택
                {
                    if (raycastHit.transform != null && raycastHit.transform.gameObject == ObjectManager.Instance.currentSelectObject.gameObject)
                    {
                        editStart = true;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && editStart)
            {
                ObjectManager.Instance.currentSelectObject.transform.localPosition = new Vector3(ObjectManager.Instance.currentSelectObject.transform.localPosition.x, ObjectManager.Instance.currentSelectObject.transform.localPosition.y + ((Time.deltaTime * touch.deltaPosition.y) * 0.1f), ObjectManager.Instance.currentSelectObject.transform.localPosition.z);
            }
            else if (touch.phase == TouchPhase.Ended && editStart)
            {
                editStart = false;
                touch.deltaPosition = Vector2.zero;
            }
        }
#else
        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction && !IsSelectObj)
        {
            xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

            if (raycastHit.transform != null && raycastHit.transform.CompareTag("InGameObject"))
            {
                if (raycastHit.transform == ObjectManager.Instance.currentSelectObject.transform)
                {
                    ControllerPosBk = rightLine.GetPosition(0);
                    MoveBackUp = ObjectManager.Instance.currentSelectObject.transform.position;
                    IsSelectObj = true;
                }
            }
        }

        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction && IsSelectObj)
        {
            float value = (rightLine.GetPosition(0).y - ControllerPosBk.y) * 30.0f;
            ObjectManager.Instance.currentSelectObject.transform.position = new Vector3(MoveBackUp.x, (MoveBackUp.y + value) > 0 ? (MoveBackUp.y + value) : 0.0f, MoveBackUp.z);
        }
        else
        {
            IsSelectObj = false;
        }
#endif
    }

    #endregion





    public void SetFlag(MoveType Type)
    {
        SelectMoveType = Type;
    }

    public void DeleteModelOnChange(bool tog_DeleteModel)
    {
        if (tog_DeleteModel)
            SetInputType(InputType.DELETE);
        else
            SetInputType(InputType.NONE);
    }

    public void VrTeleportCheck()
    {
#if OCULUS || VIVE
        if (leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isLeftTriggerButtonAction) && isLeftTriggerButtonAction)
        {
            xrLeftRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);
            VRcamera.instance.xrleftLineVisual.enabled = true;

            if (raycastHit.transform != null && raycastHit.transform.gameObject.CompareTag("Floor"))
            {
                telePortLeg.transform.position = raycastHit.point;
                var right = VRcamera.instance.CenterEyeAnchor.transform.right;
                right.y = 0;
                var forward = Vector3.Cross(right.normalized, Vector3.up);
                var rotation = Quaternion.LookRotation(forward, Vector3.up);
                if (!teleport)
                {
                    teleport = true;
                    telePortLeg.transform.rotation = rotation;
                }
                Vector2 primary2DAxisValue = Vector2.zero;
                if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out primary2DAxisValue))
                {
                    if (primary2DAxisValue.sqrMagnitude >= 0.1f)
                    {
                        Vector3 rot = rotation * new Vector3(primary2DAxisValue.x, 0f, primary2DAxisValue.y);
                        rot = telePortLeg.transform.position + rot;
                        telePortLeg.transform.LookAt(rot);
                    }
                }
            }
        }
        else
        {
            VRcamera.instance.xrleftLineVisual.enabled = false;
        }
#endif
    }

    private RaycastHit hitInfo;
#if HOLOLENS2
    private bool isHoloLens2AnchorInit = false;
#endif


    [Header("홀로렌즈용 땅 체크 레이어")]
    [SerializeField]
    LayerMask GroundLayer;

    public Action FlagAfter = null;
    public void FlagSetting()
    {
#if HOLOLENS2
        if (!isHoloLens2AnchorInit)
        {
            ObjectManager.Instance.FlagObject.SetActive(true);
            PublicUI.Instance.Flaginfo.gameObject.SetActive(true);
#if UNITY_EDITOR
            ObjectManager.Instance.FlagObject.transform.position = Vector3.zero;

            HoloLens2AnchorEnd();
#else
        if (Physics.Raycast(
                      Camera.main.transform.position,
                      Camera.main.transform.forward,
                      out hitInfo,
                      Mathf.Infinity,
                      GroundLayer.value))
        {
            ObjectManager.Instance.FlagObject.transform.position = hitInfo.point;
        }

        if (HoloLens2Camera.instance.HoloLensFingerDistanceJudge())
            {
                HoloLens2AnchorEnd();
            }
#endif
        }
        else
        {
            HoloLens2AnchorEnd();
        }
#elif OCULUS || VIVE
        SetInputType(InputType.NONE);

        ObjectManager.Instance.IsFlag = true;

        if (!PublicUI.Instance.auto)
        {
            PublicUI.Instance.SettingInteractAlawysAndLockToggle(true);
            PublicUI.Instance.SettingModeToggle(true);
            PublicUI.Instance.SettingDrawingToggle(true);
        }
        else
        {
            PublicUI.Instance.SettingInteractAlawysAndLockToggle(true);
            PublicUI.Instance.Flaginfo.gameObject.SetActive(false);
        }

        if (PublicUI.Instance.auto)
        {
            ObjectManager.Instance.FlagObject.SetActive(true);
            ObjectManager.Instance.FlagObject.transform.position = Vector3.zero;
        }
        else
        {
            ObjectManager.Instance.HubFlagObject.SetActive(true);
            ObjectManager.Instance.HubFlagObject.transform.position = Vector3.zero;
        }

        if (FlagAfter != null)
        {
            FlagAfter();
            FlagAfter = null;
        }
#else

#endif
        ObjectManager.Instance.objectController = true;
    }

    public void HoloLens2AnchorEnd()
    {
        SetInputType(InputType.NONE);

        if (FlagAfter != null)
        {
            FlagAfter();
            FlagAfter = null;
        }

        PublicUI.Instance.SettingInteractAlawysAndLockToggle(true);
        PublicUI.Instance.Flaginfo.gameObject.SetActive(false);
        ObjectManager.Instance.IsFlag = true;
#if HOLOLENS2
        isHoloLens2AnchorInit = true;
#endif
    }

    /// [김성민]
    /// 오브젝트 권한을 가진 유저가 토글을 활용하여 오브젝트를 지울때 사용하는 함수.
    public void DeleteObject()
    {
#if HOLOLENS2

#elif !OCULUS && !VIVE
        if (Input.touchCount != 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
        {
#if !UNITY_EDITOR
                Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 100));
#else
            Ray ray_Info = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
            if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity)) // 선택
            {
                if (raycastHit.transform != null && raycastHit.transform.CompareTag("InGameObject"))
                {
                    ObjectManager.Instance.DeleteObject(raycastHit.transform.gameObject);
                }
            }
        }
#else
        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction)
        {
            xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

            if (raycastHit.transform != null && raycastHit.transform.gameObject.CompareTag("InGameObject"))
            {
                Transform currentObject = raycastHit.transform;

                if (ObjectManager.Instance.currentSelectObject != null)
                {
                    if (ObjectManager.Instance.currentSelectObject.transform == currentObject)
                    {
                        ///[김성민] 삭제할 오브젝트가 유튜브 패널이고 패널 리모트가 켜져있으면 끈다.
                        if (ObjectManager.Instance.currentSelectObject.GetComponent<ARPYoutubePlayer>() != null)
                        {
                            if (LobbyUI.Instance.PanelRemote.activeSelf)
                                LobbyUI.Instance.PanelRemote.GetComponent<PanelRemote>().Close();
                        }

                        ObjectManager.Instance.FindCurrentObject(null);
                    }
                }

                //if (ObjectManager.Instance.AllObjectList.Count == 1)
                // 220222 / KCJ /Test
                if (ObjectManager.Instance.AllObjectInfo.Count == 1)
                {
                    PublicUI.Instance.SettingInteractObjectToggle(false);
                    PublicUI.Instance.SettingInteractDeleteToggle(false);
                    InputManager.Instance.DeleteModelOnChange(false);
                }

                ObjectManager.Instance.DeleteObject(raycastHit.transform.gameObject);

                if (!PublicUI.Instance.auto)
                    PublicUI.Instance.SettingDrawingToggle(true);
            }
        }
#endif
    }

    public void RaySelectObject()
    {
#if !UNITY_EDITOR
        Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 100));
#else
        Ray ray_Info = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
        if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity))
        {
            if (raycastHit.transform != null && raycastHit.transform.CompareTag("InGameObject"))
            {
                ObjectManager.Instance.FindCurrentObject(raycastHit.transform);
            }
            else
            {
                ObjectManager.Instance.FindCurrentObject(null);
            }
        }
        else
        {
            ObjectManager.Instance.FindCurrentObject(null);
        }
    }

    // 오브젝트 선택
    public void SelectObject(Transform obj = null)
    {
#if HOLOLENS2
        ObjectManager.Instance.FindCurrentObject(obj);
#elif !OCULUS && !VIVE
        if ((Input.touchCount != 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0)))
        {
            RaySelectObject();
        }
#else
        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction)
        {
            xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

            if (raycastHit.transform != null && raycastHit.transform.gameObject.CompareTag("InGameObject"))
            {
                ObjectManager.Instance.FindCurrentObject(raycastHit.transform);

                ControllerPosBk = rightLine.GetPosition(0);
                MoveBackUp = ObjectManager.Instance.currentSelectObject.transform.position;
                ScaleBackUp = ObjectManager.Instance.currentSelectObject.transform.localScale;
                yRotationValue = ObjectManager.Instance.currentSelectObject.transform.localEulerAngles.y;
            }
            else
            {
                ObjectManager.Instance.FindCurrentObject(null);

                if (!PublicUI.Instance.auto)
                    PublicUI.Instance.SettingDrawingToggle(true);
            }
        }
#endif
    }

    bool assemblySelect;
    public bool AssemblySelect { get => assemblySelect; set => assemblySelect = value; }
    public void AssemblyInputUpdate()
    {
#if !OCULUS && !VIVE
        Ray ray_Info = Camera.allCameras[0].ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 100));

        Debug.DrawRay(ray_Info.origin, ray_Info.direction * 20, Color.red, 5.0f);

        ///호버 표시를 위한 레이 체크
        if (Physics.Raycast(ray_Info, out raycastHit, Mathf.Infinity))
        {
            if (raycastHit.transform.CompareTag("InGameObject"))
            {
                if (!LobbyUI.Instance.pointer_hover.activeSelf)
                {
                    LobbyUI.Instance.pointer_hover.SetActive(true);
                    LobbyUI.Instance.pointer.GetComponent<Image>().enabled = false;
                }

                if (Input.touchCount != 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
                {
                    AssemblySelect = true;
                }
            }
            else
            {
                if (LobbyUI.Instance.pointer_hover.activeSelf && !AssemblySelect)
                {
                    LobbyUI.Instance.pointer_hover.SetActive(false);
                    LobbyUI.Instance.pointer.GetComponent<Image>().enabled = true;
                }
            }
        }
        else
        {
            if (LobbyUI.Instance.pointer_hover.activeSelf && !AssemblySelect)
            {
                LobbyUI.Instance.pointer_hover.SetActive(false);
                LobbyUI.Instance.pointer.GetComponent<Image>().enabled = true;
            }
        }

        ///[김성민]
        ///오브젝트 선택을 판단한다.
        if (AssemblySelect)
        {
            if (Input.touchCount != 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
            {
                _hitObj = raycastHit.transform;
                _distance = raycastHit.distance;
                _hitPoint = raycastHit.point - _hitObj.position;

                for (int i = 0; i < ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>().objectData.assemblyObjects.Count; ++i)
                {
                    if (_hitObj.name.Equals(ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>().objectData.assemblyObjects[i].name))
                    {
                        _hitColliderNum = i;
                        break;
                    }
                }

                Debug.Log("_hitColliderNum : " + _hitColliderNum);
            }

            else if (Input.touchCount != 0 && Input.GetTouch(0).phase == TouchPhase.Stationary || Input.GetMouseButton(0))
            {
                _hitObj.transform.position = ray_Info.origin + ray_Info.direction * _distance - _hitPoint;
            }

            else if (Input.touchCount != 0 && Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetMouseButtonUp(0))
            {
                AssemblySelect = false;
                ARP_NetMain.Instance.Send_PositionData(_hitObj.localPosition, ObjectManager.Instance.currentSelectObjectNum, _hitColliderNum);
            }
        }
#else
        xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

        if (raycastHit.transform != null && raycastHit.transform.gameObject.CompareTag("InGameObject"))
        {
            VRcamera.instance.xrRightLineVisual.invalidColorGradient = LobbyUI.Instance.objectEdit.assemblyPopup.hoverColor;
            VRcamera.instance.xrRightLineVisual.validColorGradient = LobbyUI.Instance.objectEdit.assemblyPopup.hoverColor;
        }
        else
        {
            VRcamera.instance.xrRightLineVisual.invalidColorGradient = LobbyUI.Instance.objectEdit.assemblyPopup.originColor;
            VRcamera.instance.xrRightLineVisual.validColorGradient = LobbyUI.Instance.objectEdit.assemblyPopup.originColor;
        }

        if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerButtonAction) && isRightTriggerButtonAction)
        {
            xrRightRayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

            if (raycastHit.transform != null && raycastHit.transform.CompareTag("InGameObject") && !IsSelectObj)
            {
                _hitObj = raycastHit.transform;
                _distance = raycastHit.distance;
                //_hitPoint = raycastHit.point - _hitObj.position;
                _hitPoint = (VRcamera.instance.xrRightRayInteractor.transform.position + VRcamera.instance.xrRightRayInteractor.transform.forward * _distance) - _hitObj.position;
                IsSelectObj = true;
                for (int i = 0; i < ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>().objectData.assemblyObjects.Count; ++i)
                {
                    if (_hitObj.name.Equals(ObjectManager.Instance.currentSelectObject.GetComponent<WrapperObject>().objectData.assemblyObjects[i].name))
                    {
                        _hitColliderNum = i;
                        break;
                    }
                }
            }
            else if (IsSelectObj)
            {
                //_hitObj.transform.position = VRcamera.instance.xrRightRayInteractor.transform.position + VRcamera.instance.xrRightRayInteractor.transform.forward * _distance - _hitPoint;
                _hitObj.position = (VRcamera.instance.xrRightRayInteractor.transform.position + VRcamera.instance.xrRightRayInteractor.transform.forward * _distance) - _hitPoint;
            }
        }

        else if (IsSelectObj)
        {
            IsSelectObj = false;
            ARP_NetMain.Instance.Send_PositionData(_hitObj.localPosition, LobbyUI.Instance.objectEdit.assemblyPopup.modelIndex, _hitColliderNum);
        }
#endif
    }

    public void EditMarkSetting(bool flag, EditMarkType editMarkType)
    {
#if HOLOLENS2
        Microsoft.MixedReality.Toolkit.UI.BoundsControl.BoundsControl bc = null;
        MoveAxisConstraint moveAxisConstraint = null;
        RotationAxisConstraint rotationAxisConstraint = null;
        if (ObjectManager.Instance.currentSelectObject.gameObject != null)
        {
            bc = ObjectManager.Instance.currentSelectObject.gameObject.GetComponent<Microsoft.MixedReality.Toolkit.UI.BoundsControl.BoundsControl>();
            moveAxisConstraint = ObjectManager.Instance.currentSelectObject.gameObject.GetComponent<MoveAxisConstraint>();
            rotationAxisConstraint = ObjectManager.Instance.currentSelectObject.gameObject.GetComponent<RotationAxisConstraint>();

            bc.ScaleHandlesConfig.ShowScaleHandles = (editMarkType == EditMarkType.SCALE);
            bc.RotationHandlesConfig.ShowHandleForX = bc.RotationHandlesConfig.ShowHandleForZ = false;
            bc.RotationHandlesConfig.ShowHandleForY = (editMarkType == EditMarkType.ROTATE);
            bc.TranslationHandlesConfig.ShowHandleForX = bc.TranslationHandlesConfig.ShowHandleForZ = (editMarkType == EditMarkType.MOVE);
            bc.TranslationHandlesConfig.ShowHandleForY = (editMarkType == EditMarkType.HEIGHT);

            moveAxisConstraint.ConstraintOnMovement = moveAxisConstraint.ConstraintOnMovement & 0;
            rotationAxisConstraint.ConstraintOnRotation = rotationAxisConstraint.ConstraintOnRotation & 0;
            bc.enabled = false;
        }
#endif

        switch (editMarkType)
        {
            case EditMarkType.MOVE:
#if HOLOLENS2
                if (bc != null)
                {
                    moveAxisConstraint.ConstraintOnMovement = AxisFlags.YAxis;
                    rotationAxisConstraint.ConstraintOnRotation = AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
                    bc.enabled = flag;
                }
#else
                if (ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>() != null)
                    ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>().Setting(flag, EditMarkType.MOVE);
#endif
                SetInputType(InputType.MOVE, flag);
                break;

            case EditMarkType.ROTATE:
#if HOLOLENS2
                if (bc != null)
                {
                    moveAxisConstraint.ConstraintOnMovement = AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
                    rotationAxisConstraint.ConstraintOnRotation = AxisFlags.XAxis | AxisFlags.ZAxis;
                    bc.enabled = flag;
                }
#else
                if (ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>() != null)
                    ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>().Setting(flag, EditMarkType.ROTATE);
#endif
                SetInputType(InputType.ROTATE, flag);
                break;

            case EditMarkType.SCALE:
#if HOLOLENS2
                if (bc != null)
                {
                    moveAxisConstraint.ConstraintOnMovement = AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
                    rotationAxisConstraint.ConstraintOnRotation = AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
                    bc.enabled = flag;
                }
#else
                if (ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>() != null)
                    ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>().Setting(flag, EditMarkType.SCALE);
#endif
                SetInputType(InputType.SCALE, flag);
                break;

            case EditMarkType.HEIGHT:
#if HOLOLENS2
                if (bc != null)
                {
                    moveAxisConstraint.ConstraintOnMovement = AxisFlags.XAxis | AxisFlags.ZAxis;
                    rotationAxisConstraint.ConstraintOnRotation = AxisFlags.XAxis | AxisFlags.YAxis | AxisFlags.ZAxis;
                    bc.enabled = flag;
                }
#else
                if (ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>() != null)
                    ObjectManager.Instance.currentSelectObject.GetComponentInChildren<EditMarker>().Setting(flag, EditMarkType.HEIGHT);

#endif
                SetInputType(InputType.HEIGHT, flag);
                break;
        }

    }

#endif
}


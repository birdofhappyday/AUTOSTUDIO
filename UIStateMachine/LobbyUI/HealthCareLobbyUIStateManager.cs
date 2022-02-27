using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HealthCareUIMode
{
    Conference,
    Hospice,
    Stoma,
}

public enum HealthCareCustomerType
{
    Doctor,
    Patient
}

public enum HealthCareLobbyState
{
    Session,
    Main
}

public enum HealthCareLobbyUIType
{
    Multidisciplinary,
    DoctorStomaUI,
    PatientStomaUI,
    DoctorHospiceUI,
    PatientHospiceUI,
    None,
}

public class HealthCareLobbyUIStateManager : MonoBehaviour
{
    [SerializeField]
    HealthCareLobbyStateMachine m_healthCareLobbyStateMachine;
    [SerializeField]
    HealthCareSessionStateMachine m_healthCareSeesionStateMachine;
    [SerializeField]
    public HealthCareMainUI m_healthCareMainUI;
    [SerializeField]
    HealthCareVisitUI m_healthCareVisitUI;
    [SerializeField]
    HealthCareSessionMakeUI m_healthCareSessionMakeUI;
    [SerializeField]
    HealthCareLobbyUITypeStateMachine m_healthCareLobbyUITypeStateMachine;

    public HealthCareLobbyUIType HealthCareLobbyUITypeStateMachine { get => m_healthCareLobbyUITypeStateMachine.state ; set => m_healthCareLobbyUITypeStateMachine.state = value; }

    private void Awake()
    {
        ARP_NetMain.Instance.onRoomListUpdate += m_healthCareSessionMakeUI.OnRoomListUpdate;
    }

    private void OnDestroy()
    {
        if (ARP_NetMain.Instance != null && m_healthCareSessionMakeUI != null)
        {
            ARP_NetMain.Instance.onRoomListUpdate -= m_healthCareSessionMakeUI.OnRoomListUpdate;
        }
    }

    public void SessionInit()
    {
        m_healthCareLobbyStateMachine.state = HealthCareLobbyState.Session;
        ARCamera.instance.LeftLobby();
        m_healthCareSeesionStateMachine.state = LobbySessionUIState.Platform;
        HealthCareLobbyUITypeStateMachine = HealthCareLobbyUIType.None;
        Managed.XRHubSceneManager.Instance.SetSceneState(SceneState.LOBBY);
    }

    public void MainUIInit()
    {
        m_healthCareLobbyStateMachine.state = HealthCareLobbyState.Main;
        m_healthCareMainUI.MainUIInit(HealthCareLobbyUITypeStateMachine);
        LobbyUI.Instance.UIInit();
    }

    // [김성민]
    // 앵커 찍고 방을 만들지, 들어갈지 결정해주는 함수
    public void AnchorAfterAction()
    {
        m_healthCareVisitUI.AnchorAfterAction();
    }
}

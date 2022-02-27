using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum LobbySessionUIState
{
    Platform,
    SeesionCreate,
    SessionVisit
}

public class HealthCareSessionStateMachine : UIStateMachine<LobbySessionUIState>
{
    private HealthCareLobbyUIType m_healthCareLobbyUIType = HealthCareLobbyUIType.None;

    [SerializeField]
    private HealthCareLobbyUITypeStateMachine m_healthCareLobbyUITypeStateMachine;

    public HealthCareLobbyUIType HealthCareLobbyUIType { get => m_healthCareLobbyUIType; set => m_healthCareLobbyUIType = value; }

    public void OnClickPlatformSelect()
    {
        if (HealthCareLobbyUIType != HealthCareLobbyUIType.None)
        {
            m_healthCareLobbyUITypeStateMachine.state = HealthCareLobbyUIType;
            state = LobbySessionUIState.SeesionCreate;

            // [은지] 장루체험 환자/의료진 분류 ar
            if (HealthCareLobbyUIType == HealthCareLobbyUIType.PatientStomaUI)
            {
                StomaTheoryData.Instance.userChanged = StomaTheoryData.Instance.eUserType == UserType.PATIENT ? false : true;
                StomaTheoryData.Instance.eUserType = UserType.PATIENT;
            }
            else if (HealthCareLobbyUIType == HealthCareLobbyUIType.DoctorStomaUI)
            {
                StomaTheoryData.Instance.userChanged = StomaTheoryData.Instance.eUserType == UserType.DOCTOR ? false : true;
                StomaTheoryData.Instance.eUserType = UserType.DOCTOR;
            }

            StomaTheoryData.Instance.DataInit();


            Debug.Log("OnClickPlatformSelect");
        }
    }
}

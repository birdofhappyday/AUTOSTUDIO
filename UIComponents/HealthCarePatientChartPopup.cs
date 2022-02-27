using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [김성민]
/// 헬스케어 차트 생성 팝업.
/// 환자 이름, 생일, 핸드폰 번호는 필수값으로 없을시에 값이 제대로 오지 않는다.
/// 그래서 팝업에서 입력받는데 하나라도 세팅이 안 되어 있을 경우 버튼 클릭이 되지 않는다.
/// 서버 결과에 따라서 값을 세팅하고 생성시에 서버에서 받은 string을 그대로 전달해서 프리팹에서 값을 변환해서 사용한다.
/// </summary>
public class HealthCarePatientChartPopup : PopupBase
{
    [SerializeField]
    InputField nameInputField;
    [SerializeField]
    PatientBirthDay patientBirthDay;
    [SerializeField]
    PatientPhoneNumber patientPhoneNumber;

    public override void Open()
    {
        InputFieldInit();
        base.Open();
    }

    void InputFieldInit()
    {
        nameInputField.text = "김레몬";
    }

    public void MakeClick()
    {
        if(!string.IsNullOrEmpty(nameInputField.text))
        {
            SendPatientChart _patientChart = new SendPatientChart();
            _patientChart.name = nameInputField.text;
            _patientChart.birthday = patientBirthDay.GetBirthDay();
            _patientChart.phoneNumber = patientPhoneNumber.GetPhoneNumber();

            SetDate(_patientChart);

            SendRequest(_patientChart, MakeSuccessAction, MakeFailAction);
        }
    }

    #region 네트워크

    [Serializable]
    public class SendPatientChart
    {
        public string name = string.Empty;
        public string birthday = string.Empty;
        public string phoneNumber = string.Empty;
        public string startDate = string.Empty;
        public string endDate = string.Empty;
        public string phrType = string.Empty;
    }

    [Serializable]
    public class ReceivePatientChart
    {
        public string idx = string.Empty;
        public string memberIdx = string.Empty;
        public string phrType = string.Empty;
        public string phrValue = string.Empty;
        public string phrValue2 = string.Empty;
        public string name = string.Empty;
        public string phoneNumber = string.Empty;
        public string birthday = string.Empty;
        public string recordedDate = string.Empty;
        public string createDate = string.Empty;
    }

    const string ErrorMessage = "message";

    public IEnumerator GetRequestWithBody(string _json, Action<string> _succesAction = null, Action<string> failAction = null)
    {
        string _url = "https://life-center-dev.lemonhc.com/nfnc/nfnc/api/phrData";

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(_url))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("accept", "*/*");
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(_json));
            yield return www.SendWebRequest();

            // 받는 값이 제대로 된 값이 아니면 에러 메시지를 띄어준다.
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                failAction?.Invoke(www.error);
            }
            else
            {
                if (www.isDone)
                {
                    //string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    string _jsonResult = www.downloadHandler.text;

                    // 데이터를 받아서 분기 처리한다.
                    // 변환이 제대로 되면 값이 제대로 온 것이기에 string값을 그대로 전달해준다.
                    try
                    {
                        var _data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReceivePatientChart>>(_jsonResult);
                        MakeSuccessAction(_jsonResult);
                    }

                    // 데이터를 찾을 수 없다고 에러가 온 상태이기에 체크해서 메시지를 띄워준다.
                    catch
                    {
                        var _compareTemp = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(_jsonResult);

                        if (_compareTemp.ContainsKey(ErrorMessage))
                        {
                            MakeFailAction(_compareTemp[ErrorMessage]);
                        }
                    }
                }
            }
        }
    }

    public void SendRequest(SendPatientChart _sendPatientChart, Action<string> _succesAction = null, Action<string> failAction = null)
    {
        string _json = Newtonsoft.Json.JsonConvert.SerializeObject(_sendPatientChart);

        StartCoroutine(GetRequestWithBody(_json, _succesAction, failAction));
    }

    // 날짜 설정값(현재 3달)
    public const int DateDuration = 89;
    void SetDate(SendPatientChart _senPatientChart)
    {
        DateTime _today = DateTime.Now.Date;
        DateTime _before = _today.AddDays(-DateDuration);

        //_senPatientChart.startDate = _before.ToString("yyyy-MM-dd HH:mm:ss.fff");

        //_senPatientChart.startDate = _before.ToString("2021-10-28 00:00:00.000");
        //_senPatientChart.endDate = _today.ToString("yyyy-MM-dd HH:mm:ss.fff");
        _senPatientChart.startDate = _before.ToString("yyyyMMdd");
        _senPatientChart.endDate = _today.ToString("yyyyMMdd");
    }

    ResourceInfo selectedInfo = null;
    void MakeSuccessAction(string _receivePatientChart)
    {
        selectedInfo = new ResourceInfo();
        selectedInfo.url = _receivePatientChart;

#if !OCULUS
        ObjectManagerAR.Instance.CreateObjectStart(ObjectType.Chart, selectedInfo);
#else
        ObjectManagerVR.Instance.CreateObjectStart(ObjectType.Chart, selectedInfo);
#endif
        Close();
    }

    void MakeFailAction(string _receivePatientChart)
    {
        PublicUI.Instance.ErrorPopupOpen(_receivePatientChart);
        Close();
    }

    #endregion
}

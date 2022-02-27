using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [�輺��]
/// �ｺ�ɾ� ��Ʈ ���� �˾�.
/// ȯ�� �̸�, ����, �ڵ��� ��ȣ�� �ʼ������� �����ÿ� ���� ����� ���� �ʴ´�.
/// �׷��� �˾����� �Է¹޴µ� �ϳ��� ������ �� �Ǿ� ���� ��� ��ư Ŭ���� ���� �ʴ´�.
/// ���� ����� ���� ���� �����ϰ� �����ÿ� �������� ���� string�� �״�� �����ؼ� �����տ��� ���� ��ȯ�ؼ� ����Ѵ�.
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
        nameInputField.text = "�跹��";
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

    #region ��Ʈ��ũ

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

            // �޴� ���� ����� �� ���� �ƴϸ� ���� �޽����� ����ش�.
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

                    // �����͸� �޾Ƽ� �б� ó���Ѵ�.
                    // ��ȯ�� ����� �Ǹ� ���� ����� �� ���̱⿡ string���� �״�� �������ش�.
                    try
                    {
                        var _data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReceivePatientChart>>(_jsonResult);
                        MakeSuccessAction(_jsonResult);
                    }

                    // �����͸� ã�� �� ���ٰ� ������ �� �����̱⿡ üũ�ؼ� �޽����� ����ش�.
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

    // ��¥ ������(���� 3��)
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

using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [김성민]
/// 차트 프리팹
/// 동기화 될 값을 DisPalyPatientData Class로 관리해서 포통으로 동기화한다.
/// string 값을 ReceivePatientChart리스트로 변환해서 내부 Dictionary에 날짜 key값으로 저장해서 불러온다.
/// 값은 나중에 온 값으로 덮어쓰는 방식으로 서버에서 준 값이 최근순으로 자동 정렬된다 생각하고 진행한다.
/// </summary>
public class HealthCarePatientChartPrefab : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback, IPunObservable, IPunOwnershipCallbacks
{
    [Serializable]
    public class DisPalyPatientData
    {
        public string name = string.Empty;
        public string birthday = string.Empty;
        public string phoneNumber = string.Empty;
        public string temperature = string.Empty;
        public string bloodPressureMin = string.Empty;
        public string bloodPressureMax = string.Empty;
        public string pulseRate = string.Empty;
        public string oxygenSaturation = string.Empty;
        public string bloodSugar = string.Empty;
    }
    [SerializeField]
    TMPro.TextMeshProUGUI patientNameText;
    [SerializeField]
    TMPro.TextMeshProUGUI patientBirthDayText;
    [SerializeField]
    TMPro.TextMeshProUGUI patientPhoneNumberText;
    [SerializeField]
    TMPro.TextMeshProUGUI dateTimeText;
    [SerializeField]
    TMPro.TextMeshProUGUI patientTemperatureText;
    [SerializeField]
    TMPro.TextMeshProUGUI patientBloodPressureText;
    [SerializeField]
    TMPro.TextMeshProUGUI patientPulseText;
    [SerializeField]
    TMPro.TextMeshProUGUI patientOxygenSaturationText;
    [SerializeField]
    TMPro.TextMeshProUGUI patientBloodSugarText;
    [SerializeField]
    EditMarker editMarker;

    [SerializeField]
    GameObject buttonParent;
    [SerializeField]
    Button beforeButton;
    [SerializeField]
    Button nextButton;
    [SerializeField]
    List<Toggle> graphicToggles;

    DateTime today;

    DateTime dateTime;
    string patientName;
    string patientBirthDay;
    string patientphoneNumber;
    string patienttemperature;
    string patientBloodPressureMin;
    string patientBloodPressureMax;
    string patientpulseRate;
    string patientoxygenSaturation;
    string patientbloodSugar;

    #region property
    public DateTime CurrentDateTime
    {
        get => dateTime;
        set
        {
            if (dateTime != value)
            {
                dateTime = value;
                dateTimeText.text = dateTime.ToString("yyyy-MM-dd");
                SetPatientDayInfo(dateTimeText.text);
            }
        }
    }

    public string PatientName
    {
        get => patientName;
        set
        {
            if (patientName != value)
            {
                patientName = value;
                patientNameText.text = patientName;
            }
        }
    }

    public string PatientBirthDay
    {
        get => patientBirthDay;
        set
        {
            if (patientBirthDay != value)
            {
                patientBirthDay = value;
                patientBirthDayText.text = patientBirthDay;
            }
        }
    }

    public string PatientPhoneNumber
    {
        get => patientphoneNumber;
        set
        {
            if (patientphoneNumber != value)
            {
                patientphoneNumber = value;
                patientPhoneNumberText.text = patientphoneNumber;
            }
        }
    }

    public string PatientTemperature
    {
        get => patienttemperature;
        set
        {
            if (patienttemperature != value)
            {
                patienttemperature = value;
                if (!string.IsNullOrEmpty(value))
                    patientTemperatureText.text = $"{patienttemperature}°C";
                else
                    patientTemperatureText.text = string.Empty;
            }
        }
    }

    public string PatientBloodPressureMin
    {
        get => patientBloodPressureMin;
        set
        {
            if (patientBloodPressureMin != value)
            {
                patientBloodPressureMin = value;
                if (!string.IsNullOrEmpty(value))
                    patientBloodPressureText.text = $"{PatientBloodPressureMax}/{PatientBloodPressureMin} mmHg";
                else
                    patientBloodPressureText.text = string.Empty;
            }
        }
    }
    public string PatientBloodPressureMax
    {
        get => patientBloodPressureMax;
        set
        {
            if (patientBloodPressureMax != value)
            {
                patientBloodPressureMax = value;
                if (!string.IsNullOrEmpty(value))
                    patientBloodPressureText.text = $"{PatientBloodPressureMax}/{PatientBloodPressureMin} mmHg";
                else
                    patientBloodPressureText.text = string.Empty;
            }
        }
    }
    public string PatientpulseRate
    {
        get => patientpulseRate;
        set
        {
            if (patientpulseRate != value)
            {
                patientpulseRate = value;
                if (!string.IsNullOrEmpty(value))
                    patientPulseText.text = $"{patientpulseRate} bpm";
                else
                    patientPulseText.text = string.Empty;
            }
        }
    }

    public string PatientOxygenSaturation
    {
        get => patientoxygenSaturation;
        set
        {
            if (patientoxygenSaturation != value)
            {
                patientoxygenSaturation = value;
                if (!string.IsNullOrEmpty(value))
                    patientOxygenSaturationText.text = $"{patientoxygenSaturation} %";
                else
                    patientOxygenSaturationText.text = string.Empty;
            }
        }
    }
    public string PatientBloodSugar
    {
        get => patientbloodSugar;
        set
        {
            if (patientbloodSugar != value)
            {
                patientbloodSugar = value;
                if (!string.IsNullOrEmpty(value))
                    patientBloodSugarText.text = $"{patientbloodSugar} mg/dL";
                else
                    patientBloodSugarText.text = string.Empty;
            }
        }
    }
    public Vector3 Position
    {
        get => transform.localPosition;
        set
        {
            if (transform.localPosition != value)
                transform.localPosition = value;
        }
    }

    public Quaternion Rotation
    {
        get => transform.localRotation;
        set
        {
            if (transform.localRotation != value)
                transform.localRotation = value;
        }
    }

    public Vector3 Scale
    {
        get => transform.localScale;
        set
        {
            if (transform.localScale != value)
                transform.localScale = value;
        }
    }

    int graphicToggleIndex = -1;
    public int DurationToggleIndex
    {
        get => graphicToggleIndex;
        set
        {
            if (value != graphicToggleIndex)
            {
                graphicToggleIndex = value;
                graphicToggles[DurationToggleIndex].SetIsOnWithoutNotify(true);
                SetLineTimeDuration(DurationToggleIndex);
                SetGraphXValueSetting();
            }
        }
    }
    #endregion

    [SerializeField]
    int[] Duration;
    [SerializeField]
    float CentimeterPerCoordUnitX30 = 1.45f;
    [SerializeField]
    float CentimeterPerCoordUnitX60 = 0.7f;
    [SerializeField]
    float CentimeterPerCoordUnitX90 = 0.46f;

    [SerializeField]
    List<DD_DataDiagram> dd_DataDiagram;


    int lineTimeDuration = 0;
    public void SetLineTimeDuration(int _index)
    {
        lineTimeDuration = Duration[_index];
    }

    public void OnClickSetDurationToggle(int _index)
    {
        DurationToggleIndex = _index;
    }

    void SetGraphXValueSetting()
    {
        float _xValue = 0;

        switch (lineTimeDuration)
        {
            case 29:
                _xValue = CentimeterPerCoordUnitX30;
                break;

            case 59:
                _xValue = CentimeterPerCoordUnitX60;
                break;

            case 89:
                _xValue = CentimeterPerCoordUnitX90;
                break;
        }

        foreach (var _dataDigram in dd_DataDiagram)
            _dataDigram.m_CentimeterPerCoordUnitX = _xValue;

        SetGraph();
    }

    int xOffset = 1;

    GameObject _temperatureLine = null;
    GameObject _bloodPressureLine = null;
    GameObject _bloodPressureLine2 = null;
    GameObject _pulseRateLine = null;
    GameObject _oxygenSaturationLine = null;
    GameObject _bloodSugerLine = null;


    // 그래프 그리기
    // 값별로 나눠서 그린다.
    public void SetGraph()
    {
        xOffset = 1;
        float _endYpos = float.MinValue;

        // Temperature
        {
            _endYpos = float.MinValue;

            if (_temperatureLine == null)
            {
                Color color;
                ColorUtility.TryParseHtmlString("#FF000D", out color);
                _temperatureLine = dd_DataDiagram[0].AddLine("TemperatureLine", color);
            }
            else
                _temperatureLine.GetComponent<DD_Lines>().Clear();

            for (int j = 0; j < lineTimeDuration; ++j)
            {
                DateTime _dateTime = DateTime.Now.Date;
                _dateTime = _dateTime.AddDays(-j);
                string _key = _dateTime.ToString("yyyy-MM-dd");
                if (receivePatientChartDic.ContainsKey(_key))
                {
                    float _resut;

                    if (float.TryParse(receivePatientChartDic[_key].temperature, out _resut))
                    {
                        dd_DataDiagram[0].InputPoint(_temperatureLine, new Vector2(xOffset, _resut));
                        _endYpos = _resut;
                        xOffset = 1;
                    }
                    else
                        ++xOffset;
                }
                else
                    ++xOffset;
            }

            if (xOffset != 1)
            {
                dd_DataDiagram[0].InputPoint(_temperatureLine, new Vector2(xOffset, _endYpos));
                xOffset = 1;
            }
        }

        // BloodPressure
        {
            _endYpos = float.MinValue;
            float _endMinYpos = float.MinValue;

            if (_bloodPressureLine == null)
            {
                Color color;
                ColorUtility.TryParseHtmlString("#FF000D", out color);
                _bloodPressureLine = dd_DataDiagram[1].AddLine("BloodPressureLine", color);
                ColorUtility.TryParseHtmlString("#0000FF", out color);
                _bloodPressureLine2 = dd_DataDiagram[1].AddLine("BloodPressureLine2", color);
            }
            else
            {
                _bloodPressureLine.GetComponent<DD_Lines>().Clear();
                _bloodPressureLine2.GetComponent<DD_Lines>().Clear();
            }

            for (int j = 0; j < lineTimeDuration; ++j)
            {
                DateTime _dateTime = DateTime.Now.Date;
                _dateTime = _dateTime.AddDays(-j);
                string _key = _dateTime.ToString("yyyy-MM-dd");
                if (receivePatientChartDic.ContainsKey(_key))
                {
                    float _maxResult = float.MaxValue;

                    if (float.TryParse(receivePatientChartDic[_key].bloodPressureMax, out _maxResult))
                    {
                        dd_DataDiagram[1].InputPoint(_bloodPressureLine, new Vector2(xOffset, _maxResult));
                        _endYpos = _maxResult;
                    }

                    float _minResult = float.MinValue;

                    if (float.TryParse(receivePatientChartDic[_key].bloodPressureMin, out _minResult))
                    {
                        dd_DataDiagram[1].InputPoint(_bloodPressureLine2, new Vector2(xOffset, _minResult));
                        _endMinYpos = _minResult;
                    }

                    if (_maxResult != float.MaxValue || _minResult != float.MinValue)
                        xOffset = 1;
                    else
                        ++xOffset;
                }
                else
                    ++xOffset;
            }

            if (xOffset != 1)
            {
                dd_DataDiagram[1].InputPoint(_bloodPressureLine, new Vector2(xOffset, _endYpos));
                dd_DataDiagram[1].InputPoint(_bloodPressureLine2, new Vector2(xOffset, _endMinYpos));
                xOffset = 1;
            }
        }

        // pulseRate
        {
            _endYpos = float.MinValue;

            if (_pulseRateLine == null)
            {
                Color color;
                ColorUtility.TryParseHtmlString("#FF000D", out color);
                _pulseRateLine = dd_DataDiagram[2].AddLine("PulseRateLine", color);
            }
            else
                _pulseRateLine.GetComponent<DD_Lines>().Clear();

            for (int j = 0; j < lineTimeDuration; ++j)
            {
                DateTime _dateTime = DateTime.Now.Date;
                _dateTime = _dateTime.AddDays(-j);
                string _key = _dateTime.ToString("yyyy-MM-dd");
                if (receivePatientChartDic.ContainsKey(_key))
                {
                    float _resut;

                    if (float.TryParse(receivePatientChartDic[_key].pulseRate, out _resut))
                    {
                        dd_DataDiagram[2].InputPoint(_pulseRateLine, new Vector2(xOffset, _resut));
                        _endYpos = _resut;
                        xOffset = 1;
                    }
                    else
                        ++xOffset;
                }
                else
                    ++xOffset;
            }

            if (xOffset != 1)
            {
                dd_DataDiagram[2].InputPoint(_pulseRateLine, new Vector2(xOffset, _endYpos));
                xOffset = 1;
            }
        }

        // OxygenSaturation
        {
            _endYpos = float.MinValue;

            if (_oxygenSaturationLine == null)
            {
                Color color;
                ColorUtility.TryParseHtmlString("#FF000D", out color);
                _oxygenSaturationLine = dd_DataDiagram[3].AddLine("OxygenSaturationLine", color);
            }
            else
                _oxygenSaturationLine.GetComponent<DD_Lines>().Clear();

            for (int j = 0; j < lineTimeDuration; ++j)
            {
                DateTime _dateTime = DateTime.Now.Date;
                _dateTime = _dateTime.AddDays(-j);
                string _key = _dateTime.ToString("yyyy-MM-dd");
                if (receivePatientChartDic.ContainsKey(_key))
                {
                    float _resut;

                    if (float.TryParse(receivePatientChartDic[_key].oxygenSaturation, out _resut))
                    {
                        dd_DataDiagram[3].InputPoint(_oxygenSaturationLine, new Vector2(xOffset, _resut));
                        _endYpos = _resut;
                        xOffset = 1;
                    }
                    else
                        ++xOffset;
                }
                else
                    ++xOffset;
            }

            if (xOffset != 1)
            {
                dd_DataDiagram[3].InputPoint(_oxygenSaturationLine, new Vector2(xOffset, _endYpos));
                xOffset = 1;
            }
        }

        // BloodSugar
        {
            _endYpos = float.MinValue;

            if (_bloodSugerLine == null)
            {
                Color color;
                ColorUtility.TryParseHtmlString("#FF000D", out color);
                _bloodSugerLine = dd_DataDiagram[4].AddLine("BloodSugerLine", color);
            }
            else
                _bloodSugerLine.GetComponent<DD_Lines>().Clear();

            for (int j = 0; j < lineTimeDuration; ++j)
            {
                DateTime _dateTime = DateTime.Now.Date;
                _dateTime = _dateTime.AddDays(-j);
                string _key = _dateTime.ToString("yyyy-MM-dd");
                if (receivePatientChartDic.ContainsKey(_key))
                {
                    float _resut;

                    if (float.TryParse(receivePatientChartDic[_key].bloodSugar, out _resut))
                    {
                        dd_DataDiagram[4].InputPoint(_bloodSugerLine, new Vector2(xOffset, _resut));
                        _endYpos = _resut;
                        xOffset = 1;
                    }
                    else
                        ++xOffset;
                }
                else
                    ++xOffset;
            }

            if (xOffset != 1)
            {
                dd_DataDiagram[4].InputPoint(_bloodSugerLine, new Vector2(xOffset, _endYpos));
                xOffset = 1;
            }
        }
    }

    Dictionary<string, DisPalyPatientData> receivePatientChartDic = new Dictionary<string, DisPalyPatientData>();

    void SetPatientBasicInfo(string _patientName, string _patientBirthDay, string _patientPhoneNumber)
    {
        PatientName = _patientName;
        PatientBirthDay = _patientBirthDay;
        PatientPhoneNumber = _patientPhoneNumber;
    }

    void SetPatientDayInfo(string _dateTime)
    {
        if (receivePatientChartDic.ContainsKey(_dateTime))
        {
            var _data = receivePatientChartDic[_dateTime];
            PatientTemperature = _data.temperature;
            PatientBloodPressureMax = _data.bloodPressureMax;
            PatientBloodPressureMin = _data.bloodPressureMin;
            PatientOxygenSaturation = _data.oxygenSaturation;
            PatientBloodSugar = _data.bloodSugar;
            PatientpulseRate = _data.pulseRate;
        }
        else
        {
            PatientTemperature = string.Empty;
            PatientBloodPressureMax = string.Empty;
            PatientBloodPressureMin = string.Empty;
            PatientOxygenSaturation = string.Empty;
            PatientBloodSugar = string.Empty;
            PatientpulseRate = string.Empty;
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Initialize(info.photonView.InstantiationData);
    }

    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        Debug.Log("HealthCarePatientChartPrefab OnOwnershipRequest");
        ButtonInteractable(requestingPlayer == PhotonNetwork.LocalPlayer);
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        Debug.Log("HealthCarePatientChartPrefab OnOwnershipTransfered");
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        Debug.Log("HealthCarePatientChartPrefab OnOwnershipTransfered");
    }

    int createIndex;
    //받는 값은 string으로 json변환해 주어서 사용한다.
    public void Initialize(object[] _instantiateData)
    {
        beforeButton.onClick.AddListener(OnClickBeforeButton);
        nextButton.onClick.AddListener(OnClickNextButton);

#if OCULUS
        buttonParent.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
#else
        buttonParent.AddComponent<GraphicRaycaster>();
#endif

        transform.SetParent(ObjectManager.Instance.FlagObject.transform);
        editMarker.ActiveSetting(false);

        createIndex = (int)_instantiateData[3];
        ObjectManager.Instance.AddAllObject(createIndex, this.gameObject);
        receivePatientChartDic.Clear();
        var _list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HealthCarePatientChartPopup.ReceivePatientChart>>((string)_instantiateData[1]);

        foreach (var _value in _list)
        {
            string _key = _value.recordedDate.Split(' ')[0];

            if (receivePatientChartDic.ContainsKey(_key))
            {
                SettingReceiveData(_value, receivePatientChartDic[_key]);
            }
            else
            {
                DisPalyPatientData receivePatientChart = new DisPalyPatientData();
                SettingReceiveData(_value, receivePatientChart);
                receivePatientChartDic.Add(_key, receivePatientChart);
            }
        }

        today = DateTime.Now.Date;
        CurrentDateTime = DateTime.Now.Date;

        SetPatientBasicInfo(_list[0].name, _list[0].birthday, _list[0].phoneNumber);

        if (ObjectManager.Instance.objectController)
            PublicUI.Instance.SettingInteractDeleteToggle(true);

        SetGraph();

        ButtonInteractable(photonView.AmOwner);

        PublicUI.Instance.LoadingObjSet();
    }

    const string Temperature = "temperature";
    const string PulseRate = "pulseRate";
    const string OxygenSaturation = "oxygenSaturation";
    const string BloodSugar = "bloodSugar";
    const string BloodPressure = "bloodPressure";

    void SettingReceiveData(HealthCarePatientChartPopup.ReceivePatientChart _value, DisPalyPatientData _settingValue)
    {
        switch (_value.phrType)
        {
            case Temperature:
                _settingValue.temperature = _value.phrValue;
                break;

            case BloodSugar:
                _settingValue.bloodSugar = _value.phrValue;
                break;

            case OxygenSaturation:
                _settingValue.oxygenSaturation = _value.phrValue;
                break;

            case BloodPressure:
                _settingValue.bloodPressureMax = _value.phrValue;
                _settingValue.bloodPressureMin = _value.phrValue2;
                break;

            case PulseRate:
                _settingValue.pulseRate = _value.phrValue;
                break;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(PatientName);
            stream.SendNext(patientBirthDay);
            stream.SendNext(PatientPhoneNumber);
            stream.SendNext(CurrentDateTime.ToBinary());
            stream.SendNext(PatientTemperature);
            stream.SendNext(PatientBloodPressureMin);
            stream.SendNext(PatientBloodPressureMax);
            stream.SendNext(PatientpulseRate);
            stream.SendNext(PatientOxygenSaturation);
            stream.SendNext(PatientBloodSugar);
            stream.SendNext(Position);
            stream.SendNext(Rotation);
            stream.SendNext(Scale);
            stream.SendNext(DurationToggleIndex);
        }
        else
        {
            PatientName = (string)stream.ReceiveNext();
            PatientBirthDay = (string)stream.ReceiveNext();
            PatientPhoneNumber = (string)stream.ReceiveNext();
            CurrentDateTime = DateTime.FromBinary((long)stream.ReceiveNext());
            PatientTemperature = (string)stream.ReceiveNext();
            PatientBloodPressureMin = (string)stream.ReceiveNext();
            PatientBloodPressureMax = (string)stream.ReceiveNext();
            PatientpulseRate = (string)stream.ReceiveNext();
            PatientOxygenSaturation = (string)stream.ReceiveNext();
            PatientBloodSugar = (string)stream.ReceiveNext();
            Position = (Vector3)stream.ReceiveNext();
            Rotation = (Quaternion)stream.ReceiveNext();
            Scale = (Vector3)stream.ReceiveNext();
            DurationToggleIndex = (int)stream.ReceiveNext();
        }
    }

    private void OnDestroy()
    {
        if (ObjectManager.Instance != null)
        {
            if (ObjectManager.Instance.AllObjectList != null)
                ObjectManager.Instance.AllObjectList.Remove(createIndex);
        }

        if (ARP_NetMain.Instance != null)
        {
            if (ARP_NetMain.Instance.SyncObjects.Count != 0)
                ARP_NetMain.Instance.SyncObjects.Remove(this.gameObject);
        }
    }

    // 방장인지(정확히는 이 물체의 Owner체크) 체크해서 버튼 클릭 막음.
    void ButtonInteractable(bool _interactable)
    {
        beforeButton.interactable = _interactable;
        nextButton.interactable = _interactable;

        foreach (var _toggle in graphicToggles)
            _toggle.interactable = _interactable;
    }

    #region 버튼

    public void OnClickBeforeButton()
    {
        if (today.AddDays(-HealthCarePatientChartPopup.DateDuration) > CurrentDateTime)
            return;

        CurrentDateTime = CurrentDateTime.AddDays(-1);
    }

    public void OnClickNextButton()
    {
        if (today <= CurrentDateTime)
            return;

        CurrentDateTime = CurrentDateTime.AddDays(1);
    }
    #endregion
}

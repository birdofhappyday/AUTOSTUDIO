using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BootUIBase : PC 옵져버 부분을 담당하는 소스
/// </summary>
public abstract partial class BootUIBase : MonoBehaviour
{
    [SerializeField, Tooltip("ErrorMessage 발생시 출력할 텍스트 리소스")]
    ErrorMessage m_ErrorMessage;

    // 마지막으로 발생한 에러코드를 저장. 에러 상태로 이동하면 메시지 창에 해당 에러메시지의 텍스트를 상기 텍스트 리소스에서 찾아서 출력해준다.
    public ErrorMessage.ErrorCode lastError { get; set; } = ErrorMessage.ErrorCode.UNKNOWN_ERROR_MESSAGE;

    // 위 에러 메시지 리소스에서 lastError코드에 해당하는 에러 텍스트를 찾아 반환해준다.
    public string lastErrorMessage
    {
        get { return m_ErrorMessage.GetErrorMessage(lastError); }
    }

    // BootUI 상태머신을 위한 상태저장 변수
    protected int state
    {
        get;
        set;
    } = -1;

    // 상태전환용 추상함수.
    public abstract void SetState(System.Enum state);

    public abstract UIPage GetCurrentPage();
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCObserverMultiPage : UIPage
{
    [SerializeField]
    string exitMessage;

    public void MulityPageExit()
    {
        PublicUI.Instance.MessagePopupOpen(exitMessage, () => ARP_NetMain.Instance.LeaveRoom());
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCLobbyPage : UIPage
{
    [SerializeField]
    private RoomListPopup roomListPopup;

    public override void Init()
    {
        base.Init();
        ARP_NetMain.Instance.onRoomListUpdate += OnRoomListUpdate;
    }

    private void OnDestroy()
    {
        if (PhotonWrapper.NetworkManager.Instance != null)
        {
            ARP_NetMain.Instance.onRoomListUpdate -= OnRoomListUpdate;
        }
    }
    public void OnRoomListUpdate(List<ARP_NetRoom> roomList)
    {
        if (roomListPopup != null)
            roomListPopup.RoomListUpdate(roomList);
    }

    // �˾��� ������ ���� ���õǾ��ٴ� ����� �˷��ش�.
    public void SetRoom(ARP_NetRoom _selectRoom)
    {
        roomListPopup.currentRoom = _selectRoom;
    }
}

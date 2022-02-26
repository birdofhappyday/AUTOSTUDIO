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

    // 팝업에 유저가 방이 선택되었다는 사실을 알려준다.
    public void SetRoom(ARP_NetRoom _selectRoom)
    {
        roomListPopup.currentRoom = _selectRoom;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmphasisUI : MonoBehaviour
{
    public Emphasis emphasis;

    public Toggle toggle;

    private bool _init = false;

    private void Awake()
    {
        if (toggle == null)
            toggle = this.GetComponent<Toggle>();
        if (!_init)
        {
            toggle.onValueChanged.AddListener((isSelected) => { OnClick(isSelected); });
            toggle.group = LobbyUI_RealConnect.Instance.CurrentOpenPopup.GetComponent<EmphasisPopup>().listParent.GetComponent<ToggleGroup>();
            _init = true;
        }
    }

    public void OnClick(bool _b)
    {
        EmphasisPopup _ep = LobbyUI_RealConnect.Instance.CurrentOpenPopup.GetComponent<EmphasisPopup>();

        if (_b)
        {
            _ep.selectEmphasis = emphasis;
        }
        else
        {
            _ep.selectEmphasis = null;
        }

        if (toggle.group.allowSwitchOff)
            toggle.group.allowSwitchOff = false;
    }
}

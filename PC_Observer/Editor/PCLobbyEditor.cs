using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SkywardRay;

[CustomEditor(typeof(PCObserverLobbyUI))]
public class PCLobbyEditor : Editor
{
    private InspectorEnumPairGui<PCObserverLobbyUI.PCUIState, UIPage> m_UIPagesGUI;

    private void OnEnable()
    {
        var property = serializedObject.FindProperty("m_UIPages");
        m_UIPagesGUI = new InspectorEnumPairGui<PCObserverLobbyUI.PCUIState, UIPage>(property, (GetDefaultValue) => null);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        m_UIPagesGUI.OnInspectorGUI();
        serializedObject.ApplyModifiedProperties();
    }
}

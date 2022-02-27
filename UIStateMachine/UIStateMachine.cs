using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStateMachine<T> : MonoBehaviour where T : System.Enum
{
    public SerializableDictionary<T, GameObject> m_UIPages;

    T _state;
    public T state
    {
        get => _state;
        set
        {
            PageInit();

            if (!_state.Equals(value))
            {
                if(m_UIPages[_state] != null)
                    m_UIPages[_state].SetActive(false);
                
                _state = value;

                if(m_UIPages[_state] != null)
                    m_UIPages[_state].SetActive(true);
            }
            else if(m_UIPages[_state] != null && !m_UIPages[_state].activeSelf)
            {
                m_UIPages[_state].SetActive(true);
            }
        }
    }

    private bool m_init = false;

    private void Awake()
    {
        PageInit();
    }

    private void PageInit()
    {
        if (!m_init)
        {
            foreach (var page in m_UIPages)
            {
                if(page.Value != null)
                    page.Value.SetActive(false);
            }
            m_init = true;
        }
    }
}

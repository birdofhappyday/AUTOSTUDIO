using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPage : MonoBehaviour
{
    [SerializeField]
    CanvasGroup CanvasGroup;
    [SerializeField]
    float fadeTime = 2;

    public virtual void Init()
    {
        CanvasGroupActiveChage(false);
    }

    // Page 활성화 기초 함수
    public virtual void Active()
    {
        Debug.Log($"{name} Active");
        gameObject.SetActive(true);
    }

    // Page 비활성화 기초 함수
    public virtual void Deactive(UIPage next)
    {
        Debug.Log($"{name} Deactive");
        gameObject.SetActive(false);
        CanvasGroupActiveChage(false);
        if (next != null) next.Active();
    }

    IEnumerator fadeCor;

    // 초기에 반드시 켜져야 할 UI 세팅
    public virtual void UIActive(bool _active = true)
    {
        if (fadeCor != null)
        {
            StopCoroutine(fadeCor);
            fadeCor = null;
        }

        if (_active)
            fadeCor = FadeInCanvasGroup();
        else
            fadeCor = FadeOutCanvasGroup();

        StartCoroutine(fadeCor);
    }

    // 캔버스 그룹으로 알파와 interactable 조작
    void CanvasGroupActiveChage(bool _active)
    {
        if (CanvasGroup != null)
        {
            if(_active)
            {
                CanvasGroup.alpha = 1;
                CanvasGroup.interactable = true;
            }
            else
            {
                CanvasGroup.alpha = 0;
                CanvasGroup.interactable = false;
            }
        }
    }

    IEnumerator FadeInCanvasGroup(System.Action afrterAction = null)
    {
        CanvasGroupActiveChage(false);

        while (CanvasGroup.alpha < 1)
        {
            CanvasGroup.alpha += 1 / fadeTime * Time.deltaTime;
            yield return null;
        }

        fadeCor = null;

        afrterAction?.Invoke();

        CanvasGroupActiveChage(true);
    }

    // 꺼질때 그냥 꺼지기 때문에 사용안됨.
    IEnumerator FadeOutCanvasGroup(System.Action afrterAction = null)
    {
        while (CanvasGroup.alpha > 1)
        {
            CanvasGroup.alpha -= 1 / fadeTime * Time.deltaTime;
            yield return null;
        }

        fadeCor = null;

        afrterAction?.Invoke();

        CanvasGroupActiveChage(false);
    }
}

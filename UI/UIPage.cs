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

    // Page Ȱ��ȭ ���� �Լ�
    public virtual void Active()
    {
        Debug.Log($"{name} Active");
        gameObject.SetActive(true);
    }

    // Page ��Ȱ��ȭ ���� �Լ�
    public virtual void Deactive(UIPage next)
    {
        Debug.Log($"{name} Deactive");
        gameObject.SetActive(false);
        CanvasGroupActiveChage(false);
        if (next != null) next.Active();
    }

    IEnumerator fadeCor;

    // �ʱ⿡ �ݵ�� ������ �� UI ����
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

    // ĵ���� �׷����� ���Ŀ� interactable ����
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

    // ������ �׳� ������ ������ ���ȵ�.
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

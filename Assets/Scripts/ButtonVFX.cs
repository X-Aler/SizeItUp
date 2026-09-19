using UnityEngine;
using UnityEngine.EventSystems;
using PrimeTween;
using YG;
public class ButtonVFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] bool isStart = false;
    public void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isStart) return;
        YG2.InterstitialAdvShow();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonEnterSound);
        Tween.Scale(transform, Vector2.one * 1.2f, 0.5f);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        Tween.Scale(transform, Vector2.one, 0.5f);
    }
}

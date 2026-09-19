using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SizeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private SizeController sizeController;
    [SerializeField] private bool isIncrease;

    private Coroutine coroutine;
    public void OnPointerDown(PointerEventData eventData)
    {

        coroutine = StartCoroutine(AutoSize());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        ChangeSize();
    }

    private IEnumerator AutoSize()
    {
        yield return new WaitForSeconds(0.35f);

        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
            ChangeSize();
        }
    }

    private void ChangeSize()
    {
        if (sizeController == null) return;

        if (isIncrease)
            sizeController.IncreaseSize();
        else sizeController.DecreaseSize();
    }

}

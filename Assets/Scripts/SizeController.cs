using UnityEngine;
using TMPro;
using PrimeTween;
using YG; // Подключаем PrimeTween
using System.Globalization;
public class SizeController : MonoBehaviour
{
    [Header("Ссылки на объекты")]
    [SerializeField] private GameObject redSprite;
    [SerializeField] private GameObject background;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private TextMeshProUGUI sizeText;       // Размер в метрах
    [SerializeField] private TextMeshProUGUI multiplierText; // Множитель (×2.5)

    [Header("Настройки визуала спрайта")]
    [SerializeField] private float compressionPower = 0.33f;
    [SerializeField] private float minVisualScale = 0.15f;
    [SerializeField] private float maxVisualScale = 4.0f;

    [Header("Настройки камеры и фона")]
    [SerializeField] private float baseCameraSize = 15.0f;
    [SerializeField] private float cameraZoomPower = 0.2f;
    [SerializeField] private float minCameraSize = 1f;
    [SerializeField] private float maxCameraSize = 100.0f;
    [SerializeField] private bool useTextureTilingForBackground = false;

    [Header("Настройки анимации")]
    [Tooltip("Длительность плавного отдаления/приближения камеры к ответу")]
    [SerializeField] private float answerZoomDuration = 1.4f;
    [SerializeField] private Ease cameraEase = Ease.OutCubic;

    [Header("Ограничения математического масштаба")]
    [SerializeField] private double minRatio = 1e-12;
    [SerializeField] private double maxRatio = 1e12;

    public double CurrentRatio { get; private set; } = 1.0;
    private double _blueBaseSizeMeters = 1.75;

    private Vector3 _initialBgScale;
    private Material _bgMaterial;

    // Твин для отслеживания и прерывания активной анимации зума
    private Tween _activeZoomTween;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null && targetCamera.orthographic)
            targetCamera.orthographicSize = baseCameraSize;

        if (background != null)
        {
            _initialBgScale = background.transform.localScale;
            var renderer = background.GetComponent<Renderer>();
            if (renderer != null)
            {
                _bgMaterial = renderer.material;
            }
        }
    }

    private void Start()
    {
        UpdateVisualsInstant();
    }

    public void InitRound(double blueMeters)
    {
        // Прерываем зум от предыдущего раунда, если он ещё шёл
        if (_activeZoomTween.isAlive)
        {
            _activeZoomTween.Stop();
        }

        _blueBaseSizeMeters = blueMeters;
        CurrentRatio = 1.0;
        UpdateVisualsInstant();
    }

    public void IncreaseSize(float value = 0.1f)
    {
        CurrentRatio *= (1.0 + value);
        if (CurrentRatio > maxRatio) CurrentRatio = maxRatio;
        UpdateVisualsInstant();
    }

    public void DecreaseSize(float value = 0.1f)
    {
        CurrentRatio /= (1.0 + value);
        if (CurrentRatio < minRatio) CurrentRatio = minRatio;
        UpdateVisualsInstant();
    }

    /// <summary>
    /// Задает эталонный размер AnswerObj, обновляет тексты и плавно анимирует камеру к эталону.
    /// Возвращает твин, чтобы вызывающий код (Sequence) мог дождаться завершения анимации.
    /// </summary>
    public Tween ShowAnswer(GameObject answerObj, double targetRatio, double targetRedMeters)
    {
        // 1. Моментально выставляем силуэт эталона на сцене
        float targetVisualScale = CalculateVisualScale((float)targetRatio);
        if (answerObj != null)
        {
            answerObj.transform.localScale = new Vector3(targetVisualScale, targetVisualScale, 1f);
        }

        // 2. Обновляем текстовые подсказки
        if (sizeText != null)
        {
            sizeText.text = FormatSize(targetRedMeters);
        }

        if (multiplierText != null)
        {
            multiplierText.text = targetRatio >= 1.0
                ? $"Эталон: ×{targetRatio:F1}"
                : $"Эталон: ÷{(1.0 / targetRatio):F1}";
        }

        // 3. Вычисляем целевой ортографический размер камеры под эталон
        float targetCamSize = CalculateTargetCameraSize((float)targetRatio);

        if (targetCamera == null || !targetCamera.orthographic)
        {
            return Tween.Delay(0f);
        }

        if (_activeZoomTween.isAlive)
        {
            _activeZoomTween.Stop();
        }

        float startCamSize = targetCamera.orthographicSize;

        // 4. Плавно интерполируем размер камеры и одновременно масштабируем фон
        _activeZoomTween = Tween.Custom(
            startValue: startCamSize,
            endValue: targetCamSize,
            duration: answerZoomDuration,
            ease: cameraEase,
            onValueChange: currentSize =>
            {
                targetCamera.orthographicSize = currentSize;
                ApplyBackgroundScale(currentSize);
            }
        );

        return _activeZoomTween;
    }

    /// <summary>
    /// Моментальное обновление визуала во время масштабирования игроком (без задержек)
    /// </summary>
    private void UpdateVisualsInstant()
    {
        float visualScale = CalculateVisualScale((float)CurrentRatio);
        if (redSprite != null)
        {
            redSprite.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        }

        float targetCamSize = CalculateTargetCameraSize((float)CurrentRatio);

        if (targetCamera != null && targetCamera.orthographic)
        {
            targetCamera.orthographicSize = targetCamSize;
            ApplyBackgroundScale(targetCamSize);
        }

        double currentMeters = _blueBaseSizeMeters * CurrentRatio;
        if (sizeText != null)
        {
            sizeText.text = FormatSize(currentMeters);
        }

        if (multiplierText != null)
        {
            multiplierText.text = CurrentRatio >= 1.0
                ? $"×{CurrentRatio:F1}"
                : $"÷{(1.0 / CurrentRatio):F1}";
        }
    }

    private float CalculateVisualScale(float ratio)
    {
        float visualScale;
        if (ratio >= 1.0f)
            visualScale = Mathf.Pow(ratio, compressionPower);
        else
            visualScale = 1f / Mathf.Pow(1.0f / ratio, compressionPower);

        return Mathf.Clamp(visualScale, minVisualScale, maxVisualScale);
    }

    private float CalculateTargetCameraSize(float ratio)
    {
        float cameraFactor;
        if (ratio >= 1.0f)
            cameraFactor = Mathf.Pow(ratio, cameraZoomPower);
        else
            cameraFactor = 1f / Mathf.Pow(1.0f / ratio, cameraZoomPower);

        return Mathf.Clamp(baseCameraSize * cameraFactor, minCameraSize, maxCameraSize);
    }

    private void ApplyBackgroundScale(float currentCameraSize)
    {
        if (background == null) return;

        float bgScaleMultiplier = currentCameraSize / baseCameraSize;

        if (useTextureTilingForBackground && _bgMaterial != null)
        {
            background.transform.localScale = _initialBgScale * bgScaleMultiplier;
            _bgMaterial.mainTextureScale = Vector2.one * bgScaleMultiplier;
        }
        else
        {
            background.transform.localScale = _initialBgScale * bgScaleMultiplier;
        }
    }

    private string FormatSize(double meters)
    {
        // Определяем суффиксы в зависимости от выбранного языка
        bool isEn = YG2.lang == "en";

        string mlnKm = isEn ? "M km" : "млн км";
        string thsdKm = isEn ? "k km" : "тыс. км";
        string km = isEn ? "km" : "км";
        string m = isEn ? "m" : "м";
        string cm = isEn ? "cm" : "см";
        string mm = isEn ? "mm" : "мм";
        string um = isEn ? "µm" : "мкм";

        // Используем InvariantCulture, чтобы разделителем всегда была точка, а не системная запятая
        var culture = CultureInfo.InvariantCulture;

        if (meters >= 1e9)
            return $"{(meters / 1e9).ToString("F1", culture)} {mlnKm}";
        if (meters >= 1e6)
            return $"{(meters / 1e6).ToString("F1", culture)} {thsdKm}";
        if (meters >= 1000)
            return $"{(meters / 1000).ToString("F2", culture)} {km}";
        if (meters >= 1.0)
            return $"{meters.ToString("F2", culture)} {m}";
        if (meters >= 0.01)
            return $"{(meters * 100).ToString("F1", culture)} {cm}";
        if (meters >= 1e-3)
            return $"{(meters * 1000).ToString("F1", culture)} {mm}";

        return $"{(meters * 1e6).ToString("F1", culture)} {um}";
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;
using PrimeTween; // Подключаем PrimeTween

public class GameController : MonoBehaviour
{
    [Header("Скрипты")]
    [SerializeField] private SizeController sizeController;
    [SerializeField] private ObjectDatabase database;

    [Header("Панели UI")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject gameHudPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Тексты UI")]
    [SerializeField] private TextMeshProUGUI blueNameText;
    [SerializeField] private TextMeshProUGUI redNameText;
    [SerializeField] private TextMeshProUGUI totalScoreText;

    [Header("Игровые объекты")]
    [SerializeField] private GameObject blueObj;
    [SerializeField] private GameObject redObj;
    [SerializeField] private GameObject answerObj;

    [Header("Настройки анимации PrimeTween")]
    [Tooltip("Длительность отдаления камеры")]
    [SerializeField] private float cameraZoomDuration = 1.4f;

    [Tooltip("Длительность набора процентов")]
    [SerializeField] private float scoreCountDuration = 1.2f;

    [Tooltip("Минимальный интервал между звуками тика (сек)")]
    [SerializeField] private float soundTickInterval = 0.05f;

    private SpriteRenderer _blueRenderer;
    private SpriteRenderer _redRenderer;
    private SpriteRenderer _answerRenderer;

    private double _currentBlueMeters;
    private double _targetRedMeters;

    // Ссылка на активную последовательность для безопасной отмены
    private Sequence _answerSequence;
    private float _lastSoundTime;

    private void Awake()
    {
        _blueRenderer = blueObj.GetComponent<SpriteRenderer>();
        _redRenderer = redObj.GetComponent<SpriteRenderer>();
        _answerRenderer = answerObj.GetComponent<SpriteRenderer>();

        if (database == null) database = GetComponent<ObjectDatabase>();
    }

    private void Start()
    {
        ShowStartMenu();
    }

    public void ShowStartMenu()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameHudPanel != null) gameHudPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        blueObj.SetActive(false);
        redObj.SetActive(false);
        answerObj.SetActive(false);
    }

    public void OnStartButtonClicked()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        StartNewRound();
    }

    public void StartNewRound()
    {
        AudioManager.Instance.StopLoopingSound(AudioManager.Instance.percentSound);

        if (_answerSequence.isAlive)
        {
            _answerSequence.Stop();
        }

        if (!database.IsLoaded)
        {
            database.LoadDatabase();
        }

        var (blueItem, redItem) = database.GetRandomPair();
        if (blueItem == null || redItem == null) return;

        Sprite blueSprite = database.LoadSprite(blueItem.id);
        Sprite redSprite = database.LoadSprite(redItem.id);

        _currentBlueMeters = blueItem.size_meters;
        _targetRedMeters = redItem.size_meters;

        if (_blueRenderer != null) _blueRenderer.sprite = blueSprite;
        if (_redRenderer != null) _redRenderer.sprite = redSprite;

        if (blueNameText != null) blueNameText.text = YG2.lang == "ru" ? blueItem.name_ru : blueItem.name_en;
        if (redNameText != null) redNameText.text = YG2.lang == "ru" ? redItem.name_ru : redItem.name_en;

        if (gameHudPanel != null) gameHudPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        blueObj.SetActive(true);
        redObj.SetActive(true);
        answerObj.SetActive(false);

        sizeController.InitRound(_currentBlueMeters);
    }

    public void OnAnswerButtonClicked()
    {
        if (gameHudPanel != null) gameHudPanel.SetActive(false);

        if (_answerRenderer != null && _redRenderer != null)
        {
            _answerRenderer.sprite = _redRenderer.sprite;
        }
        answerObj.SetActive(true);

        double targetRatio = _targetRedMeters / _currentBlueMeters;
        float finalAccuracy = CalculateAccuracyPercent(sizeController.CurrentRatio, targetRatio);

        UpdateTotalScoreUI(0f);
        _lastSoundTime = 0f;

        // Запуск твина камеры и подключение всей остальной цепочки
        _answerSequence = Sequence.Create()
            // 1. Плавно отдаляем камеру до эталонного размера
            .Chain(sizeController.ShowAnswer(answerObj, targetRatio, _targetRedMeters))

            // 2. Открываем окно результатов сразу после окончания отъезда камеры
            .ChainCallback(() =>
            {
                if (finalAccuracy > 0)
                {
                    AudioManager.Instance.PlayLoopingSound(AudioManager.Instance.percentSound, 0.5f);
                }
                if (resultPanel != null) resultPanel.SetActive(true);
            
            })

            // 3. Анимируем проценты с тиками звука
            .Chain(Tween.Custom(
                startValue: 0f,
                endValue: finalAccuracy,
                duration: scoreCountDuration,
                ease: Ease.OutQuad,
                onValueChange: val =>
                {
                    UpdateTotalScoreUI(val);

                }
            ))

            // 4. Финальный звук и точная фиксация
            .ChainCallback(() =>
            {
                UpdateTotalScoreUI(finalAccuracy);
                AudioManager.Instance.StopLoopingSound(AudioManager.Instance.percentSound);
            });
    }

    public void OnNextRoundButtonClicked()
    {
        StartNewRound();
    }

    private float CalculateAccuracyPercent(double playerRatio, double targetRatio)
    {
        float logTarget = Mathf.Log10((float)targetRatio);
        float logPlayer = Mathf.Log10((float)playerRatio);
        float deltaOrders = Mathf.Abs(logPlayer - logTarget);

        float tolerance = 1.0f;
        float accuracy = Mathf.Clamp01(1f - (deltaOrders / tolerance));

        return accuracy * 100f;
    }

    private void UpdateTotalScoreUI(float accuracity)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"{accuracity:F1}%";
        }
    }
}
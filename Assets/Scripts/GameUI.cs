using System.Collections;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("Панели")]
    public GameObject menuPanel;
    public GameObject hudPanel;
    public GameObject resultPanel;
    public CanvasGroup resultGroup;

    [Header("Меню")]
    public TMP_Text menuBestText;

    [Header("HUD")]
    public TMP_Text timeText;
    public TMP_Text scoreText;
    public TMP_Text bestText;
    public Color normalTimeColor = Color.white;
    public Color lowTimeColor = new Color(1f, 0.35f, 0.3f);
    public Color bonusTimeColor = new Color(0.45f, 1f, 0.5f);
    public float lowTimeThreshold = 10f;

    [Header("Экран итогов")]
    public TMP_Text resultTitleText;
    public TMP_Text resultScoreText;
    public TMP_Text recordText;
    public TMP_Text statsText;
    public TMP_Text endingTitleText;
    public TMP_Text endingBodyText;
    public Color newRecordColor = new Color(1f, 0.85f, 0.2f);
    public Color oldRecordColor = new Color(0.8f, 0.8f, 0.8f);

    [Header("Концовки")]
    public string badTitle = "Скудная зима";
    [TextArea(3, 6)]
    public string badText = "Нильс принёс домой почти пустую корзину. Тролль Грум из жалости сложил хлипкий мостик из пары брёвен, и гном добрался домой, но суп зимой получился очень жидким. Нильс решил, что в следующем году будет бегать быстрее.";

    public string mediumTitle = "Полный погреб";
    [TextArea(3, 6)]
    public string mediumText = "Корзина получилась увесистой. Грум в благодарность выстроил прочный каменный мост, а Нильс насушил грибов на всю зиму. Погреб полон, суп густой, и даже белки остались довольны.";

    public string goodTitle = "Зимний праздник";
    [TextArea(3, 6)]
    public string goodText = "Корзина ломилась от грибов! Грум не только поднял новый мост, но и пришёл на хутор на зимний праздник с огромной кастрюлей. Гном варил суп, тролль пел, и вся долина запомнила эту зиму как самую вкусную.";

    GameManager gm;
    GameState? lastState;
    float prevTime;
    float flashUntil;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("GameUI: на сцене нет объекта с GameManager", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (gm.State != lastState) ApplyState();
        if (gm.State == GameState.Playing) UpdateHud();
    }

    void ApplyState()
    {
        lastState = gm.State;

        menuPanel.SetActive(gm.State == GameState.Menu);
        hudPanel.SetActive(gm.State == GameState.Playing);
        resultPanel.SetActive(gm.State == GameState.Ended);

        switch (gm.State)
        {
            case GameState.Menu:
                menuBestText.text = "Рекорд: " + gm.BestScore;
                break;

            case GameState.Playing:
                prevTime = gm.TimeLeft;
                flashUntil = 0f;
                UpdateHud();
                break;

            case GameState.Ended:
                ShowResult();
                StartCoroutine(Fade());
                break;
        }
    }

    void UpdateHud()
    {
        float t = gm.TimeLeft;

        // Время прибавилось (голубой гриб): короткая зелёная вспышка
        if (t > prevTime + 0.5f) flashUntil = Time.unscaledTime + 0.6f;
        prevTime = t;

        timeText.text = "Время: " + Mathf.CeilToInt(t);
        timeText.color = Time.unscaledTime < flashUntil ? bonusTimeColor
                       : t <= lowTimeThreshold ? lowTimeColor
                       : normalTimeColor;

        scoreText.text = "Очки: " + gm.Score;
        bestText.text = "Рекорд: " + Mathf.Max(gm.BestScore, gm.Score);
    }

    void ShowResult()
    {
        resultTitleText.text = "Время вышло!";
        resultScoreText.text = "Очки: " + gm.Score;

        recordText.text = gm.IsNewRecord ? "Новый рекорд!" : "Рекорд: " + gm.BestScore;
        recordText.color = gm.IsNewRecord ? newRecordColor : oldRecordColor;

        statsText.text = "Собрано грибов: " + gm.MushroomsCollected +
                         "   (жёлтых: " + gm.PointMushrooms +
                         ", голубых: " + gm.TimeMushrooms + ")";

        switch (gm.Ending)
        {
            case GameEnding.Good:
                endingTitleText.text = goodTitle;
                endingBodyText.text = goodText;
                break;
            case GameEnding.Medium:
                endingTitleText.text = mediumTitle;
                endingBodyText.text = mediumText;
                break;
            default:
                endingTitleText.text = badTitle;
                endingBodyText.text = badText;
                break;
        }
    }

    // Время к этому моменту остановлено, поэтому unscaledDeltaTime
    IEnumerator Fade()
    {
        resultGroup.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.5f;
            resultGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
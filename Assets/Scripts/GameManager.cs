using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Playing, Ended }
public enum GameEnding { Bad, Medium, Good }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Время")]
    public float startTime = 60f;
    public float maxTime = 90f;          

    [Header("Запуск")]
    public bool autoStart = true;         

    [Header("Концовки (по числу собранных грибов)")]
    public int mediumEndingFrom = 12;
    public int goodEndingFrom = 25;

    [Header("Отладка")]
    public bool showDebugOverlay = true;  

    public const string RecordKey = "BestScore";

    public GameState State { get; private set; } = GameState.Menu;
    public float TimeLeft { get; private set; }
    public int Score { get; private set; }
    public int MushroomsCollected { get; private set; }
    public int PointMushrooms { get; private set; }
    public int TimeMushrooms { get; private set; }
    public int BestScore { get; private set; }
    public bool IsNewRecord { get; private set; }
    public GameEnding Ending { get; private set; }

    public event Action GameStarted;
    public event Action GameEnded;

    static bool skipMenuNextLoad;

    GameState? appliedCursorState;

    void Awake()
    {
        Instance = this;
        BestScore = PlayerPrefs.GetInt(RecordKey, 0);
        TimeLeft = startTime;
    }

    void OnEnable()
    {
        Collectible.Collected += OnCollected;
    }

    void OnDisable()
    {
        Collectible.Collected -= OnCollected;
    }

    void Start()
    {
        if (autoStart || skipMenuNextLoad)
        {
            skipMenuNextLoad = false;
            StartGame();
        }
        else
        {
            State = GameState.Menu;
            Time.timeScale = 0f;
        }
    }

    void Update()
    {
        if (State != GameState.Playing) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f) EndGame();
    }

    void LateUpdate()
    {
        if (appliedCursorState == State) return;
        appliedCursorState = State;

        bool playing = State == GameState.Playing;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
    }

    //  Управление игрой 

    public void StartGame()
    {
        Score = 0;
        MushroomsCollected = 0;
        PointMushrooms = 0;
        TimeMushrooms = 0;
        IsNewRecord = false;
        TimeLeft = startTime;

        State = GameState.Playing;
        Time.timeScale = 1f;
        GameStarted?.Invoke();
    }

    void EndGame()
    {
        TimeLeft = 0f;
        State = GameState.Ended;
        Time.timeScale = 0f;

        // Рекорд
        IsNewRecord = Score > BestScore;
        if (IsNewRecord)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(RecordKey, BestScore);
            PlayerPrefs.Save();
        }

        // Концовка
        if (MushroomsCollected >= goodEndingFrom) Ending = GameEnding.Good;
        else if (MushroomsCollected >= mediumEndingFrom) Ending = GameEnding.Medium;
        else Ending = GameEnding.Bad;

        Debug.Log("=== ИГРА ОКОНЧЕНА ===\n" +
                  "Очки: " + Score + " (рекорд " + BestScore + (IsNewRecord ? ", новый!" : "") + ")\n" +
                  "Грибов: " + MushroomsCollected + " (жёлтых " + PointMushrooms +
                  ", голубых " + TimeMushrooms + ")\n" +
                  "Концовка: " + Ending);

        GameEnded?.Invoke();
    }

    // Кнопка «Играть снова»: перезагружаем сцену и сразу стартуем
    public void Restart()
    {
        skipMenuNextLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Кнопка «В меню»: перезагружаем сцену, игра ждёт кнопки Start
    public void BackToMenu()
    {
        skipMenuNextLoad = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //  Подбор грибов 

    void OnCollected(Collectible c)
    {
        if (State != GameState.Playing) return;

        MushroomsCollected++;

        if (c.type == CollectibleType.Points)
        {
            Score += c.pointsValue;
            PointMushrooms++;
        }
        else
        {
            TimeLeft = Mathf.Min(maxTime, TimeLeft + c.timeBonus);
            TimeMushrooms++;
        }
    }

    //  Отладочная надпись 

    void OnGUI()
    {
        if (!showDebugOverlay) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;

        string text = "Time: " + Mathf.CeilToInt(TimeLeft) +
                      "    Score: " + Score +
                      "    Mushrooms: " + MushroomsCollected +
                      "    Best: " + BestScore;
        if (State == GameState.Ended)
            text += "\nGAME OVER: " + Ending + (IsNewRecord ? "  (NEW RECORD!)" : "");

        GUI.Label(new Rect(15, 10, 900, 80), text, style);
    }

    //  Тестовые кнопки 

    [ContextMenu("Test: +50 очков")]
    void TestAddScore() { Score += 50; }

    [ContextMenu("Test: +10 секунд")]
    void TestAddTime() { TimeLeft = Mathf.Min(maxTime, TimeLeft + 10f); }

    [ContextMenu("Test: закончить игру сейчас")]
    void TestEnd()
    {
        if (State == GameState.Playing) EndGame();
    }

    [ContextMenu("Test: сбросить рекорд")]
    void TestResetRecord()
    {
        PlayerPrefs.DeleteKey(RecordKey);
        BestScore = 0;
        Debug.Log("Рекорд сброшен");
    }
}
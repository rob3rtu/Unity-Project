using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _timerText;

    [SerializeField]
    private float _remainingTime;

    [SerializeField]
    private GameOver _gameOver;

    [SerializeField]
    private ScoreCounterSO _scoreCounter;

    private float _second;

    private void Awake()
    {
        _remainingTime = 10;
    }
    void Update()
    {
        if (_remainingTime > 0)
        {
            _remainingTime -= Time.deltaTime;
            _second += Time.deltaTime;
        }
        else if (_remainingTime < 0)
        {
            _remainingTime = 0;
            _timerText.color = Color.red;

            _scoreCounter.score.gameOver = "Timeout";
            _scoreCounter.SaveScore();
            _gameOver.ShowGameOverTime();
            
        }

        if(_second > 1) 
        {
            _scoreCounter.score.time += 1;
            _second -= 1;
        }

        int minutes = Mathf.FloorToInt(_remainingTime / 60);
        int seconds = Mathf.FloorToInt(_remainingTime % 60);

        _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void AddTime(float seconds)
    {
        _remainingTime += seconds;
    }
}

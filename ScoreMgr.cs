using System.Collections.Generic;
using UnityEngine;

public class ScoreMgr : MonoBehaviour
{
    public MainMgr mainMgr;
    public GameObject scoreDisplaysParent;
    public List<ScoreDisplayMgr> scoreDisplays;
    float intervalStaggerScore = .1f;
    int sumScore;
    public List<int> scores = new();
    int nTest;
    bool ynActive;

    void Start()
    {
        ResetScoreDisplays();
        //        InvokeRepeating(nameof(TestScores), 4, 4);
    }

    void TestScores()
    {
        ClearScores();
        nTest++;
        if (nTest < 1 || nTest > 3) nTest = 1;
        int numThrows = nTest;
        Debug.Log("numThrows:" + numThrows + "\n");
        for (int n = 0; n < numThrows; n++)
        {
            int s = Random.Range(-1, 2);
            AddScore(s);
        }
        ShowScoreDisplays();
    }

    public void SetActive(bool yn)
    {
        ynActive = yn;
    }

    public void ClearScores()
    {
        scores.Clear();
    }

    public void AddScore(int s)
    {
        if (!ynActive) return;
        scores.Add(s);
    }

    public void ShowScoreDisplays()
    {
        if (!ynActive) return;
        ResetScoreDisplays();
        for (int n = 0; n < scores.Count; n++)
        {
            ScoreDisplayMgr scoreDisplay = scoreDisplays[n];
            int s = scores[n];
            Color color = GetColorForScore(s);
            scoreDisplay.SetColor(color);
            float delayStart = n * intervalStaggerScore;
            scoreDisplay.StartScaleUpDelayed(delayStart);
        }
        float delay = scores.Count * intervalStaggerScore;
        Invoke(nameof(FinalScoreAndFinalScoreAudio), delay);
    }

    void FinalScoreAndFinalScoreAudio()
    {
        SumScores();
        FinalScoreAndAudio();
    }
    void FinalScoreAndAudio()
    {
        switch (scores.Count)
        {
            case 2:
                if (Mathf.Abs(sumScore) == 2)
                {
                    if (sumScore == 2) mainMgr.audioMatMgr.PlayYeah();
                    if (sumScore == -2) mainMgr.audioMatMgr.PlayBoo();
                    ClearScores();
                }
                break;
            case 3:
                if (sumScore > 0) mainMgr.audioMatMgr.PlayYeah();
                if (sumScore == 0) mainMgr.audioMatMgr.PlayBoing();
                if (sumScore < 0) mainMgr.audioMatMgr.PlayBoo();
                ClearScores();
                break;
        }
    }

    public void ResetScoreDisplays()
    {
        foreach (ScoreDisplayMgr scoreDisplay in scoreDisplays)
        {
            scoreDisplay.ResetScoreDisplay();
        }
        CancelInvoke(nameof(FinalScoreAndFinalScoreAudio));
    }

    void SumScores()
    {
        sumScore = 0;
        foreach (int score in scores)
        {
            sumScore += score;
        }
    }

    public Color GetColorForScore(int s)
    {
        Color color = Color.magenta;
        if (s == -1) color = Color.red;
        if (s == 0) color = Color.blue;
        if (s == 1) color = Color.green;
        return color;
    }
}

using UnityEngine;

public class ScoreDisplayMgr : MonoBehaviour
{
    float scaleStart = .1f;
    float scaleStop = .75f;
    float fraction;
    float duration = .1f;
    float timeStart;
    bool ynActive;

    void Update()
    {
        UpdateScaleUp();
    }

    void UpdateScaleUp()
    {
        if (!ynActive) return;
        float elapsed = Time.realtimeSinceStartup - timeStart;
        fraction = elapsed / duration;
        if (fraction > .75f)
        {
            fraction = 1;
            if (ynActive)
            {
                ynActive = false;
            }
        }
        SetScaleFromFraction();
    }

    public void StartScaleUpDelayed(float delayStart)
    {
        ResetScoreDisplay();
        Invoke(nameof(StartScaleUp), delayStart);
    }

    public void StartScaleUp()
    {
        ResetScoreDisplay();
        ynActive = true;
        timeStart = Time.realtimeSinceStartup;
    }

    public void ResetScoreDisplay()
    {
        ynActive = false;
        fraction = 0;
        SetScaleFromFraction();
    }

    void SetScaleFromFraction()
    {
        float range = scaleStop - scaleStart;
        float sca = scaleStart + fraction * range;
        transform.localScale = Vector3.one * sca;
    }

    public void SetColor(Color color)
    {
        GetComponentInChildren<MeshRenderer>().material.color = color;
    }
}

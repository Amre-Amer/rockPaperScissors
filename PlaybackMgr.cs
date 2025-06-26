using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackMgr : MonoBehaviour
{
    public MainMgr mainMgr;
    float morphFraction;
    float durationMorph;
    float morphStartTime;
    float delayScore;
    bool ynPlayback;
    int frameCurrent;
    List<List<float>> dataMorphSource = new();
    List<List<float>> dataMorphTarget = new();
    bool ynMorph;
    float timePlaybackStart;
    int nMorphSource;
    int nMorphTarget;
    bool ynActive;

    private void Awake()
    {
        ynActive = false;
        delayScore = 3.25f;
        durationMorph = .5f;
    }

    void Update()
    {
        if (!ynActive) return;
        UpdatePlayback();
        UpdateMorph();
    }

    public void SetActive(bool yn)
    {
        ynActive = yn;
    }

    public void CancelInvokes()
    {
        CancelInvoke(nameof(Morph));
        CancelInvoke(nameof(PlayGame));
        CancelInvoke(nameof(ScoreAndShow));
    }

    public void PlayGame()
    {
        CancelInvokes();
        if (dataMorphTarget.Count == 0)
        {
            mainMgr.nHandRoll = GetRandomNHandRoll();
            dataMorphSource = GetDataHandsForNHandRoll(mainMgr.nHandRoll);
        }
        else
        {
            mainMgr.nHandRoll = mainMgr.nHandRollTarget;
            dataMorphSource = dataMorphTarget;
        }
        mainMgr.nHandRollTarget = GetRandomNHandRoll();
        dataMorphTarget = GetDataHandsForNHandRoll(mainMgr.nHandRollTarget);
        float duration = GetDuration(dataMorphSource);
        PlaybackOnce(dataMorphSource);
        Invoke(nameof(ScoreAndShow), delayScore);
        Invoke(nameof(Morph), duration);
        Invoke(nameof(PlayGame), duration + durationMorph);
    }

    int GetRandomNHandRoll()
    {
        int n = Random.Range(0, 3);
        return n;
    }

    List<List<float>> GetDataHandsForNHandRoll(int n)
    {
        switch (n)
        {
            case 0:
                return mainMgr.dataHandsRock;
            case 1:
                return mainMgr.dataHandsPaper;
            case 2:
                return mainMgr.dataHandsScissors;
            default:
                return null;
        }
    }

    void ScoreAndShow()
    {
        if (!ynActive) return;
        mainMgr.audioMatMgr.PlayPing();
        ScoreThrow();
        mainMgr.scoreMgr.ShowScoreDisplays();
    }

    void ScoreThrow()
    {
        int s = 0;
        if (mainMgr.nInfer == mainMgr.nHandRoll)
        {
            s = 0;
        }
        else
        {
            switch (mainMgr.nInfer)
            {
                case 0:
                    if (mainMgr.nHandRoll == 1) s = -1;
                    if (mainMgr.nHandRoll == 2) s = 1;
                    break;
                case 1:
                    if (mainMgr.nHandRoll == 2) s = -1;
                    if (mainMgr.nHandRoll == 0) s = 1;
                    break;
                case 2:
                    if (mainMgr.nHandRoll == 0) s = -1;
                    if (mainMgr.nHandRoll == 1) s = 1;
                    break;
            }
        }
        mainMgr.scoreMgr.AddScore(s);
    }

    void Morph()
    {
        ynMorph = true;
        morphStartTime = Time.realtimeSinceStartup;
    }

    void PlaybackOnce(List<List<float>> data)
    {
        ynPlayback = true;
        mainMgr.dataHands = data;
        timePlaybackStart = Time.realtimeSinceStartup;
    }

    public float GetDuration(List<List<float>> data)
    {
        if (data.Count == 0) return 0;
        float duration = data[data.Count - 1][0];
        return duration;
    }

    void UpdatePlayback()
    {
        if (!ynPlayback) return;
        bool ynFound = false;
        float timeTarget = Time.realtimeSinceStartup - timePlaybackStart;
        for (int nLine = 0; nLine < mainMgr.dataHands.Count; nLine++)
        {
            List<float> dataLine = mainMgr.dataHands[nLine];
            float elapsed = dataLine[0];
            if (timeTarget < elapsed)
            {
                DataToHands(mainMgr.dataHands, nLine);
                ynFound = true;
                break;
            }
        }
        if (!ynFound)
        {
            timePlaybackStart = Time.realtimeSinceStartup;
        }
    }

    void UpdateMorph()
    {
        if (!ynMorph) return;
        if (dataMorphSource.Count == 0 || dataMorphTarget.Count == 0) return;
        nMorphSource = dataMorphSource.Count - 1;
        nMorphTarget = 0;
        float elapsed = Time.realtimeSinceStartup - morphStartTime;
        for (int h = 0; h < 2; h++)
        {
            HandVisual hand = null;
            if (h == 0) hand = mainMgr.handLeft;
            if (h == 1) hand = mainMgr.handRight;
            if (elapsed >= durationMorph)
            {
                ynMorph = false;
                return;
            }
            morphFraction = elapsed / durationMorph;
            int count = hand.Joints.Count;
            for (int n = 0; n < count + 1; n++)
            {
                float posX = dataMorphSource[nMorphSource][1 + (h * 162) + n * 6 + 0];
                float posY = dataMorphSource[nMorphSource][1 + (h * 162) + n * 6 + 1];
                float posZ = dataMorphSource[nMorphSource][1 + (h * 162) + n * 6 + 2];
                float eulX = dataMorphSource[nMorphSource][1 + (h * 162) + n * 6 + 3];
                float eulY = dataMorphSource[nMorphSource][1 + (h * 162) + n * 6 + 4];
                float eulZ = dataMorphSource[nMorphSource][1 + (h * 162) + n * 6 + 5];
                Vector3 pos = new(posX, posY, posZ);
                Vector3 sourcePos = pos;
                Vector3 eul = new(eulX, eulY, eulZ);
                Quaternion sourceRotation = Quaternion.Euler(eul);
                posX = dataMorphTarget[nMorphTarget][1 + (h * 162) + n * 6 + 0];
                posY = dataMorphTarget[nMorphTarget][1 + (h * 162) + n * 6 + 1];
                posZ = dataMorphTarget[nMorphTarget][1 + (h * 162) + n * 6 + 2];
                eulX = dataMorphTarget[nMorphTarget][1 + (h * 162) + n * 6 + 3];
                eulY = dataMorphTarget[nMorphTarget][1 + (h * 162) + n * 6 + 4];
                eulZ = dataMorphTarget[nMorphTarget][1 + (h * 162) + n * 6 + 5];
                pos = new(posX, posY, posZ);
                Vector3 targetPos = pos;
                eul = new(eulX, eulY, eulZ);
                Quaternion targetRotation = Quaternion.Euler(eul);
                Quaternion interpolatedRotation = Quaternion.Slerp(sourceRotation, targetRotation, morphFraction);
                Vector3 interpolatedPosition = Vector3.Slerp(sourcePos, targetPos, morphFraction);
                if (n < count)
                {
                    hand.Joints[n].transform.localPosition = interpolatedPosition;
                    hand.Joints[n].transform.localRotation = interpolatedRotation;
                }
                else
                {
                    Data2HandPlacement(hand.gameObject, interpolatedPosition, interpolatedRotation, mainMgr.hands);
                }
            }
        }
    }

    void Data2HandPlacement(GameObject go, Vector3 posL, Quaternion rotL, GameObject parent)
    {
        GameObject parentOrig = go.transform.parent.gameObject;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = posL;
        go.transform.localRotation = rotL;
        go.transform.SetParent(parentOrig.transform);
    }

    public void DataToHands(List<List<float>> data, int nRecord)
    {
        frameCurrent = nRecord;
        List<float> dataRecord = data[nRecord];
        HandVisual hand = null;
        for (int h = 0; h < 2; h++)
        {
            if (h == 0) hand = mainMgr.handLeft;
            if (h == 1) hand = mainMgr.handRight;
            int countJoints = hand.Joints.Count;
            for (int n = 0; n < countJoints; n++)
            {
                float xPosL = dataRecord[1 + (h * 162) + n * 6 + 0];
                float yPosL = dataRecord[1 + (h * 162) + n * 6 + 1];
                float zPosL = dataRecord[1 + (h * 162) + n * 6 + 2];
                Vector3 posL = new(xPosL, yPosL, zPosL);
                hand.Joints[n].transform.localPosition = posL;
                float xEulL = dataRecord[1 + (h * 162) + n * 6 + 3];
                float yEulL = dataRecord[1 + (h * 162) + n * 6 + 4];
                float zEulL = dataRecord[1 + (h * 162) + n * 6 + 5];
                Vector3 eulL = new(xEulL, yEulL, zEulL);
                hand.Joints[n].transform.localEulerAngles = eulL;
            }
            float xPos = dataRecord[1 + (h * 162) + countJoints * 6 + 0];
            float yPos = dataRecord[1 + (h * 162) + countJoints * 6 + 1];
            float zPos = dataRecord[1 + (h * 162) + countJoints * 6 + 2];
            Vector3 wristPosL = new(xPos, yPos, zPos);
            float xEul = dataRecord[1 + (h * 162) + countJoints * 6 + 3];
            float yEul = dataRecord[1 + (h * 162) + countJoints * 6 + 4];
            float zEul = dataRecord[1 + (h * 162) + countJoints * 6 + 5];
            Vector3 wristEulL = new(xEul, yEul, zEul);
            Quaternion wristRotL = Quaternion.Euler(wristEulL);
            Data2HandPlacement(hand.gameObject, wristPosL, wristRotL, mainMgr.hands);
        }
    }
}

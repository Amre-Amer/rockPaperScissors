using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

public class ScreensaverMgr : MonoBehaviour
{
    public MainMgr mainMgr;
    public HandVisual handLeft;
    public HandVisual handRight;
    public TextAsset textAsset;
    List<List<float>> dataHands = new();
    float timePlaybackStart;
    bool ynPlayback;
    public GameObject anchor;
    float duration;
    float durationPlayback;

    private void Awake()
    {
        TextAssetToData(textAsset, dataHands);
        Stop();
    }

    void Start()
    {
        SetDuration();
    }

    void Update()
    {
        UpdatePlayback();
    }

    void Stop()
    {
        ynPlayback = false;
        ShowHideHands(false);
    }

    void ShowHideHands(bool yn)
    {
        handLeft.gameObject.SetActive(yn);
        handRight.gameObject.SetActive(yn);
    }

    public void PlayOnce()
    {
        ynPlayback = true;
        ShowHideHands(true);
        PlaybackOnce();
    }

    void PlaybackOnce()
    {
        ynPlayback = true;
        timePlaybackStart = Time.realtimeSinceStartup;
    }

    void SetDuration()
    {
        if (dataHands.Count == 0) return;
        durationPlayback = dataHands[dataHands.Count - 1][0];
        duration = durationPlayback; 
    }

    public float GetDuration()
    {
        return duration;
    }

    void UpdatePlayback()
    {
        if (!ynPlayback) return;
        bool ynFound = false;
        float timeTarget = Time.realtimeSinceStartup - timePlaybackStart;
        for (int nLine = 0; nLine < dataHands.Count; nLine++)
        {
            List<float> dataLine = dataHands[nLine];
            float elapsed = dataLine[0];
            if (timeTarget < elapsed)
            {
                DataToHands(dataHands, nLine);
                ynFound = true;
                break;
            }
        }
        if (!ynFound)
        {
            Stop();
        }
    }

    void TextAssetToData(TextAsset textAsset, List<List<float>> data)
    {
        string txtAll = textAsset.text;
        StringToDataHands(txtAll, data);
    }

    public float NormalizeAngle(float vv)
    {
        float v = vv;
        if (v < -180) v += 360;
        if (v > 180) v -= 360;
        return v;
    }

    void StringToDataHands(string txtAll, List<List<float>> data)
    {
        data.Clear();
        string[] txtLines = txtAll.Split('\n');
        int nStart = 0;
        if (txtLines.Length > 0 && IsHeader(txtLines[0]))
        {
            nStart = 1;
        }
        for (int n = nStart; n < txtLines.Length; n++)
        {
            string[] txtRecord = txtLines[n].Split(',');
            List<float> dataRecord = new();
            for (int nn = 0; nn < txtRecord.Length; nn++)
            {
                float v = float.Parse(txtRecord[nn]);
                dataRecord.Add(v);
            }
            data.Add(dataRecord);
        }
    }

    public bool IsNumeric(string txt)
    {
        bool ynIsNumeric = false;
        if (float.TryParse(txt, out _))
        {
            ynIsNumeric = true;
        }
        return ynIsNumeric;
    }

    bool IsHeader(string txtLine)
    {
        string txtFirst = txtLine.Split(',')[0];
        bool ynIsNumeric = IsNumeric(txtFirst);
        return !ynIsNumeric;
    }

    public void DataToHands(List<List<float>> data, int nRecord)
    {
        List<float> dataRecord = data[nRecord];
        HandVisual hand = null;
        for (int h = 0; h < 2; h++)
        {
            if (h == 0) hand = handLeft;
            if (h == 1) hand = handRight;
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
            GameObject anchor0 = anchor;
            if (!anchor0) anchor0 = gameObject;
            Data2HandPlacement(hand.gameObject, wristPosL, wristRotL, anchor0);
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
}

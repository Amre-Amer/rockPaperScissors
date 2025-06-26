using Oculus.Interaction;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecordMgr : MonoBehaviour
{
    public MainMgr mainMgr;
    [HideInInspector]
    public bool ynRecord;
    bool ynRecordStart;
    float timeRecordStart;
    int durationRecord;
    int nCountDown;
    float morphFraction;
    float durationMorph;
    float morphStartTime;
    float delayScore;
    bool ynMorph;

    private void Awake()
    {
        durationRecord = 5;
        mainMgr.textCountdown.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateRecord();
    }

    public void CancelInvokes()
    {
        CancelInvoke(nameof(RecordStart));
    }

    public void RecordPressed()
    {
        ynRecord = !ynRecord;
        if (ynRecord)
        {
            mainMgr.audioMatMgr.PlayPing();
            CancelInvoke(nameof(RecordStopAndSave));
            CancelInvoke(nameof(CountDown));
            ynRecordStart = false;
            mainMgr.textCountdown.text = "";
            mainMgr.textCountdown.gameObject.SetActive(true);
            Invoke(nameof(RecordStart), 1);
        }
        else
        {
            RecordStop();
        }
    }

    void RecordStart()
    {
        nCountDown = durationRecord;
        InvokeRepeating(nameof(CountDown), 0, 1);
        mainMgr.dataHands.Clear();
        ynRecord = true;
        ynRecordStart = true;
        timeRecordStart = Time.realtimeSinceStartup;
        Invoke(nameof(RecordStopAndSave), durationRecord);
    }

    public void RecordStop()
    {
        mainMgr.textCountdown.gameObject.SetActive(false);
        CancelInvoke(nameof(RecordStopAndSave));
        CancelInvoke(nameof(CountDown));
        ynRecord = false;
        ynRecordStart = false;
        mainMgr.AdjustButtons();
    }

    void RecordStopAndSave()
    {
        RecordStop();
        string txt = DataHandsToString();
        string filespec = mainMgr.GetFilespec();
        File.WriteAllText(filespec, txt); // blocks (bad user experience)
    }

    public string DataHandsToString()
    {
        string txtLines = GetHeader();
        string s = "\n";
        for (int n = 0; n < mainMgr.dataHands.Count; n++)
        {
            List<float> dataRecord = mainMgr.dataHands[n];
            string txtLine = "";
            string ss = "";
            for (int nn = 0; nn < dataRecord.Count; nn++)
            {
                float v = dataRecord[nn];
                if ((nn + 1) % 6 > 2)
                {
                    v = mainMgr.NormalizeAngle(v);
                }
                txtLine += ss + v;
                ss = ",";
            }
            txtLines += s + txtLine;
        }
        return txtLines;
    }

    void CountDown()
    {
        mainMgr.textCountdown.text = nCountDown.ToString();
        mainMgr.audioMatMgr.PlayPing();
        nCountDown--;
    }

    void UpdateRecord()
    {
        if (!ynRecord) return;
        if (!ynRecordStart) return;
        HandsLiveToData();
    }

    void HandsLiveToData()
    {
        List<float> dataRecord = new();
        float elapsed = Time.realtimeSinceStartup - timeRecordStart;
        dataRecord.Add(elapsed);
        HandVisual hand = null;
        for (int h = 0; h < 2; h++)
        {
            if (h == 0)
            {
                hand = mainMgr.handLiveLeft;
                mainMgr.xrRootLive = mainMgr.xrRootLiveLeft;
            }
            if (h == 1)
            {
                hand = mainMgr.handLiveRight;
                mainMgr.xrRootLive = mainMgr.xrRootLiveRight;
            }
            for (int n = 0; n < hand.Joints.Count; n++)
            {
                Vector3 posL = hand.Joints[n].transform.localPosition;
                Vector3 eulL = hand.Joints[n].transform.localEulerAngles;
                dataRecord.Add(posL.x);
                dataRecord.Add(posL.y);
                dataRecord.Add(posL.z);
                dataRecord.Add(eulL.x);
                dataRecord.Add(eulL.y);
                dataRecord.Add(eulL.z);
            }
            (Vector3 wristPosL, Vector3 wristEulL) = HandToDataPlacement(mainMgr.xrRootLive, mainMgr.hands);
            dataRecord.Add(wristPosL.x);
            dataRecord.Add(wristPosL.y);
            dataRecord.Add(wristPosL.z);
            dataRecord.Add(wristEulL.x);
            dataRecord.Add(wristEulL.y);
            dataRecord.Add(wristEulL.z);
        }
        mainMgr.dataHands.Add(dataRecord);
    }

    (Vector3, Vector3) HandToDataPlacement(GameObject xrRootLive, GameObject parent)
    {
        GameObject parentOrig = xrRootLive.transform.parent.gameObject;
        xrRootLive.transform.SetParent(parent.transform);
        Vector3 wristPosL = xrRootLive.transform.localPosition;
        Vector3 wristEulL = xrRootLive.transform.localEulerAngles;
        xrRootLive.transform.SetParent(parentOrig.transform);
        return (wristPosL, wristEulL);
    }

    string GetHeader()
    {
        string s = ",";
        string txtLine = "0:elapsed";
        int nTrain = 0;
        for (int n = 0; n < mainMgr.handLiveLeft.Joints.Count * 6; n++)
        {
            int j = n / 6;
            string txt = (n + 1) + ":" + j + ":";
            int d = n % 6;
            if (d == 0) txt += "xPos";
            if (d == 1) txt += "yPos";
            if (d == 2) txt += "zPos";
            if (d == 3) txt += "xEul"; // tracked for training
            if (d == 4) txt += "yEul";
            if (d == 5) txt += "zEul";
            if (mainMgr.IsUsedForTraining(j) && d == 3)
            {
                txt = "TRAIN:" + nTrain + "(" + txt + ")";
                nTrain++;
            }
            txtLine += s + txt;
        }
        txtLine += s + "xPos(Wrist)";
        txtLine += s + "yPos(Wrist)";
        txtLine += s + "zPos(Wrist)";
        txtLine += s + "xEul(Wrist)";
        txtLine += s + "yEul(Wrist)";
        txtLine += s + "zEul(Wrist)";
        return txtLine;
    }
}

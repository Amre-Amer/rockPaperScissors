using Oculus.Interaction;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class MainMgr : MonoBehaviour
{
    public RecordMgr recordMgr;
    public PlaybackMgr playbackMgr;
    public ScoreMgr scoreMgr;
    public NNMgr nnMgr;
    public AudioMatMgr audioMatMgr;
    public ScreensaverMgr screensaverMgr;
    public DocMgr docMgr;
    public GameObject cameraRig;
    public TMP_Text textCountdown;
    public TMP_Text textFps;
    public TMP_Text textInfo;
    public bool ynRecord;
    public bool ynRecordStart;
    float timeRecordStart;
    public int durationRecord;
    [HideInInspector]
    public GameObject xrRootLive;
    public HandVisual handLiveLeft;
    public HandVisual handLiveRight;
    public GameObject xrRootLiveLeft;
    public GameObject xrRootLiveRight;
    public List<List<float>> dataHands = new();
    public int[] indexesTraining = new int[] { 7, 12, 17, 22 };
    int cntFrames;
    int nCountDown;
    public GameObject floor;
    public Renderer rendFloor;
    rpsType rpsTypeCurrent;
    public GameObject quadRock;
    public GameObject quadPaper;
    public GameObject quadScissors;
    public GameObject quadRecord;
    public GameObject quadHelp;
    public GameObject buttonRock;
    public GameObject buttonPaper;
    public GameObject buttonScissors;
    public GameObject buttonRecord;
    public GameObject buttonHelp;
    float morphFraction;
    float durationMorph;
    float morphStartTime;
    float delayScore;
    bool ynPlayback;
    int frameCurrent;
    public List<List<float>> dataHandsRock = new();
    public List<List<float>> dataHandsPaper = new();
    public List<List<float>> dataHandsScissors = new();
    public HandVisual handLeft;
    public HandVisual handRight;
    public GameObject hands;
    public ModeType modeTypeCurrent;
    ModeType modeTypeCurrentLast;
    Vector3 posHeadLast;
    float minDistSwitch;
    public TextAsset textAssetRockThrow;
    public TextAsset textAssetPaperThrow;
    public TextAsset textAssetScissorsThrow;
    public float[] inputsInfer;
    float minMoveAutoLR;
    float minMoveAutoFingersLR;
    Vector3 posMoveLeftLast;
    Vector3 posMoveRightLast;
    float angMoveLeftFingersLast;
    float angMoveRightFingersLast;
    public LeftRightType leftRightCurrent;
    public bool ynMoved;
    int nInferEditor;
    [HideInInspector]
    public HandVisual handLive;
    float timestampSwitchModeLast;
    public int nInfer;
    public int nHandRoll;
    public int nHandRollTarget;
    int cntFps;
    public GameObject quadSwitchModeLeft;
    public GameObject quadSwitchModeRight;
    Vector3 posQuadSwitchModeLeft;
    Vector3 posQuadSwitchModeRight;
    bool ynActive;
    bool ynHelp;
    bool ynWorldReady;
    float cntSwitchMode;

    private void Awake()
    {
        posQuadSwitchModeLeft = quadSwitchModeLeft.transform.localPosition;
        posQuadSwitchModeRight = quadSwitchModeRight.transform.localPosition;
        TextAssetToData(textAssetRockThrow, dataHandsRock);
        TextAssetToData(textAssetPaperThrow, dataHandsPaper);
        TextAssetToData(textAssetScissorsThrow, dataHandsScissors);
        SetActive(false);
        ynWorldReady = false;
        minDistSwitch = .0254f * 2;
        durationRecord = 5;
        delayScore = 3.25f;
        durationMorph = .5f;
        minMoveAutoLR = .01254f;
        minMoveAutoFingersLR = 10;
        textCountdown.gameObject.SetActive(false);
        textInfo.gameObject.SetActive(false);
        quadHelp.SetActive(false);
        if (!Application.isEditor)
        {
            OVRManager.display.RecenteredPose += () =>
            {
                AdjustWorld();
            };
        }
    }

    void Start()
    {
        audioMatMgr.PlayBoing();
        InvokeRepeating(nameof(Fps), 1, 1);
        //InvokeRepeating(nameof(ShowInfo), .5f, 1);
        AdjustMode();
        hands.SetActive(false);
        if (Application.isEditor)
        {
            modeTypeCurrent = ModeType.game;
            Invoke(nameof(TestMoveEditor), .1f);
            Invoke(nameof(HelpPressed), 10);
            //InvokeRepeating(nameof(DocPressed), 13, 3);
        }
        AdjustWorldWhenReady();
    }

    void Update()
    {
        UpdateSwitchModeHelp();
        if (!ynActive) return;
        DetectModeSwitch();
        AutoDetectLeftRightCurrent();
        AutoDetectFingersLeftRightCurrent();
        SetHandLiveAfterAutoDetect();
        UpdateChangeMode();
        UpdateInfer();
        UpdateFlashFloor();
        cntFrames++;
        cntFps++;
    }

    void PlayScreensaver()
    {
        SetActive(false);
        screensaverMgr.PlayOnce();
        float duration = screensaverMgr.GetDuration();
        Invoke(nameof(SetActiveTrue), duration);
    }

    void SetActiveTrue()
    {
        SetActive(true);
    }

    void AdjustWorldWhenReady()
    {
        if (ynWorldReady) return;
        float interval = .1f;
        if (Camera.main.transform.position.y > 0)
        {
            ynWorldReady = true;
            SetActive(true);
            AdjustWorld();
            HelpPressed();
            //PlayScreensaver();
            float duration = screensaverMgr.GetDuration();
            Invoke(nameof(TurnOffHelp), duration);
        }
        Invoke(nameof(AdjustWorldWhenReady), interval);
    }

    void TurnOffHelp()
    {
        if (ynHelp)
        {
            HelpPressed();
        }
    }

    public void SetActive(bool yn)
    {
        ynActive = yn;
        playbackMgr.SetActive(yn);
        hands.SetActive(yn);
        scoreMgr.SetActive(yn);
    }

    void UpdateSwitchModeHelp()
    {
        if (!ynHelp) return;
        float speed = 4;
        float range = 1;
        float ang = speed * cntSwitchMode;
        float dX = range * Mathf.Cos(ang * Mathf.Deg2Rad);
        AnimateLR(quadSwitchModeLeft, posQuadSwitchModeLeft, dX);
        AnimateLR(quadSwitchModeRight, posQuadSwitchModeRight, -dX);
        cntSwitchMode++;
    }

    void AnimateLR(GameObject go, Vector3 posLOrig, float dX)
    {
        Vector3 posL = posLOrig + new Vector3(dX, 0, 0);
        go.transform.localPosition = posL;
    }

    void UpdateFlashFloor()
    {
        Color colorA = Color.green;
        Color colorB = Color.green / 2;
        float delay = 2;
        Color lerpedColor = Color.Lerp(colorA, colorB, Mathf.PingPong(Time.time, delay));
        rendFloor.material.color = lerpedColor;
    }

    void SwitchRPS()
    {
        switch (rpsTypeCurrent)
        {
            case rpsType.none:
                RockPressed();
                break;
            case rpsType.rock:
                PaperPressed();
                break;
            case rpsType.paper:
                ScissorsPressed();
                break;
            case rpsType.scissors:
                RockPressed();
                break;
        }
        SetDataHandsForRPSType();
        float duration = playbackMgr.GetDuration(dataHands);
        Invoke(nameof(SwitchRPS), duration);
    }

    void SetDataHandsForRPSType()
    {
        switch (rpsTypeCurrent) {
            case rpsType.none:
                dataHands = dataHandsRock;
                break;
            case rpsType.rock:
                dataHands = dataHandsRock;
                break;
            case rpsType.paper:
                dataHands = dataHandsPaper;
                break;
            case rpsType.scissors:
                dataHands = dataHandsScissors;
                break;
        }
    }

    void Fps()
    {
        textFps.text = cntFps + " fps";
        cntFps = 0;
    }

    void TestAutoDetect()
    {
        int n = Random.Range(0, 2);
        if (n == 0)
        {
            xrRootLiveLeft.transform.Translate(0, 0, minMoveAutoFingersLR * 1.5f);
        }
        if (n == 1)
        {
            xrRootLiveRight.transform.Translate(0, 0, minMoveAutoFingersLR * 1.5f);
        }
    }

    void TestMoveEditor() {
        Camera.main.transform.Translate(0, 1, 0);
        AdjustWorld();
    }

    void DetectModeSwitch()
    {
        if (Application.isEditor) return;
        int j = indexesTraining[0];
        Vector3 posLeftIndex = handLiveLeft.Joints[j].transform.position;
        Vector3 posRightIndex = handLiveRight.Joints[j].transform.position;
        float dist = Vector3.Distance(posLeftIndex, posRightIndex);
        float delay = .5f;
        float elapsed = Time.realtimeSinceStartup - timestampSwitchModeLast;
        if (dist <= minDistSwitch && elapsed > delay)
        {
            timestampSwitchModeLast = Time.realtimeSinceStartup;
            SwitchMode();
        }
    }

    void SwitchMode()
    {
        audioMatMgr.PlayBoing();
        switch (modeTypeCurrent)
        {
            case ModeType.game:
                modeTypeCurrent = ModeType.record;
                break;
            case ModeType.record:
                modeTypeCurrent = ModeType.game;
                break;
        }
        AdjustMode();
    }

    void CancelInvokes()
    {
        recordMgr.CancelInvokes();
        playbackMgr.CancelInvoke();
    }

    void UpdateChangeMode()
    {
        if (modeTypeCurrentLast != modeTypeCurrent)
        {
            AdjustMode();
        }
        modeTypeCurrentLast = modeTypeCurrent;
    }

    void SetHandLiveAfterAutoDetect()
    {
        switch (leftRightCurrent)
        {
            case LeftRightType.left:
                handLive = handLiveLeft;
                xrRootLive = xrRootLiveLeft;
                break;
            case LeftRightType.right:
                handLive = handLiveRight;
                xrRootLive = xrRootLiveRight;
                break;
        }
        if (modeTypeCurrent == ModeType.game)
        {
            switch (leftRightCurrent)
            {
                case LeftRightType.left:
                    handLeft.gameObject.SetActive(true);
                    handRight.gameObject.SetActive(false);
                    break;
                case LeftRightType.right:
                    handLeft.gameObject.SetActive(false);
                    handRight.gameObject.SetActive(true);
                    break;
            }
        }
        else
        {
            HideHandsIfRecording();
        }
    }

    void HideHandsIfRecording()
    {
        handLeft.gameObject.SetActive(!ynRecord);
        handRight.gameObject.SetActive(!ynRecord);
    }

    void AdjustMode()
    {
        CancelInvokes();
        recordMgr.RecordStop();
        AdjustHandsRotation();
        if (modeTypeCurrent == ModeType.game)
        {
            scoreMgr.ClearScores();
            playbackMgr.PlayGame();
        }
        if (modeTypeCurrent == ModeType.record)
        {
            if (rpsTypeCurrent == rpsType.none)
            {
                if (nHandRoll == 0) rpsTypeCurrent = rpsType.rock;
                if (nHandRoll == 1) rpsTypeCurrent = rpsType.paper;
                if (nHandRoll == 2) rpsTypeCurrent = rpsType.scissors;
            }
        }
        scoreMgr.gameObject.SetActive(modeTypeCurrent == ModeType.game);
        buttonRecord.SetActive(modeTypeCurrent == ModeType.record);
        AdjustButtons();
    }

    void ShowInfo()
    {
        string f1 = "<color=green>";
        string f2 = "<color=white>";
        string s = "\n";
        string txt = f1 + "contact: <b>" + f2 + "me@amre-amer.com" + "</b>";
        txt += s + f1 + "version: <b>" + f2 + Application.version + "</b>";
        txt += s + f1 + "productName: <b>" + f2 + Application.productName + "</b>";
        txt += s + f1 + "unityVersion: <b>" + f2 + Application.unityVersion + "</b>";
        txt += s + f1 + "deviceModel: <b>" + f2 + SystemInfo.deviceModel + "</b>";
        txt += s + f1 + "deviceName: <b>" + f2 + SystemInfo.deviceName + "</b>";
        txt += s + f1 + "deviceType: <b>" + f2 + SystemInfo.deviceType + "</b>";
        string f3 = f2;
        if (SystemInfo.batteryLevel < .25f) f3 = "<color=red>";
        txt += s + f1 + "batteryLevel: <b>" + f3 + SystemInfo.batteryLevel + "</b></color>";
        txt += s + f1 + "processorCount: <b>" + f2 + SystemInfo.processorCount + "</b>";
        txt += s + f1 + "processorFrequency: <b>" + f2 + SystemInfo.processorFrequency + "</b>";
        txt += s + f1 + "processorManufacturer: <b>" + f2 + SystemInfo.processorManufacturer + "</b>";
        txt += s + f1 + "processorModel: <b>" + f2 + SystemInfo.processorModel + "</b>";
        txt += s + f1 + "processorType: <b>" + f2 + SystemInfo.processorType + "</b>";
        txt += s + f1 + "graphicsDeviceName: <b>" + f2 + SystemInfo.graphicsDeviceName + "</b>";
        txt += s + f1 + "graphicsMemorySize: <b>" + f2 + SystemInfo.graphicsMemorySize + "</b>";
        txt += s + f1 + "graphicsDeviceVendor: <b>" + f2 + SystemInfo.graphicsDeviceVendor + "</b>";
        txt += s + f1 + "System.GC.GetTotalMemory(true): <b>" + f2 + System.GC.GetTotalMemory(true).ToString("N0") + "</b>";
        txt += s + f1 + "Secret Record Mode Switch: <b>" + f2 + "touch knuckles as show:" + "</b>";
        textInfo.text = txt;
    }

    void AdjustHandsRotation()
    {
        Vector3 posL;
        switch (modeTypeCurrent)
        {
            case ModeType.game:
                posL = hands.transform.localPosition;
                posL.z = .5f;
                hands.transform.localPosition = posL;
                hands.transform.localEulerAngles = new Vector3(0, 180, 0);
                break;
            case ModeType.record:
                posL = hands.transform.localPosition;
                posL.z = 0;
                hands.transform.localPosition = posL;
                hands.transform.localEulerAngles = new Vector3(0, 0, 0);
                break;
        }
    }

    //void FileToData(rpsType rpsType, List<List<float>> data)
    //{
    //    rpsTypeCurrent = rpsType;
    //    string filespec = GetFilespec();
    //    string txtAll = File.ReadAllText(filespec);
    //    StringToDataHands(txtAll, data);
    //}

    void TextAssetToData(TextAsset textAsset, List<List<float>> data)
    {
        string txtAll = textAsset.text;
        StringToDataHands(txtAll, data);
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

    bool IsHeader(string txtLine)
    {
        string txtFirst = txtLine.Split(',')[0];
        bool ynIsNumeric = IsNumeric(txtFirst);
        return !ynIsNumeric;
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

    public void AdjustButtons()
    {
        quadRock.SetActive(rpsTypeCurrent == rpsType.rock);
        quadPaper.SetActive(rpsTypeCurrent == rpsType.paper);
        quadScissors.SetActive(rpsTypeCurrent == rpsType.scissors);
        quadRecord.SetActive(recordMgr.ynRecord);
    }

    public void HelpPressed()
    {
        ynHelp = !ynHelp;
        quadHelp.SetActive(ynHelp);
        textInfo.gameObject.SetActive(ynHelp);
        docMgr.SetActive(ynHelp);
        cntSwitchMode = 0;
        if (ynHelp)
        {
            PlayScreensaver();
        }
    }

    public void RockPressed()
    {
        if (modeTypeCurrent == ModeType.game) return;
        rpsTypeCurrent = rpsType.rock;
        AdjustButtons();
        SetDataHandsForRPSType();
    }

    public void PaperPressed()
    {
        if (modeTypeCurrent == ModeType.game) return;
        rpsTypeCurrent = rpsType.paper;
        AdjustButtons();
        SetDataHandsForRPSType();
    }

    public void ScissorsPressed()
    {
        if (modeTypeCurrent == ModeType.game) return;
        rpsTypeCurrent = rpsType.scissors;
        AdjustButtons();
        SetDataHandsForRPSType();
    }

    public void RecordPressed()
    {
        if (modeTypeCurrent == ModeType.game) return;
        recordMgr.RecordPressed();
        AdjustButtons();
    }

    public void DocPressed()
    {
        docMgr.Advance();
    }

    public string GetFilespec()
    {
        string filename = rpsTypeCurrent + "_throw.csv";
        string filespec = Path.Combine(Application.persistentDataPath, filename);
        return filespec;
    }

    public bool IsUsedForTraining(int n)
    {
        bool yn = false;
        for (int i = 0; i < indexesTraining.Length; i++)
        {
            if (indexesTraining[i] == n)
            {
                yn = true;
                break;
            }
        }
        return yn;
    }

    public float NormalizeAngle(float vv)
    {
        float v = vv;
        if (v < -180) v += 360;
        if (v > 180) v -= 360;
        return v;
    }

    void AdjustWorld()
    {
        //audioMatMgr.PlayPing();
        GameObject world = gameObject;
        Vector3 pos = Camera.main.transform.position;
        world.transform.position = pos;
        world.transform.eulerAngles = Camera.main.transform.eulerAngles;
        LevelEuler(world);
        AdjustUI();
        AdjustMode();
    }

    void AdjustUI()
    {
        float dy = .1f;
        AdjustUIElement(textFps.gameObject, dy * 2);
        AdjustUIElement(scoreMgr.gameObject, dy);
        AdjustUIElementToFloor(floor);
        AdjustUIElement(buttonRock, -dy);
        AdjustUIElement(buttonPaper, -dy);
        AdjustUIElement(buttonScissors, -dy);
        AdjustUIElement(buttonHelp, -dy * 2);
        AdjustUIElement(buttonRecord, 0);
        AdjustUIElement(hands, 0);
    }

    void AdjustUIElement(GameObject go, float yOffsetEye)
    {
        float yEye = Camera.main.transform.position.y;
        Vector3 pos = go.transform.position;
        pos.y = yEye + yOffsetEye;
        go.transform.position = pos;
    }

    void AdjustUIElementToFloor(GameObject go)
    {
        Vector3 pos = go.transform.position;
        pos.y = 0;
        go.transform.position = pos;
    }

    void LevelEuler(GameObject go)
    {
        Vector3 eul = go.transform.localEulerAngles;
        eul.x = 0;
        eul.z = 0;
        go.transform.localEulerAngles = eul;
    }
    void AutoDetectLeftRightCurrent()
    {
        if (modeTypeCurrent == ModeType.record) return;
        Vector3 posMoveLeft = xrRootLiveLeft.transform.position;
        Vector3 posMoveRight = xrRootLiveRight.transform.position;
        float distLeft = Vector3.Distance(posMoveLeft, posMoveLeftLast);
        float distRight = Vector3.Distance(posMoveRight, posMoveRightLast);
        if (distLeft > minMoveAutoLR)
        {
            ynMoved = true;
            leftRightCurrent = LeftRightType.left;
        }
        if (distRight > minMoveAutoLR)
        {
            ynMoved = true;
            leftRightCurrent = LeftRightType.right;
        }
        posMoveLeftLast = posMoveLeft;
        posMoveRightLast = posMoveRight;
    }

    void AutoDetectFingersLeftRightCurrent()
    {
        if (Application.isEditor) return;
        if (modeTypeCurrent == ModeType.record) return;
        int n = indexesTraining[0];
        float angMoveLeftFingers = handLiveLeft.Joints[n].transform.localEulerAngles.x;
        angMoveLeftFingers = NormalizeAngle(angMoveLeftFingers);
        float angMoveRightFingers = handLiveRight.Joints[n].transform.localEulerAngles.x;
        angMoveRightFingers = NormalizeAngle(angMoveRightFingers);
        float deltaLeftFingers = Mathf.Abs(angMoveLeftFingers - angMoveLeftFingersLast);
        float deltaRightFingers = Mathf.Abs(angMoveRightFingers - angMoveRightFingersLast);
        if (deltaLeftFingers > minMoveAutoFingersLR)
        {
            ynMoved = true;
            leftRightCurrent = LeftRightType.left;
        }
        if (deltaRightFingers > minMoveAutoFingersLR)
        {
            ynMoved = true;
            leftRightCurrent = LeftRightType.right;
        }
        angMoveLeftFingersLast = angMoveLeftFingers;
        angMoveRightFingersLast = angMoveRightFingers;
    }

    void UpdateInfer()
    {
        if (ynRecord) return;
        HandLiveToInferInputs();
        Infer();
        SetMatInferFromInferOutputs();
        SetMatHandInfer();
        if (modeTypeCurrent == ModeType.game) HighlightInferRPS();
    }

    public void HighlightInferRPS()
    {
        quadRock.SetActive(nInfer == 0);
        quadPaper.SetActive(nInfer == 1);
        quadScissors.SetActive(nInfer == 2);
    }

    void Infer()
    {
        nInfer = nnMgr.InferWithNOutput(inputsInfer);
    }

    void SetMatInferFromInferOutputs()
    {
        audioMatMgr.matInfer = null;
        if (nInfer == 0) audioMatMgr.matInfer = audioMatMgr.matRed;
        if (nInfer == 1) audioMatMgr.matInfer = audioMatMgr.matGreen;
        if (nInfer == 2) audioMatMgr.matInfer = audioMatMgr.matBlue;
    }

    void SetMatHandInfer()
    {
        if (modeTypeCurrent == ModeType.game)
        {
            switch (leftRightCurrent)
            {
                case LeftRightType.left:
                    SetMatHand(handLiveLeft, audioMatMgr.matInfer);
                    SetMatHand(handLiveRight, audioMatMgr.matOrig);
                    break;
                case LeftRightType.right:
                    SetMatHand(handLiveLeft, audioMatMgr.matOrig);
                    SetMatHand(handLiveRight, audioMatMgr.matInfer);
                    break;
            }
        }
        else
        {
            switch (leftRightCurrent)
            {
                case LeftRightType.left:
                    SetMatHand(handLiveLeft, audioMatMgr.matInfer);
                    SetMatHand(handLiveRight, audioMatMgr.matWhite);
                    break;
                case LeftRightType.right:
                    SetMatHand(handLiveLeft, audioMatMgr.matWhite);
                    SetMatHand(handLiveRight, audioMatMgr.matInfer);
                    break;
            }
        }
    }

    public void SetMatHand(HandVisual hand, Material mat)
    {
        hand.GetComponentInChildren<SkinnedMeshRenderer>().material = mat;
    }

    void AdvanceNInferEditor()
    {
        nInferEditor++;
        if (nInferEditor >= 3) nInferEditor = 0;
    }

    void HandLiveToInferInputs()
    {
        if (Application.isEditor)
        {
            float d = 10 *Mathf.Cos(cntFrames * Mathf.Deg2Rad);
            if (nInferEditor == 0) inputsInfer = new float[] { 70 + d, 70 + d, 70 + d, 70 + d };
            if (nInferEditor == 1) inputsInfer = new float[] { 0 + d, 0 + d, 0 + d, 0 + d };
            if (nInferEditor == 2) inputsInfer = new float[] { 0 + d, 0 + d, 70 + d, 70 + d };
            nInfer = nInferEditor;
        }
        else
        {
            inputsInfer = new float[indexesTraining.Length];
            for (int n = 0; n < indexesTraining.Length; n++)
            {
                int nb = indexesTraining[n];
                float v = handLive.Joints[nb].transform.localEulerAngles.x;
                v = NormalizeAngle(v);
                inputsInfer[n] = v;
            }
        }
    }
}

public enum ModeType
{
    game,
    record
}

public enum rpsType
{
    none,
    rock,
    paper,
    scissors
}

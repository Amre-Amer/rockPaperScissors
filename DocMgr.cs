using System;
using UnityEngine;

public class DocMgr : MonoBehaviour
{
    public GameObject quadIntro;
    public GameObject quadDiagram;
    public GameObject quadVideo;
    float interval;
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public UnityEngine.Video.VideoClip clipVideo;
    float clipVideoLength;
    public enum DocType
    {
        intro,
        diagram,
        video
    }
    public DocType docCurrent;

    private void Awake()
    {
        videoPlayer.clip = clipVideo;
    }

    void Start()
    {
        interval = 10;
        StopAndCancelInvokes();
    }

    void SetActiveOn()
    {
        SetActive(true);
    }

    public void SetActive(bool yn)
    {
        if (yn)
        {
            docCurrent = DocType.intro;
            ShowDocCurrent();
        } else
        {
            StopAndCancelInvokes();
        }
    }

    public void Advance()
    {
        AdvanceDocCurrent();
        ShowDocCurrent();
    }

    void ShowDocCurrent()
    {
        StopAndCancelInvokes();
        switch (docCurrent)
        {
            case DocType.intro:
                ShowIntro();
                Invoke(nameof(Advance), interval / 2);
                break;
            case DocType.diagram:
                ShowDiagram();
                Invoke(nameof(Advance), interval);
                break;
            case DocType.video:
                ShowVideo();
                Invoke(nameof(Advance), clipVideoLength);
                break;
        }
    }

    void AdvanceDocCurrent()
    {
        int nDoc = (int)docCurrent;
        nDoc++;
        if (nDoc >= Enum.GetNames(typeof(DocType)).Length) nDoc = 0;
        docCurrent = (DocType)nDoc;
    }

    void ShowIntro()
    {
        Debug.Log("ShowIntro\n");
        quadIntro.SetActive(true);
    }

    void ShowDiagram()
    {
        Debug.Log("ShowDiagram\n");
        quadDiagram.SetActive(true);
    }

    void ShowVideo()
    {
        Debug.Log("ShowVideo\n");
        quadVideo.SetActive(true);
        videoPlayer.Play();
        clipVideoLength = (float)clipVideo.length;
    }

    void StopAndCancelInvokes()
    {
        StopIntro();
        StopDiagram();
        StopVideo();
        CancelInvoke(nameof(Advance));
    }

    void StopIntro()
    {
        quadIntro.SetActive(false);
    }

    void StopDiagram()
    {
        quadDiagram.SetActive(false);
    }

    void StopVideo()
    {
        videoPlayer.Pause();
        quadVideo.SetActive(false);
    }
}
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

public class NNMgr : MonoBehaviour
{
    private Model m_RuntimeModel;

    public NNModel modelAsset;
    IWorker worker;
    float[] inferenceValues = new float[3];
    float aveInferDuration;
    float sumInferDuration;
    int cntInfer;
    public List<float> outputs = new List<float>();

    void Start()
    {
        InferLoadAiModel();
        float[] testInput = { 70, 70, 70, 70};
        float[] testOutput = InferWithOutput(testInput);
        Debug.Log("NNMgr:infer test:" + string.Join(',', testOutput) + " [1,0,0] rock\n");
    }

    public int InferWithNOutput(float[] tensorData)
    {
        float t = Time.realtimeSinceStartup;
        Tensor input = new Tensor(1, 1, 1, tensorData.Length, tensorData);
        Tensor output = worker.Execute(input).PeekOutput();
        float[] arrayOutput = InferSetTxtInferenceFromOutputToFloatArray(output);
        input.Dispose();
        output.Dispose();
        t = Time.realtimeSinceStartup - t;
        sumInferDuration += t;
        cntInfer++;
        aveInferDuration = sumInferDuration / cntInfer;
        outputs = Array2List(arrayOutput);
        int nOutput = GetIndexOfMaxOutputs();
        return nOutput;
    }

    List<float> Array2List(float[] array)
    {
        List<float> list = new List<float>();
        for (int n = 0; n < array.Length; n++)
        {
            list.Add(array[n]);
        }
        return list;
    }

    int GetIndexOfMaxOutputs()
    {
        float maxV = -1;
        int maxN = -1;
        for (int n = 0; n < outputs.Count; n++)
        {
            float v = outputs[n];
            if (n == 0 || v > maxV)
            {
                maxV = v;
                maxN = n;
            }
        }
        return maxN;
    }

    public float[] InferWithOutput(float[] tensorData)
    {
        float t = Time.realtimeSinceStartup;
        Tensor input = new Tensor(1, 1, 1, tensorData.Length, tensorData);
        Tensor output = worker.Execute(input).PeekOutput();
        float[] arrayOutput = InferSetTxtInferenceFromOutputToFloatArray(output);
        input.Dispose();
        output.Dispose();
        t = Time.realtimeSinceStartup - t;
        sumInferDuration += t;
        cntInfer++;
        aveInferDuration = sumInferDuration / cntInfer;
        return arrayOutput;
    }

    public float GetInferDuration()
    {
        return aveInferDuration;
    }

    float[] InferSetTxtInferenceFromOutputToFloatArray(Tensor output)
    {
        float[] array = new float[inferenceValues.Length];
        for (int n = 0; n < inferenceValues.Length; n++)
        {
            array[n] = output[0, 0, 0, n];
        }
        return array;
    }

    void InferLoadAiModel()
    {
        if (modelAsset == null)
        {
            Debug.Log("modelAsset is null");
            return;
        }
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        WorkerFactory.Type typ = WorkerFactory.Type.CSharpBurst;
        worker = WorkerFactory.CreateWorker(typ, m_RuntimeModel);
    }

}

using UnityEngine;

public class DLAMaster : MonoBehaviour
{
    [SerializeField] private ComputeShader pointComputeShader;
    [SerializeField] private int pointAmount;

    [SerializeField] private Bounds bounds;

    private ComputeBuffer pointComputeBuffer;
    private Point[] cpuData;

    private float seed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        pointComputeShader = Instantiate(pointComputeShader);

        seed = Random.Range(0, 10000);
        CreateBuffer();

        Dispatch();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("oy");
    }

    private void OnDestroy()
    {
        pointComputeBuffer.Dispose();
    }

    void CreateBuffer()
    {
        pointComputeBuffer = new ComputeBuffer(pointAmount, (sizeof(float) * 3) + sizeof(uint));
    }

    void SetBuffer()
    {
        pointComputeShader.SetBuffer(0, "points", pointComputeBuffer);
    }

    void SetData()
    {
        SetBuffer();

        pointComputeShader.SetFloat("deltaTime", Time.deltaTime);

        pointComputeShader.SetFloat("seed", seed);
    }

    void GetData()
    {
        if (cpuData == null)
            cpuData = new Point[pointAmount];

        pointComputeBuffer.GetData(cpuData);
    }

    void Dispatch()
    {
        SetData();

        int numThreads = 1;
        int groups = Mathf.CeilToInt((float)pointAmount / (float)numThreads);

        pointComputeShader.Dispatch(0, groups, 1, 1);

        GetData();
        for (int i = 0; i < pointAmount; i++)
        {
            Debug.Log($"float {i}: \nPos={cpuData[i].position}");
        }
    }

    public ComputeBuffer GetComputeBuffer() { return pointComputeBuffer; }
    public int GetPointAmount() { return pointAmount; }
}
struct Point
{
    public Vector3 position;
    public uint isSolid;
};
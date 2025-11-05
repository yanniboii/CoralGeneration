using UnityEngine;

public class DLAMaster : MonoBehaviour
{
    public static DLAMaster Instance { get; private set; }

    [SerializeField] private ComputeShader pointComputeShader;
    [SerializeField] private int pointAmount;

    [SerializeField] private Bounds bounds;

    private GraphicsBuffer pointComputeBuffer;
    private Point[] cpuData;

    private float seed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
        pointComputeShader = Instantiate(pointComputeShader);

        seed = Random.Range(0, 10000);
        CreateBuffer();

        Dispatch();
    }

    private void OnDestroy()
    {
        pointComputeBuffer.Dispose();
    }

    void CreateBuffer()
    {
        pointComputeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointAmount, Point.GetSize());
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

    private void OnDisable()
    {
        pointComputeBuffer.Dispose();
    }

    public GraphicsBuffer GetComputeBuffer() { return pointComputeBuffer; }
    public int GetPointAmount() { return pointAmount; }


}
struct Point
{
    public Vector3 position;
    public uint isSolid;

    public static int GetSize() { return (sizeof(float) * 3) + sizeof(uint); }
};
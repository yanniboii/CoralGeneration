using UnityEngine;

public class DLAMaster : MonoBehaviour
{
    public static DLAMaster Instance { get; private set; }

    [SerializeField] private ComputeShader pointComputeShader;
    [SerializeField] private int pointAmount;

    [SerializeField] private Bounds bounds;
    [SerializeField] private Vector3 voxelSize;

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

        StartDispatch();
    }

    private void Update()
    {
        if(Time.frameCount < 5) 
        UpdateDispatch();
    }

    private void OnDestroy()
    {
        pointComputeBuffer.Dispose();
    }

    void CreateBuffer()
    {
        pointComputeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointAmount, Point.GetSize());
    }

    void SetBuffer(string kernel)
    {
        pointComputeShader.SetBuffer(pointComputeShader.FindKernel(kernel), "points", pointComputeBuffer);
    }

    void SetData()
    {
        pointComputeShader.SetFloat("realtimeSinceStartup", Time.realtimeSinceStartup);
        pointComputeShader.SetVector("voxelSize", voxelSize);

        pointComputeShader.SetFloat("seed", seed);
    }

    void GetData()
    {
        if (cpuData == null)
            cpuData = new Point[pointAmount];

        pointComputeBuffer.GetData(cpuData);
    }

    void StartDispatch()
    {
        SetBuffer("GeneratePoints");
        SetData();

        float threadAmount = pointAmount / 1.0f;

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)threadAmount / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)threadAmount / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)threadAmount / (float)numThreads);

        pointComputeShader.Dispatch(pointComputeShader.FindKernel("GeneratePoints"), groupsX, 1, 1);

        GetData();
        for (int i = 0; i < pointAmount; i++)
        {
            Debug.Log($"float {i}: \nPos={cpuData[i].position}");
        }
    }

    void UpdateDispatch()
    {
        SetBuffer("MovePoints");
        SetData();

        float threadAmount = pointAmount / 1.0f;

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)threadAmount / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)threadAmount / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)threadAmount / (float)numThreads);

        pointComputeShader.Dispatch(pointComputeShader.FindKernel("MovePoints"), groupsX, 1, 1);

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
    public uint exists;
    public static int GetSize() { return (sizeof(float) * 3) + (sizeof(uint) * 2); }
};
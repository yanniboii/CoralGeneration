using UnityEngine;

public class DLAMaster : MonoBehaviour
{
    public static DLAMaster Instance { get; private set; }

    [SerializeField] private ComputeShader pointComputeShader;
    [SerializeField] private int m_pointAmount;

    [SerializeField] private Vector3 center;
    [SerializeField] private Vector3 m_BoundStart;
    [SerializeField] private Vector3 m_BoundEnd;
    [SerializeField] private Vector3 m_voxelSize;
    [SerializeField] private int m_gridDivisions;

    public Vector3 voxelSize => m_voxelSize;
    public Vector3 boundStart => m_BoundStart;
    public Vector3 boundEnd => m_BoundEnd;
    public int gridDivisions => m_gridDivisions;
    public int pointAmount => m_pointAmount;

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
        UpdateDispatch();
    }

    private void OnDestroy()
    {
        pointComputeBuffer.Dispose();
    }

    void CreateBuffer()
    {
        pointComputeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, m_pointAmount, Point.GetSize());
    }

    void SetBuffer(string kernel)
    {
        pointComputeShader.SetBuffer(pointComputeShader.FindKernel(kernel), "points", pointComputeBuffer);
    }

    void SetData()
    {
        pointComputeShader.SetFloat("realtimeSinceStartup", Time.realtimeSinceStartup);
        pointComputeShader.SetVector("voxelSize", m_voxelSize);
        pointComputeShader.SetInt("gridResolution", m_gridDivisions);
        pointComputeShader.SetVector("seedPoint", center);
        pointComputeShader.SetVector("boundStart", m_BoundStart);
        pointComputeShader.SetVector("boundEnd", m_BoundEnd);

        pointComputeShader.SetFloat("seed", seed);
    }

    void GetData()
    {
        if (cpuData == null)
            cpuData = new Point[m_pointAmount];

        pointComputeBuffer.GetData(cpuData);
    }

    void StartDispatch()
    {
        SetBuffer("GeneratePoints");
        SetData();

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)m_pointAmount / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)m_pointAmount / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)m_pointAmount / (float)numThreads);

        pointComputeShader.Dispatch(pointComputeShader.FindKernel("GeneratePoints"), groupsX, 1, 1);

        //GetData();
        //for (int i = 0; i < pointAmount; i++)
        //{
        //    Debug.Log($"float {i}: \nPos={cpuData[i].position}");
        //}
    }

    void UpdateDispatch()
    {
        SetBuffer("MovePoints");
        SetData();

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)m_pointAmount / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)m_pointAmount / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)m_pointAmount / (float)numThreads);

        pointComputeShader.Dispatch(pointComputeShader.FindKernel("MovePoints"), groupsX, 1, 1);

        //GetData();
        //for (int i = 0; i < pointAmount; i++)
        //{
        //    Debug.Log($"float {i}: \nPos={cpuData[i].position}");
        //}
    }

    private void OnDisable()
    {
        pointComputeBuffer.Dispose();
    }

    public GraphicsBuffer GetComputeBuffer() { return pointComputeBuffer; }
}
struct Point
{
    public Vector3 position;
    public uint isSolid;
    public uint exists;
    public static int GetSize() { return (sizeof(float) * 3) + (sizeof(uint) * 2); }
};
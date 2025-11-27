using UnityEngine;

public class DualContourMaster : MonoBehaviour
{
    [SerializeField] private ComputeShader dualContourShader;

    private GraphicsBuffer pointComputeBuffer;

    private GraphicsBuffer sdfValues;
    private GraphicsBuffer activeCells;
    private GraphicsBuffer hermiteData;
    private GraphicsBuffer cellVertices;
    private GraphicsBuffer triangles;

    private float[] cpuData;

    private float seed;
    private int gridCorners;
    private int gridEdges;

    private DLAMaster DLAMaster;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DLAMaster = DLAMaster.Instance;

        gridCorners = (int)Mathf.Pow((DLAMaster.gridDivisions + 2), 3);
        gridEdges = 3 * (DLAMaster.gridDivisions + 1) * (int)Mathf.Pow(((DLAMaster.gridDivisions + 1) + 1), 2);

        dualContourShader = Instantiate(dualContourShader);

        CreateBuffers();

        SDFDispatch();
    }

    private void Update()
    {
        UpdateDispatch();
    }

    private void OnDestroy()
    {

    }

    void CreateBuffers()
    {
        sdfValues = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridCorners, sizeof(float));
        activeCells = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridEdges, sizeof(uint));
        hermiteData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridEdges, HermiteData.GetSize());
        cellVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridEdges, sizeof(float) * 3);
        triangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridCorners, sizeof(uint) * 3);
    }

    void SetSDFBuffers()
    {
        dualContourShader.SetBuffer(dualContourShader.FindKernel("SampleSDF"), "sdfValues", sdfValues);
        dualContourShader.SetBuffer(dualContourShader.FindKernel("SampleSDF"), "Spheres", DLAMaster.GetComputeBuffer());
    }

    void SetData()
    {
        dualContourShader.SetFloat("realtimeSinceStartup", Time.realtimeSinceStartup);
        dualContourShader.SetVector("voxelSize", DLAMaster.voxelSize);
        dualContourShader.SetVector("boundStart", DLAMaster.boundStart);
        dualContourShader.SetVector("boundEnd", DLAMaster.boundEnd);
        dualContourShader.SetInt("gridResolution", DLAMaster.gridDivisions);
        dualContourShader.SetInt("_NumSpheres", DLAMaster.pointAmount);
        dualContourShader.SetFloat("sRadius", 0.3f);
        dualContourShader.SetFloat("smoothing", 0.3f);

        dualContourShader.SetFloat("seed", seed);
    }

    void GetData()
    {
        if (cpuData == null)
            cpuData = new float[gridCorners];

        sdfValues.GetData(cpuData);
    }

    void SDFDispatch()
    {
        SetSDFBuffers();
        SetData();

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)gridCorners / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)gridCorners / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)gridCorners / (float)numThreads);

        dualContourShader.Dispatch(dualContourShader.FindKernel("SampleSDF"), groupsX, groupsY, groupsZ);

        GetData();
        for (int i = 0; i < gridCorners; i++)
        {
            //if (cpuData[i] > 0)
            //    Debug.Log("plus");
            //if (cpuData[i] < 0)
            //    Debug.Log("minus");
            //if (cpuData[i] == 0)
            //    Debug.Log("equals");
            //Debug.Log($"float {i}: \nPos={cpuData[i]}");
        }
    }

    void UpdateDispatch()
    {
        //SetBuffer("MovePoints");
        SetData();

        int numThreads = 8;
        //int groupsX = Mathf.CeilToInt(m_pointAmount / numThreads);
        //int groupsY = Mathf.CeilToInt(m_pointAmount / numThreads);
        //int groupsZ = Mathf.CeilToInt(m_pointAmount / numThreads);

        //dualContourShader.Dispatch(dualContourShader.FindKernel("MovePoints"), groupsX, 1, 1);

        //GetData();
        //for (int i = 0; i < pointAmount; i++)
        //{
        //    Debug.Log($"float {i}: \nPos={cpuData[i].position}");
        //}
    }

    private void OnDisable()
    {
        sdfValues.Dispose();
        //activeCells.Dispose();
        //hermiteData.Dispose();
        //cellVertices.Dispose();
        //triangles.Dispose();
    }
}

struct HermiteData
{
    public Vector3 position;
    public Vector3 normal;

    public static int GetSize() { return (sizeof(float) * 6); }
}
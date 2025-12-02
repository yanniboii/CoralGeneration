using Unity.Mathematics;
using UnityEngine;

public class DualContourMaster : MonoBehaviour
{
    [SerializeField] private ComputeShader dualContourShader;

    private GraphicsBuffer pointComputeBuffer;

    private GraphicsBuffer sdfValues;
    private GraphicsBuffer activeCells;
    private GraphicsBuffer hermiteData;
    private GraphicsBuffer hermiteCounts;
    private GraphicsBuffer cellVertices;
    private GraphicsBuffer triangles;

    private float[] sdfData;
    private uint[] activeCellData;
    private HermiteData[] hermite;
    private uint[] hermiteCount;
    private Vector3[] cellVertexData;
    private uint3[] triangleData;

    private float seed;
    private int gridCorners;
    private int cellAmount;
    private int cornerResolution;
    private int gridEdges;
    private int maxHermites;

    private DLAMaster DLAMaster;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DLAMaster = DLAMaster.Instance;

        cornerResolution = DLAMaster.gridDivisions + 1;
        gridCorners = (int)Mathf.Pow(cornerResolution, 3);
        cellAmount = (int)Mathf.Pow(DLAMaster.gridDivisions, 3);

        gridEdges = 3 * (DLAMaster.gridDivisions + 1) * (int)Mathf.Pow(((DLAMaster.gridDivisions + 1) + 1), 2);

        maxHermites = cellAmount * 12;

        dualContourShader = Instantiate(dualContourShader);

        CreateBuffers();

        SDFDispatch();
        SignFlipDispatch();
        HermiteDispatch();
    }

    private void OnDestroy()
    {

    }

    void CreateBuffers()
    {
        sdfValues = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridCorners, sizeof(float));
        activeCells = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellAmount, sizeof(uint));
        hermiteData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxHermites, HermiteData.GetSize());
        hermiteCounts = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cellAmount, sizeof(uint));
        cellVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridEdges, sizeof(float) * 3);
        triangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gridCorners, sizeof(uint) * 3);
    }

    void SetSDFBuffers()
    {
        dualContourShader.SetBuffer(dualContourShader.FindKernel("SampleSDF"), "sdfValues", sdfValues);
        dualContourShader.SetBuffer(dualContourShader.FindKernel("SampleSDF"), "Spheres", DLAMaster.GetComputeBuffer());
    }

    void SetSDFData()
    {
        dualContourShader.SetFloat("realtimeSinceStartup", Time.realtimeSinceStartup);
        dualContourShader.SetVector("voxelSize", DLAMaster.voxelSize);
        dualContourShader.SetVector("boundStart", DLAMaster.boundStart);
        dualContourShader.SetVector("boundEnd", DLAMaster.boundEnd);
        dualContourShader.SetInt("gridResolution", DLAMaster.gridDivisions);
        dualContourShader.SetInt("cornerResolution", cornerResolution);
        dualContourShader.SetInt("gridCorners", gridCorners);
        dualContourShader.SetInt("gridEdges", gridEdges);
        dualContourShader.SetInt("_NumSpheres", DLAMaster.pointAmount);
        dualContourShader.SetFloat("sRadius", 1.4f);
        dualContourShader.SetFloat("smoothing", 0.3f);

        dualContourShader.SetFloat("seed", seed);
    }


    void GetSDFData()
    {
        if (sdfData == null)
            sdfData = new float[gridCorners];

        sdfValues.GetData(sdfData);
    }


    void SDFDispatch()
    {
        SetSDFBuffers();
        SetSDFData();

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)cornerResolution / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)cornerResolution / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)cornerResolution / (float)numThreads);

        dualContourShader.Dispatch(dualContourShader.FindKernel("SampleSDF"), groupsX, groupsY, groupsZ);

        GetSDFData();
        for (int i = 0; i < gridCorners; i++)
        {
            if (sdfData[i] > 0)
                Debug.Log("plus");
            if (sdfData[i] < 0)
                Debug.Log("minus");
            //if (cpuData[i] == 0)
            //    Debug.Log("equals");
            //Debug.Log(cpuData[i]);
            //Debug.Log($"float {i}: \nPos={cpuData[i]}");
        }
    }

    void SetSignFlipBuffers()
    {
        dualContourShader.SetBuffer(dualContourShader.FindKernel("CheckSignFlips"), "sdfValues", sdfValues);
        dualContourShader.SetBuffer(dualContourShader.FindKernel("CheckSignFlips"), "activeCells", activeCells);
    }

    void SetSignFlipData()
    {
        dualContourShader.SetInt("gridResolution", DLAMaster.gridDivisions);
    }

    void GetActiveCellData()
    {
        if (activeCellData == null)
            activeCellData = new uint[cellAmount];

        activeCells.GetData(activeCellData);
    }

    void SignFlipDispatch()
    {
        SetSignFlipBuffers();
        SetSignFlipData();

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)DLAMaster.gridDivisions / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)DLAMaster.gridDivisions / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)DLAMaster.gridDivisions / (float)numThreads);

        dualContourShader.Dispatch(dualContourShader.FindKernel("CheckSignFlips"), groupsX, groupsY, groupsZ);

        GetActiveCellData();
        for (int i = 0; i < cellAmount; i++)
        {
            Debug.Log(activeCellData[i]);
        }
    }

    private void SetHermiteBuffers()
    {
        dualContourShader.SetBuffer(dualContourShader.FindKernel("ExtractHermiteData"), "Spheres", DLAMaster.GetComputeBuffer());
        dualContourShader.SetBuffer(dualContourShader.FindKernel("ExtractHermiteData"), "sdfValues", sdfValues);
        dualContourShader.SetBuffer(dualContourShader.FindKernel("ExtractHermiteData"), "activeCells", activeCells);
        dualContourShader.SetBuffer(dualContourShader.FindKernel("ExtractHermiteData"), "hermiteData", hermiteData);
        dualContourShader.SetBuffer(dualContourShader.FindKernel("ExtractHermiteData"), "hermiteCounts", hermiteCounts);
    }

    private void SetHermiteData()
    {
        dualContourShader.SetVector("boundStart", DLAMaster.boundStart);
        dualContourShader.SetVector("boundEnd", DLAMaster.boundEnd);
        dualContourShader.SetInt("gridResolution", DLAMaster.gridDivisions);
        dualContourShader.SetInt("cornerResolution", cornerResolution);
    }


    private void GetHermiteData()
    {
        if (hermite == null)
            hermite = new HermiteData[maxHermites];

        hermiteData.GetData(hermite);

        if (hermiteCount == null)
            hermiteCount = new uint[cellAmount];

        hermiteCounts.GetData(hermiteCount);
    }

    void HermiteDispatch()
    {
        SetHermiteBuffers();
        SetHermiteData();

        int numThreads = 8;
        int groupsX = Mathf.CeilToInt((float)DLAMaster.gridDivisions / (float)numThreads);
        int groupsY = Mathf.CeilToInt((float)DLAMaster.gridDivisions / (float)numThreads);
        int groupsZ = Mathf.CeilToInt((float)DLAMaster.gridDivisions / (float)numThreads);

        dualContourShader.Dispatch(dualContourShader.FindKernel("ExtractHermiteData"), groupsX, groupsY, groupsZ);

        GetHermiteData();
        for (int i = 0; i < cellAmount * 12; i++)
        {
            Debug.Log(i + " : " + hermite[i].position + " : " + hermite[i].normal);
        }
        for (int i = 0; i < cellAmount * 12; i++)
        {
            Debug.Log(i + " : " + hermite[i].d0 + " : " + hermite[i].d1);
        }
        for (int i = 0; i < cellAmount; i++)
        {
            Debug.Log(i + " : " + hermiteCount[i]);
        }
    }

    private void OnDisable()
    {
        sdfValues.Dispose();
        activeCells.Dispose();
        hermiteCounts.Dispose();
        hermiteData.Dispose();
        //cellVertices.Dispose();
        //triangles.Dispose();
    }
}

struct HermiteData
{
    public Vector3 position;
    public Vector3 normal;
    public float d0;
    public float d1;

    public static int GetSize() { return (sizeof(float) * 8); }
}
using System.Collections.Generic;
using UnityEngine;

public class RayMarchingMaster : MonoBehaviour
{
    [SerializeField] ComputeShader m_ComputeShader;
    [SerializeField] DLAMaster m_DLAMaster;

    [Range(-0.5f, 0.5f)]
    [SerializeField] float smoothing;
    [SerializeField] float ambientIntensity;
    [SerializeField] float diffuseIntensity;
    [SerializeField] float specularIntensity;
    [SerializeField] bool hasLight;
    [SerializeField] bool showDepth;

    [SerializeField] float radius;


    float speed = 0.001f;

    private RenderTexture renderTexture;
    private RenderTexture depthTexture;
    private Camera _camera;

    List<ComputeBuffer> deleteComputeBuffers = new List<ComputeBuffer>();

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.depthTextureMode = DepthTextureMode.Depth;
    }

    // Start is called before the first frame update
    void Start()
    {
        depthTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        depthTexture.enableRandomWrite = true;
        depthTexture.Create();
    }

    void Init()
    {
        _camera = GetComponent<Camera>();
    }

    public void SliderValue(float value)
    {
        smoothing = value;
    }

    void CreateScene()
    {
        m_ComputeShader.SetInt("_NumSpheres", m_DLAMaster.GetPointAmount());

        ComputeBuffer sphereBuffer = m_DLAMaster.GetComputeBuffer();


        m_ComputeShader.SetBuffer(0, "Spheres", sphereBuffer);


        m_ComputeShader.SetBool("hasLight", hasLight);
        m_ComputeShader.SetBool("showDepth", showDepth);


        m_ComputeShader.SetFloat("ambientIntensity", ambientIntensity);
        m_ComputeShader.SetFloat("diffuseIntensity", diffuseIntensity);
        m_ComputeShader.SetFloat("specularIntensity", specularIntensity);
        m_ComputeShader.SetFloat("smoothing", smoothing);
        m_ComputeShader.SetFloat("sRadius", radius);

        renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }

    void InitRenderTexture()
    {
        if (renderTexture == null || renderTexture.width != _camera.pixelWidth || renderTexture.height != _camera.pixelHeight)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
            renderTexture = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
    }

    void SetParameters()
    {
        m_ComputeShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        m_ComputeShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, depthTexture);
        Init();

        InitRenderTexture();
        CreateScene();
        SetParameters();

        m_ComputeShader.SetTexture(0, "Source", src);
        m_ComputeShader.SetTexture(0, "_DepthTexture", depthTexture);
        m_ComputeShader.SetTexture(0, "Result", renderTexture);

        int threadGroupsX = Mathf.CeilToInt(_camera.pixelWidth / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(_camera.pixelHeight / 32.0f);
        m_ComputeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Display the result texture 
        Graphics.Blit(renderTexture, dest);
        for (int i = 0; i < deleteComputeBuffers.Count; i++)
        {
            deleteComputeBuffers[i].Dispose();
            deleteComputeBuffers.RemoveAt(i);
        }
    }
}

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;


// This RendererFeature shows how a compute shader can be used together with RenderGraph.

// What this example doesn't show is that it can run together with render passes. If the
// compute shader is using resources which are also used by render passes then a dependency
// between the passes are created as they would have done for two render passes.
public class ComputeRendererFeature : ScriptableRendererFeature
{
    public static ComputeRendererFeature Instance { get; private set; }

    // We will treat the compute pass as a normal Scriptable Render Pass.
    class ComputePass : ScriptableRenderPass
    {
        #region PassFields
        // Compute shader.
        ComputeShader cs;

        // Compute buffers:
        GraphicsBuffer sphereBuffer;
        GraphicsBuffer cubeBuffer;
        RTHandle raymarcherHandle;

        Material material;
        string textureName = "_InputTexture";

        public RTHandle RayMarchTexture => raymarcherHandle;

        #endregion

        // Constructor is used to initialize the compute buffers.
        public ComputePass(Material material)
        {
            BufferDesc sphereDesc = new BufferDesc(20, (sizeof(float) * 3) + sizeof(uint));
            sphereBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 20, (sizeof(float)*3) + sizeof(uint));
            var sphereList = new List<Sphere>();
            for (int i = 0; i < 20; i++)
            {
                Sphere sphere = new Sphere();
                sphere.pos = new Vector3(0, 0, i);
                sphere.isSolid = 0;
                sphereList.Add(sphere);
            }
            sphereBuffer.SetData(sphereList);

            BufferDesc cubeDesc = new BufferDesc(20, sizeof(float) * 9);
            cubeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 20, sizeof(float)*9);
            var cubeList = new List<Cube>();
            for (int i = 0; i < 20; i++)
            {
                Cube cube = new Cube();
                cube.position = new Vector3(0, 0, i);
                cube.color = new Vector3(0, 0, i);
                cube.bounds = new Vector3(0, 0, i);
                cubeList.Add(cube);
            }
            cubeBuffer.SetData(cubeList);

            this.material = material;


            if(raymarcherHandle == null)
            {
                raymarcherHandle?.Release();

                RenderTexture materialRT = new RenderTexture(Screen.width, Screen.height, 0);
                materialRT.enableRandomWrite = true;
                materialRT.useDynamicScale = true;
                materialRT.Create();

                raymarcherHandle = RTHandles.Alloc(materialRT);
            }
        }

        // Setup function to transfer the compute shader from the renderer feature to
        // the render pass.
        public void Setup(ComputeShader cs)
        {
            this.cs = cs;
        }

        // PassData is used to pass data when recording to the execution of the pass.
        class PassData
        {
            // Compute shader.
            public ComputeShader cs;
            // Buffer handles for the compute buffers.
            public BufferHandle Spheres;
            public BufferHandle Cubes;
            public TextureHandle resultTexture;
            public TextureHandle sourceTexture;

            public float smoothing;
            public float radius;
        }

        // Records a render graph render pass which blits the BlitData's active texture back to the camera's color attachment.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            BufferHandle sphereBufferHandle = renderGraph.ImportBuffer(sphereBuffer);
            BufferHandle cubeBufferHandle = renderGraph.ImportBuffer(cubeBuffer);
            //TextureHandle textureHandle = renderGraph.ImportTexture(raymarcherHandle);

            TextureHandle materialTextureHandle = renderGraph.ImportTexture(raymarcherHandle);

            UniversalResourceData frameData = frameContext.Get<UniversalResourceData>();

            TextureHandle activeColorTexture = frameData.activeColorTexture;

            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            // Notice that we use "AddComputePass" when we are working with compute.
            using (var builder = renderGraph.AddComputePass("ComputePass", out PassData passData))
            {
                // Set the pass data so the data can be transfered from the recording to the execution.
                passData.cs = cs;
                passData.Spheres = sphereBufferHandle;
                passData.Cubes = cubeBufferHandle;
                passData.resultTexture = materialTextureHandle;
                passData.sourceTexture = activeColorTexture;

                passData.smoothing = 1.5f;
                passData.radius = 2;
                // UseBuffer is used to setup render graph dependencies together with read and write flags.
                builder.UseBuffer(passData.Spheres);
                builder.UseBuffer(passData.Cubes);

                builder.UseTexture(passData.resultTexture, AccessFlags.Write);
                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
                // The execution function is also call SetRenderfunc for compute passes.
                builder.SetRenderFunc((PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
            }


            var feature = Instance;
            if (feature == null) return;

            var texture = feature.GetRaymarchTexture();
            if (texture != null)
            {
                material.SetTexture(textureName, texture);
            }
        }

        // ExecutePass is the render function set in the render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, ComputeGraphContext cgContext)
        {
            int kernel = data.cs.FindKernel("CSMain");

            // Attaches the compute buffers.

            CreateScene(data, cgContext);
            SetParameters(data, cgContext);

            cgContext.cmd.SetComputeTextureParam(data.cs, kernel, "Source", data.sourceTexture);

            cgContext.cmd.SetComputeBufferParam(data.cs, kernel, "Spheres", data.Spheres);
            cgContext.cmd.SetComputeBufferParam(data.cs, kernel, "Cubes", data.Cubes);
            cgContext.cmd.SetComputeTextureParam(data.cs, kernel, "Result", data.resultTexture);
            cgContext.cmd.SetComputeFloatParam(data.cs, "multiplier", 5);

            int threadGroupsX = Mathf.CeilToInt(Camera.main.pixelWidth / 32.0f);
            int threadGroupsY = Mathf.CeilToInt(Camera.main.pixelHeight / 32.0f);

            // Dispaches the compute shader with a given kernel as entrypoint.
            // The amount of thread groups determine how many groups to execute of the kernel.
            cgContext.cmd.DispatchCompute(data.cs, kernel, threadGroupsX, threadGroupsY, 1);
        }

        static void CreateScene(PassData data, ComputeGraphContext cgContext)
        {
            cgContext.cmd.SetComputeIntParam(data.cs, "_NumSpheres", 20);

            cgContext.cmd.SetComputeFloatParam(data.cs, "smoothing", data.smoothing);
            cgContext.cmd.SetComputeFloatParam(data.cs,"sRadius", data.radius);
        }

        static void SetParameters(PassData data, ComputeGraphContext cgContext)
        {
            cgContext.cmd.SetComputeMatrixParam(data.cs,"_CameraToWorld", Camera.main.cameraToWorldMatrix);
            cgContext.cmd.SetComputeMatrixParam(data.cs, "_CameraInverseProjection", Camera.main.projectionMatrix.inverse);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
            sphereBuffer.Dispose();
            cubeBuffer.Dispose();
        }
    }


    [SerializeField]
    ComputeShader computeShader;
    ComputePass m_ComputePass;
    
    [SerializeField]
    Material material;

    /// <inheritdoc/>
    public override void Create()
    {
        // Initialize the compute pass.
        m_ComputePass = new ComputePass(material);
        // Sets the renderer feature to execute before rendering.
        m_ComputePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        
        Instance = this;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Check if the system support compute shaders, if not make an early exit.
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("Device does not support compute shaders. The pass will be skipped.");
            return;
        }
        // Skip the render pass if the compute shader is null.
        if (computeShader == null)
        {
            Debug.LogWarning("The compute shader is null. The pass will be skipped.");
            return;
        }
        // Call Setup on the render pass and transfer the compute shader.
        m_ComputePass.Setup(computeShader);
        // Enqueue the compute pass.
        renderer.EnqueuePass(m_ComputePass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    public RTHandle GetRaymarchTexture()
    {
        return m_ComputePass?.RayMarchTexture;
    }
}

struct Sphere {
    public Vector3 pos;
    public uint isSolid;
}

struct Cube
{
    public Vector3 position;
    public Vector3 bounds;
    public Vector3 color;
}

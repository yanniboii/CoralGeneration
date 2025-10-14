using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class Raymarcher : ScriptableRendererFeature
{
    [SerializeField] private ComputeShader rayMarcherComputeShader;
    [SerializeField] private DLAMaster m_DLAMaster;

    [Range(-0.5f, 0.5f)]
    [SerializeField] private float smoothing;

    [SerializeField] private float radius;

    class ComputePass : ScriptableRenderPass
    {
        public GraphicsBuffer outputBuffer;
        private ComputeShader rayMarcherComputeShader;
        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class PassData
        {
            public BufferHandle output;
            public RenderTexture renderTexture;
            public ComputeShader rayMarcherComputeShader;
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecutePass(PassData data, ComputeGraphContext context)
        {
        }

        public ComputePass(ComputeShader computeShader)
        {
            // Create the output buffer as a structured buffer
            // Create the buffer with a length of 5 integers, so the compute shader can output 5 values.
            outputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 5, sizeof(int));
            rayMarcherComputeShader = computeShader;
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Raymarcher";

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddComputePass<PassData>(passName, out var passData))
            {
                BufferHandle outputHandleRG = renderGraph.ImportBuffer(outputBuffer);
                passData.output = outputHandleRG;

                builder.UseBuffer(passData.output, AccessFlags.Write);

                passData.rayMarcherComputeShader = rayMarcherComputeShader;

                // Use this scope to set the required inputs and outputs of the pass and to
                // setup the passData with the required properties needed at pass execution time.

                // Make use of frameData to access resources and camera data through the dedicated containers.
                // Eg:
                // UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();


                // Setup pass inputs and outputs through the builder interface.
                // Eg:
                // builder.UseTexture(sourceTexture);
                // TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, cameraData.cameraTargetDescriptor, "Destination Texture", false);

                // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                //builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                builder.SetRenderFunc((PassData data, ComputeGraphContext context) =>
                {
                    // The first parameter is the compute shader
                    // The second parameter is the function that uses the buffer
                    // The third parameter is the StructuredBuffer output variable to attach the buffer to
                    // The fourth parameter is the handle to the output buffer
                    context.cmd.SetComputeBufferParam(passData.rayMarcherComputeShader, passData.rayMarcherComputeShader.FindKernel("CSMain"), "outputData", passData.output);
                    context.cmd.DispatchCompute(passData.rayMarcherComputeShader, passData.rayMarcherComputeShader.FindKernel("CSMain"), 1, 1, 1);

                    // Create an array to store the output data
                    int[] outputData = new int[5];

                    // Copy the output data from the output buffer to the array
                    outputBuffer.GetData(outputData);



                    ExecutePass(data, context);
                });
            }
        }

        // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

        }

        // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }

        // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {

        }
    }

    ComputePass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new ComputePass(rayMarcherComputeShader);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}

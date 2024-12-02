using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class PathTracingRenderPassData
{
    public TextureHandle outputTexture;
};

public partial class MiraiRayTracer : RenderPipeline
{
    public RayTracingShader rayTracingShader = null;

    public Cubemap envTexture = null;

    [Range(1, 100)]
    public uint bounceCountOpaque = 5;

    [Range(1, 100)]
    public uint bounceCountTransparent = 8;

    uint cameraWidth = 0;
    uint cameraHeight = 0;

    int convergenceStep = 0;

    Matrix4x4 prevCameraMatrix;
    uint prevBounceCountOpaque = 0;
    uint prevBounceCountTransparent = 0;

    RenderTexture rayTracingOutput = null;

    RayTracingAccelerationStructure accelStructure = null;

    void CreateAccelStructure()
    {
        if (accelStructure == null)
        {
            var settings = new RayTracingAccelerationStructure.RASSettings(
                RayTracingAccelerationStructure.ManagementMode.Manual,
                RayTracingAccelerationStructure.RayTracingModeMask.Everything,
                255
            );

            accelStructure = new RayTracingAccelerationStructure(settings);
        }
    }

    void ReleaseResources()
    {
        if (accelStructure != null)
        {
            accelStructure.Release();
            accelStructure = null;
        }

        if (rayTracingOutput != null)
        {
            rayTracingOutput.Release();
            rayTracingOutput = null;
        }

        cameraWidth = 0;
        cameraHeight = 0;
    }

    void CreateResources()
    {
        CreateAccelStructure();

        if(camera == null)
        {
            Debug.Log("nullptr");
        }

        if (cameraWidth != camera.pixelWidth || cameraHeight != camera.pixelHeight)
        {
            if (rayTracingOutput)
                rayTracingOutput.Release();

            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor()
            {
                dimension = TextureDimension.Tex2D,
                width = camera.pixelWidth,
                height = camera.pixelHeight,
                depthBufferBits = 0,
                volumeDepth = 1,
                msaaSamples = 1,
                graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat,
                enableRandomWrite = true,
            };

            rayTracingOutput = new RenderTexture(rtDesc);
            rayTracingOutput.Create();

            cameraWidth = (uint)camera.pixelWidth;
            cameraHeight = (uint)camera.pixelHeight;

            convergenceStep = 0;
        }
    }

    bool RenderPathTracing(RTHandle outputRTHandle, RenderGraphParameters renderGraphParams)
    {
        if (rayTracingShader == null || accelStructure == null)
        {

            return false;
        }

        if (prevCameraMatrix != camera.cameraToWorldMatrix)
            convergenceStep = 0;

        if (prevBounceCountOpaque != bounceCountOpaque)
            convergenceStep = 0;

        if (prevBounceCountTransparent != bounceCountTransparent)
            convergenceStep = 0;

        using(renderGraph.RecordAndExecute(renderGraphParams))
        {
            TextureHandle output = renderGraph.ImportTexture(outputRTHandle);

            RenderGraphBuilder builder = renderGraph.AddRenderPass<PathTracingRenderPassData>("Path Tracing Pass", out var passData);

            passData.outputTexture = builder.WriteTexture(output);

            TextureDesc desc = new TextureDesc()
            {
                dimension = TextureDimension.Tex2D,
                width = camera.pixelWidth,
                height = camera.pixelHeight,
                depthBufferBits = 0,
                colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                slices = 1,
                msaaSamples = MSAASamples.None,
                enableRandomWrite = true,
            };
            TextureHandle debugTexture = builder.CreateTransientTexture(desc);

            builder.SetRenderFunc((PathTracingRenderPassData data, RenderGraphContext ctx) =>
            {
                ctx.cmd.BuildRayTracingAccelerationStructure(accelStructure);

                ctx.cmd.SetRayTracingShaderPass(rayTracingShader, "PathTracing");

                ctx.cmd.SetGlobalInt(Shader.PropertyToID("g_BounceCountOpaque"), (int)bounceCountOpaque);
                ctx.cmd.SetGlobalInt(Shader.PropertyToID("g_BounceCountTransparent"), (int)bounceCountTransparent);

                // Input
                ctx.cmd.SetRayTracingAccelerationStructure(rayTracingShader, Shader.PropertyToID("g_AccelStruct"), accelStructure);
                ctx.cmd.SetRayTracingFloatParam(rayTracingShader, Shader.PropertyToID("g_Zoom"), Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f));
                ctx.cmd.SetRayTracingFloatParam(rayTracingShader, Shader.PropertyToID("g_AspectRatio"), cameraWidth / (float)cameraHeight);
                ctx.cmd.SetRayTracingIntParam(rayTracingShader, Shader.PropertyToID("g_ConvergenceStep"), convergenceStep);
                ctx.cmd.SetRayTracingIntParam(rayTracingShader, Shader.PropertyToID("g_FrameIndex"), Time.frameCount);
                ctx.cmd.SetRayTracingTextureParam(rayTracingShader, Shader.PropertyToID("g_EnvTex"), envTexture);

                // Output
                ctx.cmd.SetRayTracingTextureParam(rayTracingShader, Shader.PropertyToID("g_Radiance"), rayTracingOutput);

                ctx.cmd.DispatchRays(rayTracingShader, "MainRayGenShader", (uint)cameraWidth, (uint)cameraHeight, 1, camera);

                convergenceStep++;
            });
        }

        prevCameraMatrix = camera.cameraToWorldMatrix;
        prevBounceCountOpaque = bounceCountOpaque;
        prevBounceCountTransparent = bounceCountTransparent;

        return true;
    }
}
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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

        if (cameraWidth != Camera.main.pixelWidth || cameraHeight != Camera.main.pixelHeight)
        {
            if (rayTracingOutput)
                rayTracingOutput.Release();

            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor()
            {
                dimension = TextureDimension.Tex2D,
                width = Camera.main.pixelWidth,
                height = Camera.main.pixelHeight,
                depthBufferBits = 0,
                volumeDepth = 1,
                msaaSamples = 1,
                vrUsage = VRTextureUsage.OneEye,
                graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat,
                enableRandomWrite = true,
            };

            rayTracingOutput = new RenderTexture(rtDesc);
            rayTracingOutput.Create();

            cameraWidth = (uint)Camera.main.pixelWidth;
            cameraHeight = (uint)Camera.main.pixelHeight;

            convergenceStep = 0;
        }
    }

    bool RenderPathTracing()
    {
        if (rayTracingShader == null || accelStructure == null)
        {

            return false;
        }

        if (prevCameraMatrix != Camera.main.cameraToWorldMatrix)
            convergenceStep = 0;

        if (prevBounceCountOpaque != bounceCountOpaque)
            convergenceStep = 0;

        if (prevBounceCountTransparent != bounceCountTransparent)
            convergenceStep = 0;

        // Not really needed per frame if the scene is static.
        accelStructure.Build();

        rayTracingShader.SetShaderPass("PathTracing");

        Shader.SetGlobalInt(Shader.PropertyToID("g_BounceCountOpaque"), (int)bounceCountOpaque);
        Shader.SetGlobalInt(Shader.PropertyToID("g_BounceCountTransparent"), (int)bounceCountTransparent);

        // Input
        rayTracingShader.SetAccelerationStructure(Shader.PropertyToID("g_AccelStruct"), accelStructure);
        rayTracingShader.SetFloat(Shader.PropertyToID("g_Zoom"), Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f));
        rayTracingShader.SetFloat(Shader.PropertyToID("g_AspectRatio"), cameraWidth / (float)cameraHeight);
        rayTracingShader.SetInt(Shader.PropertyToID("g_ConvergenceStep"), convergenceStep);
        rayTracingShader.SetInt(Shader.PropertyToID("g_FrameIndex"), Time.frameCount);
        rayTracingShader.SetTexture(Shader.PropertyToID("g_EnvTex"), envTexture);

        // Output
        rayTracingShader.SetTexture(Shader.PropertyToID("g_Radiance"), rayTracingOutput);

        rayTracingShader.Dispatch("MainRayGenShader", (int)cameraWidth, (int)cameraHeight, 1, Camera.main);

        convergenceStep++;

        prevCameraMatrix = Camera.main.cameraToWorldMatrix;
        prevBounceCountOpaque = bounceCountOpaque;
        prevBounceCountTransparent = bounceCountTransparent;

        return true;
    }
}
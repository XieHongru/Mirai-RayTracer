using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using static Unity.Burst.Intrinsics.X86.Avx;
using UnityEngine.Rendering.Universal;

public partial class MiraiRayTracer : RenderPipeline
{
    ScriptableRenderContext context;
    Camera camera;

    public void RenderCamera(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams)) return;
        CullingResults cullingResults = context.Cull(ref cullingParams);

        context.SetupCameraProperties(camera);

        CreateResources();

        CommandBuffer buffer = new CommandBuffer { name = "Mirai RayTracer" };

        var renderGraphParams = new RenderGraphParameters()
        {
            executionName = "Render Graph",
            scriptableRenderContext = context,
            commandBuffer = buffer,
            currentFrameIndex = convergenceStep
        };

        ProfilingSampler cameraSampler = new ProfilingSampler("Camera Render");
        RTHandle outputRTHandle = rtHandleSystem.Alloc(rayTracingOutput, "_Output");

        //context.DrawSkybox(camera);

        switch (asset.renderType)
        {
            case RenderType.PATH_TRACING:
                if(RenderPathTracing(outputRTHandle, renderGraphParams))
                {
                    buffer.Blit(rayTracingOutput, camera.activeTexture);
                }
                else
                {
                    buffer.ClearRenderTarget(false, true, Color.black);
                    Debug.Log("Error occurred when Path Tracing!");
                }
                break;
        }
        outputRTHandle.Release();
        context.ExecuteCommandBuffer(buffer);
        buffer.Release();

        context.Submit();
        renderGraph.EndFrame();
    }
}

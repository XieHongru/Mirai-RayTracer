using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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

        context.DrawSkybox(camera);

        switch (asset.renderType)
        {
            case RenderType.PATH_TRACING:
                RenderPathTracing();
                break;
        }

        context.ExecuteCommandBuffer(buffer);
        buffer.Release();

        context.Submit();
    }
}

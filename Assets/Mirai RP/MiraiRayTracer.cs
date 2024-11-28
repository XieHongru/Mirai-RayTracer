using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public partial class MiraiRayTracer : RenderPipeline
{
    MiraiRayTracerAsset asset = null;
    MiraiCamera miraiCamera = new MiraiCamera();

    public MiraiRayTracer(MiraiRayTracerAsset asset)
    {
        this.asset = asset;
        //this.rayTracingShader = asset.rayGeneratorShader;
        //this.envTexture = asset.envTex;
    }

    // The entrance of rendering
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);
            RenderCamera(context, camera);
            EndCameraRendering(context, camera);
        }
    }
}

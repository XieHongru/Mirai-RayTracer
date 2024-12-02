using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum RenderType
{
    PATH_TRACING
};

[CreateAssetMenu(menuName = "Rendering/Mirai Ray Trace Pipeline")]
public class MiraiRayTracerAsset : RenderPipelineAsset
{
    public RenderType renderType = RenderType.PATH_TRACING;
    public RayTracingShader rayGeneratorShader = null;
    public Cubemap envTex = null;

    protected override RenderPipeline CreatePipeline()
    {
        return new MiraiRayTracer(this);
    }
}

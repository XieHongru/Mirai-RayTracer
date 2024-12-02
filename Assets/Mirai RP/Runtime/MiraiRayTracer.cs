using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public partial class MiraiRayTracer : RenderPipeline
{
    MiraiRayTracerAsset asset = null;
    MiraiCamera miraiCamera = new MiraiCamera();
    RenderGraph renderGraph = null;
    RTHandleSystem rtHandleSystem = null;

    public MiraiRayTracer(MiraiRayTracerAsset asset)
    {
        this.asset = asset;
        this.rayTracingShader = asset.rayGeneratorShader;
        this.envTexture = asset.envTex;

        var settings = new RayTracingAccelerationStructure.RASSettings(
            RayTracingAccelerationStructure.ManagementMode.Manual,
            RayTracingAccelerationStructure.RayTracingModeMask.Everything,
            255
        );

        accelStructure = new RayTracingAccelerationStructure(settings);

        renderGraph = new RenderGraph("Ray Tracing Render Graph");

        rtHandleSystem = new RTHandleSystem();
    }

    private void CullInstance()
    {
        var instanceCullingTest = new RayTracingInstanceCullingTest()
        {
            allowOpaqueMaterials = true,
            allowTransparentMaterials = false,
            allowAlphaTestedMaterials = true,
            layerMask = -1,
            shadowCastingModeMask = (1 << (int)ShadowCastingMode.Off)
            | (1 << (int)ShadowCastingMode.On)
            | (1 << (int)ShadowCastingMode.TwoSided),
            instanceMask = 1 << 0,
        };

        var instanceCullingTests = new List<RayTracingInstanceCullingTest>() { instanceCullingTest };

        var cullingConfig = new RayTracingInstanceCullingConfig()
        {
            flags = RayTracingInstanceCullingFlags.None,
            subMeshFlagsConfig = new RayTracingSubMeshFlagsConfig()
            {
                opaqueMaterials = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly,
                transparentMaterials = RayTracingSubMeshFlags.Disabled,
                alphaTestedMaterials = RayTracingSubMeshFlags.Enabled,
            },
            instanceTests = instanceCullingTests.ToArray(),
        };

        accelStructure.ClearInstances();
        accelStructure.CullInstances(ref cullingConfig);
    }

    // The entrance of rendering
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        CullInstance();

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);
            RenderCamera(context, camera);
            EndCameraRendering(context, camera);
        }
    }
}

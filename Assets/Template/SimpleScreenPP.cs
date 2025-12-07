using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SimpleScreenPP : ScriptableRendererFeature
{
    public Material TestM;
    [System.Serializable]       // 类的序列化：方便传输、存储、读取该类
    public class Settings       // RenderFeature面板中P  ass参数设置----新建个类，方便管理
    {        
        public RenderPassEvent my_RenderPassEvent;      // 设置Pass渲染的位置-初始值

        //材质
        public Material Mat;

    }

    public Settings settings = new Settings();      //新建设置

    class CustomTVPass : ScriptableRenderPass
{
    public Material m_Mat;
    private RenderTargetHandle m_Destination;
    private static readonly int mainTexID = Shader.PropertyToID("_BaseMap");
    private static readonly string cmdName = "ScreenEffectRTHandle";

    public CustomTVPass(RenderPassEvent evt)
    {
        this.renderPassEvent = evt;
        m_Destination.Init("_TemporaryColorTexture");
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // 在 Configure 中获取临时RT
        var descriptor = cameraTextureDescriptor;
        descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
        descriptor.depthBufferBits = 0;
        
        cmd.GetTemporaryRT(m_Destination.id, descriptor);
        ConfigureTarget(m_Destination.Identifier());
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {   
        if (m_Mat == null)
        {
            Debug.LogError("ScreenEffect Material is null!");
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(cmdName);

        RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

        // 设置全局纹理
        cmd.SetGlobalTexture(mainTexID, source);

        // 执行Blit操作
        cmd.Blit(source, m_Destination.Identifier(), m_Mat, 0);
        cmd.Blit(m_Destination.Identifier(), source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        // 在 FrameCleanup 中释放临时RT
        if (cmd != null)
        {
            cmd.ReleaseTemporaryRT(m_Destination.id);
        }
    }
}

    CustomTVPass m_TVPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_TVPass = new CustomTVPass(settings.my_RenderPassEvent);
        m_TVPass.m_Mat = settings.Mat;

    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //if(renderingData.cameraData.cameraType == CameraType.Game){
            renderer.EnqueuePass(m_TVPass);
        //}
        
    }
}
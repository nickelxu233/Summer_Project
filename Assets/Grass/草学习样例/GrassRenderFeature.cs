using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 定义自定义渲染器特性类，继承自ScriptableRendererFeature
//ScriptableRendererFeature 是 URP（Universal Render Pipeline）可编程渲染管线的核心扩展点。
public class GrassRenderFeature : ScriptableRendererFeature
{
    // 定义内部渲染Pass类，继承自ScriptableRenderPass
    class CustomRenderPass : ScriptableRenderPass
    {
        public CustomRenderPass(){
            this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }
        private const string NameOfCommandBuffer = "Grass";
        // 相机设置回调：在相机渲染开始前调用，用于配置渲染目标等
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 这里可以设置渲染目标、清除状态等
        }

        // 执行渲染Pass：主要的渲染逻辑在这里实现
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 这里编写具体的渲染命令，如绘制网格、设置材质等

            var cmd = CommandBufferPool.Get(NameOfCommandBuffer);
            try{
                cmd.Clear();               
                // foreach(var grassTerrian in GrassTerrian.actives){
                //     if(!grassTerrian){
                //         continue;
                //     }
                //     if(!grassTerrian.material){
                //         continue;
                //     }
                //     grassTerrian.UpdateMaterialProperties();
                //     cmd.DrawMeshInstancedProcedural(GrassUtil.unitMesh,0,grassTerrian.material,0,grassTerrian.grassCount,grassTerrian.materialPropertyBlock);
                //     index ++;
                // } 
                context.ExecuteCommandBuffer(cmd);
            }finally{
                cmd.Release();
            }
        }

        // 相机清理回调：在相机渲染结束后调用，用于释放资源
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // 这里可以释放临时资源、重置状态等
        }
    }

    // 声明自定义渲染Pass的实例变量
    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    // 创建方法：在渲染器特性初始化时调用，用于创建和配置渲染Pass
    public override void Create()
    {
        // 实例化自定义渲染Pass
        //在上面不是创建了一个新的pass class吗，需要把它调用到feature里。
        m_ScriptablePass = new CustomRenderPass();

        // 配置渲染Pass的注入时机：在不透明物体渲染之后执行
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // 添加渲染Pass方法：在设置渲染器时每帧每相机调用
    // 这里可以将一个或多个渲染Pass注入到渲染器中
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        //只给主相机渲染草
        if(cameraData.renderType == CameraRenderType.Base){
            renderer.EnqueuePass(m_ScriptablePass); 
        }
        // 将自定义渲染Pass加入到渲染器的执行队列中。EnqueuePass是加入队列的指令，render就是继承的urp渲染器，管理整个渲染流程
        
    }
}
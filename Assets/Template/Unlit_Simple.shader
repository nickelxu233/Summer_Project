Shader "Custom/URPUnlitTemplate_Simple"
{
    Properties
    {
        // 基础属性
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Texture", 2D) = "white" {}
        
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 包含URP核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 顶点输入结构
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            // 顶点输出结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;

            };

            // 声明属性变量
            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END

            // 顶点着色器
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionCS = input.positionOS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            // 片段着色器
            half4 frag(Varyings input) : SV_Target
            {
                // 采样纹理
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                half4 color = baseColor * _BaseColor;
                
                return color;
            }

            
            ENDHLSL
        }

    }
    
}
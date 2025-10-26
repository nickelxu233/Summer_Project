Shader "Unlit/草地"
{
    Properties
    {
        _WindTex ("风力贴图", 2D) = "white" {} // 控制风向和风力的贴图
        _AlphaTex("遮罩贴图", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5 // 光滑度参数
        [Space(30)]
        _TopColor ("顶部颜色", Color) = (1,0.823,0.309,1) // 草顶部颜色
        _CenterColor ("中部颜色", Color) = (0.549,0.776,0.247,1) // 草中部颜色
        _BottomColor ("底部底色", Color) = (0,0.341,0.274,1) // 草底部颜色
        [Space(30)]
        _TopPosition("顶部颜色权重",Range(0,1)) = 1 // 顶部颜色开始的相对位置（0~1）
        _CenterPosition("中部颜色权重",Range(0,1)) = 0.5 // 中部颜色位置
        _RootPosition("根部颜色权重",Range(0,1)) = 0 // 根部颜色位置
        [Space(30)]
        _WindStrength ("风力强度", Float) = 0.5 // 风吹弯的程度
        _WindScale ("风力范围缩放", Range(0.01, 1)) = 0.01 // 风图采样缩放值
        _WindSpeed ("风速", Float) = 1.0 // 风的速度
        _GrassHeightFactor ("草高影响权重", Float) = 1.0 // 草的高度影响风的权重
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Tags
            {
                "LightMode"="UniversalForward"
            }

            // 渲染设置
            ZWrite On // 开启深度写入
            ZTest LEqual // 深度测试小于等于
            Cull off // 不剔除任何面（通常用于双面草）

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            // 引入通用和光照库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 材质常量缓冲区
            CBUFFER_START(UnityPerMaterial)
                float _Metallic;
                float _Smoothness;
                float4 _WindTex_ST;

                float4 _TopColor;
                float4 _CenterColor;
                float4 _BottomColor;

                float _TopPosition;
                float _CenterPosition;
                float _RootPosition;

                float _WindStrength;
                float _WindSpeed;
                float _WindScale;
                float _GrassHeightFactor;
                float _BendStrength;

                // 实例化缓冲区（可用于 GPU Instancing）
                UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_INSTANCING_BUFFER_END(PerInstance)

            CBUFFER_END

            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            // 支持阴影
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            // 顶点输入结构体
            struct Attributes
            {
                float4 positionOS : POSITION; // 模型空间位置
                float3 normalOS : NORMAL; // 模型空间法线
                float2 uv : TEXCOORD0; // UV坐标
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 顶点与片元之间的中间结构体
            struct Varings
            {
                float4 positionCS : SV_POSITION; // 裁剪空间位置
                float2 uv : TEXCOORD0; // 用于风图采样的UV
                float2 uvOriginal : TEXCOORD1; // 原始UV，用于颜色渐变
                float3 positionWS : TEXCOORD2; // 世界空间位置
                float3 normalWS : TEXCOORD3; // 世界空间法线
                float3 viewDirWS : TEXCOORD4; // 世界空间视角方向

                float3 test : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 风力贴图采样器
            TEXTURE2D(_WindTex);
            SAMPLER(sampler_WindTex);
            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);

            // 简单噪声函数（非连续）
            float SimpleNoise(float2 uv, float time)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233)) + time) * 43758.5453);
            }

            // 平滑噪声函数（有连续性）
            float SmoothNoise(float2 uv, float time)
            {
                uv *= 1.0; // 控制频率
                uv += time * 0.5; // 控制动画速度


                float2 i = floor(uv);
                float2 f = frac(uv);

                // 4个角落的hash值（未定义Hash函数，可能需补充）
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));

                // Hermite插值
                float2 u = f * f * (3.0 - 2.0 * f);

                // 双线性插值
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 顶点着色器
            Varings vert(Attributes IN)
            {
                Varings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = normalInput.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                OUT.uv = IN.uv * _WindTex_ST.xy + _WindTex_ST.zw; // 采样用 UV
                OUT.uvOriginal = IN.uv; // 原始UV用于颜色计算

                // 根据高度计算风影响因子
                // float heightFactor = saturate(
                //     (IN.positionOS.y - _RootPosition) / (_CenterPosition + 0.8 - _RootPosition)) * _GrassHeightFactor;
                float heightFactor = saturate(
                    (IN.positionOS.y) / (0.5 + 0.8)) * _GrassHeightFactor; //改了一下，不想让颜色影响风力动态

                // 获取噪声值（用于草的摆动）
                float noise = SmoothNoise(IN.positionOS.xz * 4, _Time.y * _WindSpeed) * 0.2;

                // 采样风力贴图
                float2 windTexUV = vertexInput.positionWS.xz * _WindTex_ST.xy + _WindTex_ST.zw + _Time.y * _WindSpeed;
                windTexUV = windTexUV.xy * _WindScale;
                windTexUV += SmoothNoise(windTexUV, _WindSpeed); // 叠加噪声扰动
                float4 windTex = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, windTexUV, 0);
                float2 windDirection = windTex.xy * 2 - 1; // 转换到 [-1,1] 范围

                // 计算风偏移
                float windOffset = sin(_Time.y * _WindSpeed + IN.positionOS.z) * _WindStrength * noise;

                float3 positionOS = IN.positionOS;
                positionOS.xz += windDirection * heightFactor * windOffset; // 根据高度进行偏移

                // 转换为裁剪空间
                OUT.positionCS = TransformObjectToHClip(positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(positionOS.xyz);

                OUT.test = windOffset;

                return OUT;
            }

            // 片元着色器
            half4 frag(Varings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // 获取主光源
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS.xyz);
                Light light = GetMainLight(shadowCoord);
                float3 lightDirWS = light.direction;
                float3 lightColor = light.color;

                // 漫反射光照计算
                float3 normal = normalize(IN.normalWS);
                float NdotL = saturate(dot(normal, lightDirWS));
                half3 diffuse = lightColor * NdotL;
                diffuse = diffuse * 0.5 + 0.5; // 提亮草的颜色

                // 环境光
                float3 ambientColor = SampleSH(IN.normalWS);

                // 根据高度混合颜色（底 → 中 → 顶）
                float3 baseColor = lerp(_CenterColor, _TopColor, saturate((IN.uvOriginal.y - _CenterPosition) / (_TopPosition - _CenterPosition)));
                baseColor = lerp(_BottomColor, baseColor, saturate((IN.uvOriginal.y - _RootPosition) / (_CenterPosition - _RootPosition)));

                // 获取阴影衰减
                float shadow = light.shadowAttenuation;

                // 最终颜色 = 颜色 * 光照 * 阴影
                float3 finalColor = baseColor * (diffuse + ambientColor) * max(shadow, 0.5);

                float final_alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.uv).r;
                if(final_alpha<0.5)
                    discard;

                return float4(finalColor, 1.0); // 输出颜色
            }
            ENDHLSL
        }

        // 阴影投射通道
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0 // 不输出颜色
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
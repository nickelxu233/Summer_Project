Shader "GPUInstanceGrass_01"
{
    Properties //着色器的输入 
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _NoiseMap ("Noise Map", 2D) = "white" {}
        _BaseColorTop("BaseColor Top", Color) = (1,1,1,1)
        _BaseColorBottom ("BaseColor Bottom", Color) = (1,1,1,1)
        _BaseColorThresholed("BaseColor Thresholed", float) = 0.5

        _WaveSizeX("WaveSizeX", float) = 1
        _WaveSizeY("WaveSizeY", float) = 1
        _WaveSpeedX("WaveSpeedX", float) = 0.5
        _WaveSpeedY("WaveSpeedY", float) = 0.5
        _WaveStrength("WaveStrength", float) = 2
    }
    SubShader
    {
        Tags {
            "RenderType"="Opaque"
            "RenderPipeLine"="UniversalRenderPipeline" 
        }
        

        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //第一步：增加 GPU Instancing 变体开关
            #pragma multi_compile_instancing 
            #pragma instancing_options procedural:setup //过程式实例化，instance数据将通过一个名为setup的函数进行自定义设置（可选）

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //声明实例化材质的参数
            UNITY_INSTANCING_BUFFER_START(Props)
            
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)

            UNITY_INSTANCING_BUFFER_END(Props)

            float4 _BaseMap_ST;
            half4 _BaseColorTop;
            half4 _BaseColorBottom;
            half _BaseColorThresholed;
            half _WaveSizeX;
            half _WaveSizeY;
            half _WaveSpeedX;
            half _WaveSpeedY;
            half _WaveStrength;

            TEXTURE2D(_BaseMap); 
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap); 
            SAMPLER(sampler_NoiseMap);

            struct a2v 
            {
                float4 positionOS: POSITION;
                float3 normal: NORMAL;
                float2 uv : TEXCOORD0;

                //第二步：instanceID 加入顶点着色器输入结构
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2f 
            {
                float4 positionCS: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 worldNormal:TEXCOORD1;
                float3 worldPos: TEXCOORD2;
                half3 waveColor: TEXCOORD3;
                
                //第三步：instanceID 加入顶点着色器输出结构
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            }; 

            v2f vert (a2v v)
            {
                v2f o;

                //第四步：instanceid在顶点的相关设置
                UNITY_SETUP_INSTANCE_ID(v);         //设置当前实例ID
                //第五步：传递instanceid 从顶点到片元
                UNITY_TRANSFER_INSTANCE_ID(v,o);    //传递实例ID到输出结构

                
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.positionOS).xyz;
                

                //采样噪声贴图
                float2 sampleUV = float2(o.worldPos.x / _WaveSizeX, o.worldPos.z / _WaveSizeY);  //用世界坐标作为UV
                sampleUV.x += _Time.x * _WaveSpeedX;           //
                sampleUV.y += _Time.x * _WaveSpeedY;           //
                float3 waveSample = SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, sampleUV, 0).xyz;
                
                
                o.worldPos.xz += sin(waveSample * _WaveSpeedX) * o.uv.y * _WaveStrength;
                //o.worldPos.z += sin(waveSample * _WaveSpeedY) * o.uv.y * _WaveStrength;

                o.waveColor = waveSample;

                o.positionCS = TransformWorldToHClip(o.worldPos);
                return o;
            }

            half4 frag (v2f i) : SV_Target 
            {
                
                //第六步：instanceid在片元的相关设置
                UNITY_SETUP_INSTANCE_ID(i);         //设置当前实例ID

                //Normalize
                half3 worldNormal=normalize(i.worldNormal);
                half3 viewDir= normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                half3 colorTop = _BaseColorTop;
                half3 colorBottom = _BaseColorBottom;
                half3 albedo = lerp(colorBottom,colorTop,i.uv.y+_BaseColorThresholed);

                return half4(albedo,1.0);

                //return UNITY_ACCESS_INSTANCED_PROP(Props, _Color);  //调用实例化ID中的参数（这里是颜色）
            }
            ENDHLSL
        }


        
    }
}

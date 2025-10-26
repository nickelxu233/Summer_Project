Shader "Study/CreateCube_3"
{
    Properties //着色器的输入 
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {
            "RenderType"="Opaque"
            "RenderPipeLine"="UniversalRenderPipeline" 
        }
        

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //第一步：增加 GPU Instancing 变体开关
            #pragma multi_compile_instancing 
            #pragma instancing_options procedural:setup //过程式实例化，instance数据将通过一个名为setup的函数进行自定义设置（可选）

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //声明实例化材质的参数
            // UNITY_INSTANCING_BUFFER_START(Props)
            //     UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            // UNITY_INSTANCING_BUFFER_END(Props)

            CBUFFER_START(UnityPerMaterial) //比较有意思的是urp力这个cbuffer，如果将参数放入其中并且使用了这个参数，GPU instance会无法正确合批，请注意。
                                            //如果有不需要放入UNITY_INSTANCING_BUFFER的，也注意不要放在这个里面，直接扔外面就好
                
            CBUFFER_END

            float4 _BaseMap_ST;
            half4 _BaseColor;

            TEXTURE2D(_BaseMap); 
            SAMPLER(sampler_BaseMap);

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
                
                //第三步：instanceID 加入顶点着色器输出结构
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            }; 

            struct GrassInfo{
                    float4x4 localToTerrian;
                    float4 texParams;
                };
            StructuredBuffer<GrassInfo> _GrassInfos;

            v2f vert (a2v v)
            {
                v2f o;

                //第四步：instanceid在顶点的相关设置
                UNITY_SETUP_INSTANCE_ID(v);         //设置当前实例ID
                //第五步：传递instanceid 从顶点到片元
                UNITY_TRANSFER_INSTANCE_ID(v,o);    //传递实例ID到输出结构

                #ifdef INSTANCING_ON
                    uint instanceID = v.instanceID;
                    GrassInfo grassInfo = _GrassInfos[instanceID];
                    float3 positionOS = v.positionOS;
                    positionOS = mul(grassInfo.localToTerrian,float4(positionOS,1)).xyz;
                #endif

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.positionOS).xyz;
                

                //GrassInfo grassInfo = _GrassInfos[instanceID];
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
                //return half4(col.xyz,1.0);

                #ifdef INSTANCING_ON
                    return float4(0,0,0,1);
                #endif

                return _BaseColor;  //调用实例化ID中的参数（这里是颜色）
            }
            ENDHLSL
        }


        
    }
}

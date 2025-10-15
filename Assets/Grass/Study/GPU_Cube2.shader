Shader "Study/CreateCube_2"
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
            UNITY_INSTANCING_BUFFER_START(Props)
            
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Phi)

            UNITY_INSTANCING_BUFFER_END(Props)

            float4 _BaseMap_ST;

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

            v2f vert (a2v v)
            {
                v2f o;

                //第四步：instanceid在顶点的相关设置
                UNITY_SETUP_INSTANCE_ID(v);         //设置当前实例ID
                //第五步：传递instanceid 从顶点到片元
                UNITY_TRANSFER_INSTANCE_ID(v,o);    //传递实例ID到输出结构

                //顶点偏移
                float phi = UNITY_ACCESS_INSTANCED_PROP(Props, _Phi);
                v.positionOS = v.positionOS + sin(_Time.y + phi);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.positionOS).xyz;
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

                return UNITY_ACCESS_INSTANCED_PROP(Props, _Color);  //调用实例化ID中的参数（这里是颜色）
            }
            ENDHLSL
        }


        
    }
}

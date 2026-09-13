// ShaderLab — язык описания шейдера Unity.
// HLSL — язык программирования для GPU.
Shader "Course/01 Position Color"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass
        {
            Name "ForwardOnly"
            Cull Back
            ZWrite On
            Tags
            {
                "LightMode" = "ForwardOnly"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionRWS = TransformObjectToWorld(input.positionOS.xyz);

                output.positionWS = GetAbsolutePositionWS(positionRWS);

                output.positionHCS = TransformWorldToHClip(positionRWS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (input.positionWS.y > 0)
                    return half4(1, 0, 0, 1);

                return half4(0, 0, 1, 1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Cull Back
            ZWrite On
            ColorMask 0
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}

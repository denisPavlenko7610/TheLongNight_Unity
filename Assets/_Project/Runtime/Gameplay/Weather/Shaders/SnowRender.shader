Shader "TLN/VFX/SnowParticle"
{
    Properties
    {
        _Color("Tint", Color) = (1, 1, 1, 1)
        _Softness("Softness", Range(0, 1)) = 0.5
        _SparkleIntensity("Sparkle", Range(0, 1)) = 0.3
        _DepthFade("Depth Fade", Range(0, 1)) = 0.4
        _FadeDistance("Fade Distance", Range(10, 120)) = 45
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent"
        }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "Forward"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Particle
            {
                float3 position;
                float seed;
                float3 velocity;
                float size;
            };

            StructuredBuffer<Particle> _SnowParticles;

            float4 _Color;
            float _Softness, _SparkleIntensity, _DepthFade, _FadeDistance, _TimeGlobal;

            float Hash(float s)
            {
                return frac(sin(s) * 43758.5453);
            }

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float alpha : TEXCOORD1;
                float seed : TEXCOORD2;
                float depth : TEXCOORD3;
            };

            float SnowflakeShape(float2 uv)
            {
                float dist = length(uv);
                float circle = 1.0 - smoothstep(0.6, 1.0, dist);

                float angle = atan2(uv.y, uv.x);
                float armPattern = cos(angle * 6.0);
                float armRadial = 1.0 - smoothstep(0.0, 0.7, dist);
                float arms = max(0, armPattern) * armRadial;

                return saturate(circle * 0.5 + arms * 0.5);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                uint id = input.instanceID;

                Particle particle;
                if (id < 0xFFFFFFFF) particle = _SnowParticles[id];
                else particle = (Particle)0;

                float3 particleRWS = GetCameraRelativePositionWS(particle.position);
                float3 toCamera = _WorldSpaceCameraPos.xyz - particle.position;
                float distanceSqr = max(dot(toCamera, toCamera), 0.0001);

                float4x4 viewMatrix = GetWorldToViewMatrix();
                float3 right = normalize(viewMatrix[0].xyz);
                float3 up = normalize(viewMatrix[1].xyz);

                float halfSize = particle.size * 0.5;
                float3 worldPos = particleRWS + right * input.vertex.x * halfSize + up * input.vertex.y * halfSize;

                output.positionCS = TransformWorldToHClip(worldPos);

                float rot = Hash(particle.seed + 10.0) * 6.2832;
                float cr = cos(rot), sr = sin(rot);
                float2 uvCentered = input.uv - 0.5;
                output.uv = float2(
                    uvCentered.x * cr - uvCentered.y * sr,
                    uvCentered.x * sr + uvCentered.y * cr
                ) + 0.5;

                output.seed = particle.seed;

                float distanceToCamera = sqrt(distanceSqr);
                float distFade = saturate(1.0 - distanceToCamera / max(_FadeDistance, 1.0));
                output.alpha = saturate(distFade * (0.6 + Hash(particle.seed) * 0.4));
                output.depth = distanceToCamera;

                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 uvCentered = input.uv * 2.0 - 1.0;
                float shape = SnowflakeShape(uvCentered);
                float softEdge = smoothstep(0.0, _Softness, shape);

                float3 color = _Color.rgb * (0.5 + 0.5 * softEdge);

                if (_SparkleIntensity > 0)
                {
                    float twinkle = sin(_TimeGlobal * (1.5 + Hash(input.seed + 0.5) * 3.0) + Hash(input.seed + 1.0) * 6.28) * 0.5 + 0.5;
                    float centerBright = 1.0 - length(uvCentered);
                    float sparkle = twinkle * Hash(input.seed) * _SparkleIntensity * saturate(centerBright * 3.0);
                    color += sparkle * 0.8;
                }

                float alpha = softEdge * input.alpha;
                float depthFade = saturate(1.0 - input.depth / max(_FadeDistance * 1.6, 1.0));
                alpha *= lerp(1.0, depthFade, _DepthFade);

                return float4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}

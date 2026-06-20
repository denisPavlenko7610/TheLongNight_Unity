Shader "TLN/VFX/SnowParticle"
{
	Properties
	{
		_Color("Tint", Color) = (1, 1, 1, 1)
		_Softness("Softness", Range(0, 1)) = 0.5
		_SparkleIntensity("Sparkle", Range(0, 1)) = 0.3
		_SparkleFrequency("Sparkle Frequency", Range(1, 10)) = 3
		_DepthFade("Depth Fade", Range(0, 1)) = 0.4
		_FadeDistance("Fade Distance", Range(10, 120)) = 45
	}

	SubShader
	{
		Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Name "Forward"
			Tags { "LightMode" = "Forward" }

			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment
			#pragma target 4.5
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

			struct Particle { float3 position; float seed; float3 velocity; float size; };
			StructuredBuffer<Particle> _SnowParticles;

			float4 _Color;
			float _Softness, _SparkleIntensity, _SparkleFrequency, _DepthFade, _FadeDistance, _TimeGlobal;

			float Random(float s) { return frac(sin(s) * 43758.5453); }
			float RandomRange(float s, float minVal, float maxVal) { return minVal + Random(s) * (maxVal - minVal); }

			struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; uint instanceID : SV_InstanceID; };
			struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float alpha : TEXCOORD1; float seed : TEXCOORD2; float depth : TEXCOORD3; };

			// Procedural snowflake shape: soft circle + 6 crystal arms
			float SnowflakeShape(float2 uv, float seed)
			{
				float dist = length(uv);
				float circle = 1.0 - smoothstep(0.85, 1.05, dist);

				float arms = 0;
				for (int i = 0; i < 6; i++)
				{
					float angle = i * 1.0472 + Random(seed + i * 0.01) * 0.2;
					float2 dir = float2(cos(angle), sin(angle));
					float projection = dot(uv, dir);
					float armWidth = 0.08 + Random(seed + i * 0.02) * 0.06;
					float armLength = projection / (0.7 + Random(seed + i * 0.03) * 0.4);
					float arm = smoothstep(-armWidth, 0, projection) * smoothstep(armLength, armLength - 0.1, projection);
					arm *= smoothstep(1.0, 0.7, dist);
					arms = max(arms, arm);
				}

				float detail = 0;
				for (int j = 0; j < 3; j++)
				{
					float dAngle = Random(seed + j * 0.1) * 6.28;
					float2 dDir = float2(cos(dAngle), sin(dAngle));
					float dProj = abs(dot(uv, dDir));
					detail += smoothstep(0.15, 0.05, dProj) * 0.1;
				}

				return saturate(circle * 0.4 + arms * 0.6 + detail);
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
				output.uv = input.uv;
				output.seed = particle.seed;

				float distanceToCamera = sqrt(distanceSqr);
				float distFade = saturate(1.0 - distanceToCamera / max(_FadeDistance, 1.0));
				output.alpha = saturate(distFade * (0.6 + Random(particle.seed) * 0.4));
				output.depth = distanceToCamera;

				return output;
			}

			float4 Fragment(Varyings input) : SV_Target
			{
				float rotation = Random(input.seed + 10.0) * 6.2832;
				float cosRot = cos(rotation);
				float sinRot = sin(rotation);
				float2 rotatedUV = float2(
					(input.uv.x - 0.5) * cosRot - (input.uv.y - 0.5) * sinRot + 0.5,
					(input.uv.x - 0.5) * sinRot + (input.uv.y - 0.5) * cosRot + 0.5);

				float shape = SnowflakeShape(rotatedUV * 2.0 - 1.0, input.seed);

				float softEdge = smoothstep(0.0, _Softness, shape);

				float sparkle = 0;
				if (_SparkleIntensity > 0)
				{
					float phase = 1.5 + Random(input.seed + 0.5) * 3.0;
					float offset = Random(input.seed + 1.0) * 6.28;
					float twinkle = sin(_TimeGlobal * phase + offset) * 0.5 + 0.5;
					float centerBright = 1.0 - length(rotatedUV * 2.0 - 1.0);
					sparkle = twinkle * Random(input.seed) * _SparkleIntensity * saturate(centerBright * 3.0);
				}

				float alpha = softEdge * input.alpha;
				float brightness = 0.5 + 0.5 * softEdge + sparkle * 0.8;
				float3 color = _Color.rgb * brightness;

				float blueShift = Random(input.seed + 2) * 0.12;
				color += float3(-blueShift * 0.25, -blueShift * 0.1, blueShift);

				float depthFade = saturate(1.0 - input.depth / max(_FadeDistance * 1.6, 1.0));
				alpha *= lerp(1.0, depthFade, _DepthFade);

				return float4(color, saturate(alpha));
			}
			ENDHLSL
		}
	}
}

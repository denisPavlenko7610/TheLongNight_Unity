using UnityEngine;

namespace TLN.Infrastructure.Scenes
{
	internal static class ShaderPreloader
	{
		public static void PrewarmSnowShaders()
		{
			ComputeShader computeShader = Resources.Load<ComputeShader>("Shaders/SnowCompute");
			if (computeShader != null)
			{
				computeShader.FindKernel("CSMain");
			}

			Shader renderShader = Shader.Find("TLN/VFX/SnowParticle");
			if (renderShader != null)
			{
				Material material = new Material(renderShader)
				{
					hideFlags = HideFlags.HideAndDontSave
				};
				Object.DestroyImmediate(material);
			}
		}
	}
}

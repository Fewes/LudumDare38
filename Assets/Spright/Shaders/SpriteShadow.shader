Shader "Spright/SpriteShadow"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _FlipUV ("Flip UVs", Vector) = (1,1,1,1)
		[PerRendererData] _Shadows ("Cast Shadows", Float) = 0
		[PerRendererData] _ShadowLength ("Shadow Length", Float) = 0
		[PerRendererData] _ShadowChannel ("Shadow Channel", Vector) = (1,1,1,1)
	}

	SubShader
	{
		Tags
		{
			"RenderType"="Sprite"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		ZWrite Off
		Blend One One

		Pass
		{
			CGPROGRAM
				#pragma target 2.0
				#pragma vertex vert
				#pragma fragment frag
				#include "SpriteLighting.cginc"

				struct VS_INPUT
				{
					float4 vPosition 	: POSITION;
					float2 vTexCoord0 	: TEXCOORD0;
					float4 vColor 		: COLOR;
				};

				struct PS_INPUT
				{
					float4 vPosition 	: SV_POSITION;
					float2 vTexCoord0 	: TEXCOORD0;
					float4 vColor 		: COLOR;
				};

				float4 _FlipUV;
				float _Shadows;
				float _ShadowLength;

				PS_INPUT vert(VS_INPUT i)
				{
					PS_INPUT o;

					// Clip space position
					o.vPosition = UnityObjectToClipPos(i.vPosition) * _Shadows; // Hack >:(
					o.vPosition.xyz += g_tDirectionalDir.xyz * float3(1, -1, 1) * _ShadowLength * 0.1;

					// Texture coordinates
					o.vTexCoord0.xy = i.vTexCoord0.xy;

					// Color
					o.vColor = i.vColor;

					return o;
				}

				sampler2D 	_MainTex;
				float4 		_Color;
				float4		_ShadowChannel;

				float4 frag(PS_INPUT i) : SV_Target
				{
					float4 vColor = 0;

					// Sample texture
					float4 vAlbedoTex = tex2D( _MainTex, i.vTexCoord0.xy );

					vColor.rgba = _Color.rgba * _Color.a * i.vColor.rgba * i.vColor.a * vAlbedoTex.a * _ShadowChannel.rgba;

					return vColor;
				}
			ENDCG
		}
	}
}
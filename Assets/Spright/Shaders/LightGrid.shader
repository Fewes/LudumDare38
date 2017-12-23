Shader "Spright/LightGrid"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color( "Color", Color ) = ( 1, 1, 1, 1 )
	}

	SubShader
	{
		Tags
		{
			"RenderType"="MeshShadow"
		}

		// Cull Back
		Cull Off
		Lighting Off
		ZWrite Off
		// Blend One One

		Pass
		{
			CGPROGRAM
				#pragma target 2.0
				#pragma vertex vert
				#pragma fragment frag

				struct VS_INPUT
				{
					float4 vPosition 	: POSITION;
					float4 vColor 		: COLOR;
					float2 vTexCoord0 	: TEXCOORD0;
				};

				struct PS_INPUT
				{
					float4 vPosition 	: SV_POSITION;
					float4 vColor 		: COLOR;
					float2 vTexCoord0 	: TEXCOORD0;
				};

				float4 _FlipUV;

				PS_INPUT vert(VS_INPUT i)
				{
					PS_INPUT o;

					// Clip space position
					o.vPosition = UnityObjectToClipPos(i.vPosition);

					// Color
					o.vColor = i.vColor;

					// Texture coordinates
					o.vTexCoord0.xy = i.vTexCoord0.xy;

					return o;
				}

				float4 		_Color;
				sampler2D	_MainTex;

				float4 frag(PS_INPUT i) : SV_Target
				{
					float4 vAlbedoTex = tex2D( _MainTex, i.vTexCoord0.xy );

					float4 vColor = 0;
					// vColor.rgb = _Color.rgb * _Color.a * i.vColor.rgb * i.vColor.a;
					vColor = i.vColor;
					vColor = vAlbedoTex;

					return vColor;
				}
			ENDCG
		}
	}
}
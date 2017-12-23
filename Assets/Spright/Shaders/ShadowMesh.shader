Shader "Spright/ShadowMesh"
{
	Properties
	{
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
		Blend One One

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
				};

				struct PS_INPUT
				{
					float4 vPosition 	: SV_POSITION;
					float4 vColor 		: COLOR;
				};

				float4 _FlipUV;

				PS_INPUT vert(VS_INPUT i)
				{
					PS_INPUT o;

					// Clip space position
					o.vPosition = UnityObjectToClipPos(i.vPosition);

					// Color
					o.vColor = i.vColor;

					return o;
				}

				float4 		_Color;

				float4 frag(PS_INPUT i) : SV_Target
				{
					float4 vColor = 0;
					vColor.rgb = _Color.rgb * _Color.a * i.vColor.rgb * i.vColor.a;

					return vColor;
				}
			ENDCG
		}
	}
}
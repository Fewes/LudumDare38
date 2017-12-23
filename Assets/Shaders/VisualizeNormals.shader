Shader "utils/VisualizeNormals"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "PerformanceChecks" = "False" }
		//LOD 300

		Pass
		{
			//Name "FORWARD"
			//Tags { "LightMode" = "ForwardBase" "PassFlags" = "OnlyDirectional" "Queue" = "Transparent" } // NOTE: "OnlyDirectional" prevents Unity from baking dynamic lights into SH terms at runtime

			// Blend One One
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back

			CGPROGRAM
				#pragma target 2.0

				#pragma vertex MainVs
				#pragma fragment MainPs

				// Includes -------------------------------------------------------------------------------------------------------------------------------------------------
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"
				#include "UnityStandardUtils.cginc"
				#include "UnityStandardInput.cginc"

				// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
				struct VS_INPUT
				{
					float4 vPositionOs : POSITION;
					float3 vNormalOs : NORMAL;
					// float2 vTexCoord0 : TEXCOORD0;
					float4 vColor : COLOR;
				};

				struct PS_INPUT
				{
					float4 vPositionPs : SV_Position;

					float3 vPositionWs : TEXCOORD0;
					float3 vNormalWs : TEXCOORD1;
					// float2 vTextureCoords : TEXCOORD2;
					float4 vColor : TEXCOORD3;

					float2 vFogCoords : TEXCOORD4;
				};

				// MainVs ---------------------------------------------------------------------------------------------------------------------------------------------------
				PS_INPUT MainVs( VS_INPUT i )
				{
					PS_INPUT o = ( PS_INPUT )0;

					// Vertex colors
					o.vColor = i.vColor;

					// Normals
					o.vNormalWs = UnityObjectToWorldNormal( i.vNormalOs.xyz );

					// Position
					o.vPositionWs.xyz = mul( unity_ObjectToWorld, i.vPositionOs.xyzw ).xyz;
					o.vPositionPs.xyzw = UnityObjectToClipPos( i.vPositionOs.xyzw );

					// Texture coords (Copied from Unity's TexCoords() helper function)
					// o.vTextureCoords.xy = TRANSFORM_TEX( i.vTexCoord0, _MainTex );

					return o;
				}

				// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------

				struct PS_OUTPUT
				{
					float4 vColor : SV_Target0;
				};

				float _Exp;

				PS_OUTPUT MainPs( PS_INPUT i )
				{
					PS_OUTPUT o = ( PS_OUTPUT )0;

					float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_V, i.vNormalWs));
					viewNormal = viewNormal.xyz * 0.5 + 0.5;
					viewNormal = pow(viewNormal, 2.2);

					o.vColor = float4(viewNormal, i.vColor.a);

					return o;
				}
			ENDCG
		}
	}
}

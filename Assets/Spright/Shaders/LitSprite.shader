Shader "Spright/LitSprite"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[Normal] _BumpMap( "Normal Map", 2D ) = "bump" {}
		_Color( "Color", Color ) = ( 1, 1, 1, 1 )
		[PerRendererData] _ColorO( "Color Override", Color ) = ( 1, 1, 1, 0 )
		[PerRendererData] _RendererColor ("Instance Color", Color) = (1,1,1,1)
		[PerRendererData] _FlipUV ("Flip UVs", Vector) = (1,1,1,1)
		[PerRendererData] _RimLit ("Rim Lit", Float) = 0
		[PerRendererData] _SpriteRect ("Sprite Rect", Vector) = (0,0,1,1)
		[PerRendererData] _TexelSize ("Texel Size", Vector) = (1,1,1,1)
		[PerRendererData] _ShadowChannel ("Shadow Channel", Vector) = (1,1,1,1)
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			// "IgnoreProjector"="True" 
			"RenderType"="Sprite" 
			// "PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		// Cull Back
		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
				#pragma target 2.0
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"
				#include "UnityStandardUtils.cginc"
				#include "SpriteLighting.cginc"

				#include "UnitySprites.cginc"

				#pragma shader_feature _NORMAL_MAP

				struct VS_INPUT
				{
					float4 vPosition 	: POSITION;
					float3 vNormalOs 	: NORMAL;
					float2 vTexCoord0 	: TEXCOORD0;
					float4 vColor 		: COLOR;
					float4 vTangent		: TANGENT;
				};

				struct PS_INPUT
				{
					float4 vPosition 	: SV_POSITION;
					float4 vPositionWs 	: TEXCOORD0;
					float4 vPositionSs 	: TEXCOORD1;
					float2 vTexCoord0 	: TEXCOORD2;
					float2 vShadowCoords: TEXCOORD6;
					float4 vColor 		: COLOR;
					float3 vNormalWs 	: TEXCOORD3;
					float3 vTangentWs 	: TEXCOORD4;
					float3 vBitangentWs : TEXCOORD5;
				};

				float4 _FlipUV;

				PS_INPUT vert(VS_INPUT i)
				{
					PS_INPUT o;

					// i.vPosition.xy *= _FlipUV.xy;

					// Pixel snap
					// i.vPosition = UnityPixelSnap (i.vPosition);
					
					// Clip space position
					o.vPosition = UnityObjectToClipPos(i.vPosition);

					// World space position
					o.vPositionWs = mul( unity_ObjectToWorld, i.vPosition.xyzw );

					// Screen space position
					o.vPositionSs = ComputeScreenPos(i.vPosition);
					o.vPositionSs = (UnityObjectToClipPos(i.vPosition) + 1) * 0.5;

					// Camera space
					o.vShadowCoords = o.vPositionWs.xy - g_vCameraBounds.xy;
					o.vShadowCoords /= (g_vCameraBounds.zw - g_vCameraBounds.xy);

					// Texture coordinates
					o.vTexCoord0.xy = i.vTexCoord0.xy;

					// Color
					o.vColor = i.vColor;

					// Normal
					o.vNormalWs = UnityObjectToWorldNormal( i.vNormalOs.xyz );

					// Tangent
					o.vTangentWs.xyz = UnityObjectToWorldDir( i.vTangent.xyz ); // Transform tangentU into world space
					// vTangentUWs.xyz = normalize( vTangentUWs.xyz - ( o.vNormalWs.xyz * dot( vTangentUWs.xyz, o.vNormalWs.xyz ) ) ); // Force tangentU perpendicular to normal and normalize

					// o.vNormalWs.xyz = float3(0, 0, 1);

					// float fHandedNess = dot(o.vNormalWs)

					// Binormal
					// o.vBitangentWs.xyz = cross( o.vNormalWs.xyz, o.vTangentWs.xyz );// * i.vTangent.w;
					o.vBitangentWs.xyz = cross( o.vNormalWs.xyz, o.vTangentWs.xyz );

					o.vTangentWs.xyz *= _FlipUV.x;
					o.vBitangentWs.xyz *= _FlipUV.y;

					// o.vTangentWs.xyz = _FlipUV.x;

					return o;
				}

				// sampler2D 	_MainTex;
				sampler2D	_BumpMap;
				// float4 		¨;
				// float4		_RendererColor;
				float4		_ColorO;
				float4		_SpriteRect;
				float4		_TexelSize;
				float		_RimLit;
				float4		_ShadowChannel;

				float4 frag(PS_INPUT i) : SV_Target
				{
					float4 vColor = 0;

					// Sample texture
					float4 vAlbedoTex = tex2D( _MainTex, i.vTexCoord0.xy ) * _Color * _RendererColor;

					// Override color at the start of fragment (will take lighting into account)
					// vAlbedoTex.rgb = lerp(vAlbedoTex.rgb, _ColorO.rgb, _ColorO.a);

					// TEMPORARY FIX FOR EVERYTHING BEING DARK AS SHIT IN SCENE VIEW
					// vAlbedoTex.rgb *= lerp(3.5, 1, g_bUnlit);

					// Sample normal
					// #if ( _NORMAL_MAP )
						float3 vNormalTs = UnpackScaleNormal( tex2D( _BumpMap, i.vTexCoord0.xy ), 1.0).xyz;

						vNormalTs.x *= -1;

						// Convert to world space
						float3 vNormalWs = Vec3TsToWsNormalized( vNormalTs.xyz, i.vNormalWs.xyz, i.vTangentWs.xyz, i.vBitangentWs.xyz );
					// #else
						// float3 vNormalWs = i.vNormalWs;
					// #endif

					// Calculate lighting
					// float3 vLighting = ComputeLighting(i.vPositionWs, i.vNormalWs.xyz, i.vTangentWs.xyz, i.vBitangentWs.xyz, _MainTex, i.vTexCoord0.xy, _SpriteRect, _RimLit, _TexelSize.xy);
					float3 vLighting = ComputeLighting(i.vPositionWs, i.vNormalWs.xyz, vNormalWs.xyz, i.vTangentWs.xyz, i.vBitangentWs.xyz,
						_MainTex, i.vTexCoord0.xy, i.vShadowCoords, _SpriteRect, _TexelSize.xy, _RimLit, _ShadowChannel);

					// Apply lighting
					vColor.rgb = vAlbedoTex.rgb * i.vColor.rgb * vLighting.rgb;

					// Unlit mask TODO: Should probably be a toggleable feature
					float fUnlit = step(vAlbedoTex.a, 0.75);
					// vAlbedoTex.a = step(0.25, vAlbedoTex.a);
					vColor.rgb = lerp(vColor.rgb, vAlbedoTex.rgb, fUnlit);

					// Multiply alpha
					vColor.a = vAlbedoTex.a * i.vColor.a;

					// Debug tangent
					// vColor.rgb = i.vTangentWs.xyz * 0.5 + 0.5;
					// vColor.rgb = i.vBitangentWs.xyz * 0.5 + 0.5;
					// vColor.rgb = vNormalWs.xyz * 0.5 + 0.5;
					// vColor.rgb = vLighting;

					// Override color at the end of fragment (will ignore lighting)
					vColor.rgb = lerp(vColor.rgb, _ColorO.rgb, _ColorO.a);

					return vColor;
				}
			ENDCG
		}
	}
}
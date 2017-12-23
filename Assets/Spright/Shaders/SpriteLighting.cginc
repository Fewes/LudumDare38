#define MAX_LIGHTS 18

CBUFFER_START( SpriteLighting )
	float4		g_vCameraBounds;
	float4 		g_vAmbientLight;
	float4 		g_vAmbientLight2;
	sampler2D	g_tDirectionalShadow;
	sampler2D	g_tLightBuffer;
	float4 		g_tDirectionalColor;
	float4 		g_tDirectionalDir;
	int 		g_nLightSteps;
	int 		g_nShadeSteps;
	int 		g_nNumLights;
	float		g_bUnlit;
	float4 		g_vLightColor[ MAX_LIGHTS ];
	float4 		g_vLightPosition_flInvRadius[ MAX_LIGHTS ];
	float4		g_vLightPosition2_fWidth[ MAX_LIGHTS ];
	float4 		g_fLightIntensity_fRimIntensity_fRimSize_fBackFront[ MAX_LIGHTS ];
	float4 		g_fLightFalloff[ MAX_LIGHTS ];
CBUFFER_END

float3 WsDirToTsDir( float3 vDirWs, float3 vNormalWs, float3 vTangentWs, float3 vBitangentWs )
{
	float3x3 mWorldToTangent = (float3x3(vTangentWs, vBitangentWs, vNormalWs));
	return normalize( mul( mWorldToTangent, vDirWs ));
}

float3 ClosestPointOnLine(float3 a, float3 b, float3 p)
{
	float3 c = p - a;				// Vector from a to Point
	float3 v = normalize(b - a);	// Unit Vector from a to b
	float d = length(b - a);		// Length of the line segment
	float t = dot(v, c);			// Intersection point Distance from a

	// Check to see if the point is on the line,
	// if not, return the endpoint
	if(t < 0) return a;
	if(t > d) return b;

	// Get the distance to move from point a
	v *= t;

	// Move from point a to the nearest point on the segment
	return a + v;
}

float SpriteRim(float2 vLightDirTs, float fMaxLength, float2 vTexCoord, float2 fTexelSize, float4 vUvBounds, sampler2D _Tex, float fFrontFacing)
{
	vLightDirTs *= fFrontFacing * fMaxLength;

	// Clamp distance to rim size
	if (length(vLightDirTs) > fMaxLength)
		vLightDirTs = normalize(vLightDirTs) * fMaxLength;

	// Flip Y for some reason. Best not to ask questions
	vLightDirTs.y *= -1;

	// Rim alpha sample coordinates
	float2 vOffsetCoord = vTexCoord.xy + vLightDirTs * fTexelSize;
	float fOffsetAlpha = 0;

	// Check if offset coords is inside UV bounds
	if (vOffsetCoord.x > vUvBounds.x && vOffsetCoord.x < vUvBounds.y &&
		vOffsetCoord.y > vUvBounds.z && vOffsetCoord.y < vUvBounds.w )
		fOffsetAlpha = tex2D( _Tex, vOffsetCoord ).a;

	return lerp(1 - fOffsetAlpha, fOffsetAlpha, (fFrontFacing + 1) * 0.5);
}

float StepGradient(float f, float steps)
{
	// return f;
	return floor(f * steps + 1) / (steps + 1);
}

float3 ComputeLighting(float4 vPositionWs, float3 vGeometricNormalWs, float3 vNormalWs, float3 vTangentWs, float3 vBitangentWs,
	sampler2D _Tex, float2 vTexCoord, float2 vShadowCoords, float4 vUvBounds, float2 fTexelSize, float fRimIntensity, float4 _ShadowChannel)
{
	float bAmbientOnly = 0;

	// Shadow texture
	float fNormalOffset = 0.005;
	fNormalOffset = 0;
	float4 vDirShadowMask = tex2D( g_tDirectionalShadow, vShadowCoords.xy - vNormalWs.xy * fNormalOffset );
	float vDirShadow1 = 1 - vDirShadowMask.r; // World shadows
	float vDirShadow2 = 1 - vDirShadowMask.g; // Object shadows
	float vDirShadow3 = 1 - vDirShadowMask.b; // Character shadows
	float fAmbientMask = vDirShadowMask.a;    // Ambient light mask

	// Object shadows do not fall on other objects, or on characters
	float vDirShadow = vDirShadow1;
	vDirShadow *= lerp(vDirShadow2, 1, max(_ShadowChannel.g, _ShadowChannel.b));

	// Character shadows do not fall on other characters
	vDirShadow *= lerp(vDirShadow3, 1, _ShadowChannel.b);

	// No directional light outside camera bounds in editor
	if (vShadowCoords.x < 0 || vShadowCoords.x > 1 || vShadowCoords.y < 0 || vShadowCoords.y > 1)
		vDirShadow = 0;

	vDirShadow = int(vDirShadow * g_nShadeSteps);
	vDirShadow /= g_nShadeSteps;

	float4 vLightBuffer = tex2D( g_tLightBuffer, vShadowCoords.xy );

	// Ambient
	float3 vLighting = lerp(g_vAmbientLight.rgb * g_vAmbientLight.a, g_vAmbientLight2.rgb * g_vAmbientLight2.a, StepGradient(vNormalWs.y, /*g_nShadeSteps*/ 300)) * vLightBuffer.a;

	// Directional light
	float fNdotL_dir = dot(normalize(g_tDirectionalDir), vNormalWs);
	fNdotL_dir = StepGradient(fNdotL_dir, g_nShadeSteps);
	float fMaxLength_dir = 0.075;
	float fFrontFacing_dir = -1;
	float2 vLightDirTs_dir = normalize(WsDirToTsDir(float3(g_tDirectionalDir.xy, 0), vNormalWs, vTangentWs, vBitangentWs).xy);
	float fRim_dir = SpriteRim(vLightDirTs_dir, fMaxLength_dir, vTexCoord, fTexelSize, vUvBounds, _Tex, fFrontFacing_dir);

	// Assume that bounced light is coming off the floor
	float2 vLightDirTs_bounce = normalize(WsDirToTsDir(float3(g_tDirectionalDir.xy * float2(1, -1), 0), vNormalWs, vTangentWs, vBitangentWs).xy);
	float fRim_bounce = SpriteRim(vLightDirTs_bounce, fMaxLength_dir, vTexCoord, fTexelSize, vUvBounds, _Tex, fFrontFacing_dir);

	// Baked direct
	float fMixedBakedShadow = lerp(vDirShadow2, 1, max(_ShadowChannel.g, _ShadowChannel.b)) * lerp(vDirShadow3, 1, _ShadowChannel.b);
	fMixedBakedShadow = lerp(fMixedBakedShadow, 1, 0.65);
	vLighting += vLightBuffer.rgb * (1 - vNormalWs.z) * 0.5 * fMixedBakedShadow; // Mask with normal z just cause it looks sorta nice

	// Baked rim
	vLighting += vLightBuffer.rgb * fRim_bounce * fRimIntensity * (1 - bAmbientOnly);

	// Directional direct normal mapped
	float fDiretionalIntensity = pow(g_tDirectionalDir.w, 4);
	float fDiretionalRim = pow(g_tDirectionalColor.a, 4);
	vLighting += fNdotL_dir * g_tDirectionalColor.xyz * vDirShadow * fDiretionalIntensity * (1 - bAmbientOnly);

	// Directional rim
	vLighting += fRim_dir * g_tDirectionalColor.xyz * vDirShadow * fRimIntensity * fDiretionalRim * (1 - bAmbientOnly);

	// Point/line lights
	[loop] for ( int i = 0; i < g_nNumLights; i++ )
	{
		// Max rim length
		float fMaxLength = g_fLightIntensity_fRimIntensity_fRimSize_fBackFront[i].z;

		// World space light direction
		float3 vLightPosition = ClosestPointOnLine(g_vLightPosition_flInvRadius[i].xyz, g_vLightPosition2_fWidth[i].xyz, vPositionWs);
		float3 vLightDirWs = vPositionWs.xyz - vLightPosition.xyz;

		// Normal dot light direction
		float fNdotL = max(dot(normalize(vLightDirWs), vNormalWs), 0);

		fNdotL = StepGradient(fNdotL, g_nShadeSteps);

		// Tangent/UV space light direction
		float2 vLightDirTs = WsDirToTsDir(float3(vLightDirWs.xy, 0), vGeometricNormalWs, vTangentWs, vBitangentWs).xy * length(vLightDirWs.xy);

		// Falloff
		float fFalloff = pow(1 - saturate(length(vLightDirTs) * g_vLightPosition_flInvRadius[i].w), g_fLightFalloff[i].x);

		// Check if pixel is outside light range
		if (fFalloff <= 0) 
			continue;

		fFalloff = StepGradient(fFalloff, g_nLightSteps);

		// Front or back facing light?
		float fFrontFacing = g_fLightIntensity_fRimIntensity_fRimSize_fBackFront[i].w;

		// Direct normal mapped
		vLighting += fNdotL * fFalloff * g_fLightIntensity_fRimIntensity_fRimSize_fBackFront[i].x * g_vLightColor[i].rgb * g_vLightColor[i].a * (1 - bAmbientOnly);

		// Rim
		float fRim = SpriteRim(vLightDirTs, fMaxLength, vTexCoord, fTexelSize, vUvBounds, _Tex, fFrontFacing);
		vLighting += fRim * g_fLightIntensity_fRimIntensity_fRimSize_fBackFront[i].y * fRimIntensity * g_vLightColor[i].rgb * g_vLightColor[i].a * fFalloff * (1 - bAmbientOnly);

		// vLighting += fNdotL * fFalloff;
	}

	vLighting = lerp(vLighting, 1, g_bUnlit);

	return vLighting;
}

float3 Vec3TsToWs( float3 vVectorTs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
{
	float3 vVectorWs;
	vVectorWs.xyz = vVectorTs.x * vTangentUWs.xyz;
	vVectorWs.xyz += vVectorTs.y * vTangentVWs.xyz;
	vVectorWs.xyz += vVectorTs.z * vNormalWs.xyz;
	return vVectorWs.xyz; // Return without normalizing
}

float3 Vec3TsToWsNormalized( float3 vVectorTs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
{
	return normalize( Vec3TsToWs( vVectorTs.xyz, vNormalWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz ) );
}
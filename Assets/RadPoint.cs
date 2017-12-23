using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadPoint
{
	public Vector3				position;

	float						ambientOcclusion = 0;
	float						skyOcclusion = 0;
	float						direct = 1;
	float						bounced = 0;
	public Color				bouncedColor = Color.black;

	public Color				color = Color.black;

	//void OnDrawGizmos ()
	//{
	//	Gizmos.color = color;
	//	Gizmos.DrawSphere(position, 0.5f);
	//}

	//void OnDrawGizmosSelected ()
	//{
	//	for (int i = 0; i < rayCount; i++)
	//	{
	//		float angle = ((float)i / (float)rayCount) * 360f;
	//		Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.up;
			
	//		var hit = Physics2D.Raycast(position, dir, maxRayDist, layerMask);

	//		if (hit)
	//		{
	//			float a = Vector3.Distance(position, hit.point) / maxRayDist;
	//			Gizmos.color = new Color(a, a, a, 1);
	//			Gizmos.DrawLine(position, hit.point);
	//		}
	//		else
	//		{
	//			Gizmos.color = new Color(1, 1, 1, 1);
	//			Gizmos.DrawRay(position, dir * maxRayDist);
	//		}
	//	}

	//	Gizmos.color = color;
	//	Gizmos.DrawSphere(position, 0.5f);
	//}

	public void Bake (LightGridBakeSettings bakeSettings, RadPoint[,] points)
	{
		ambientOcclusion = 0;
		skyOcclusion = 0;
		direct = 0;
		bounced = 0;
		//bounced = 0;
		RaycastHit2D hit;
		Vector3 dir;

		// ------------------------------ AO ------------------------------
		if (bakeSettings.ambientOcclusion)
		{
			for (int i = 0; i < bakeSettings.AORayCount; i++)
			{
				float angle = ((float)i / (float)bakeSettings.AORayCount) * 360f;
				dir = Quaternion.Euler(0, 0, angle) * Vector3.up;

				hit = Physics2D.Raycast(position, dir, bakeSettings.AOMaxDist, bakeSettings.rayMask);
				if (hit)
					ambientOcclusion += 1 - Mathf.Pow(Vector3.Distance(position, hit.point) / bakeSettings.AOMaxDist, 2); // Quadratic falloff
			}
			ambientOcclusion /= bakeSettings.AORayCount;
			ambientOcclusion *= bakeSettings.AOIntensity;
		}

		// -------------------------- SKY LIGHT ---------------------------
		if (bakeSettings.skyOcclusion)
		{
			for (int i = 0; i < bakeSettings.skyRayCount; i++)
			{
				float angle = ((float)i / (float)bakeSettings.skyRayCount) * 180f;
				dir = Quaternion.Euler(0, 0, angle) * Vector3.right;

				hit = Physics2D.Raycast(position, dir, 9999999, bakeSettings.rayMask);
				if (hit)
					skyOcclusion += 1;
			}

			skyOcclusion /= bakeSettings.skyRayCount;
			skyOcclusion *= bakeSettings.skyOcclusionIntensity;
		}

		// -------------------------- SUN LIGHT ---------------------------
		if (bakeSettings.direct)
		{
			direct = 0;
			dir = -bakeSettings.sunDir;
			hit = Physics2D.Raycast(position, dir, 9999999, bakeSettings.rayMask);

			for (int i = 0; i < bakeSettings.directRayCount; i++)
			{
				float angle = ((float)i / (float)bakeSettings.directRayCount) * bakeSettings.directRaySpread;
				dir = Quaternion.Euler(0, 0, -bakeSettings.directRaySpread * 0.5f) * Quaternion.Euler(0, 0, angle) * -bakeSettings.sunDir;

				float angleMul = 1 - Mathf.Abs((float)i - (float)bakeSettings.directRayCount / 2f) / ((float)bakeSettings.directRayCount / 2f);

				hit = Physics2D.Raycast(position, dir, 9999999, bakeSettings.rayMask);
				if (hit)
					direct += 0;
				else
					direct += 1 * angleMul;
			}

			direct /= bakeSettings.directRayCount;
			direct *= bakeSettings.directIntensity;
			// direct = Mathf.Lerp(1, direct, bakeSettings.directIntensity);
		}

		// ------------------------ BOUNCE LIGHT --------------------------
		if (bakeSettings.bounce && false)
		{
			for (int i = 0; i < bakeSettings.bounceRayCount; i++)
			{
				float angle = ((float)i / (float)bakeSettings.bounceRayCount) * 360f;
				dir = Quaternion.Euler(0, 0, angle) * Vector3.up;

				hit = Physics2D.Raycast(position, dir, bakeSettings.bounceRadius, bakeSettings.rayMask);
				if (hit)
				{
					// This point COULD be bounce lit by the sun, do a distance check and then trace in the sun direction!
					Vector3 lightSource = hit.point + hit.normal * 0.01f;
					Vector3 normal = hit.normal;
					dir = -bakeSettings.sunDir;
					var dist = (position - lightSource).magnitude;
					if (dist <= bakeSettings.bounceRadius)
					{
						float falloff = Mathf.Pow(Mathf.Max(1 - dist / bakeSettings.bounceRadius, 0), bakeSettings.bounceFalloff); // Quadratic falloff

						float bouncedLight = 0;
						int rayCount = 8; // Fixed amount of rays for this. Seems to work fine with 8 and any more quickly becomes unbearable to bake.
						for (int j = 0; j < rayCount; j++)
						{
							angle = ((float)j / (float)rayCount) * bakeSettings.directRaySpread;
							dir = Quaternion.Euler(0, 0, -bakeSettings.directRaySpread * 0.5f) * Quaternion.Euler(0, 0, angle) * -bakeSettings.sunDir;

							// float angleMul = 1 - Mathf.Abs((float)i - (float)rayCount / 2f) / ((float)rayCount / 2f);

							hit = Physics2D.Raycast(lightSource, dir, 9999999, bakeSettings.rayMask);
							if (hit)
								bouncedLight += 0;
							else
								bouncedLight += 1;// * angleMul;
						}

						bouncedLight /= rayCount;
						bounced += bouncedLight * falloff;

						// 2nd pass
						if (bakeSettings.bounceTwoPass)
						{
							dir = Vector3.Reflect((lightSource - position).normalized, normal);
							float bounceRadius2 = bakeSettings.bounceRadius - dist;
							hit = Physics2D.Raycast(lightSource, dir, bounceRadius2, bakeSettings.rayMask);
							if (hit)
							{
								// This point COULD be bounce lit by the sun, do a distance check and then trace in the sun direction!
								lightSource = hit.point + hit.normal * 0.01f;
								normal = hit.normal;
								dir = -bakeSettings.sunDir;
								dist = (position - lightSource).magnitude;
								if (dist <= bounceRadius2)
								{
									falloff = Mathf.Pow(Mathf.Max(1 - dist / bounceRadius2, 0), bakeSettings.bounceFalloff); // Quadratic falloff

									bouncedLight = 0;
									rayCount = 8; // Fixed amount of rays for this. Seems to work fine with 8 and any more quickly becomes unbearable to bake.
									for (int j = 0; j < rayCount; j++)
									{
										angle = ((float)j / (float)rayCount) * bakeSettings.directRaySpread;
										dir = Quaternion.Euler(0, 0, -bakeSettings.directRaySpread * 0.5f) * Quaternion.Euler(0, 0, angle) * -bakeSettings.sunDir;

										// float angleMul = 1 - Mathf.Abs((float)i - (float)rayCount / 2f) / ((float)rayCount / 2f);

										hit = Physics2D.Raycast(lightSource, dir, 9999999, bakeSettings.rayMask);
										if (hit)
											bouncedLight += 0;
										else
											bouncedLight += 1;// * angleMul;
									}

									bouncedLight /= rayCount;
									bounced += bouncedLight * falloff;
								}
							}
						}

					}
				}
			}

			bounced *= bakeSettings.bounceIntensity;
			//bounced /= bakeSettings.bounceRadius;
			bounced /= bakeSettings.bounceRayCount;
		}

		Composite(bakeSettings.sunColor);
	}

	public void Composite (Color directColor)
	{
		Color lighting = new Color(directColor.r * direct, directColor.g * direct, directColor.b * direct);
		lighting += new Color(directColor.r * bounced, directColor.g * bounced, directColor.b * bounced);
		float ambient = (1 - skyOcclusion) * (1 - ambientOcclusion);
		//ambient = 0;
		//lighting = Mathf.Lerp(bounced, 0, direct);
		//lighting = Mathf.Max(bounced, direct);
		//lighting = bounced;
		//float lighting = (1 - skyOcclusion) * (1 - ambientOcclusion) + direct;
		//float lighting = 0.5f * (1 - ambientOcclusion) + (1 - skyOcclusion) + direct;
		//lighting = 1 - skyOcclusion;

		//color = new Color(lighting, lighting, lighting, 1);
		color = new Color(lighting.r, lighting.g, lighting.b, ambient);
	}

	//public void BakeAll ()
	//{
	//	foreach (var radPoint in FindObjectsOfType<RadPoint>())
	//		radPoint.Bake();
	//}
}

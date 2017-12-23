using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public struct LightGridBakeSettings
{
	public LayerMask				rayMask;

	public Vector2					sunDir;
	public Color					sunColor;

	public bool						ambientOcclusion;
	public int						AORayCount;
	public float					AOMaxDist;
	public float					AOIntensity;

	public bool						skyOcclusion;
	public int						skyRayCount;
	public float					skyOcclusionIntensity;

	public bool						direct;
	public int						directRayCount;
	public float					directRaySpread;
	public float					directIntensity;

	public bool						bounce;
	public bool						bounceTwoPass;
	public int						bounceRayCount;
	public float					bounceSearchRadius;
	public float					bounceRadius;
	public float					bounceFalloff;
	public float					bounceIntensity;
}

//[ExecuteInEditMode]
public class LightGrid : MonoBehaviour
{
	[Header("GLOBALS")]
	public LayerMask				rayMask;
	[Header("Ambient Occlusion")]
	public bool						ambientOcclusion = true;
	[Range(8, 128)]
	public int						AORayCount = 16;
	[Range(0, 128)]
	public float					AOMaxDist = 4f;
	[Range(0, 1)]
	public float					AOIntensity = 1f;

	[Header("Sky Occlusion")]
	public bool						skyOcclusion = true;
	[Range(8, 128)]
	public int						skyRayCount = 32;
	[Range(0, 1)]
	public float					skyOcclusionIntensity = 1f;

	[Header("Direct")]
	public bool						direct = true;
	[Range(1, 128)]
	public int						directRayCount = 8;
	[Range(0, 90)]
	public float					directRaySpread = 30f;
	[Range(0, 1)]
	public float					directIntensity = 1f;

	[Header("Direct Bounced")]
	public bool						bounce = false;
	[HideInInspector]
	public bool						bounceTwoPass = false;
	[Range(1, 256)] [HideInInspector]
	public int						bounceRayCount = 8;
	[Range(0, 16)] [HideInInspector]
	public float					bounceRadius = 2f;
	[Range(1, 8)] [HideInInspector]
	public float					bounceFalloff = 1f;
	[Range(0, 2)] [HideInInspector]
	public float					bounceIntensity = 1f;

	[Header("INSTANCE")]
	[SerializeField]
	private int						width = 10;
	[SerializeField]
	private int						height = 10;
	[Range(1, 16)] [SerializeField]
	private int						pixelsPerMeter = 4;
	public bool						globalBakeExclude = false;
	[SerializeField]
	private Material				lightMapMaterial;
	//[SerializeField] [HideInInspector]
	public Texture2D				lightMap;
	public Texture2D				lightMapOverride;
	public FilterMode				lightMapFilterMode = FilterMode.Bilinear;

	private RadPoint[,]				points;

	private bool					setup = false;

	private MeshFilter				meshFilter;
	private MeshRenderer			meshRenderer;

	// Use this for initialization
	void Start ()
	{
		ReconstructGrid();

		UpdatePropertyBlock();
	}

	public void ReconstructGrid ()
	{
		int xCount = width * pixelsPerMeter + 1;
		int yCount = height * pixelsPerMeter + 1;
		points = new RadPoint[xCount, yCount];

		for (int x = 0; x < points.GetLength(0); x++)
		{
			for (int y = 0; y < points.GetLength(1); y++)
			{
				points[x, y] = new RadPoint();
				points[x, y].position = transform.position + new Vector3(
					((float)x / ((float)points.GetLength(0)-1)) * width  - (float)width  * 0.5f,
					((float)y / ((float)points.GetLength(1)-1)) * height - (float)height * 0.5f,
					0
				);
			}
		}

		setup = true;
	}

	void CheckLightMesh ()
	{
		if (!meshFilter)
		{
			meshFilter = GetComponent<MeshFilter>();
			if (!meshFilter)
				meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = new Mesh();
			meshFilter.sharedMesh.name = "LightMesh";
			
		}
		if (!meshRenderer)
		{
			meshRenderer = GetComponent<MeshRenderer>();
			if (!meshRenderer)
				meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = lightMapMaterial;
		}
	}

	//void OnValidate ()
	//{
	//	ReconstructGrid();
	//}

	public struct LightImpact
	{
		public Vector2 position;
		public Vector2 origin;
		public Vector2 direction;
		public Vector2 normal;
		public Color color;
		public float dist;

		public LightImpact (Vector2 pos, Vector2 org, Vector2 dir, Vector2 nrm, float d, Color col)
		{
			position = pos;
			origin = org;
			direction = dir;
			normal = nrm;
			color = col;
			dist = d;
		}

		public LightImpact (Vector2 pos, Vector2 org, Vector2 dir, Vector2 nrm, float d)
		{
			position = pos;
			origin = org;
			direction = dir;
			normal = nrm;
			dist = d;
			color = Color.white;
		}
	}

	List<LightImpact> impact0;
	List<LightImpact> impact1;
	List<LightImpact> impact2;

	public void BakeBounced ()
	{
		//ReconstructGrid();

		impact0 = new List<LightImpact>();
		impact1 = new List<LightImpact>();
		impact2 = new List<LightImpact>();

		Color sunColor = FindObjectOfType<LightManager>().directionalColor * FindObjectOfType<LightManager>().directionalIntensity;

		Vector2 origin;
		Vector2 normal;
		Vector2 sunDir = FindObjectOfType<LightManager>().GetSunDirection();
		Vector2 dir = new Vector2(Mathf.Sign(sunDir.x) / pixelsPerMeter, Mathf.Sign(sunDir.y) / pixelsPerMeter);
		RaycastHit2D hit;

		// Gather impact points
		for (int x = 0; x < points.GetLength(0); x++)
		{
			for (int y = 0; y < points.GetLength(1); y++)
			{
				origin = points[x, y].position;
				hit = Physics2D.Raycast(origin, dir, dir.magnitude + 0.01f, rayMask);
				if (hit)
				{
					float dist = (hit.point - origin).magnitude;
					if (dist > Mathf.Epsilon) // Point inside geo check
					{
						origin = hit.point - sunDir * 0.01f;
						normal = hit.normal;

						int rayCount = Mathf.Max((int)(directRaySpread / 4), 3);
						for (int i = 0; i < rayCount; i++)
						{
							float angle = rayCount > 1 ? ((float)i / (float)(rayCount - 1)) * directRaySpread : directRaySpread * 0.5f;
							Vector2 dir2 = Quaternion.Euler(0, 0, -directRaySpread * 0.5f) * Quaternion.Euler(0, 0, angle) * -sunDir;

							float angleMul = 1 - Mathf.Abs((float)i - (float)rayCount / 2f) / ((float)rayCount / 2f);

							angleMul /= rayCount;

							hit = Physics2D.Raycast(origin, dir2, 9999f, rayMask);
							if (!hit)
							{
								impact0.Add(new LightImpact(origin, origin + dir2 * 9999f, -dir2, normal, 0, sunColor * angleMul));
							}
						}
					}
				}
			}
		}

		// Bounce pass 1
		for (int i = 0; i < impact0.Count; i++)
		{
			normal	= impact0[i].normal;
			dir		= Vector3.Reflect(impact0[i].direction, normal);
			origin	= impact0[i].position + dir * 0.01f;

			hit = Physics2D.Raycast(origin, dir, bounceRadius, rayMask);
			if (hit)
			{
				float dist = (origin - hit.point).magnitude + impact0[i].dist;
				float falloff = Mathf.Pow(Mathf.Max(1 - dist / bounceRadius, 0), bounceFalloff);
				if (falloff > Mathf.Epsilon)
					impact1.Add(new LightImpact(hit.point, origin, dir, hit.normal, dist, impact0[i].color * falloff));
			}
		}

		// Bounce pass 2
		for (int i = 0; i < impact1.Count; i++)
		{
			normal	= impact1[i].normal;
			dir		= Vector3.Reflect(impact1[i].direction, normal);
			origin	= impact1[i].position + dir * 0.01f;

			hit = Physics2D.Raycast(origin, dir, 9999f, rayMask);
			if (hit)
			{
				float dist = (origin - hit.point).magnitude + impact1[i].dist;
				float falloff = Mathf.Pow(Mathf.Max(1 - dist / bounceRadius, 0), bounceFalloff);
				if (falloff > Mathf.Epsilon)
					impact2.Add(new LightImpact(hit.point, origin, dir, hit.normal, dist, impact1[i].color * falloff));
			}

			
		}

		int k = 0;
		float refreshRate = 1f / 30f;
		float refreshTimer = refreshRate;
		float lastTime = (float)EditorApplication.timeSinceStartup;

		// Gather light from impact points
		for (int x = 0; x < points.GetLength(0); x++)
		{
			for (int y = 0; y < points.GetLength(1); y++)
			{
				Color bouncedLight = new Color(0, 0, 0, 0);

				// Bounce 1
				foreach (var impact in impact0)
				{
					float dist = Vector2.Distance(points[x, y].position, impact.position);
					float falloff = Mathf.Pow(Mathf.Max(1 - dist / bounceRadius, 0), bounceFalloff);
					if (falloff > Mathf.Epsilon)
					{
						Vector2 pos = points[x, y].position;
						dir = pos - impact.position;
						hit = Physics2D.Raycast(impact.position + impact.normal * 0.01f, dir, dir.magnitude, rayMask);
						if (!hit)
							bouncedLight += impact.color * falloff;
					}
				}

				// Bounce 2
				foreach (var impact in impact1)
				{
					float dist = Vector2.Distance(points[x, y].position, impact.position);
					float falloff = Mathf.Pow(Mathf.Max(1 - dist / bounceRadius, 0), bounceFalloff);
					if (falloff > Mathf.Epsilon)
					{
						Vector2 pos = points[x, y].position;
						dir = pos - impact.position;
						hit = Physics2D.Raycast(impact.position + impact.normal * 0.01f, dir, dir.magnitude, rayMask);
						if (!hit)
							bouncedLight += impact.color * falloff;
					}
				}

				// Bounce 3
				foreach (var impact in impact2)
				{
					float dist = Vector2.Distance(points[x, y].position, impact.position);
					float falloff = Mathf.Pow(Mathf.Max(1 - dist / bounceRadius, 0), bounceFalloff);
					if (falloff > Mathf.Epsilon)
					{
						Vector2 pos = points[x, y].position;
						dir = pos - impact.position;
						hit = Physics2D.Raycast(impact.position + impact.normal * 0.01f, dir, dir.magnitude, rayMask);
						if (!hit)
							bouncedLight += impact.color * falloff;
					}
				}

				bouncedLight /= pixelsPerMeter;
				bouncedLight /= bounceRadius;

				bouncedLight *= bounceIntensity;

				points[x, y].color.r += bouncedLight.r;
				points[x, y].color.g += bouncedLight.g;
				points[x, y].color.b += bouncedLight.b;
				//points[x, y].color.a = 0.05f;

				k++;
				float deltaTime = (float)EditorApplication.timeSinceStartup - lastTime;
				lastTime = (float)EditorApplication.timeSinceStartup;
				refreshTimer += deltaTime;
				if (refreshTimer > refreshRate)
				{
					float progress = (float)k / (float)(points.GetLength(0) * points.GetLength(1));
					EditorUtility.DisplayProgressBar("Baking 2D Light Map...", "Baking bounced light for point " + k, progress);
					refreshTimer -= refreshRate;
				}
			}
		}

		//UpdateMesh();

		//UpdateTexture();
	}

	public void Bake (int num = -1, int count = 1)
	{
		ReconstructGrid();

		LightGridBakeSettings bakeSettings;
		bakeSettings.sunDir					= FindObjectOfType<LightManager>().GetSunDirection();
		bakeSettings.sunColor				= FindObjectOfType<LightManager>().directionalColor * FindObjectOfType<LightManager>().directionalIntensity;
		bakeSettings.rayMask				= rayMask;
		bakeSettings.ambientOcclusion		= ambientOcclusion;
		bakeSettings.AORayCount				= AORayCount;
		bakeSettings.AOMaxDist				= AOMaxDist;
		bakeSettings.AOIntensity			= AOIntensity;
		bakeSettings.skyOcclusion			= skyOcclusion;
		bakeSettings.skyRayCount			= skyRayCount;
		bakeSettings.skyOcclusionIntensity	= skyOcclusionIntensity;
		bakeSettings.direct					= direct;
		bakeSettings.directRayCount			= directRayCount;
		bakeSettings.directRaySpread		= directRaySpread;
		bakeSettings.directIntensity		= directIntensity;
		bakeSettings.bounce					= bounce;
		bakeSettings.bounceTwoPass			= bounceTwoPass;
		bakeSettings.bounceRayCount			= bounceRayCount;
		bakeSettings.bounceSearchRadius		= 1f / (float)pixelsPerMeter;
		bakeSettings.bounceRadius			= bounceRadius;
		bakeSettings.bounceFalloff			= bounceFalloff;
		bakeSettings.bounceIntensity		= bounceIntensity;

		int i = 0;
		float refreshRate = 1f / 30f;
		float refreshTimer = refreshRate;
		float lastTime = (float)EditorApplication.timeSinceStartup;

		// Reset bounced color
		for (int x = 0; x < points.GetLength(0); x++)
		{
			for (int y = 0; y < points.GetLength(1); y++)
			{
				points[x, y].bouncedColor = Color.black;
			}
		}

		// Bake points
		for (int x = 0; x < points.GetLength(0); x++)
		{
			for (int y = 0; y < points.GetLength(1); y++)
			{
				points[x, y].Bake(bakeSettings, points);

				i++;
				float deltaTime = (float)EditorApplication.timeSinceStartup - lastTime;
				lastTime = (float)EditorApplication.timeSinceStartup;
				refreshTimer += deltaTime;
				if (refreshTimer > refreshRate)
				{
					float progress = (float)i / (float)(points.GetLength(0) * points.GetLength(1));
					if (num < 0)
						EditorUtility.DisplayProgressBar("Baking 2D Light Map...", "Baking ambiance for point " + i, progress);
					else
						EditorUtility.DisplayProgressBar("Baking 2D Light Map (" + num + "/" + count +")...", "Baking ambiance for point " + i, progress);
					refreshTimer -= refreshRate;
				}
			}
		}

		i = 0;
		refreshTimer = refreshRate;
		lastTime = (float)EditorApplication.timeSinceStartup;

		// Composite
		//for (int x = 0; x < points.GetLength(0); x++)
		//{
		//	for (int y = 0; y < points.GetLength(1); y++)
		//	{
		//		points[x, y].Composite(FindObjectOfType<LightManager>().directionalColor * FindObjectOfType<LightManager>().directionalIntensity);

		//		i++;
		//		float deltaTime = (float)EditorApplication.timeSinceStartup - lastTime;
		//		lastTime = (float)EditorApplication.timeSinceStartup;
		//		refreshTimer += deltaTime;
		//		if (refreshTimer > refreshRate)
		//		{
		//			float progress = (float)i / (float)(points.GetLength(0) * points.GetLength(1));
		//			if (num < 0)
		//				EditorUtility.DisplayProgressBar("Baking 2D Light Map...", "Compositing point " + i, progress);
		//			else
		//				EditorUtility.DisplayProgressBar("Baking 2D Light Map (" + num + "/" + count +")...", "Compositing point " + i, progress);
		//			refreshTimer -= refreshRate;
		//		}
		//	}
		//}

		if (bounce)
			BakeBounced();

		EditorUtility.ClearProgressBar();

		UpdateMesh();

		UpdateTexture();
	}

	public void BakeAll (bool transfer = true)
	{
		if (transfer)
			SyncGlobals();

		int i = 1;
		var lightGrids = FindObjectsOfType<LightGrid>();
		int bakeCount = 0;
		foreach (var lightGrid in lightGrids)
		{
			if (!lightGrid.globalBakeExclude)
				bakeCount++;
		}
		foreach (var lightGrid in lightGrids)
		{
			if (!lightGrid.globalBakeExclude)
				lightGrid.Bake(i, bakeCount);
			i++;
		}
	}

	public void SyncGlobals ()
	{
		foreach (var lightGrid in FindObjectsOfType<LightGrid>())
		{
			if (lightGrid != this)
			{
				lightGrid.rayMask					= rayMask;
				lightGrid.ambientOcclusion			= ambientOcclusion;
				lightGrid.AORayCount				= AORayCount;
				lightGrid.AOMaxDist					= AOMaxDist;
				lightGrid.AOIntensity				= AOIntensity;
				lightGrid.skyOcclusion				= skyOcclusion;
				lightGrid.skyRayCount				= skyRayCount;
				lightGrid.skyOcclusionIntensity		= skyOcclusionIntensity;
				lightGrid.direct					= direct;
				lightGrid.directRayCount			= directRayCount;
				lightGrid.directRaySpread			= directRaySpread;
				lightGrid.directIntensity			= directIntensity;
				lightGrid.bounce					= bounce;
				lightGrid.bounceTwoPass				= bounceTwoPass;
				lightGrid.bounceRayCount			= bounceRayCount;
				lightGrid.bounceFalloff				= bounceFalloff;
				lightGrid.bounceRadius				= bounceRadius;
				lightGrid.bounceIntensity			= bounceIntensity;
			}
		}
	}

	void UpdateMesh ()
	{
		CheckLightMesh();

		// Textured quad
		Vector3[]	verts	= new Vector3[4];
		Vector2[]	coords	= new Vector2[4];
		int[]		indices = new int[6];

		verts[0] = new Vector2(-width * 0.5f, -height * 0.5f);
		verts[1] = new Vector2( width * 0.5f, -height * 0.5f);
		verts[2] = new Vector2( width * 0.5f,  height * 0.5f);
		verts[3] = new Vector2(-width * 0.5f,  height * 0.5f);

		coords[0] = new Vector2(0, 0);
		coords[1] = new Vector2(1, 0);
		coords[2] = new Vector2(1, 1);
		coords[3] = new Vector2(0, 1);

		indices[0] = 0;
		indices[1] = 1;
		indices[2] = 2;
		indices[3] = 0;
		indices[4] = 2;
		indices[5] = 3;

		meshFilter.sharedMesh.vertices = verts;
		meshFilter.sharedMesh.uv = coords;
		meshFilter.sharedMesh.triangles = indices;

		//float refreshRate = 1f / 30f;
		//float refreshTimer = refreshRate;
		//float lastTime = (float)EditorApplication.timeSinceStartup;

		//int pointCount = points.GetLength(0) * points.GetLength(1);
		//int indexCount = (points.GetLength(0) - 1) * (points.GetLength(1) - 1) * 2 * 3;
		//Vector3[]	verts	= new Vector3[pointCount];
		//Color[]		colors	= new Color[pointCount];
		//int[]		indices = new int[indexCount];

		//int i = 0;
		//int j = 0;
		//for (int x = 0; x < points.GetLength(0); x++)
		//{
		//	for (int y = 0; y < points.GetLength(1); y++)
		//	{
		//		float deltaTime = (float)EditorApplication.timeSinceStartup - lastTime;
		//		lastTime = (float)EditorApplication.timeSinceStartup;
		//		refreshTimer += deltaTime;
		//		if (refreshTimer > refreshRate)
		//		{
		//			float progress = (float)i / (float)pointCount;
		//			EditorUtility.DisplayProgressBar("Updating light mesh...", "Vertex " + i, progress);
		//			refreshTimer -= refreshRate;
		//		}

		//		verts[i] = points[x, y].position - transform.position;
		//		colors[i] = points[x, y].color;

		//		if (x < (points.GetLength(0) - 1) && y < (points.GetLength(1) - 1))
		//		{
		//			// Triangle 1
		//			indices[j+0] = i;
		//			indices[j+1] = i + 1;
		//			indices[j+2] = i + points.GetLength(1) + 1;

		//			// Triangle 2
		//			indices[j+3] = i;
		//			indices[j+4] = i + points.GetLength(1) + 1;
		//			indices[j+5] = i + points.GetLength(1);

		//			j += 6;
		//		}

		//		i++;
		//	}
		//}

		//meshFilter.sharedMesh.vertices = verts;
		//meshFilter.sharedMesh.colors = colors;
		//meshFilter.sharedMesh.triangles = indices;

		//EditorUtility.ClearProgressBar();
	}

	void DeleteLightMap ()
	{
		if (lightMap)
		{
			File.Delete(AssetDatabase.GetAssetPath(lightMap));
		}
	}

	void UpdateTexture ()
	{
		// Delete previous light map if one exists
		DeleteLightMap();

		// Get scene path
		var dir = EditorSceneManager.GetActiveScene().path;
		if (dir.Contains("/"))
			dir = dir.Substring(0, dir.LastIndexOf('/') + 1);

		// Generate file path from unique instance ID
		string filePath = dir + "LightMap2D_" + gameObject.GetInstanceID() + ".exr";

		// Create a new RGBAFloat texture with no mip maps
		Texture2D texture = new Texture2D(points.GetLength(0), points.GetLength(1), TextureFormat.RGBAFloat, false);

		// Set pixel values
		for (int x = 0; x < points.GetLength(0); x++)
			for (int y = 0; y < points.GetLength(1); y++)
				texture.SetPixel(x, y, points[x, y].color);
         
        // Apply all SetPixel calls
        texture.Apply();
		
		// Encode texture into EXR
		byte[] bytes = texture.EncodeToEXR();

		// Write to a file in the project folder
		File.WriteAllBytes(filePath, bytes);

		// Clean up the texture object
		Object.DestroyImmediate(texture);

		// Refresh the asset database since we just added a new file to it
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

		// Connect texture file to serialized texture variable
		lightMap = (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));

		// Set light map filtering mode
		lightMap.filterMode = lightMapFilterMode;

		// Update material property block
		UpdatePropertyBlock();
	}

	public bool UpdatePropertyBlock ()
	{
		// Connect texture to property block
		MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
		meshRenderer.GetPropertyBlock(propertyBlock);
		if (lightMapOverride)
			propertyBlock.SetTexture("_MainTex", lightMapOverride);
		else if (lightMap)
			propertyBlock.SetTexture("_MainTex", lightMap);
		else
			return false;
		meshRenderer.SetPropertyBlock(propertyBlock);

		return true;
	}

	void OnDrawGizmos ()
	{
		Gizmos.color = new Color(1, 1, 1, 0.1f);
		Gizmos.DrawLine(transform.position + new Vector3(-width, -height, 0) * 0.5f, transform.position + new Vector3( width, -height, 0) * 0.5f);
		Gizmos.DrawLine(transform.position + new Vector3( width, -height, 0) * 0.5f, transform.position + new Vector3( width,  height, 0) * 0.5f);
		Gizmos.DrawLine(transform.position + new Vector3( width,  height, 0) * 0.5f, transform.position + new Vector3(-width,  height, 0) * 0.5f);
		Gizmos.DrawLine(transform.position + new Vector3(-width,  height, 0) * 0.5f, transform.position + new Vector3(-width, -height, 0) * 0.5f);

		if (impact0 != null)
		{
			foreach (var impact in impact0)
			{
				Gizmos.color = new Color(1, 0, 0, 0.05f);
				Gizmos.DrawLine(impact.position, impact.origin);
				Gizmos.color = new Color(1, 0, 0, 1);
				Gizmos.DrawSphere(impact.position, 0.05f);
			}
		}

		if (impact1 != null)
		{
			foreach (var impact in impact1)
			{
				Gizmos.color = new Color(0, 1, 0, 0.05f);
				Gizmos.DrawLine(impact.position, impact.origin);
				Gizmos.color = new Color(0, 1, 0, 1);
				Gizmos.DrawSphere(impact.position, 0.05f);
			}
		}

		if (impact2 != null)
		{
			foreach (var impact in impact2)
			{
				Gizmos.color = new Color(0, 0, 1, 0.05f);
				Gizmos.DrawLine(impact.position, impact.origin);
				Gizmos.color = new Color(0, 0, 1, 1);
				Gizmos.DrawSphere(impact.position, 0.05f);
			}
		}

		// Visualize grid points
		//Gizmos.color = new Color(1, 0, 0, 1);
		//for (int x = 0; x < points.GetLength(0); x++)
		//{
		//	for (int y = 0; y < points.GetLength(1); y++)
		//	{
		//		Gizmos.DrawSphere(points[x, y].position, 0.05f);
		//	}
		//}
	}

	void OnValidate ()
	{
		setup = false;
		UpdateMesh();
		if (lightMap)
			lightMap.filterMode = lightMapFilterMode;
		//SyncGlobals();
		UpdatePropertyBlock();
	}

	/*
	void OnDrawGizmosSelected ()
	{
		if (!setup)
			ReconstructGrid();

		for (int x = 0; x < points.GetLength(0); x++)
		{
			for (int y = 0; y < points.GetLength(1); y++)
			{
				Gizmos.color = points[x, y].color;
				//Gizmos.DrawSphere(points[x, y].position, cellSize * 0.5f);
				Gizmos.DrawCube(points[x, y].position, new Vector3(1f / (float)cellCount, 1f / (float)cellCount, 1f / (float)cellCount));
			}
		}
	}
	*/
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	static void CleanLightMaps ()
	{
		// Find and delete old lightmaps not in use
	}
}

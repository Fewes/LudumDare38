using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteLightPrioritizer : IComparer<SpriteLight>
{
	public int Compare(SpriteLight l1, SpriteLight l2)
	{
		// Radius compare
		return l2.radius.CompareTo(l1.radius);

		// Distance compare
		//float dist1 = Vector3.Distance(l1.transform.position, Camera.main.transform.position);
		//float dist2 = Vector3.Distance(l2.transform.position, Camera.main.transform.position);
		//return dist1.CompareTo(dist2);
	}
}

[ExecuteInEditMode]
public class LightManager : MonoBehaviour
{
	[Header("Ambient")]
	public Color					ambientColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
	public Color					ambientColor2 = new Color(0.1f, 0.1f, 0.1f, 0.1f);

	[Header("Directional")]
	[Range(-90, 90)]
	public float					directionalAngle = 0;
	[Range(0, 2)]
	public float					directionalDepth = 1;
	public Color					directionalColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
	public Gradient					dayColor;
	[Range(0, 1)]
	public float					directionalIntensity = 1;
	[Range(0, 1)]
	public float					directionalRim = 1;

	[Header("Style")]
	[Range(1, 64)]
	public int						lightSteps = 8;
	[Range(1, 64)]
	public int						shadeSteps = 3;

	[Header("Switches")]
	public bool						visualizeBounds;
	public bool						visualizeShadowCasters;
	[Range(0, 1)]
	public float					debugUnlit = 0;
	public bool						debug;

	[Header("Misc")]
	[SerializeField] Text			debugText;
	[SerializeField] Material		shadowMaterial;
	[SerializeField] RenderTexture	shadowTexture;
	[SerializeField] Camera			shadowCamera;
	[SerializeField] Shader			shadowShader;
	[SerializeField] RenderTexture	lightTexture;
	[SerializeField] Camera			lightCamera;
	private Camera					mainCamera;
	private Rect					cameraBounds;
	private SpriteLight[]			lights;
	private int						activeLights;

	// Directional light
	private GameObject				shadowObject;
	private MeshFilter				shadowFilter;
	private MeshRenderer			shadowRenderer;
	private Vector3					directionalDir;

	// Debug info
	private int						sceneLightCount;
	private int						boundsCulledLights;
	private int						distanceCulledLights;

	[SerializeField] bool			updateInSceneView = true;

	const int MAX_LIGHTS = 18;

	void Start ()
	{
		CheckCamera();

		CheckShadowMesh();

		CheckArray();

		if (Application.isPlaying)
			debugUnlit = 0;
	}

	void CheckCamera ()
	{
		if (!mainCamera)
			mainCamera = GetComponent<Camera>();
		mainCamera.transparencySortMode = TransparencySortMode.Orthographic;
	}

	public Vector3 GetSunDirection ()
	{
		return Quaternion.Euler(0, 0, -directionalAngle) * Vector3.down;
	}

	void CheckShadowMesh ()
	{
		if (!shadowObject)
		{
			shadowObject = GameObject.Find("GlobalShadowMesh");
			if (shadowObject)
			{
				shadowFilter = shadowObject.GetComponent<MeshFilter>();
				shadowRenderer = shadowObject.GetComponent<MeshRenderer>();
			}
			if (!shadowObject || !shadowFilter || !shadowRenderer)
			{
				shadowObject = new GameObject();
				shadowObject.name = "GlobalShadowMesh";
				shadowFilter = shadowObject.AddComponent<MeshFilter>();
				shadowFilter.sharedMesh = new Mesh();
				shadowFilter.sharedMesh.name = "ShadowMesh";
				shadowRenderer = shadowObject.AddComponent<MeshRenderer>();
				shadowRenderer.sharedMaterial = shadowMaterial;
			}
		}
		shadowObject.layer = LayerMask.NameToLayer("ShadowMesh");
		shadowObject.transform.position = new Vector3(0, 0, -1);
		shadowObject.transform.rotation = Quaternion.Euler(Vector3.zero);
		shadowObject.transform.localScale = Vector3.one;
	}

	void CheckArray ()
	{
		if (lights == null || lights.Length != MAX_LIGHTS)
			lights = new SpriteLight[MAX_LIGHTS];
	}

	public void EditorUpdate ()
	{
		if (!Application.isPlaying && updateInSceneView)
		{
			Update();
		}
	}
	
	void Update ()
	{
		if (!Application.isPlaying)
			CheckCamera();

		// Mirror camera settings to shadow & light camera
		shadowCamera.orthographic = mainCamera.orthographic;
		shadowCamera.orthographicSize = mainCamera.orthographicSize;

		lightCamera.orthographic = mainCamera.orthographic;
		lightCamera.orthographicSize = mainCamera.orthographicSize;

		if (!Application.isPlaying)
		{
			CheckShadowMesh();
			CheckArray();

			Camera camera = Camera.current;
			if (camera)
			{
				Camera mainCam = Camera.main;
				if (camera != mainCam)
				{
					var camPos = camera.transform.position;
					camPos.z = mainCam.transform.position.z;
					mainCam.transform.position = camPos;

					if (camera.orthographic)
					{
						mainCam.orthographic = camera.orthographic;
						mainCam.orthographicSize = camera.orthographicSize;
					}
				}
			}

			DoLighting();
		}

		debugText.gameObject.SetActive(debug);
		if (debugText)
		{
			debugText.text  = "---------------------------\n";
			debugText.text += " Lighting debug info\n";
			debugText.text += "---------------------------\n";
			debugText.text += " Scene lights: " + sceneLightCount + "\n";
			debugText.text += " Rendered lights: " + activeLights + "\n";
			debugText.text += " Bounds culled lights: " + boundsCulledLights + "\n";
			debugText.text += " Radii culled lights: " + distanceCulledLights + "\n";
			debugText.text += "---------------------------\n";
		}
	}

	void OnPreCull()
	{
		if (!Application.isPlaying)
		{
			CheckShadowMesh();
			CheckArray();
		}

		DoLighting();
	}

	void DoLighting ()
	{
		UpdateCameraBounds();

		UpdateDirectionalLight();

		RenderShadows();

		UpdateLightConstants();
	}

	void OnValidate()
	{
		CheckArray();
		UpdateLightConstants();
	}

	void RenderShadows ()
	{
		shadowCamera.enabled = false;

		// Draw shadow mesh
		shadowCamera.clearFlags = CameraClearFlags.SolidColor;
		shadowCamera.backgroundColor = new Color(0, 0, 0, 1);
		int cullMask = shadowCamera.cullingMask;
		shadowCamera.Render();

		// Draw shadowed objects
		shadowCamera.clearFlags = CameraClearFlags.Nothing;
		shadowCamera.cullingMask = -1;
		shadowCamera.RenderWithShader(shadowShader, "");
		shadowCamera.cullingMask = cullMask;

		lightCamera.enabled = false;
		lightCamera.Render();
	}

	void UpdateCameraBounds ()
	{
		// Build camera bounds
		Camera camera = Camera.main;
		Vector2 cameraSpan = new Vector2(camera.orthographicSize * 2 * camera.aspect, camera.orthographicSize * 2);
		cameraBounds = new Rect(camera.transform.position - new Vector3(cameraSpan.x, cameraSpan.y, 0) * 0.5f, cameraSpan);

		// Used for shadow buffer UV calculation
		Shader.SetGlobalVector( "g_vCameraBounds", new Vector4(cameraBounds.xMin, cameraBounds.yMin, cameraBounds.xMax, cameraBounds.yMax));

		if (visualizeBounds) DrawRect(cameraBounds, new Color(0, 1, 0, 0.025f));
	}

	void UpdateDirectionalLight ()
	{
		directionalDir = Quaternion.Euler(0, 0, -directionalAngle) * Vector3.down;
		float shadowLength = 9999f;

		// Shadow casters
		var shadowCasters = GameObject.FindGameObjectsWithTag("ShadowCaster");

		// Count shadow mesh vertices
		int shadowVertCount = 0;
		foreach (var shadowCaster in shadowCasters)
		{
			var boxCollider2D = shadowCaster.GetComponent<BoxCollider2D>();
			if (boxCollider2D)
				shadowVertCount += 4;
		}

		Vector3[] shadowVerts = new Vector3[shadowVertCount * 2];
		Color[] shadowColors = new Color[shadowVertCount * 2];
		int[] shadowIndices = new int[shadowVertCount * 2 * 3];

		int i = 0;
		foreach (var shadowCaster in shadowCasters)
		{
			var boxCollider2D = shadowCaster.GetComponent<BoxCollider2D>();

			Color shadowColor = new Color(1, 0, 0, 1);

			var shadowParams = shadowCaster.GetComponent<ShadowParams>();
			if (shadowParams)
			{
				shadowColor = new Color(shadowParams.r ? 1 : 0, shadowParams.g ? 1 : 0, shadowParams.b ? 1 : 0, shadowParams.intensity);
			}

			if (boxCollider2D)
			{
				Vector3 pos = boxCollider2D.transform.position + new Vector3(boxCollider2D.offset.x * boxCollider2D.transform.lossyScale.x, boxCollider2D.offset.y * boxCollider2D.transform.lossyScale.y);

				Vector3 p0 = pos
					- boxCollider2D.transform.lossyScale.x * boxCollider2D.transform.right * boxCollider2D.size.x * 0.5f
					- boxCollider2D.transform.lossyScale.y * boxCollider2D.transform.up    * boxCollider2D.size.y * 0.5f;
				Vector3 p1 = pos
					+ boxCollider2D.transform.lossyScale.x * boxCollider2D.transform.right * boxCollider2D.size.x * 0.5f
					- boxCollider2D.transform.lossyScale.y * boxCollider2D.transform.up    * boxCollider2D.size.y * 0.5f;
				Vector3 p2 = pos
					+ boxCollider2D.transform.lossyScale.x * boxCollider2D.transform.right * boxCollider2D.size.x * 0.5f
					+ boxCollider2D.transform.lossyScale.y * boxCollider2D.transform.up    * boxCollider2D.size.y * 0.5f;
				Vector3 p3 = pos
					- boxCollider2D.transform.lossyScale.x * boxCollider2D.transform.right * boxCollider2D.size.x * 0.5f
					+ boxCollider2D.transform.lossyScale.y * boxCollider2D.transform.up    * boxCollider2D.size.y * 0.5f;

				// Vertices
				shadowVerts[i * 8 + 0] = p0;
				shadowVerts[i * 8 + 1] = p1;
				shadowVerts[i * 8 + 2] = p2;
				shadowVerts[i * 8 + 3] = p3;
				shadowVerts[i * 8 + 4] = p0 + directionalDir * shadowLength;
				shadowVerts[i * 8 + 5] = p1 + directionalDir * shadowLength;
				shadowVerts[i * 8 + 6] = p2 + directionalDir * shadowLength;
				shadowVerts[i * 8 + 7] = p3 + directionalDir * shadowLength;

				// Quad 0, triangle 1
				shadowIndices[i * 24 + 0 ] = i * 8 + 0;
				shadowIndices[i * 24 + 1 ] = i * 8 + 1;
				shadowIndices[i * 24 + 2 ] = i * 8 + 5;
				// Quad 0, triangle 2
				shadowIndices[i * 24 + 3 ] = i * 8 + 0;
				shadowIndices[i * 24 + 4 ] = i * 8 + 5;
				shadowIndices[i * 24 + 5 ] = i * 8 + 4;

				// Quad 1, triangle 1
				shadowIndices[i * 24 + 6 ] = i * 8 + 1;
				shadowIndices[i * 24 + 7 ] = i * 8 + 2;
				shadowIndices[i * 24 + 8 ] = i * 8 + 6;
				// Quad 1, triangle 2
				shadowIndices[i * 24 + 9 ] = i * 8 + 1;
				shadowIndices[i * 24 + 10] = i * 8 + 6;
				shadowIndices[i * 24 + 11] = i * 8 + 5;

				// Quad 2, triangle 1
				shadowIndices[i * 24 + 12] = i * 8 + 2;
				shadowIndices[i * 24 + 13] = i * 8 + 3;
				shadowIndices[i * 24 + 14] = i * 8 + 7;
				// Quad 2, triangle 2
				shadowIndices[i * 24 + 15] = i * 8 + 2;
				shadowIndices[i * 24 + 16] = i * 8 + 7;
				shadowIndices[i * 24 + 17] = i * 8 + 6;

				// Quad 3, triangle 1
				shadowIndices[i * 24 + 18] = i * 8 + 3;
				shadowIndices[i * 24 + 19] = i * 8 + 0;
				shadowIndices[i * 24 + 20] = i * 8 + 4;
				// Quad 3, triangle 2
				shadowIndices[i * 24 + 21] = i * 8 + 3;
				shadowIndices[i * 24 + 22] = i * 8 + 4;
				shadowIndices[i * 24 + 23] = i * 8 + 7;

				// Colors
				shadowColors[i * 8 + 0] = shadowColor;
				shadowColors[i * 8 + 1] = shadowColor;
				shadowColors[i * 8 + 2] = shadowColor;
				shadowColors[i * 8 + 3] = shadowColor;
				shadowColors[i * 8 + 4] = shadowColor;
				shadowColors[i * 8 + 5] = shadowColor;
				shadowColors[i * 8 + 6] = shadowColor;
				shadowColors[i * 8 + 7] = shadowColor;

				i++;

				if (visualizeShadowCasters)
				{
					Debug.DrawLine(p0, p1);
					Debug.DrawLine(p1, p2);
					Debug.DrawLine(p2, p3);
					Debug.DrawLine(p3, p0);
				}
			}
		}

		shadowFilter.sharedMesh.vertices = shadowVerts;
		shadowFilter.sharedMesh.triangles = shadowIndices;
		shadowFilter.sharedMesh.colors = shadowColors;
	}

	void UpdateLightConstants ()
	{
		CullLights();

		//Debug.Log(activeLights);

		Vector4[] positionInvRadius = new Vector4[MAX_LIGHTS];
		Vector4[] position2Width	= new Vector4[MAX_LIGHTS];
		Vector4[] color				= new Vector4[MAX_LIGHTS];
		Vector4[] rimIntensity		= new Vector4[MAX_LIGHTS];
		Vector4[] falloff			= new Vector4[MAX_LIGHTS];

		for (int i = 0; i < activeLights; i++)
		{
			if (lights[i].width < Mathf.Epsilon)
			{
				// Regular light
				positionInvRadius[i]	= new Vector4(lights[i].transform.position.x, lights[i].transform.position.y, lights[i].transform.position.z, 1.0f / lights[i].radius);
				position2Width[i]		= new Vector4(lights[i].transform.position.x, lights[i].transform.position.y, lights[i].transform.position.z, lights[i].width);
			}
			else
			{
				// Line light
				Vector3 pos1 = lights[i].transform.position + lights[i].transform.right * lights[i].width * 0.5f;
				Vector3 pos2 = lights[i].transform.position - lights[i].transform.right * lights[i].width * 0.5f;
				positionInvRadius[i]	= new Vector4(pos1.x, pos1.y, pos1.z, 1.0f / lights[i].radius);
				position2Width[i]		= new Vector4(pos2.x, pos2.y, pos2.z, lights[i].width);
			}
			color[i]					= new Vector4(lights[i].color.r, lights[i].color.g, lights[i].color.b, lights[i].color.a);
			rimIntensity[i]				= new Vector4(lights[i].intensity * lights[i].intensityMultiplier, lights[i].rimIntensity * lights[i].intensityMultiplier, lights[i].rimSize, lights[i].frontFacing ? 1.0f : -1.0f);
			falloff[i]					= new Vector4(Mathf.Max(lights[i].falloff, 0.001f), 1, 1, 1);
		}

		Shader.SetGlobalVector( "g_vAmbientLight", new Vector4(ambientColor.r, ambientColor.g, ambientColor.b, ambientColor.a));
		Shader.SetGlobalVector( "g_vAmbientLight2", new Vector4(ambientColor2.r, ambientColor2.g, ambientColor2.b, ambientColor2.a));
		Shader.SetGlobalTexture( "g_tDirectionalShadow", shadowTexture);
		Shader.SetGlobalTexture( "g_tLightBuffer", lightTexture);
		//Shader.SetGlobalVector( "g_tDirectionalColor", new Vector4(directionalColor.r, directionalColor.g, directionalColor.b, directionalRim));
		Color col = dayColor.Evaluate((directionalAngle + 90f) / 180f);
		Shader.SetGlobalVector( "g_tDirectionalColor", new Vector4(col.r, col.g, col.b, directionalRim));
		Shader.SetGlobalVector( "g_tDirectionalDir", new Vector4(directionalDir.x, directionalDir.y, directionalDir.z - directionalDepth, directionalIntensity));
		Shader.SetGlobalInt( "g_nLightSteps", lightSteps );
		Shader.SetGlobalInt( "g_nShadeSteps", shadeSteps );
		Shader.SetGlobalInt( "g_nNumLights", activeLights );
		Shader.SetGlobalFloat( "g_bUnlit", debugUnlit);
		Shader.SetGlobalVectorArray( "g_vLightPosition_flInvRadius", positionInvRadius );
		Shader.SetGlobalVectorArray( "g_vLightPosition2_fWidth", position2Width );
		Shader.SetGlobalVectorArray( "g_vLightColor", color );
		Shader.SetGlobalVectorArray( "g_fLightIntensity_fRimIntensity_fRimSize_fBackFront", rimIntensity );
		Shader.SetGlobalVectorArray( "g_fLightFalloff", falloff );
	}

	void CullLights()
	{
		var spriteLights = FindObjectsOfType<SpriteLight>();
		List<SpriteLight> visibleLights = new List<SpriteLight>();

		sceneLightCount = spriteLights.Length;

		// Gather visible lights
		foreach (var light in spriteLights)
		{
			if (!light.isActiveAndEnabled)
				continue;

			// Build light bounds
			Vector3 pos1 = light.Position1();
			Vector3 pos2 = light.Position2();
			float xMin = Mathf.Min(pos1.x, pos2.x) - light.radius;
			float xMax = Mathf.Max(pos1.x, pos2.x) + light.radius;
			float yMin = Mathf.Min(pos1.y, pos2.y) - light.radius;
			float yMax = Mathf.Max(pos1.y, pos2.y) + light.radius;

			Rect lightBounds = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

			//Rect lightBounds = new Rect(new Vector2(light.transform.position.x - light.radius, light.transform.position.y - light.radius), new Vector2(light.radius * 2, light.radius * 2));
			if (visualizeBounds) DrawRect(lightBounds, new Color(light.color.r, light.color.g, light.color.b, 0.025f));
			if (cameraBounds.Overlaps(lightBounds))
				visibleLights.Add(light);
		}

		boundsCulledLights = sceneLightCount - visibleLights.Count;
		int temp = visibleLights.Count;

		// Cull lights based on distance to center of screen (if there are more lights active within the camera bounds than are allowed)
		if (visibleLights.Count > MAX_LIGHTS)
		{
			// Sort based on distance to center of screen
			visibleLights.Sort(new SpriteLightPrioritizer());

			// Pop last light until list fits into buffer
			while (visibleLights.Count > MAX_LIGHTS)
				visibleLights.RemoveAt(visibleLights.Count - 1);
		}

		distanceCulledLights = temp - visibleLights.Count;

		// Set active lights
		for (int i = 0; i < visibleLights.Count; i++)
			lights[i] = visibleLights[i];
		activeLights = visibleLights.Count;
	}

	void DrawRect (Rect r, Color color)
	{
		Debug.DrawLine(new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin), color);
		Debug.DrawLine(new Vector3(r.xMax, r.yMin), new Vector3(r.xMax, r.yMax), color);
		Debug.DrawLine(new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax), color);
		Debug.DrawLine(new Vector3(r.xMin, r.yMax), new Vector3(r.xMin, r.yMin), color);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteLight : MonoBehaviour
{
	public Color		color = Color.white;
	[Range(0, 1)]
	public float		intensity = 1;
	[Range(0, 1)]
	public float		rimIntensity = 1;
	public float		intensityMultiplier = 1;
	[Range(0, 0.5f)]
	public float		rimSize = 0.1f;
	[Range(0, 16)]
	public float		radius = 1;
	[Range(0, 32)]
	public float		width = 1;
	[Range(0, 8)]
	public float		falloff = 1;
	public bool			frontFacing = false;

	void Start ()
	{
		
	}
	
	void Update ()
	{
		
	}

	public Vector3 Position1 ()
	{
		return transform.position + transform.right * width * 0.5f;
	}

	public Vector3 Position2 ()
	{
		return transform.position - transform.right * width * 0.5f;
	}

	void OnDrawGizmos ()
	{
		Gizmos.color = new Color(color.r, color.g, color.b);
		Gizmos.DrawLine(Position1(), Position2());
		//Gizmos.DrawWireSphere(transform.position, radius);
		//Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
		//Gizmos.DrawSphere(transform.position, 0.1f);
		Gizmos.DrawIcon(transform.position, "light.png", false);
	}

	void OnDrawGizmosSelected ()
	{
		//Gizmos.color = new Color(color.r, color.g, color.b);
		//Gizmos.DrawLine(Position1(), Position2());
		//Gizmos.DrawWireSphere(transform.position, radius);
		//Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
		//Gizmos.DrawSphere(transform.position, 0.1f);
		Gizmos.DrawIcon(transform.position, "light_selected.png", false);
	}

	//void OnInspectorGUI ()
	//{
	//	FindObjectOfType<LightManager>().UpdateLightConstants();
	//}

	void OnValidate()
	{
		if (intensityMultiplier < 0)
			intensityMultiplier = 0;
	}
}

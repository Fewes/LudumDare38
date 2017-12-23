using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
	static List<CameraZone> stackedZones;

	[Range(0, 10)]
	public float			cameraDistance = 3f;
	[Range(0, 2)]
	public float			cameraDrag = 0.1f;
	[Range(0, 1)]
	public float			playerPositionFactor = 0.5f;
	public bool				usePlayerDistance;
	public bool				usePlayerDrag;

	void Start ()
	{
		if (stackedZones == null)
			stackedZones = new List<CameraZone>();
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		var player = other.GetComponent<Player>();
		if (player)
		{
			stackedZones.Add(this);
			CameraManager.SetActiveCameraZone(stackedZones[stackedZones.Count-1]);
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		var player = other.GetComponent<Player>();
		if (player)
		{
			stackedZones.Remove(this);
			if (stackedZones.Count > 0)
				CameraManager.SetActiveCameraZone(stackedZones[stackedZones.Count-1]);
			else
				CameraManager.SetActiveCameraZone(null);
		}
	}

	void OnDrawGizmosSelected ()
	{
		// TODO: Calculate and preview camera bounds here
	}

	//void OnValidate ()
	//{
	//	Camera camera = CameraManager.GetMainCamera().GetComponent<Camera>();
	//	if (camera)
	//	{
	//		var pos = transform.position;
	//		pos.z = -cameraDistance;
	//		camera.transform.position = pos;
	//		camera.orthographicSize = cameraDistance;
	//	}
	//}
}

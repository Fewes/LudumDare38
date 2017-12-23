using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
	private static CameraManager			manager;

	public Transform						mainCamera;
	public Player							player;

	private CameraZone						activeCameraZone;

	// Use this for initialization
	void Start ()
	{
		if (manager && manager != this)
			Destroy(manager);
		manager = this;
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	void LateUpdate ()
	{
		Vector3 targetPosition = Vector3.zero;
		float orthographicSize = 5f;
		float cameraDrag = 0.1f;

		if (activeCameraZone)
		{
			targetPosition = activeCameraZone.transform.position;
			orthographicSize = activeCameraZone.cameraDistance;
			cameraDrag = activeCameraZone.cameraDrag;
		}
		if (player)
		{
			if (activeCameraZone)
				targetPosition = Vector3.Lerp(targetPosition, player.GetCameraTargetPosition(), activeCameraZone.playerPositionFactor);
			else
				targetPosition = player.GetCameraTargetPosition();
			if (!activeCameraZone || activeCameraZone.usePlayerDistance)
				orthographicSize = player.GetCameraDistance();
			if (!activeCameraZone || activeCameraZone.usePlayerDrag)
				cameraDrag = player.GetCameraDrag();
		}

		// Update camera position
		targetPosition.z = mainCamera.position.z;
		Vector3 cameraPos = Vector3.Lerp(mainCamera.position, targetPosition, Time.deltaTime / cameraDrag);
		mainCamera.position = cameraPos;

		// Update camera distance
		float cameraDist = Mathf.Lerp(mainCamera.GetComponent<Camera>().orthographicSize, orthographicSize, Time.deltaTime / cameraDrag * 0.5f);
		cameraPos.z = -cameraDist;
		mainCamera.GetComponent<Camera>().orthographicSize = cameraDist;
	}

	private void SetCameraZone (CameraZone zone)
	{
		activeCameraZone = zone;
	}

	public static Transform GetMainCamera ()
	{
		if (!manager)
			return FindObjectOfType<CameraManager>().mainCamera; // Editor fix
		return manager.mainCamera;
	}

	public static void SetActiveCameraZone (CameraZone zone)
	{
		manager.SetCameraZone(zone);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionAnticipator : MonoBehaviour
{
	public bool				debug;

	[SerializeField] int	framesOfAnticipation = 15;

	private List<Vector3>	trackedFrames;
	private Vector3			lastPos;

	void Start ()
	{
		// Initialize and fill list
		trackedFrames = new List<Vector3>();
		for (int i = 0; i < framesOfAnticipation; i++)
			trackedFrames.Add(transform.position);
	}
	
	void LateUpdate ()
	{
		Vector3 frameVelocity = transform.position - lastPos;

		// Erase oldest element
		trackedFrames.RemoveAt(0);
		// Store new position
		trackedFrames.Add(transform.position + frameVelocity * framesOfAnticipation);

		if (debug)
		{
			Vector3 prevPos = trackedFrames[0];
			int i = 0;
			foreach (var pos in trackedFrames)
			{
				Debug.DrawLine(pos, prevPos, new Color(1, 1, 0, 1 - (float)i / (float)trackedFrames.Count));
				prevPos = pos;
				i++;
			}

			//Vector3 prevPos = transform.position;
			//for (int i = trackedFrames.Count - 1; i >= 0; i--)
			//{
			//	Debug.DrawLine(trackedFrames[i], prevPos, Color.yellow);
			//	prevPos = trackedFrames[i];
			//}
		}

		lastPos = transform.position;
	}

	public Vector3 GetAnticipatiedPosition ()
	{
		return trackedFrames[0];
	}
}

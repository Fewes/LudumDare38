#if UNITY_EDITOR 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LightGrid))]
public class LightGridEditor : Editor
{
	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector();

		GUILayout.Space(8);

		LightGrid lg = (LightGrid)target;
		//if (GUILayout.Button("Reconstruct"))
		//{
		//	lg.ReconstructGrid();
		//}
		if (GUILayout.Button("Bake"))
		{
			lg.Bake();
		}
		//if (GUILayout.Button("Bake 2"))
		//{
		//	lg.Bake2();
		//}
		if (GUILayout.Button("Bake All"))
		{
			lg.BakeAll(false);
		}
		if (GUILayout.Button("Transfer Settings"))
		{
			lg.SyncGlobals();
		}
		if (GUILayout.Button("Transfer & Bake All"))
		{
			lg.BakeAll();
		}
		//if (GUILayout.Button("Update Property Block"))
		//{
		//	lg.UpdatePropertyBlock();
		//}
	}
}
#endif
 using UnityEditor;
 using UnityEngine;
 
[InitializeOnLoad]
public static class EditorHotkeysTracker
{
	static EditorHotkeysTracker()
	{
		SceneView.onSceneGUIDelegate += view =>
		{
			Event e = Event.current;
			if (e != null && e.keyCode != KeyCode.None)
			{
				// Deselect All
				if (e.keyCode == KeyCode.Escape)
					Selection.activeGameObject = null;
			}
		};
	}
}
using UnityEditor;
using UnityEngine;

public class CameraPositionGetter : EditorWindow
{
    [MenuItem("Window/Get Camera Position and Rotation")]
    public static void ShowWindow()
    {
        GetWindow<CameraPositionGetter>("Camera Position and Rotation");
    }

    private void OnGUI()
    {
        if (SceneView.lastActiveSceneView == null)
        {
            GUILayout.Label("Open a Scene view in the Unity Editor.");
        }
        else
        {
            Camera camera = SceneView.lastActiveSceneView.camera;
            Vector3 cameraPosition = camera.transform.position;
            Quaternion cameraRotation = camera.transform.rotation;

            GUILayout.Label("Camera Position: " + cameraPosition);
            GUILayout.Label("Camera Rotation: " + cameraRotation.eulerAngles);
        }
    }
}

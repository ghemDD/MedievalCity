using UnityEngine;
using UnityEditor;

public class PrototypeCreator : EditorWindow
{
    [MenuItem("Assets/Create/Prototype")]
    public static void CreatePrototype()
    {
        Prototype newPrototype = ScriptableObject.CreateInstance<Prototype>();
        AssetDatabase.CreateAsset(newPrototype, "Assets/Prototypes/NewPrototype.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newPrototype;
    }
}

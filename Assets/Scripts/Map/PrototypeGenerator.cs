using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class PrototypeGenerator : MonoBehaviour
{
    public List<Prototype> prototypes;

    private void Start() {
        GenerateRotationsPrototypes();
    }

    /**
        Generate the rotated versions of the prototypes, using the meshRotation argument as the angle of rotation meshRotation * 90deg with respect to the up vector Y : rotate the socket list, meshRotation argument from 0 to 3, keeping the same prefab
    **/
    public void GenerateRotationsPrototypes()
    {
        List<Prototype> rotatedPrototypes = new List<Prototype>();

        foreach (Prototype prototype in prototypes)
        {
            for (int i = 1; i < 4; i++)
            {
                Prototype rotatedPrototype = Instantiate(prototype);
                rotatedPrototype.meshRotation = i;

                // Rotate the sockets
                for (int j = 0; j < i; j++)
                {
                    // Rotate the socket lists by swapping adjacent sockets
                    Socket temp = rotatedPrototype.posX;
                    rotatedPrototype.posX = rotatedPrototype.posZ;
                    rotatedPrototype.posZ = rotatedPrototype.negX;
                    rotatedPrototype.negX = rotatedPrototype.negZ;
                    rotatedPrototype.negZ = temp;
                }

                rotatedPrototypes.Add(rotatedPrototype);
            }
        }
        
        prototypes.AddRange(rotatedPrototypes);
        SavePrototypes(prototypes);
    }

    public void SavePrototypes(List<Prototype> prototypes)
    {
        string folderPath = "Assets/TestPrototypes";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "TestPrototypes");
        }

        for (int i = 0; i < prototypes.Count; i++)
        {
            string filePath = $"{folderPath}/{prototypes[i].prefab.name}_{i}.asset";
            Prototype prototypeObject = AssetDatabase.LoadAssetAtPath<Prototype>(filePath);

            if (prototypeObject == null)
            {
                prototypeObject = ScriptableObject.CreateInstance<Prototype>();
                AssetDatabase.CreateAsset(prototypeObject, filePath);
            }

            prototypeObject.meshRotation = prototypes[i].meshRotation;
            prototypeObject.prefab = prototypes[i].prefab;
            prototypeObject.posX = prototypes[i].posX;
            prototypeObject.negX = prototypes[i].negX;
            prototypeObject.posY = prototypes[i].posY;
            prototypeObject.negY = prototypes[i].negY;
            prototypeObject.posZ = prototypes[i].posZ;
            prototypeObject.negZ = prototypes[i].negZ;

            EditorUtility.SetDirty(prototypeObject);
            AssetDatabase.SaveAssets();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

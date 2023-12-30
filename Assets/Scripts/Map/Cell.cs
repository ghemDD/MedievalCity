using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour {
    public GameObject instantiated;
    public bool isCollapsed;

    // possible game objects 
    public List<Prototype> possibilities = new List<Prototype>();

    public Vector3 translate;
    public int meshRotation;

    // corresponding weights of the possible game objects 
    public List<float> weights;

    // coordinate of the cell in the grid 
    public Vector3 coords = new Vector3(0, 0, 0);

    // neighbours 
    public Cell posZneighbour;
    public bool hasPosZneighbour;

    public Cell negZneighbour;
    public bool hasNegZneighbour;

    public Cell posXneighbour;
    public bool hasPosXneighbour;

    public Cell negXneighbour;
    public bool hasNegXneighbour;

    public Cell posYneighbour;
    public bool hasPosYneighbour;

    public Cell negYneighbour;
    public bool hasNegYneighbour;

    public void GenerateWeight() {
        weights = new List<float>(new float[possibilities.Count]);
        int i = 0;

        foreach(Prototype p in possibilities) {
            weights[i] = p.weight;
            i++;
        }
    }

    public void neighbourConstraints(int width, int height, int depth) {
        int x = (int)coords.x;
        int y = (int)coords.y;
        int z = (int)coords.z;

        if (x > 0) {
            hasNegXneighbour = true;
        }

        if (x < width - 1) {
            hasPosXneighbour = true;
        }

        if (y > 0) {
            hasNegYneighbour = true;
        }

        if (y < height - 1) {
            hasPosYneighbour = true;
        }

        if (z > 0) {
            hasNegZneighbour = true;
        }

        if (z < depth - 1) {
            hasPosZneighbour = true;
        }
    }

    public bool Equals(Cell obj) {
        if (obj == null || GetType() != obj.GetType()) {
            return false;
        }
    
        return (obj.coords == coords);
    }
}

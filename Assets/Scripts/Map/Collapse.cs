using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;


public class Collapse : MonoBehaviour {
    public List<Prototype> prototypes;
    public List<Prototype> emptyPrototype;

    public Vector2 size;
    public Vector3 start_position;
    public List<Cell> cells;
    public List<Cell> cellsAffected = new List<Cell>();
    public GameObject[] prefabs;
    public GameObject[] walls;
    public int width, height, depth;
    public float tileSize;
    private Cell[,,] grid;
    private int currentLevel;
    public Vector2 tilingTexture = new Vector2(5f, 5f); 
    
    public GameObject plane;
    public List<GameObject> buildings;
    public List<Vector3> positionBuildings;
    public List<Vector3> rotationBuildings;
    public List<Vector3> boundingBuildings;

    public List<Texture2D> pavementTextures;
    public List<int> pavementIndices;

    private void SetupPavementIndices() {

        for (int i = 0; i < depth; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pavementTextures.Count);
            pavementIndices.Add(randomIndex);
        }
    }

    private void Start() {
        SetupPavementIndices();
        LoadPrototypes();
        // Uncomment if you want to see the progressive generation and comment StartCollapse()
        //StartCoroutine(CollapseOverTime());
        StartCollapse();
    }

    private void LoadPrototypes() {
        prototypes = new List<Prototype>();
        emptyPrototype = new List<Prototype>();

        string[] assetGuids = AssetDatabase.FindAssets("t:Prototype", new[] { "Assets/TestPrototypes" });

        foreach (string guid in assetGuids) {
            // load the asset at the given GUID
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Prototype prototype = AssetDatabase.LoadAssetAtPath<Prototype>(path);

            if (prototype != null)
            {
                prototypes.Add(prototype);
                Debug.Log("Load Prototypes : Added prototype "+prototype);
            }

            if (prototype.prefab.name == "empty") {
                emptyPrototype.Add(prototype);
            }
        }
    }

    public void InitializeWaveFunction() {
        currentLevel = 0;
        grid = new Cell[width, height, depth];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                for (int z = 0; z < depth; z++) {
                    grid[x, y, z] = new Cell();

                    grid[x, y, z].coords = new Vector3(x, y, z);

                    if (y > 0) {
                        grid[x, y, z].possibilities = new List<Prototype>(prototypes);
                    } else {
                        grid[x, y, z].possibilities = new List<Prototype>(emptyPrototype); 
                    }
                }
            }
        }


        foreach(Cell c in grid) {
            c.neighbourConstraints(width, height, depth);
            FindNeighbours(c);
            c.GenerateWeight();
        }
    }

    private void FindNeighbours(Cell c) {
        c.posZneighbour = GetCell(c.coords.x, c.coords.y, c.coords.z+1);
        c.negZneighbour = GetCell(c.coords.x, c.coords.y, c.coords.z-1);
        c.posXneighbour = GetCell(c.coords.x+1, c.coords.y, c.coords.z);
        c.negXneighbour = GetCell(c.coords.x-1, c.coords.y, c.coords.z);
        c.posYneighbour = GetCell(c.coords.x, c.coords.y+1, c.coords.z);
        c.negYneighbour = GetCell(c.coords.x, c.coords.y-1, c.coords.z);
    }

    private Cell GetCell(float x, float y, float z) {
        if (x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth) {
            Cell cell = grid[(int) x, (int) y, (int) z];
            return cell;
        }
            
        return null;
    }

    int collapsed;

    public void StartCollapse() {
        InitializeWaveFunction();
        collapsed=0;
        while(!isCollapsed()) {
            Iterate();
        }

        CreateBorder();
        PlaceBuildings();
    }

    public IEnumerator CollapseOverTime() {
        InitializeWaveFunction();
        while(!isCollapsed())
        {
            Iterate();
            yield return new WaitForSeconds(0.1f);
        }

        StartCoroutine(CreateBorderOverTime(0.1f));
        StartCoroutine(PlaceBuildingsOverTime(0.5f));
    }

    private bool isCollapsed() {
        foreach(Cell c in grid) {
            if (!c.isCollapsed) {
                return false;
            } 
        }

        return true;
    }

    private void Iterate() {
        Cell cell = SelectLowestEntropyCell();
        CollapseAt(cell);
        currentLevel = collapsed / (width  * depth);
        Propagate(cell);
    }

    private Cell SelectLowestEntropyCell() {
        List<Cell> cellWithLowestEntropy = new List<Cell>();
        int x = 10000;

        foreach(Cell c in grid) {
            if(!c.isCollapsed) {
                if(c.possibilities.Count==x && c.coords.y == currentLevel) {
                    cellWithLowestEntropy.Add(c);
                }
                
                else if(c.possibilities.Count<x && c.coords.y == currentLevel) {
                    cellWithLowestEntropy.Clear();
                    cellWithLowestEntropy.Add(c);
                    x = c.possibilities.Count;
                }
            }
        }

        return cellWithLowestEntropy[UnityEngine.Random.Range(0, cellWithLowestEntropy.Count)];
    }


     private void CollapseAt(Cell cell) {
        int selectedPrototype = SelectPrototype(cell.weights);
        Prototype finalPrototype = cell.possibilities[selectedPrototype];

        cell.translate = finalPrototype.translate;
        cell.meshRotation = finalPrototype.meshRotation;
        
        // Adapt the meshRotation 
        if (finalPrototype.parentLinked) {
            if (cell.hasNegYneighbour) {
                Cell negYneighbour = cell.negYneighbour;
                if (negYneighbour.isCollapsed & negYneighbour.possibilities[0].prefab.name != "empty") {
                    cell.meshRotation = cell.negYneighbour.meshRotation;
                    cell.translate += cell.negYneighbour.translate;
                    Vector3 neighPos = cell.negYneighbour.coords;
                }
            }
        }

        Vector3 trans = cell.translate;
        int meshRot = cell.meshRotation;

        finalPrototype.prefab = cell.possibilities[selectedPrototype].prefab;
        cell.possibilities.Clear();
        cell.possibilities.Add(finalPrototype);
        Vector3 position = new Vector3(cell.coords.x * tileSize, cell.coords.y * tileSize, cell.coords.z * tileSize);
        
        if (finalPrototype.prefab.name.Contains("tall_house") || finalPrototype.prefab.name.Contains("tall_thin_house")) {
            GameObject finalPrefab = Instantiate(finalPrototype.prefab, start_position + trans + position, Quaternion.Euler(-90, meshRot*90f + Convert.ToInt32(finalPrototype.random_rotation) * UnityEngine.Random.Range(0, 20), 0), transform);
            cell.instantiated = finalPrefab;
        } else {
            GameObject finalPrefab = Instantiate(finalPrototype.prefab, start_position + trans + position, Quaternion.Euler(finalPrototype.rotate.x, meshRot*90f + Convert.ToInt32(finalPrototype.random_rotation) * UnityEngine.Random.Range(0, 20), 0), transform);
            cell.instantiated = finalPrefab;
        }

        // ground placement 
        if (cell.coords.y == 1) {
            // set different pavement texture for each row
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", pavementTextures[pavementIndices[(int)cell.coords.z]]);
            material.SetTextureScale("_BaseMap", tilingTexture);
            Renderer renderer = plane.GetComponent<Renderer>();
            renderer.material = material;
            Instantiate(plane, start_position + position + new Vector3(0f, 0.3f, 0f), Quaternion.Euler(-90, 0, 0), transform);
        }
        
        collapsed++;
        cell.isCollapsed = true;
    }


    private int SelectPrototype(List<float> prototypeWeights) {
        // Calculate the total weight of all the prototypes
        float totalWeight = 0f;
        foreach (float weight in prototypeWeights)
        {
            totalWeight += weight;
        }

        float randomNumber = UnityEngine.Random.Range(0f, totalWeight);

        // iterate through the prototypes and return the index of the selected one
        float weightSum = 0f;
        for (int i = 0; i < prototypeWeights.Count; i++)
        {
            weightSum += prototypeWeights[i];
            if (randomNumber < weightSum) {
                return i;
            }
        }

        return 0;
    }


    private void Propagate(Cell cell) {
        cellsAffected.Add(cell);

        while(cellsAffected.Count > 0)
        {
            Cell currentCell = cellsAffected[0];
            cellsAffected.Remove(currentCell);
            Cell otherCell = currentCell.posXneighbour;

            if (currentCell.hasPosXneighbour)
            {
                otherCell = currentCell.posXneighbour;
                List<Socket> possibleConnections = GetPossibleSocketsPosX(currentCell.possibilities);

                bool constrained = false;
                for (int i = 0; i < otherCell.possibilities.Count; i++)
                {
                    // check compatibility of the right sockets of the left neighbour with respect to the left socket of our current cell
                    if(!possibleConnections.Any(possibleConnection => SocketCompatibility.GetCompatibleSockets(otherCell.possibilities[i].negX).Contains(possibleConnection)))
                    {
                        otherCell.possibilities.RemoveAt(i);
                        otherCell.weights.RemoveAt(i);
                        i-=1;
                        constrained = true;
                    }
                }

                if(constrained)
                    cellsAffected.Add(otherCell);
            }

            
            if(currentCell.hasPosZneighbour)
            {
                otherCell = currentCell.posZneighbour;
                List<Socket> possibleConnections = GetPossibleSocketsPosZ(currentCell.possibilities);
                bool constrained = false;
                
                for (int i = 0; i < otherCell.possibilities.Count; i++) {   

                    if (!otherCell.possibilities[i].terminal & (otherCell.coords.y == height - 1)) {
                        otherCell.possibilities.RemoveAt(i);
                        otherCell.weights.RemoveAt(i);
                        i-=1;
                        constrained = true;
                    }
                    

                    else if(!possibleConnections.Any(possibleConnection => SocketCompatibility.GetCompatibleSockets(otherCell.possibilities[i].negZ).Contains(possibleConnection))) {
                        otherCell.possibilities.RemoveAt(i);
                        otherCell.weights.RemoveAt(i);
                        i-=1;
                        constrained = true;
                    }
                }

                if(constrained)
                    cellsAffected.Add(otherCell);
            }

            
            if(currentCell.hasNegXneighbour) {
                otherCell = currentCell.negXneighbour;
                List<Socket> possibleConnections = GetPossibleSocketsNegX(currentCell.possibilities);
                bool constrained = false;
                for (int i = 0; i < otherCell.possibilities.Count; i++)
                {
                    if(!possibleConnections.Any(possibleConnection => SocketCompatibility.GetCompatibleSockets(otherCell.possibilities[i].posX).Contains(possibleConnection)))
                    {
                        otherCell.possibilities.RemoveAt(i);
                        otherCell.weights.RemoveAt(i);
                        i-=1;
                        constrained = true;
                    }
                }

                if(constrained)
                    cellsAffected.Add(otherCell);
            }


            if(currentCell.hasNegZneighbour) {
                otherCell = currentCell.negZneighbour;
                List<Socket> possibleConnections = GetPossibleSocketsNegZ(currentCell.possibilities);
                bool constrained = false;
                for (int i = 0; i < otherCell.possibilities.Count; i++)
                {
                    if(!possibleConnections.Any(possibleConnection => SocketCompatibility.GetCompatibleSockets(otherCell.possibilities[i].posZ).Contains(possibleConnection)))
                    {
                        otherCell.possibilities.RemoveAt(i);
                        otherCell.weights.RemoveAt(i);
                        i-=1;
                        constrained = true;
                    }
                }

                if(constrained)
                    cellsAffected.Add(otherCell);
            }

            
            if(currentCell.hasNegYneighbour) {
                otherCell = currentCell.negYneighbour;
                if (!otherCell.isCollapsed) {
                    List<Socket> possibleConnections = GetPossibleSocketsNegY(currentCell.possibilities);            
                
                    bool constrained = false;
                    for (int i = 0; i < otherCell.possibilities.Count; i++)
                    {
                        if(!possibleConnections.Any(possibleConnection => SocketCompatibility.GetCompatibleSockets(otherCell.possibilities[i].posY).Contains(possibleConnection)))
                        {
                            otherCell.possibilities.RemoveAt(i);
                            otherCell.weights.RemoveAt(i);
                            i-=1;
                            constrained = true;
                        }
                    }

                    if(constrained)
                        cellsAffected.Add(otherCell);
                }
            }

            
            if(currentCell.hasPosYneighbour) {
                otherCell = currentCell.posYneighbour;
                if (!otherCell.isCollapsed) {
                    List<Socket> possibleConnections = GetPossibleSocketsPosY(currentCell.possibilities);
                    bool constrained = false;

                    for (int i = 0; i < otherCell.possibilities.Count; i++)
                    {
                        if(!possibleConnections.Any(possibleConnection => SocketCompatibility.GetCompatibleSockets(otherCell.possibilities[i].negY).Contains(possibleConnection))) {
                            otherCell.possibilities.RemoveAt(i);
                            otherCell.weights.RemoveAt(i);
                            i-=1;
                            constrained = true;
                        }
                    }  

                    if(constrained)
                        cellsAffected.Add(otherCell); 
                }
            }   
        }
    }

    private List<Socket> GetPossibleSocketsNegX(List<Prototype> prototypesAvailable) {
        List<Socket> socketsAccepted = new List<Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.negX))
                socketsAccepted.Add(proto.negX);
        }
        return socketsAccepted;
    }

    private List<Socket> GetPossibleSocketsNegZ(List<Prototype> prototypesAvailable) {
        List<Socket> socketsAccepted = new List<Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.negZ))
                socketsAccepted.Add(proto.negZ);
        }
        return socketsAccepted;
    }

    private List<Socket> GetPossibleSocketsPosZ(List<Prototype> prototypesAvailable) {
        List<Socket> socketsAccepted = new List<Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if (!socketsAccepted.Contains(proto.posZ))
                socketsAccepted.Add(proto.posZ);
        }

        return socketsAccepted;
    }


    private List<Socket> GetPossibleSocketsPosX(List<Prototype> prototypesAvailable) {
        List<Socket> socketsAccepted = new List<Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.posX))
            {
                socketsAccepted.Add(proto.posX);
            }
        }
        return socketsAccepted;
    }

    private List<Socket> GetPossibleSocketsPosY(List<Prototype> prototypesAvailable) {
        List<Socket> socketsAccepted = new List<Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.posY))
            {
                socketsAccepted.Add(proto.posY);
            }
        }
        return socketsAccepted;
    }

    private List<Socket> GetPossibleSocketsNegY(List<Prototype> prototypesAvailable) {
        List<Socket> socketsAccepted = new List<Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.negY))
            {
                socketsAccepted.Add(proto.negY);
            }
        }
        return socketsAccepted;
    }

    private void CreateBorder() {
        bool previousTower = false;
        int width_border = width;
        int depth_border = depth;

        for (int x = 0; x < width_border; x++) {
                int randomNumber = UnityEngine.Random.Range(0, 11);
                if (randomNumber < 3 && !previousTower) {
                    Instantiate(walls[2], start_position + new Vector3(x * tileSize, tileSize, -tileSize/2), Quaternion.Euler(-90, 0, 0), transform);
                    Instantiate(walls[2], start_position + new Vector3(x * tileSize, tileSize, depth_border * tileSize - tileSize/2), Quaternion.Euler(-90, 0, 0), transform);
                    previousTower = true;
                } else {
                    previousTower = false;
                }

                Instantiate(walls[1], start_position + new Vector3(x * tileSize, tileSize, -tileSize/2), Quaternion.Euler(0, 0, 0), transform);
                Instantiate(walls[1], start_position + new Vector3(x * tileSize, tileSize, depth_border * tileSize - tileSize/2), Quaternion.Euler(0, 0, 0), transform);
        }

        for (int z = 0; z < depth_border; z++) {
                int randomNumber = UnityEngine.Random.Range(0, 11);
                if (randomNumber < 3 && !previousTower) {
                    Instantiate(walls[2], start_position + new Vector3(-tileSize/2, tileSize, z * tileSize), Quaternion.Euler(-90, 90, 0), transform);
                    Instantiate(walls[2], start_position + new Vector3(width_border * tileSize - tileSize/2, tileSize, z * tileSize), Quaternion.Euler(-90, 90, 0), transform);
                    previousTower = true;
                } else {
                    previousTower = false;
                }

                Instantiate(walls[1], start_position + new Vector3(-tileSize/2, tileSize, z * tileSize), Quaternion.Euler(0, 90, 0), transform);
                Instantiate(walls[1], start_position + new Vector3(width_border * tileSize - tileSize/2, tileSize, z * tileSize), Quaternion.Euler(0, 90, 0), transform);
        }
    }

    private IEnumerator CreateBorderOverTime(float waitTime) {   
        bool previousTower = false;
        int width_border = width;
        int depth_border = depth;

        for (int x = 0; x < width_border; x++)
        {
            int randomNumber = UnityEngine.Random.Range(0, 11);
            if (randomNumber < 3 && !previousTower)
            {
                Instantiate(walls[2], start_position + new Vector3(x * tileSize, tileSize, -tileSize / 2), Quaternion.Euler(-90, 0, 0), transform);
                Instantiate(walls[2], start_position + new Vector3(x * tileSize, tileSize, depth_border * tileSize - tileSize / 2), Quaternion.Euler(-90, 0, 0), transform);
                previousTower = true;
            }
            else
            {
                previousTower = false;
            }

            Instantiate(walls[1], start_position + new Vector3(x * tileSize, tileSize, -tileSize / 2), Quaternion.Euler(0, 0, 0), transform);
            Instantiate(walls[1], start_position + new Vector3(x * tileSize, tileSize, depth_border * tileSize - tileSize / 2), Quaternion.Euler(0, 0, 0), transform);

            yield return new WaitForSeconds(waitTime);
        }

        for (int z = 0; z < depth_border; z++)
        {
            int randomNumber = UnityEngine.Random.Range(0, 11);
            if (randomNumber < 3 && !previousTower)
            {
                Instantiate(walls[2], start_position + new Vector3(-tileSize / 2, tileSize, z * tileSize), Quaternion.Euler(-90, 90, 0), transform);
                Instantiate(walls[2], start_position + new Vector3(width_border * tileSize - tileSize / 2, tileSize, z * tileSize), Quaternion.Euler(-90, 90, 0), transform);
                previousTower = true;
            }
            else
            {
                previousTower = false;
            }

            Instantiate(walls[1], start_position + new Vector3(-tileSize / 2, tileSize, z * tileSize), Quaternion.Euler(0, 90, 0), transform);
            Instantiate(walls[1], start_position + new Vector3(width_border * tileSize - tileSize / 2, tileSize, z * tileSize), Quaternion.Euler(0, 90, 0), transform);

            yield return new WaitForSeconds(waitTime);
        }
    }

    private void PlaceBuildings() {
        int i = 0;

        foreach(GameObject building in buildings) {
            Instantiate(building, positionBuildings[i], Quaternion.Euler(rotationBuildings[i]), transform);
            Bounds objectBounds = building.GetComponent<Renderer>().bounds;
            
            Vector3 cellPos = new Vector3(
                        Mathf.RoundToInt((positionBuildings[i].x - start_position.x)/tileSize),
                        Mathf.RoundToInt((positionBuildings[i].y - start_position.y)/tileSize),
                        Mathf.RoundToInt((positionBuildings[i].z - start_position.z)/tileSize));

            List<Vector3Int> occupiedCells = CollectOccupiedCells(cellPos, boundingBuildings[i]);
            foreach(Vector3Int cell in occupiedCells) {
                if(cell.x > 0 && cell.x < width && cell.y > 0 && cell.y < height && cell.z > 0 && cell.z < depth)
                    Destroy(grid[cell.x, cell.y, cell.z].instantiated);
            }

            ++i;
        }
    }

    private IEnumerator PlaceBuildingsOverTime(float waitTime) {
        int i = 0;

        foreach(GameObject building in buildings) {
            Instantiate(building, positionBuildings[i], Quaternion.Euler(rotationBuildings[i]), transform);
            Bounds objectBounds = building.GetComponent<Renderer>().bounds;
            Vector3 cellPos = new Vector3(
                        Mathf.RoundToInt((positionBuildings[i].x - start_position.x)/tileSize),
                        Mathf.RoundToInt((positionBuildings[i].y - start_position.y)/tileSize),
                        Mathf.RoundToInt((positionBuildings[i].z - start_position.z)/tileSize));

            List<Vector3Int> occupiedCells = CollectOccupiedCells(cellPos, boundingBuildings[i]);
            foreach(Vector3Int cell in occupiedCells) {
                if(cell.x > 0 && cell.x < width && cell.y > 0 && cell.y < height && cell.z > 0 && cell.z < depth)
                    Destroy(grid[cell.x, cell.y, cell.z].instantiated);
            }

            ++i;
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    List<Vector3Int> CollectOccupiedCells(Vector3 position, Vector3 bounding) {
        List<Vector3Int> occupiedCells = new List<Vector3Int>();

        Vector3Int boundingCell = new Vector3Int(
                        Mathf.RoundToInt(bounding.x/tileSize),
                        Mathf.RoundToInt(bounding.y/tileSize),
                        Mathf.RoundToInt(bounding.z/tileSize));

        for (float x = 0; x < boundingCell.x; x++) {
            for (float y = 0; y < boundingCell.y; y++) {
                for (float z = 0; z < boundingCell.z; z++) {
                    Vector3 boundPos = new Vector3(x, y, z);
                    Vector3 cellCoords = position + boundPos;

                    Vector3Int cell = new Vector3Int(
                        Mathf.RoundToInt(cellCoords.x - (int) boundingCell.x/2),
                        Mathf.RoundToInt(cellCoords.y),
                        Mathf.RoundToInt(cellCoords.z - (int) boundingCell.z/2)
                    );

                    occupiedCells.Add(cell);
                }
            }
        }

        return occupiedCells;
    }

    Vector3 WorldToGridCoordinates(Vector3 worldPos) {
        Vector3 relativePos = worldPos - start_position;

        // convert to grid coordinates
        Vector3 gridCoords = new Vector3(
            relativePos.x / tileSize,
            relativePos.y / tileSize,
            relativePos.z / tileSize
        );

        return gridCoords;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prototype : ScriptableObject {
    public int meshRotation;
    public GameObject prefab;
    public float weight = 10f;

    public Vector3 translate;
    public Vector3 rotate;
    public bool random_rotation = true;
    public bool terminal = true;
    public bool parentLinked = false;

    public Socket posX;
    public Socket negX;

    public Socket posY;
    public Socket negY;

    public Socket posZ;
    public Socket negZ;

    public override string ToString() {
        string socketsString = $"Positive X Socket: {posX}\n" +
                               $"Negative X Socket: {negX}\n" +
                               $"Positive Y Socket: {posY}\n" +
                               $"Negative Y Socket: {negY}\n" +
                               $"Positive Z Socket: {posZ}\n" +
                               $"Negative Z Socket: {negZ}";

        return $"Prefab: {prefab.name}\n" +
               $"Mesh Rotation: {meshRotation}\n" +
               $"Sockets:\n{socketsString}";
    }
}

public enum Building_Type {
    House, Market, Vegetation, Sign
}

public enum Socket {
    Socket_Road,
    Socket_Back,
    Socket_Building_Side,
    Socket_Empty,
    Socket_House_Top,
    Socket_Empty_Top,
    Socket_Front, 
    Socket_Floor, 
    Socket_Roof, 
    Socket_High_Floor, 
    Socket_High_Roof
}

public static class SocketCompatibility {
    private static Dictionary<Socket, List<Socket>> compatibleSockets;

    static SocketCompatibility() {
        compatibleSockets = new Dictionary<Socket, List<Socket>>();

        compatibleSockets.Add(Socket.Socket_Road, new List<Socket> { Socket.Socket_Road });
        compatibleSockets.Add(Socket.Socket_Back, new List<Socket> { Socket.Socket_Back });
        compatibleSockets.Add(Socket.Socket_Building_Side, new List<Socket> { Socket.Socket_Building_Side, Socket.Socket_Empty });
        compatibleSockets.Add(Socket.Socket_Empty, new List<Socket> { Socket.Socket_Building_Side, Socket.Socket_Empty});
        compatibleSockets.Add(Socket.Socket_House_Top, new List<Socket> { Socket.Socket_House_Top });
        compatibleSockets.Add(Socket.Socket_Empty_Top, new List<Socket> { Socket.Socket_Empty_Top, Socket.Socket_Road});
        compatibleSockets.Add(Socket.Socket_Front, new List<Socket> { Socket.Socket_Road });
 
        compatibleSockets.Add(Socket.Socket_Floor, new List<Socket>{Socket.Socket_Floor, Socket.Socket_Roof});
        compatibleSockets.Add(Socket.Socket_Roof, new List<Socket>{Socket.Socket_Floor});

        compatibleSockets.Add(Socket.Socket_High_Floor, new List<Socket>{Socket.Socket_High_Floor, Socket.Socket_High_Roof});
        compatibleSockets.Add(Socket.Socket_High_Roof, new List<Socket>{Socket.Socket_High_Floor});

    }

    public static List<Socket> GetCompatibleSockets(Socket socket) {
        return compatibleSockets[socket];
    }
}



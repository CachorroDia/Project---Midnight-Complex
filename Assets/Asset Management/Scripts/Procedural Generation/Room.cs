using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Room
{
    private bool visited;
    private Vector2Int position = new Vector2Int();
    private int depth;
    Room[] rooms = new Room[4];

    private GameObject[] spawnPositions;

    public Room(Vector2Int position, int depth)
    {
        this.position = position;
        this.depth = depth;
    }

    public Room[] GetRooms() => rooms;
    public Room GetRoom(int index) => rooms[index];
    public bool GetVisited() => visited;
    public Vector2Int GetPosition() => position;
    public int GetDepth() => depth;

    public void SetVisited(bool value) => visited = value;
    public void SetRoom(int index, Room room) => rooms[index] = room;

    public void SetSpawnPositions(RoomPrefabData roomData)
    {
        if(roomData.GetSpawnPosition() == null)
        {
            spawnPositions = new GameObject[0];
            return;
        }

        spawnPositions = new GameObject[roomData.GetSpawnPosition().Count];
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            spawnPositions[i] = roomData.GetSpawnPosition()[i];
        }
    }

    public GameObject[] GetSpawnPositions() => spawnPositions;

    public Direction GetConnections()
    {
        Direction _mask = 0;

        if (rooms[0] != null) _mask |= Direction.North;
        if(rooms[1] != null) _mask |= Direction.South;
        if (rooms[2] != null) _mask |= Direction.East;
        if (rooms[3] != null) _mask |= Direction.West;
        return _mask;
    }

}

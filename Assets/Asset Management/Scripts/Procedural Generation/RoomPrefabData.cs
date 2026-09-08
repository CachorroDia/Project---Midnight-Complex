using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RoomPrefabData : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Entradas que essa sala terá. Norte aponta para Z, Sul para -Z. Leste aponta X+ e Oeste para X-.")]
    private Direction connections;
    [SerializeField]
    [Tooltip("Lista de objetos vazios que irão dar servir para dar spawn em itens e criaturas dentro das salas.")]
    private List<GameObject> spawnPosition;

    public Direction GetConnections() => connections;

    public List<GameObject> GetSpawnPosition() => spawnPosition;
}

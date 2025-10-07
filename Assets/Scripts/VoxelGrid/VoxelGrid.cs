using System.Collections.Generic;
using UnityEngine;

public struct Voxel
{
    public Vector3 position;
    public bool inside;
    public Vector3 normal;
}

public class VoxelGrid : MonoBehaviour
{
    [SerializeField] private Bounds bounds;
    [SerializeField] private int subDivisions;

    [SerializeField] private List<Vector3> voxelList = new List<Vector3>();

    Vector3 voxelSize;

    private void Awake()
    {
        voxelSize = new Vector3((bounds.extents.x * 2) / subDivisions,
                                    (bounds.extents.y * 2) / subDivisions,
                                    (bounds.extents.z * 2) / subDivisions);

        Vector3 offset = (voxelSize / 2);
        offset.x -= bounds.extents.x;
        offset.z -= bounds.extents.z;

        offset.x -= voxelSize.x / 2;
        offset.y -= voxelSize.y / 2;
        offset.z -= voxelSize.z / 2;

        for (int i = 0; i < subDivisions + 1; i++)
        {
            for (int j = 0; j < subDivisions + 1; j++)
            {
                for (int k = 0; k < subDivisions + 1; k++)
                {
                    voxelList.Add(offset);
                    offset.z += voxelSize.z;
                    if (k == subDivisions)
                    {
                        offset.z = voxelSize.z / 2;
                        offset.z -= bounds.extents.z;
                        offset.z -= voxelSize.z / 2;
                    }
                }
                voxelList.Add(offset);
                offset.y += voxelSize.y;
                if (j == subDivisions)
                {
                    offset.y = voxelSize.y / 2;
                    offset.y -= voxelSize.y / 2;
                }

            }
            voxelList.Add(offset);
            offset.x += voxelSize.x;
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < voxelList.Count; i++)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireCube(voxelList[i], voxelSize);
        }
    }

    public Bounds GetBounds()
    {
        return bounds;
    }

    public Vector3 GetVoxelSize()
    {
        return voxelSize;
    }

    public List<Vector3> GetVoxelList()
    {
        return voxelList;
    }
}

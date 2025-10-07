using System.Collections.Generic;
using UnityEngine;

public class DiffusionLimitedAggregation : MonoBehaviour
{
    [SerializeField] private Vector3 start;
    [SerializeField] private VoxelGrid voxelGrid;

    [SerializeField] private List<Vector3> pointCloud = new List<Vector3>();
    [SerializeField] private List<Vector3> points = new List<Vector3>();
    [SerializeField] private int pointAmount;
    [SerializeField] private float radius;
    [SerializeField] private Transform point;
    List<Vector3> voxelList;

    private void Start()
    {
        Vector3 voxelSize = voxelGrid.GetVoxelSize();
        start = voxelSize;
        pointCloud.Add(start);
        voxelList = voxelGrid.GetVoxelList();

        for (int i = 0; i < pointAmount; i++)
        {
            points.Add(NewPoint());
        }
    }

    private Vector3 NewPoint()
    {
        return voxelList[(int)Random.Range(0, voxelList.Count - 1)];
    }


    private Vector3 RandomVec3()
    {
        Vector3 voxelSize = voxelGrid.GetVoxelSize();
        float chance = Random.Range(0f, 6f);
        if (chance <= 1)
            return new Vector3(voxelSize.x, 0, 0);
        if (chance <= 2)
            return new Vector3(-voxelSize.x, 0, 0);
        if (chance <= 3)
            return new Vector3(0, voxelSize.y, 0);
        if (chance <= 4)
            return new Vector3(0, -voxelSize.y, 0);
        if (chance <= 5)
            return new Vector3(0, 0, voxelSize.z);
        if (chance <= 6)
            return new Vector3(0, 0, -voxelSize.z);

        return Vector3.zero;
    }

    private void Move()
    {
        Bounds bounds = voxelGrid.GetBounds();
        Vector3 voxelSize = voxelGrid.GetVoxelSize();
        if (pointCloud.Count > pointAmount)
            return;
        for (int i = points.Count - 1; i >= 0; i--)
        {
            Vector3 point = points[i];
            Vector3 step;

            do
            {
                step = RandomVec3();

                if (!bounds.Contains(point + step))
                {
                    points.RemoveAt(i);
                    points.Add(NewPoint());
                }
            } while (!bounds.Contains(point + step));

            point += step;

            points[i] = point;
            for (int j = 0; j < pointCloud.Count; j++)
            {
                if (Mathf.Abs(Vector3.Distance(point, pointCloud[j])) < voxelSize.magnitude)
                {
                    pointCloud.Add(point);
                    points.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void Update()
    {
        Move();
    }

    private void OnDrawGizmos()
    {
        Bounds bounds = voxelGrid.GetBounds();
        Vector3 voxelSize = voxelGrid.GetVoxelSize();

        Gizmos.color = Color.blueViolet;
        for (int i = 0; i < pointCloud.Count; i++)
        {
            for (int j = 0; j < voxelList.Count; j++)
            {
                float gaussian = CalculateGaussian(pointCloud[i], voxelList[j]);
                Color col = Color.coral;
                col.a = gaussian;

                Gizmos.color = col;

                if (gaussian > 0)
                    Gizmos.DrawCube(voxelList[j], voxelSize);
            }
        }
        Gizmos.color = Color.red;
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawWireSphere(points[i], radius);
        }
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Gizmos.DrawWireSphere(new Vector3(0, -10, 0), radius);

        //Debug.Log(1 / (1 + Mathf.Pow((Vector3.Distance(point.position, new Vector3(0, -10, 0)) / radius), 2))); // inverse quadratic
        Debug.Log(CalculateGaussian(point.position, new Vector3(0, -10, 0))); // gaussian
    }

    private float CalculateGaussian(Vector3 pos, Vector3 point)
    {
        float gaussian = 1 * Mathf.Exp(-Mathf.Pow(Vector3.Distance(point, pos), 2) / (2 * Mathf.Pow((radius / 3), 2)));
        float eps = 0.01f;
        if (gaussian < eps)
            return 0;

        return gaussian;
    }
}

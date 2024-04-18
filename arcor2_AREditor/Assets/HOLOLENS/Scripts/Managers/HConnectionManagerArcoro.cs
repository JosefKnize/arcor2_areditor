using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Base;
using UnityEngine;


public class HConnectionManagerArcoro : Base.Singleton<HConnectionManagerArcoro>
{
    public GameObject ConnectionPrefab;
    public List<Connection> Connections = new List<Connection>();
    private Connection virtualConnectionToMouse;
    private GameObject virtualPointer;
    [SerializeField]
    private Material EnabledMaterial, DisabledMaterial;

    private void Start()
    {
        virtualPointer = HandInteractionHandler.Instance.GhostActionPoint.transform.Find("Connection").gameObject;
    }

    public Connection CreateConnection(GameObject o1, GameObject o2)
    {
        Connection c = Instantiate(ConnectionPrefab).GetComponent<Connection>();

        if (o1.GetComponent<HInputOutput>().GetType() == typeof(HPuckOutput))
        {
            c.target[0] = o1.GetComponent<RectTransform>();
            c.target[1] = o2.GetComponent<RectTransform>();
        }
        else
        {
            c.target[1] = o1.GetComponent<RectTransform>();
            c.target[0] = o2.GetComponent<RectTransform>();

        }
        Connections.Add(c);
        return c;
    }


    public Vector3 GetBezierPoint(float t, Vector3 p1, Vector3 p2, Vector3 c1, Vector3 c2, int derivative = 0)
    {
        derivative = Mathf.Clamp(derivative, 0, 2);
        float u = (1f - t);
        //	Vector3 p1 = points[0].p, p2 = points[1].p, c1 = points[0].c, c2 = points[1].c;

        if (derivative == 0)
        {
            return u * u * u * p1 + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * p2;

        }
        else if (derivative == 1)
        {
            return 3f * u * u * (c1 - p1) + 6f * u * t * (c2 - c1) + 3f * t * t * (p2 - c2);

        }
        else if (derivative == 2)
        {
            return 6f * u * (c2 - 2f * c1 + p1) + 6f * t * (p2 - 2f * c2 + c1);

        }
        else
        {
            return Vector3.zero;
        }
    }

    public void CreateConnectionToPointer(GameObject o)
    {
        if (virtualConnectionToMouse != null)
        {
            Connections.Remove(virtualConnectionToMouse);
            Destroy(virtualConnectionToMouse.gameObject);
        }
        //   VirtualConnectionOnTouch.Instance.DrawVirtualConnection = true;
        virtualConnectionToMouse = CreateConnection(o, virtualPointer);
    }

    public void DestroyConnectionToMouse()
    {
        Connections.Remove(virtualConnectionToMouse);
        Destroy(virtualConnectionToMouse.gameObject);
        //    VirtualConnectionOnTouch.Instance.DrawVirtualConnection = false;
    }

    public void DestroyConnection(Connection connection)
    {
        Connections.Remove(connection);
        Destroy(connection.gameObject);
    }

    public bool IsConnecting()
    {
        return virtualConnectionToMouse != null;
    }

    public HAction GetActionConnectedToPointer()
    {
        Debug.Assert(virtualConnectionToMouse != null);
        GameObject obj = GetConnectedTo(virtualConnectionToMouse, virtualPointer);
        return obj.GetComponent<HInputOutput>().Action;
    }

    public GameObject GetConnectedToPointer()
    {
        Debug.Assert(virtualConnectionToMouse != null);
        return GetConnectedTo(virtualConnectionToMouse, virtualPointer);
    }

    public HAction GetActionConnectedTo(Connection c, GameObject o)
    {
        return GetConnectedTo(c, o).GetComponent<HInputOutput>().Action;
    }

    private int GetIndexOf(Connection c, GameObject o)
    {
        if (c.target[0] != null && c.target[0].gameObject == o)
        {
            return 0;
        }
        else if (c.target[1] != null && c.target[1].gameObject == o)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    private int GetIndexByType(Connection c, System.Type type)
    {
        if (c.target[0] != null && c.target[0].gameObject.GetComponent<HInputOutput>() != null && c.target[0].gameObject.GetComponent<HInputOutput>().GetType().IsSubclassOf(type))
            return 0;
        else if (c.target[1] != null && c.target[1].gameObject.GetComponent<HInputOutput>() != null && c.target[1].gameObject.GetComponent<HInputOutput>().GetType().IsSubclassOf(type))
            return 1;
        else
            return -1;

    }

    public GameObject GetConnectedTo(Connection c, GameObject o)
    {
        if (c == null || o == null)
            return null;
        int i = GetIndexOf(c, o);
        if (i < 0)
            return null;
        return c.target[1 - i].gameObject;
    }

    /**
     * Checks that there is input on one end of connection and output on the other side
     */
    public bool ValidateConnection(Connection c)
    {
        if (c == null)
            return false;
        int input = GetIndexByType(c, typeof(HInputOutput)), output = GetIndexByType(c, typeof(HPuckOutput));
        if (input < 0 || output < 0)
            return false;
        return input + output == 1;
    }

    public async Task<bool> ValidateConnection(HInputOutput output, HInputOutput input, IO.Swagger.Model.ProjectLogicIf condition)
    {
        string[] startEnd = new[] { "START", "END" };
        if (output.GetType() == input.GetType() ||
            output.Action.Data.Id.Equals(input.Action.Data.Id) ||
            (startEnd.Contains(output.Action.Data.Id) && startEnd.Contains(input.Action.Data.Id)))
        {
            return false;
        }
        try
        {
            // TODO: how to pass condition?
            await WebSocketManagerH.Instance.AddLogicItem(output.Action.Data.Id, input.Action.Data.Id, condition, true);
        }
        catch (RequestFailedException)
        {
            return false;
        }
        return true;
    }

    public void Clear()
    {
        foreach (Connection c in Connections)
        {
            if (c != null && c.gameObject != null)
            {
                Destroy(c.gameObject);
            }
        }
        Connections.Clear();
    }

    public void DisplayConnections(bool active)
    {
        foreach (Connection connection in Connections)
        {
            connection.gameObject.SetActive(active);
        }
    }

    public void DisableConnectionToMouse()
    {
        if (virtualConnectionToMouse != null)
            virtualConnectionToMouse.GetComponent<LineRenderer>().material = DisabledMaterial;
    }

    public void EnableConnectionToMouse()
    {
        if (virtualConnectionToMouse != null)
            virtualConnectionToMouse.GetComponent<LineRenderer>().material = EnabledMaterial;
    }

    public void makeMesh(Connection c)
    {
        List<Vector3> points = new List<Vector3>();
        points.Clear();
        LineRenderer line = c.line;
        GameObject caret = null;
        caret = new GameObject("Lines");

        Vector3 left, right; // A position to the left of the current line

        // For all but the last point
        for (var i = 0; i < line.positionCount - 1; i++)
        {
            caret.transform.position = line.GetPosition(i);
            caret.transform.LookAt(line.GetPosition(i + 1));
            right = caret.transform.position + transform.right * line.startWidth / 2;
            left = caret.transform.position - transform.right * line.startWidth / 2;
            points.Add(left);
            points.Add(right);
        }

        // Last point looks backwards and reverses
        caret.transform.position = line.GetPosition(line.positionCount - 1);
        caret.transform.LookAt(line.GetPosition(line.positionCount - 2));
        right = caret.transform.position + transform.right * line.startWidth / 2;
        left = caret.transform.position - transform.right * line.startWidth / 2;
        points.Add(left);
        points.Add(right);
        Destroy(caret);
        DrawMesh(points, c);
    }

    private void DrawMesh(List<Vector3> points, Connection c)
    {
        Vector3[] verticies = new Vector3[points.Count];

        for (int i = 0; i < verticies.Length; i++)
        {
            verticies[i] = points[i];
        }

        int[] triangles = new int[((points.Count / 2) - 1) * 6];

        //Works on linear patterns tn = bn+c
        int position = 6;
        for (int i = 0; i < (triangles.Length / 6); i++)
        {
            triangles[i * position] = 2 * i;
            triangles[i * position + 3] = 2 * i;

            triangles[i * position + 1] = 2 * i + 3;
            triangles[i * position + 4] = (2 * i + 3) - 1;

            triangles[i * position + 2] = 2 * i + 1;
            triangles[i * position + 5] = (2 * i + 1) + 2;
        }


        Mesh mesh = c.gameObject.AddComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }


}

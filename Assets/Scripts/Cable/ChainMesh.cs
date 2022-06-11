using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChainMesh : MonoBehaviour
{
    //Undersök att använda gizmos för att redigera och generera kroppar med rundade hörn

    [System.Serializable]
    public class Vert
    {
        public Vector2 point;

        public bool Chainable;
    }

    [System.Serializable]
    public class Cent
    {
        public Vector2 point;
    }

    [System.Serializable]
    public struct Edge
    {
        public Vert start;

        public Vert end;

        public Vector2 vector;

        public Cent center;

        public bool hit;
    }

    [SerializeField] public Vert[] vertecies;

    [SerializeField] public Edge[] edges;

    [SerializeField] public Cent[] centers;

    private List<Vector2> polygon;
    private List<List<Vector2>> convPolygons;


    [SerializeField] LineRenderer previewRender = null;

    [ContextMenu("Generate Chain Mesh")]
    void GenerateChainMesh()
    {
        PolygonCollider2D coll = this.GetComponent<PolygonCollider2D>();
        if (coll == null) return;

        Vector2[] temp = coll.points;

        UnityEditor.Undo.RecordObject(this, "Generated chainMesh");

        vertecies = new Vert[temp.Length];

        polygon = new List<Vector2>();

        for (int i = 0; i < temp.Length; i++)
        {
            vertecies[i] = new Vert();
            vertecies[i].point = temp[i] + coll.offset;
            polygon.Add(vertecies[i].point);
        }

        edges = new Edge[vertecies.Length];

        int last = vertecies.Length - 1;

        for (int i = 0; i < last; i++)
        {
            edges[i].start = vertecies[i];
            edges[i].end = vertecies[i + 1];
            edges[i].vector = edges[i].end.point - edges[i].start.point;
        }

        edges[last].start = vertecies[last];
        edges[last].end = vertecies[0];
        edges[last].vector = edges[last].end.point - edges[last].start.point;

        for (int i = 1; i < vertecies.Length; i++)
        {
            vertecies[i].Chainable = Vector2.SignedAngle(edges[i - 1].vector, edges[i].vector) > 0;
        }
        vertecies[0].Chainable = Vector2.SignedAngle(edges[last].vector, edges[0].vector) > 0;

        convPolygons = BayazitDecomposer.ConvexPartition(polygon);



        centers = new Cent[convPolygons.Count];

        Queue<int> query = new Queue<int>();

        for (int i = 0; i < edges.Length; i++)
        {
            query.Enqueue(i);
        }

        int index = 0;

        foreach (List<Vector2> convex in convPolygons)
        {
            centers[index] = new Cent();
            centers[index].point = Polycenter(convex);

            int start = convex.Count - 1;

            int end = 0;


            for (int count = query.Count; count > 0; count--)
            {
                int current = query.Dequeue();

                if (convex[start] == edges[current].start.point && convex[end] == edges[current].end.point)
                {
                    edges[current].center = centers[index];
                    break;
                }

                query.Enqueue(current);
            }



            for (int i = 1; i < convex.Count; i++)
            {
                start = end;

                end = i;


                for (int count = query.Count; count > 0; count--)
                {
                    int current = query.Dequeue();

                    if (convex[start] == edges[current].start.point && convex[end] == edges[current].end.point)
                    {
                        edges[current].center = centers[index];
                        break;
                    }

                    query.Enqueue(current);
                }
            }

            index++;
        }

        print("Mesh generated");
    }

    Vector2 Polycenter(List<Vector2> polygon)
    {
        Vector2 sumCenter = Vector2.zero;
        float sumWeight = 0;

        for (int i = 0; i < polygon.Count; i++)
        {
            int next = i + 1;
            if (next >= polygon.Count)
            {
                next = 0;
            }

            int prev = i - 1;
            if (prev <= -1)
            {
                prev = polygon.Count - 1;
            }

            float weight = (polygon[i] - polygon[next]).magnitude + (polygon[i] - polygon[prev]).magnitude;
            sumCenter += polygon[i] * weight;
            sumWeight += weight;
        }
        return sumCenter / sumWeight;
    }

    [ContextMenu("Add LineRender")]
    void AddLinerenderer()
    {

    }

    [ContextMenu("Remove LineRender")]
    void RemoveLinerenderer()
    {

    }

    private void Awake()
    {
        if (CMDictionary.CMD.dictionary == null)
        {
            CMDictionary.CMD.dictionary = new Dictionary<Collider2D, ChainMesh>();
        }
        CMDictionary.CMD.dictionary.Add(this.GetComponent<PolygonCollider2D>(), this);
    }
}

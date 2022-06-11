using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GraphManager : MonoBehaviour
{
    [SerializeField] private GameObject rootPrefab;

    private class AdjacencyMatrix
    {
        private class TranslatorADJM
        {
            private Dictionary<GraphObject, int> objInt;
            private Dictionary<int, GraphObject> intObj;

            public TranslatorADJM()
            {
                objInt = new Dictionary<GraphObject, int>();
                intObj = new Dictionary<int, GraphObject>();
            }

            public void Add(GraphObject key, int value)
            {
                objInt.Add(key, value);
                intObj.Add(value, key);
            }

            public void Remove(GraphObject key)
            {
                intObj.Remove(objInt[key]);
                objInt.Remove(key);
            }

            public void Remove(int value)
            {
                objInt.Remove(intObj[value]);
                intObj.Remove(value);
            }

            public int Translate(GraphObject key)
            {
                return objInt[key];
            }

            public GraphObject Translate(int value)
            {
                return intObj[value];
            }

            public GraphObject[] Keys
            {
                get { return objInt.Keys.ToArray(); }
            }

            public int[] Values
            {
                get { return objInt.Values.ToArray(); } 
            }

            public bool ContainsKey(GraphObject key)
            {
                return objInt.ContainsKey(key);
            }

            public bool ContainsValue(int value)
            {
                return objInt.ContainsValue(value);
            }
        }

        private int size;

        public int Size
        {
            get { return size; }
        }

        int allocSize = 10;

        private bool[,] ADJ;
        private TranslatorADJM translator;
        private Stack<int> freeValues;

        public AdjacencyMatrix()
        {
            size = 1;

            freeValues = new Stack<int>();

            for (int i = 1; i < allocSize; i++)
            {
                freeValues.Push(i);
            }
            
            ADJ = new bool[allocSize, allocSize];

            translator = new TranslatorADJM();
        }

        public void AddNode(GraphObject item)
        {
            if (size >= allocSize)
            {
                Reallocate();
            }

            if (!translator.ContainsKey(item))
            {
                translator.Add(item, freeValues.Pop());
            }
            size++;
        }

        public void RemoveNode(GraphObject item)
        {
            int id = translator.Translate(item);
            for (int i = 0; i < allocSize; i++)
            {
                ADJ[id, i] = false;
                ADJ[i, id] = false;
            }

            translator.Remove(item);

            freeValues.Push(id);

            size--;
        }

        public void Reallocate()
        {
            int temp = allocSize;

            allocSize *= 2;

            bool[,] newMatrix = new bool[allocSize, allocSize];

            for (int i = 0; i < temp; i++)
            {
                for (int j = 0; j < temp; j++)
                {
                    newMatrix[i, j] = ADJ[i, j];
                }
            }

            ADJ = newMatrix;

            for (int i = temp; i < allocSize; i++)
            {
                freeValues.Push(i);
            }
        }

        public void MakeEdge(GraphObject firstNode, GraphObject secondNode)
        {
            if (firstNode != null && secondNode != null)
            {
                ADJ[translator.Translate(firstNode), translator.Translate(secondNode)] = true;
            }
        }

        public void BreakEdge(GraphObject firstNode, GraphObject secondNode)
        {
            if (firstNode != null && secondNode != null)
            {
                ADJ[translator.Translate(firstNode), translator.Translate(secondNode)] = false;
            }
        }

        public void MakeStatic(GraphObject node)
        {
            if (node != null)
            {
                ADJ[translator.Translate(node), 0] = true;
            }
        }

        public void BreakStatic(GraphObject node)
        {
            if (node != null)
            {
                ADJ[translator.Translate(node), 0] = false;
            }
        }

        public bool OnSameTree(GraphObject source, GraphObject goal)
        {
            int[] nodes = new int[size];

            int index = 0;
            foreach (int value in translator.Values)
            {
                nodes[index++] = value;
            }

            foreach (int id in nodes)
            {
                ADJ[id, id] = false;
            }

            int goalValue = translator.Translate(goal);

            Stack<int> searchPool = new Stack<int>();

            searchPool.Push(translator.Translate(source));

            while (searchPool.Count > 0)
            {
                int current = searchPool.Pop();
                foreach (int node in nodes)
                {
                    if (!ADJ[node, node] && (ADJ[current, node] || ADJ[node, current]))
                    {
                        if (node == goalValue)
                        {
                            return true;
                        }
                        searchPool.Push(node);
                    }
                }
                ADJ[current, current] = true;
            }

            return false;
        }

        public void GetTreeNodes(GraphObject source, out GraphObject[] results)
        {
            Stack<GraphObject> stack = new Stack<GraphObject>();

            int[] nodes = new int[size];

            int index = 0;
            foreach (int value in translator.Values)
            {
                nodes[index++] = value;
            }

            foreach (int id in nodes)
            {
                ADJ[id, id] = false;
            }

            Stack<int> searchPool = new Stack<int>();

            searchPool.Push(translator.Translate(source));
            stack.Push(source);

            while (searchPool.Count > 0)
            {
                int current = searchPool.Pop();
                foreach (int node in nodes)
                {
                    if (!ADJ[node, node] && (ADJ[current, node] || ADJ[node, current]))
                    {
                        searchPool.Push(node);
                        stack.Push(translator.Translate(node));
                    }
                }
                ADJ[current, current] = true;
            }
            
            results = new GraphObject[stack.Count];
            int count = stack.Count;
            for (int i = 0; i < count; i++)
            {
                results[i] = stack.Pop();
            }
        }

        public void GetConnected(GraphObject source, out GraphObject[] connected)
        {
            Stack<GraphObject> stack = new Stack<GraphObject>();

            int id = translator.Translate(source);

            ADJ[id, id] = false;

            for (int i = 0; i < allocSize; i++)
            {
                if (ADJ[i, id])
                {
                    stack.Push(translator.Translate(i));
                }
            }

            connected = new GraphObject[stack.Count];
            int count = stack.Count;
            for (int i = 0; i < count; i++)
            {
                connected[i] = stack.Pop();
            }
        }

        public int TreeSize(GraphObject source)
        {
            int[] nodes = new int[size];

            int count = 1;

            int index = 0;
            foreach (int value in translator.Values)
            {
                nodes[index++] = value;
            }

            foreach (int id in nodes)
            {
                ADJ[id, id] = false;
            }

            Stack<int> searchPool = new Stack<int>();

            searchPool.Push(translator.Translate(source));

            while (searchPool.Count > 0)
            {
                int current = searchPool.Pop();
                foreach (int node in nodes)
                {
                    if (!ADJ[node, node] && (ADJ[current, node] || ADJ[node, current]))
                    {
                        searchPool.Push(node);
                        count++;
                    }
                }
                ADJ[current, current] = true;
            }

            return count;
        }

        public int StaticEdges(GraphObject source)
        {
            int[] nodes = new int[size];

            int count = 0;

            int index = 1;

            nodes[0] = 0;
            foreach (int value in translator.Values)
            {
                nodes[index++] = value;
            }

            foreach (int id in nodes)
            {
                ADJ[id, id] = false;
            }

            Stack<int> searchPool = new Stack<int>();

            searchPool.Push(translator.Translate(source));

            while (searchPool.Count > 0)
            {
                int current = searchPool.Pop();
                foreach (int node in nodes)
                {
                    if (!ADJ[node, node] && (ADJ[current, node] || ADJ[node, current]))
                    {
                        if (node == 0)
                        {
                            count++;
                        }
                        else
                        {
                            searchPool.Push(node);
                        }
                    }
                }
                ADJ[current, current] = true;
            }

            return count;
        }
    }

    private class RootPool
    {
        private int rootInstances = 0;

        private Queue<Rigidbody2D> rootPool;

        private GameObject prefab;

        private Transform parent;

        public RootPool(GameObject prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
            rootPool = new Queue<Rigidbody2D>();
        }

        public void Alloc(int nodeCount)
        {
            if (rootInstances < nodeCount / 2)
            {
                Store(NewRoot());
            }
        }

        public Rigidbody2D Request(Vector3 positon)
        {
            if(rootPool.Count < 1)
            {
                return NewRoot();
            }

            Rigidbody2D root = rootPool.Dequeue();
            root.transform.position = positon;
            root.simulated = true;
            return root;
        }

        public void Store(Rigidbody2D root)
        {
            if (root != null)
            {
                root.simulated = false;
                rootPool.Enqueue(root);
            }
        }

        private Rigidbody2D NewRoot()
        {
            rootInstances++;
            return Instantiate(prefab, parent).GetComponent<Rigidbody2D>();
        }
    }

    public static GraphManager GM;

    private AdjacencyMatrix adjacencyMatrix = null;

    private RootPool rootPool = null;

    void Awake()
    {
        if (GM != null)
            GameObject.Destroy(GM);
        else
            GM = this;

        DontDestroyOnLoad(this);

        rootPool = new RootPool(rootPrefab, this.transform);
    }

    public void ObjectAdd(GraphObject item)
    {
        if (adjacencyMatrix == null)
        {
            adjacencyMatrix = new AdjacencyMatrix();
        }

        adjacencyMatrix.AddNode(item);

        rootPool.Alloc(adjacencyMatrix.Size);
    }

    public void ObjektRemove(GraphObject item)
    {
        if (adjacencyMatrix != null)
        {
            adjacencyMatrix.RemoveNode(item);
        }
    }

    public void Join(GraphObject firstNode, GraphObject secondNode)
    {
        if (firstNode == null) return;

        if (secondNode == null)
        {
            if (firstNode.root != null)
            {
                if (!firstNode.root.isKinematic)
                {
                    firstNode.root.bodyType = RigidbodyType2D.Kinematic;
                    firstNode.root.velocity = Vector2.zero;
                    firstNode.root.angularVelocity = 0;
                }
            }
            else
            {
                firstNode.Attach(rootPool.Request(firstNode.transform.position));
                firstNode.root.bodyType = RigidbodyType2D.Kinematic;
                firstNode.root.velocity = Vector2.zero;
                firstNode.root.angularVelocity = 0;
            }
            adjacencyMatrix.MakeStatic(firstNode);
            return;
        }

        adjacencyMatrix.MakeEdge(firstNode, secondNode);

        if (secondNode.root != null && firstNode.root == secondNode.root) return;

        GraphObject[] firstTree;

        Rigidbody2D graphRoot = null;

        if (secondNode.root != null)
        {
            graphRoot = secondNode.root;
            rootPool.Store(firstNode.root);
        }

        if (firstNode.root != null)
        {
            adjacencyMatrix.GetTreeNodes(firstNode, out firstTree);
            if (graphRoot == null)
            {
                graphRoot = firstNode.root;
            }
        }
        else
        {
            firstTree = new GraphObject[1] { firstNode };
        }

        if (graphRoot == null)
        {
            graphRoot = rootPool.Request(secondNode.transform.position);
        }

        if (adjacencyMatrix.StaticEdges(firstNode) > 0)
        {
            graphRoot.bodyType = RigidbodyType2D.Kinematic;
            graphRoot.velocity = Vector2.zero;
            graphRoot.angularVelocity = 0;
        }

        if (graphRoot != firstNode.root)
        {
            foreach (GraphObject node in firstTree)
            {
                node.Attach(graphRoot);
            }
        }
        
        if (graphRoot != secondNode.root)
        {
            secondNode.Attach(graphRoot);
        }
    }

    public void split(GraphObject firstNode, GraphObject secondNode)
    {
        if (firstNode == null || firstNode.root == null) return;

        if (secondNode == null)
        {
            if (adjacencyMatrix.StaticEdges(firstNode) == 1)
            {
                firstNode.root.bodyType = RigidbodyType2D.Dynamic;

                if (adjacencyMatrix.TreeSize(firstNode) == 1)
                {
                    rootPool.Store(firstNode.root);
                    firstNode.Release();
                }
            }
            adjacencyMatrix.BreakStatic(firstNode);
            
            return;
        }


        adjacencyMatrix.BreakEdge(firstNode, secondNode);

        if (adjacencyMatrix.OnSameTree(firstNode, secondNode)) return;

        GraphObject[] firstTree;
        adjacencyMatrix.GetTreeNodes(firstNode, out firstTree);

        if (firstTree.Length == 1)
        {
            firstNode.Release();
        }
        else
        {
            Rigidbody2D newRoot = rootPool.Request(firstNode.transform.position);

            if (adjacencyMatrix.StaticEdges(firstNode) > 0)
            {
                newRoot.bodyType = RigidbodyType2D.Kinematic;
            }

            foreach (GraphObject node in firstTree)
            {
                node.Attach(newRoot);
            }
        }

        if (adjacencyMatrix.TreeSize(secondNode) == 1)
        {
            Rigidbody2D temp = secondNode.root;
            secondNode.Release();
            rootPool.Store(temp);
        }
    }

    public void UnstickConnected(GraphObject node)
    {
        // kopplar loss alla GraphObjects kopplade till detta object
        GraphObject[] connected;
        adjacencyMatrix.GetConnected(node, out connected);
        
        foreach (GraphObject item in connected)
        {
            item.Unstick();
        }
    }
}

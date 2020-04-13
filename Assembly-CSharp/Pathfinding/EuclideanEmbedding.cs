using Pathfinding.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class EuclideanEmbedding
    {
        private const uint ra = 12820163u;

        private const uint rc = 1140671485u;

        public HeuristicOptimizationMode mode;

        public int seed;

        public Transform pivotPointRoot;

        public int spreadOutCount = 1;

        [NonSerialized]
        public bool dirty;

        private uint[] costs = new uint[8];

        private int maxNodeIndex;

        private int pivotCount;

        private GraphNode[] pivots;

        private uint rval;

        private object lockObj = new object();

        private uint GetRandom()
        {
            rval = 12820163 * rval + 1140671485;
            return rval;
        }

        private void EnsureCapacity(int index)
        {
            if (index > maxNodeIndex)
            {
                lock (lockObj)
                {
                    if (index > maxNodeIndex)
                    {
                        if (index >= costs.Length)
                        {
                            uint[] array = new uint[Math.Max(index * 2, pivots.Length * 2)];
                            for (int i = 0; i < costs.Length; i++)
                            {
                                array[i] = costs[i];
                            }
                            costs = array;
                        }
                        maxNodeIndex = index;
                    }
                }
            }
        }

        public uint GetHeuristic(int nodeIndex1, int nodeIndex2)
        {
            nodeIndex1 *= pivotCount;
            nodeIndex2 *= pivotCount;
            if (nodeIndex1 >= costs.Length || nodeIndex2 >= costs.Length)
            {
                EnsureCapacity((nodeIndex1 <= nodeIndex2) ? nodeIndex2 : nodeIndex1);
            }
            uint num = 0u;
            for (int i = 0; i < pivotCount; i++)
            {
                uint num2 = (uint)Math.Abs((int)(costs[nodeIndex1 + i] - costs[nodeIndex2 + i]));
                if (num2 > num)
                {
                    num = num2;
                }
            }
            return num;
        }

        private void GetClosestWalkableNodesToChildrenRecursively(Transform tr, List<GraphNode> nodes)
        {
            foreach (Transform item in tr)
            {
                NNInfo nearest = AstarPath.active.GetNearest(item.position, NNConstraint.Default);
                if (nearest.node != null && nearest.node.Walkable)
                {
                    nodes.Add(nearest.node);
                }
                GetClosestWalkableNodesToChildrenRecursively(item, nodes);
            }
        }

        private void PickNRandomNodes(int count, List<GraphNode> buffer)
        {
            int i = 0;
            NavGraph[] graphs = AstarPath.active.graphs;
            for (int j = 0; j < graphs.Length; j++)
            {
                graphs[j].GetNodes(delegate (GraphNode node)
                {
                    if (!node.Destroyed && node.Walkable)
                    {
                        i++;
                        if ((long)GetRandom() % (long)i < count)
                        {
                            if (buffer.Count < count)
                            {
                                buffer.Add(node);
                            }
                            else
                            {
                                buffer[(int)((long)GetRandom() % (long)buffer.Count)] = node;
                            }
                        }
                    }
                    return true;
                });
            }
        }

        private GraphNode PickAnyWalkableNode()
        {
            NavGraph[] graphs = AstarPath.active.graphs;
            GraphNode first = null;
            for (int i = 0; i < graphs.Length; i++)
            {
                graphs[i].GetNodes(delegate (GraphNode node)
                {
                    if (node != null && node.Walkable)
                    {
                        first = node;
                        return false;
                    }
                    return true;
                });
            }
            return first;
        }

        public void RecalculatePivots()
        {
            if (mode == HeuristicOptimizationMode.None)
            {
                pivotCount = 0;
                pivots = null;
                return;
            }
            rval = (uint)seed;
            List<GraphNode> list = ListPool<GraphNode>.Claim();
            switch (mode)
            {
                case HeuristicOptimizationMode.Custom:
                    if (pivotPointRoot == null)
                    {
                        throw new Exception("heuristicOptimizationMode is HeuristicOptimizationMode.Custom, but no 'customHeuristicOptimizationPivotsRoot' is set");
                    }
                    GetClosestWalkableNodesToChildrenRecursively(pivotPointRoot, list);
                    break;
                case HeuristicOptimizationMode.Random:
                    PickNRandomNodes(spreadOutCount, list);
                    break;
                case HeuristicOptimizationMode.RandomSpreadOut:
                    {
                        if (pivotPointRoot != null)
                        {
                            GetClosestWalkableNodesToChildrenRecursively(pivotPointRoot, list);
                        }
                        if (list.Count == 0)
                        {
                            GraphNode graphNode = PickAnyWalkableNode();
                            if (graphNode == null)
                            {
                                Debug.LogError("Could not find any walkable node in any of the graphs.");
                                ListPool<GraphNode>.Release(list);
                                return;
                            }
                            list.Add(graphNode);
                        }
                        int num = spreadOutCount - list.Count;
                        for (int i = 0; i < num; i++)
                        {
                            list.Add(null);
                        }
                        break;
                    }
                default:
                    throw new Exception("Invalid HeuristicOptimizationMode: " + mode);
            }
            pivots = list.ToArray();
            ListPool<GraphNode>.Release(list);
        }

        public void RecalculateCosts()
        {
            if (pivots == null)
            {
                RecalculatePivots();
            }
            if (mode == HeuristicOptimizationMode.None)
            {
                return;
            }
            pivotCount = 0;
            for (int i = 0; i < pivots.Length; i++)
            {
                if (pivots[i] != null && (pivots[i].Destroyed || !pivots[i].Walkable))
                {
                    throw new Exception("Invalid pivot nodes (destroyed or unwalkable)");
                }
            }
            if (mode != HeuristicOptimizationMode.RandomSpreadOut)
            {
                for (int j = 0; j < pivots.Length; j++)
                {
                    if (pivots[j] == null)
                    {
                        throw new Exception("Invalid pivot nodes (null)");
                    }
                }
            }
            Debug.Log("Recalculating costs...");
            pivotCount = pivots.Length;
            Action<int> startCostCalculation = null;
            int numComplete = 0;
            OnPathDelegate onComplete = delegate
            {
                numComplete++;
                if (numComplete == pivotCount)
                {
                    Debug.Log("Grid graph special case!");
                    ApplyGridGraphEndpointSpecialCase();
                }
            };
            startCostCalculation = delegate (int k)
            {
                GraphNode pivot = pivots[k];
                FloodPath fp = null;
                fp = FloodPath.Construct(pivot, onComplete);
                fp.immediateCallback = delegate (Path _p)
                {
                    _p.Claim(this);
                    MeshNode meshNode = pivot as MeshNode;
                    uint costOffset = 0u;
                    if (meshNode != null && meshNode.connectionCosts != null)
                    {
                        for (int m = 0; m < meshNode.connectionCosts.Length; m++)
                        {
                            costOffset = Math.Max(costOffset, meshNode.connectionCosts[m]);
                        }
                    }
                    NavGraph[] graphs = AstarPath.active.graphs;
                    for (int num = graphs.Length - 1; num >= 0; num--)
                    {
                        graphs[num].GetNodes(delegate (GraphNode node)
                        {
                            int num7 = node.NodeIndex * pivotCount + k;
                            EnsureCapacity(num7);
                            PathNode pathNode = fp.pathHandler.GetPathNode(node);
                            if (costOffset != 0)
                            {
                                costs[num7] = ((pathNode.pathID == fp.pathID && pathNode.parent != null) ? Math.Max(pathNode.parent.G - costOffset, 0u) : 0u);
                            }
                            else
                            {
                                costs[num7] = ((pathNode.pathID == fp.pathID) ? pathNode.G : 0u);
                            }
                            return true;
                        });
                    }
                    if (mode == HeuristicOptimizationMode.RandomSpreadOut && k < pivots.Length - 1)
                    {
                        if (pivots[k + 1] == null)
                        {
                            int num2 = -1;
                            uint num3 = 0u;
                            int num4 = maxNodeIndex / pivotCount;
                            for (int n = 1; n < num4; n++)
                            {
                                uint num5 = 1073741824u;
                                for (int num6 = 0; num6 <= k; num6++)
                                {
                                    num5 = Math.Min(num5, costs[n * pivotCount + num6]);
                                }
                                GraphNode node2 = fp.pathHandler.GetPathNode(n).node;
                                if ((num5 > num3 || num2 == -1) && node2 != null && !node2.Destroyed && node2.Walkable)
                                {
                                    num2 = n;
                                    num3 = num5;
                                }
                            }
                            if (num2 == -1)
                            {
                                Debug.LogError("Failed generating random pivot points for heuristic optimizations");
                                return;
                            }
                            pivots[k + 1] = fp.pathHandler.GetPathNode(num2).node;
                        }
                        startCostCalculation(k + 1);
                    }
                    _p.Release(this);
                };
                AstarPath.StartPath(fp, pushToFront: true);
            };
            if (mode != HeuristicOptimizationMode.RandomSpreadOut)
            {
                for (int l = 0; l < pivots.Length; l++)
                {
                    startCostCalculation(l);
                }
            }
            else
            {
                startCostCalculation(0);
            }
            dirty = false;
        }

        private void ApplyGridGraphEndpointSpecialCase()
        {
            NavGraph[] graphs = AstarPath.active.graphs;
            for (int i = 0; i < graphs.Length; i++)
            {
                GridGraph gridGraph = graphs[i] as GridGraph;
                if (gridGraph == null)
                {
                    continue;
                }
                GridNode[] nodes = gridGraph.nodes;
                int num = (gridGraph.neighbours == NumNeighbours.Four) ? 4 : ((gridGraph.neighbours != NumNeighbours.Eight) ? 6 : 8);
                for (int j = 0; j < gridGraph.depth; j++)
                {
                    for (int k = 0; k < gridGraph.width; k++)
                    {
                        GridNode gridNode = nodes[j * gridGraph.width + k];
                        if (gridNode.Walkable)
                        {
                            continue;
                        }
                        int num2 = gridNode.NodeIndex * pivotCount;
                        for (int l = 0; l < pivotCount; l++)
                        {
                            costs[num2 + l] = uint.MaxValue;
                        }
                        for (int m = 0; m < num; m++)
                        {
                            int num3;
                            int num4;
                            if (gridGraph.neighbours == NumNeighbours.Six)
                            {
                                num3 = k + gridGraph.neighbourXOffsets[GridGraph.hexagonNeighbourIndices[m]];
                                num4 = j + gridGraph.neighbourZOffsets[GridGraph.hexagonNeighbourIndices[m]];
                            }
                            else
                            {
                                num3 = k + gridGraph.neighbourXOffsets[m];
                                num4 = j + gridGraph.neighbourZOffsets[m];
                            }
                            if (num3 < 0 || num4 < 0 || num3 >= gridGraph.width || num4 >= gridGraph.depth)
                            {
                                continue;
                            }
                            GridNode gridNode2 = gridGraph.nodes[num4 * gridGraph.width + num3];
                            if (gridNode2.Walkable)
                            {
                                for (int n = 0; n < pivotCount; n++)
                                {
                                    uint val = costs[gridNode2.NodeIndex * pivotCount + n] + gridGraph.neighbourCosts[m];
                                    costs[num2 + n] = Math.Min(costs[num2 + n], val);
                                    Debug.DrawLine((Vector3)gridNode.position, (Vector3)gridNode2.position, Color.blue, 1f);
                                }
                            }
                        }
                        for (int num5 = 0; num5 < pivotCount; num5++)
                        {
                            if (costs[num2 + num5] == uint.MaxValue)
                            {
                                costs[num2 + num5] = 0u;
                            }
                        }
                    }
                }
            }
        }

        public void OnDrawGizmos()
        {
            if (pivots == null)
            {
                return;
            }
            for (int i = 0; i < pivots.Length; i++)
            {
                Gizmos.color = new Color(53f / 85f, 94f / 255f, 194f / 255f, 0.8f);
                if (pivots[i] != null && !pivots[i].Destroyed)
                {
                    Gizmos.DrawCube((Vector3)pivots[i].position, Vector3.one);
                }
            }
        }
    }
}

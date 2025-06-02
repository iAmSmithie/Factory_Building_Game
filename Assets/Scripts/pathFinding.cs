using System.Collections.Generic;
using UnityEngine;

public static class pathFinding
{
    //The distance between adjacent hex centers
    private static float hexSpacing = 0.1f;
    //Stores all valid hex positions in the grid
    private static List<Vector3> allHexPositions;

    //Initializes the pathfinding system with all hex positions
    public static void Initialize(List<Vector3> hexPositions)
    {
        allHexPositions = hexPositions;
    }
    public static List<Vector3> GetAllHexPositions()
    {
        return HexPoints.Instance?.GetHexPositions() ?? new List<Vector3>();
    }

    //Finds a path between two points using either straight-line or A* pathfinding
    public static List<Vector3> FindPath(Vector3 start, Vector3 end, List<Vector3> allHexPositions)
    {
        //Snap both positions to exact hex grid coordinates
        Vector3 startHex = GetExactHexPosition(start, allHexPositions);
        Vector3 endHex = GetExactHexPosition(end, allHexPositions);

        //Early exit if start and end are the same
        if (startHex == endHex)
        {
            return new List<Vector3> { startHex };
        }

        //First try straight line path (more efficient)
        List<Vector3> straightPath = GetStraightHexPath(startHex, endHex, allHexPositions);
        if (straightPath.Count > 0)
        {
            return straightPath;
        }

        //Fall back to A* if straight path isn't possible
        return FindPathAStar(startHex, endHex, allHexPositions);
    }

    //Attempts to create a straight-line path between two hex positions
    private static List<Vector3> GetStraightHexPath(Vector3 start, Vector3 end, List<Vector3> allHexPositions)
    {
        List<Vector3> path = new List<Vector3> { start };
        Vector3 direction = (end - start).normalized;
        float maxDistance = Vector3.Distance(start, end);
        //Use slightly less than exact spacing to ensure full coverage
        float spacing = hexSpacing * 0.99f;

        //Step along the straight line at hexSpacing intervals
        for (float dist = spacing; dist < maxDistance; dist += spacing)
        {
            Vector3 testPoint = start + direction * dist;
            Vector3 nearestHex = GetExactHexPosition(testPoint, allHexPositions);

            //Skip if this is the same as the last position
            if (nearestHex == path[path.Count - 1]) continue;
            //Abort if we hit an invalid hex position
            if (!allHexPositions.Contains(nearestHex)) return new List<Vector3>();

            path.Add(nearestHex);
            //Early exit if we reach the end
            if (nearestHex == end) break;
        }

        //Ensure we always include the end position
        if (path[path.Count - 1] != end)
        {
            path.Add(end);
        }

        return path;
    }

    //A* pathfinding algorithm implementation
    private static List<Vector3> FindPathAStar(Vector3 start, Vector3 end, List<Vector3> allHexPositions)
    {
        //Nodes to be evaluated
        var openSet = new List<Vector3> { start };
        //Nodes already evaluated
        var closedSet = new HashSet<Vector3>();
        //Tracks the optimal path
        var cameFrom = new Dictionary<Vector3, Vector3>();
        //Cost from start to each node
        var gScore = new Dictionary<Vector3, float>();
        //Estimated total cost (gScore + heuristic)
        var fScore = new Dictionary<Vector3, float>();

        //Initialize starting values
        gScore[start] = 0;
        fScore[start] = Vector3.Distance(start, end);

        while (openSet.Count > 0)
        {
            //Find node in openSet with lowest fScore
            Vector3 current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore[openSet[i]] < fScore[current])
                {
                    current = openSet[i];
                }
            }

            //If we reached the end, reconstruct the path
            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            //Move current from open to closed set
            openSet.Remove(current);
            closedSet.Add(current);

            //Evaluate all neighbors
            foreach (Vector3 neighbor in GetHexNeighbors(current, allHexPositions))
            {
                //Skip already evaluated nodes
                if (closedSet.Contains(neighbor)) continue;

                //Calculate tentative path cost
                float tentativeGScore = gScore[current] + Vector3.Distance(current, neighbor);

                //If this path to neighbor is better than any previous one
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    // Record this path
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Vector3.Distance(neighbor, end);

                    //Add to open set if not already there
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        //If we get here, no path was found
        return new List<Vector3>();
    }

    //Gets all valid hex positions adjacent to the given position
    private static List<Vector3> GetHexNeighbors(Vector3 position, List<Vector3> allHexPositions)
    {
        List<Vector3> neighbors = new List<Vector3>();
        //Search radius with small tolerance for floating point precision
        float neighborDistance = hexSpacing * 1.1f;

        foreach (Vector3 hex in allHexPositions)
        {
            //Skip self and check if within neighbor distance
            if (hex == position) continue;
            if (Vector3.Distance(position, hex) <= neighborDistance)
            {
                neighbors.Add(hex);
            }
        }

        return neighbors;
    }

    //Reconstructs the path from end to start using the cameFrom dictionary
    private static List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 end)
    {
        List<Vector3> path = new List<Vector3> { end };
        Vector3 current = end;

        //Work backwards from end to start
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        //Fill in any intermediate hexes between path points
        List<Vector3> completePath = new List<Vector3>();
        for (int i = 0; i < path.Count - 1; i++)
        {
            completePath.Add(path[i]);
            completePath.AddRange(GetIntermediateHexes(path[i], path[i + 1]));
        }
        completePath.Add(path[path.Count - 1]);

        return completePath;
    }

    //Gets intermediate hex positions between two points
    private static List<Vector3> GetIntermediateHexes(Vector3 a, Vector3 b)
    {
        List<Vector3> intermediates = new List<Vector3>();
        Vector3 direction = (b - a).normalized;
        float distance = Vector3.Distance(a, b);
        //Calculate how many intermediate steps we need
        int steps = Mathf.FloorToInt(distance / hexSpacing);

        //Create points at regular intervals between a and b
        for (int i = 1; i < steps; i++)
        {
            Vector3 point = a + direction * (hexSpacing * i);
            intermediates.Add(point);
        }

        return intermediates;
    }

    //Snaps a world position to the nearest exact hex position
    public static Vector3 GetExactHexPosition(Vector3 position, List<Vector3> allHexPositions)
    {
        Vector3 nearest = allHexPositions[0];
        float minDistance = Vector3.Distance(position, nearest);

        //Find the closest hex position in the grid
        foreach (Vector3 hex in allHexPositions)
        {
            float dist = Vector3.Distance(position, hex);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = hex;
            }
        }

        return nearest;
    }
}
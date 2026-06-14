using System.Collections.Generic;
using UnityEngine;

public class CrystalConnectionManager : MonoBehaviour
{
    [System.Serializable]
    public class Connection
    {
        public string label;
        public CrystalInteract stoneA;
        public CrystalInteract stoneB;
    }

    [Header("Connections")]
    public Connection[] connections;

    [Header("Random Connections")]
    public bool randomizeOnStart = false;
    public CrystalInteract[] randomStones;
    public bool requireFourColors = true;
    public int randomConnectionCount = 18;
    public int minRandomConnectionCount = 16;
    public int maxRandomConnectionCount = 20;
    public int minConnectionsPerStone = 3;
    public int maxConnectionsPerStone = 6;
    public bool avoidCrossingLines = false;
    public bool allowSmallCrossingChallenges = true;
    public int maxTotalCrossings = 5;
    public int maxCrossingsPerLine = 1;
    public int nearbyCandidateCount = 4;
    public int maxLongChallengeLinks = 2;
    public float minDistanceFromUnrelatedStone = 3f;
    public bool allowForcedLinksNearUnrelatedStones = false;

    [Header("Line Defaults")]
    public bool revealLinksOnlyAfterColoring = true;
    public float heightOffset = 0.12f;
    public float lineWidth = 0.08f;
    public Color waitingColor = new Color(0.8f, 0.9f, 1f, 0.45f);
    public Color validColor = new Color(0f, 1f, 0.18f, 1f);
    public Color conflictColor = new Color(1f, 0f, 0f, 1f);
    public int dashCount = 7;

    private string lastRandomSignature = "";
    private bool puzzleCompleted = false;

    private void OnValidate()
    {
        minRandomConnectionCount = Mathf.Max(16, minRandomConnectionCount);
        maxRandomConnectionCount = Mathf.Max(minRandomConnectionCount, maxRandomConnectionCount);
        randomConnectionCount = Mathf.Clamp(randomConnectionCount, minRandomConnectionCount, maxRandomConnectionCount);
        minConnectionsPerStone = Mathf.Clamp(minConnectionsPerStone, 2, 3);
        maxConnectionsPerStone = Mathf.Clamp(maxConnectionsPerStone, 5, 6);
        minDistanceFromUnrelatedStone = Mathf.Max(2.8f, minDistanceFromUnrelatedStone);
    }

    private void Start()
    {
        if (randomizeOnStart)
            RandomizeConnections();
        else
            BuildConnections();

        // Subscribe to color changes so we can detect when the puzzle is solved
        foreach (CrystalInteract stone in GetPuzzleStones())
        {
            if (stone != null)
                stone.ColorChanged += OnCrystalColorChanged;
        }
    }

    private void OnDestroy()
    {
        foreach (CrystalInteract stone in GetPuzzleStones())
        {
            if (stone != null)
                stone.ColorChanged -= OnCrystalColorChanged;
        }
    }

    private void Update()
    {
        TryCompletePuzzle();
    }

    // Fires every time any crystal's color changes; checks for a full solve once per event
    private void OnCrystalColorChanged(CrystalInteract crystal)
    {
        TryCompletePuzzle();
    }

    private void TryCompletePuzzle()
    {
        if (puzzleCompleted || !IsSolved())
            return;

        puzzleCompleted = true;

        BoatPieceManager boatManager = FindFirstObjectByType<BoatPieceManager>();
        if (boatManager != null)
            boatManager.OnStonePuzzleComplete();
        else
            Debug.LogWarning("Stone puzzle solved, but BoatPieceManager was not found.");
    }

    [ContextMenu("Build Connections")]
    public void BuildConnections()
    {
        ClearGeneratedConnections();

        if (connections == null)
            return;

        foreach (Connection connection in connections)
        {
            if (connection == null || connection.stoneA == null || connection.stoneB == null)
                continue;

            string lineName = string.IsNullOrWhiteSpace(connection.label)
                ? connection.stoneA.name + " to " + connection.stoneB.name
                : connection.label;

            GameObject lineObject = new GameObject("Line_" + lineName);
            lineObject.transform.SetParent(transform, false);

            CrystalConnectionLine line = lineObject.AddComponent<CrystalConnectionLine>();
            line.stoneA = connection.stoneA;
            line.stoneB = connection.stoneB;
            line.revealOnlyWhenEndpointColored = revealLinksOnlyAfterColoring;
            line.heightOffset = heightOffset;
            line.lineWidth = lineWidth;
            line.waitingColor = waitingColor;
            line.validColor = validColor;
            line.conflictColor = conflictColor;
            line.dashCount = dashCount;
            line.Refresh();
        }
    }

    [ContextMenu("Randomize Connections")]
    public void RandomizeConnections()
    {
        List<CrystalInteract> stones = GetRandomStoneList();
        if (stones.Count < 2)
            return;

        int maxConnections = stones.Count * (stones.Count - 1) / 2;
        int maxByStoneDegree = Mathf.Max(stones.Count - 1, stones.Count * GetMaxConnectionsPerStone() / 2);
        int minimumDegreeEdges = Mathf.CeilToInt(stones.Count * Mathf.Clamp(minConnectionsPerStone, 1, GetMaxConnectionsPerStone()) / 2f);
        int minimumConnections = requireFourColors && stones.Count >= 4 ? stones.Count + 2 : stones.Count - 1;
        minimumConnections = Mathf.Max(minimumConnections, minimumDegreeEdges);
        int allowedMaximum = Mathf.Min(maxRandomConnectionCount, maxConnections, maxByStoneDegree);
        int requestedMinimum = Mathf.Min(Mathf.Max(minRandomConnectionCount, minimumConnections), allowedMaximum);
        int targetCount = Mathf.Clamp(randomConnectionCount, requestedMinimum, allowedMaximum);
        int minimumTargetCount = Mathf.Clamp(requestedMinimum, minimumConnections, allowedMaximum);
        List<Vector2Int> pairs = CreateRandomPairs(stones, targetCount);

        string signature = GetPairSignature(pairs);
        int guard = 0;
        while ((signature == lastRandomSignature || pairs.Count < minimumTargetCount) && guard < 20)
        {
            pairs = CreateRandomPairs(stones, targetCount);
            signature = GetPairSignature(pairs);
            guard++;
        }

        lastRandomSignature = signature;
        connections = new Connection[pairs.Count];

        for (int i = 0; i < pairs.Count; i++)
        {
            CrystalInteract stoneA = stones[pairs[i].x];
            CrystalInteract stoneB = stones[pairs[i].y];

            connections[i] = new Connection
            {
                label = stoneA.name + "-" + stoneB.name,
                stoneA = stoneA,
                stoneB = stoneB
            };
        }

        BuildConnections();
    }

    public void ResetWithRandomConnections()
    {
        RandomizeConnections();
    }

    public bool IsSolved()
    {
        List<CrystalInteract> stones = GetRandomStoneList();
        if (stones.Count == 0)
            return false;

        foreach (CrystalInteract stone in stones)
        {
            if (stone == null || !stone.HasColor)
                return false;
        }

        return !HasAnyConflict();
    }

    public bool HasAnyConflict()
    {
        if (connections == null)
            return false;

        foreach (Connection connection in connections)
        {
            if (connection == null || connection.stoneA == null || connection.stoneB == null)
                continue;

            if (!connection.stoneA.HasColor || !connection.stoneB.HasColor)
                continue;

            if (connection.stoneA.SelectedColorIndex == connection.stoneB.SelectedColorIndex)
                return true;
        }

        return false;
    }

    public bool HasConflictFor(CrystalInteract stone)
    {
        if (stone == null || connections == null || !stone.HasColor)
            return false;

        foreach (Connection connection in connections)
        {
            if (connection == null || connection.stoneA == null || connection.stoneB == null)
                continue;

            bool touchesStone = connection.stoneA == stone || connection.stoneB == stone;
            if (!touchesStone || !connection.stoneA.HasColor || !connection.stoneB.HasColor)
                continue;

            if (connection.stoneA.SelectedColorIndex == connection.stoneB.SelectedColorIndex)
                return true;
        }

        return false;
    }

    public void ClearAllStoneColors()
    {
        foreach (CrystalInteract stone in GetRandomStoneList())
        {
            if (stone != null)
                stone.ClearColor();
        }
    }

    public CrystalInteract[] GetPuzzleStones()
    {
        return GetRandomStoneList().ToArray();
    }

    private List<CrystalInteract> GetRandomStoneList()
    {
        List<CrystalInteract> stones = new List<CrystalInteract>();

        if (randomStones != null)
        {
            foreach (CrystalInteract stone in randomStones)
            {
                if (stone != null && !stones.Contains(stone))
                    stones.Add(stone);
            }
        }

        if (stones.Count > 0)
            return stones;

        if (connections == null)
            return stones;

        foreach (Connection connection in connections)
        {
            if (connection == null)
                continue;

            if (connection.stoneA != null && !stones.Contains(connection.stoneA))
                stones.Add(connection.stoneA);

            if (connection.stoneB != null && !stones.Contains(connection.stoneB))
                stones.Add(connection.stoneB);
        }

        return stones;
    }

    private List<Vector2Int> CreateRandomPairs(List<CrystalInteract> stones, int targetCount)
    {
        int stoneCount = stones.Count;
        int[] connectionCounts = new int[stoneCount];
        int totalCrossings = 0;
        int totalLongLinks = 0;
        float longLinkDistance = GetLongLinkDistance(stones);
        List<Vector2Int> pairs = new List<Vector2Int>();

        if (requireFourColors && stoneCount >= 4)
            BuildFourColorCore(stones, pairs, connectionCounts, longLinkDistance, ref totalCrossings, ref totalLongLinks);

        BuildRandomBackbone(stones, pairs, connectionCounts, longLinkDistance, ref totalCrossings, ref totalLongLinks);

        List<Vector2Int> allPossiblePairs = new List<Vector2Int>();
        for (int a = 0; a < stoneCount; a++)
        {
            for (int b = a + 1; b < stoneCount; b++)
                allPossiblePairs.Add(new Vector2Int(a, b));
        }

        Shuffle(allPossiblePairs);
        SortPairsByReadability(stones, pairs, allPossiblePairs, longLinkDistance);
        StrengthenLowDegreeStones(stones, pairs, connectionCounts, allPossiblePairs, targetCount, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        TryAddChallengePairs(stones, pairs, connectionCounts, allPossiblePairs, targetCount, longLinkDistance, ref totalCrossings, ref totalLongLinks);

        foreach (Vector2Int pair in allPossiblePairs)
        {
            if (pairs.Count >= targetCount)
                break;

            AddPairIfAllowed(pairs, connectionCounts, stones, pair.x, pair.y, false, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        }

        int hardMinimum = Mathf.Clamp(minRandomConnectionCount, 0, targetCount);
        ForceReachMinimumConnectionCount(stones, pairs, connectionCounts, allPossiblePairs, hardMinimum);

        return pairs;
    }

    private void ForceReachMinimumConnectionCount(List<CrystalInteract> stones, List<Vector2Int> pairs, int[] connectionCounts, List<Vector2Int> candidates, int minimumCount)
    {
        if (pairs.Count >= minimumCount)
            return;

        List<Vector2Int> readableCandidates = new List<Vector2Int>(candidates);
        readableCandidates.Sort((left, right) =>
        {
            float leftScore = GetPairReadabilityScore(stones, pairs, left, GetLongLinkDistance(stones));
            float rightScore = GetPairReadabilityScore(stones, pairs, right, GetLongLinkDistance(stones));
            return leftScore.CompareTo(rightScore);
        });

        foreach (Vector2Int pair in readableCandidates)
        {
            if (pairs.Count >= minimumCount)
                return;

            if (PairExists(pairs, pair))
                continue;

            int maxPerStone = GetMaxConnectionsPerStone();
            if (connectionCounts[pair.x] >= maxPerStone || connectionCounts[pair.y] >= maxPerStone)
                continue;

            if (!allowForcedLinksNearUnrelatedStones && PassesTooCloseToUnrelatedStone(stones, pair))
                continue;

            AddPairDirectly(pairs, connectionCounts, pair.x, pair.y);
        }
    }

    private void StrengthenLowDegreeStones(List<CrystalInteract> stones, List<Vector2Int> pairs, int[] connectionCounts, List<Vector2Int> candidates, int targetCount, float longLinkDistance, ref int totalCrossings, ref int totalLongLinks)
    {
        int minimumConnections = Mathf.Clamp(minConnectionsPerStone, 1, GetMaxConnectionsPerStone());
        int guard = 0;

        while (HasStoneBelowMinimum(connectionCounts, minimumConnections) && guard < stones.Count * candidates.Count)
        {
            bool addedThisPass = false;

            for (int stoneIndex = 0; stoneIndex < stones.Count; stoneIndex++)
            {
                if (connectionCounts[stoneIndex] >= minimumConnections)
                    continue;

                foreach (Vector2Int pair in candidates)
                {
                    if (pair.x != stoneIndex && pair.y != stoneIndex)
                        continue;

                    int beforeCount = pairs.Count;
                    AddPairIfAllowed(pairs, connectionCounts, stones, pair.x, pair.y, false, longLinkDistance, ref totalCrossings, ref totalLongLinks);

                    if (pairs.Count == beforeCount)
                        continue;

                    addedThisPass = true;
                    break;
                }
            }

            if (!addedThisPass)
                break;

            guard++;
        }

        ForceFixLowDegreeStones(stones, pairs, connectionCounts, candidates, targetCount, minimumConnections);
    }

    private void ForceFixLowDegreeStones(List<CrystalInteract> stones, List<Vector2Int> pairs, int[] connectionCounts, List<Vector2Int> candidates, int targetCount, int minimumConnections)
    {
        while (HasStoneBelowMinimum(connectionCounts, minimumConnections))
        {
            Vector2Int bestPair = new Vector2Int(-1, -1);
            float bestScore = float.MaxValue;

            foreach (Vector2Int pair in candidates)
            {
                if (PairExists(pairs, pair))
                    continue;

                if (connectionCounts[pair.x] >= GetMaxConnectionsPerStone() || connectionCounts[pair.y] >= GetMaxConnectionsPerStone())
                    continue;

                if (connectionCounts[pair.x] >= minimumConnections && connectionCounts[pair.y] >= minimumConnections)
                    continue;

                if (!allowForcedLinksNearUnrelatedStones && PassesTooCloseToUnrelatedStone(stones, pair))
                    continue;

                float score = Vector3.Distance(stones[pair.x].LineAnchorPosition, stones[pair.y].LineAnchorPosition);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestPair = pair;
            }

            if (bestPair.x < 0)
                return;

            AddPairDirectly(pairs, connectionCounts, bestPair.x, bestPair.y);
        }
    }

    private bool PairExists(List<Vector2Int> pairs, Vector2Int candidate)
    {
        Vector2Int normalizedCandidate = candidate.x < candidate.y
            ? candidate
            : new Vector2Int(candidate.y, candidate.x);

        foreach (Vector2Int pair in pairs)
        {
            if (pair == normalizedCandidate)
                return true;
        }

        return false;
    }

    private void AddPairDirectly(List<Vector2Int> pairs, int[] connectionCounts, int a, int b)
    {
        if (a == b)
            return;

        Vector2Int normalizedPair = a < b ? new Vector2Int(a, b) : new Vector2Int(b, a);
        if (PairExists(pairs, normalizedPair))
            return;

        pairs.Add(normalizedPair);
        connectionCounts[normalizedPair.x]++;
        connectionCounts[normalizedPair.y]++;
    }

    private bool HasStoneBelowMinimum(int[] connectionCounts, int minimumConnections)
    {
        foreach (int count in connectionCounts)
        {
            if (count < minimumConnections)
                return true;
        }

        return false;
    }

    private void BuildRandomBackbone(List<CrystalInteract> stones, List<Vector2Int> pairs, int[] connectionCounts, float longLinkDistance, ref int totalCrossings, ref int totalLongLinks)
    {
        List<int> connected = new List<int>();
        List<int> unconnected = new List<int>();

        for (int i = 0; i < stones.Count; i++)
            unconnected.Add(i);

        foreach (Vector2Int pair in pairs)
        {
            if (!connected.Contains(pair.x))
                connected.Add(pair.x);

            if (!connected.Contains(pair.y))
                connected.Add(pair.y);
        }

        foreach (int connectedIndex in connected)
            unconnected.Remove(connectedIndex);

        if (connected.Count == 0)
        {
            int startIndex = Random.Range(0, unconnected.Count);
            connected.Add(unconnected[startIndex]);
            unconnected.RemoveAt(startIndex);
        }

        while (unconnected.Count > 0)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();

            foreach (int unconnectedIndex in unconnected)
            {
                List<int> nearbyConnected = new List<int>(connected);
                nearbyConnected.Sort((left, right) =>
                {
                    float leftDistance = Vector3.Distance(stones[unconnectedIndex].LineAnchorPosition, stones[left].LineAnchorPosition);
                    float rightDistance = Vector3.Distance(stones[unconnectedIndex].LineAnchorPosition, stones[right].LineAnchorPosition);
                    return leftDistance.CompareTo(rightDistance);
                });

                int candidateLimit = Mathf.Clamp(nearbyCandidateCount, 1, nearbyConnected.Count);
                for (int i = 0; i < candidateLimit; i++)
                    candidates.Add(new Vector2Int(nearbyConnected[i], unconnectedIndex));
            }

            Shuffle(candidates);
            bool added = false;

            foreach (Vector2Int candidate in candidates)
            {
                int beforeCount = pairs.Count;
                AddPairIfAllowed(pairs, connectionCounts, stones, candidate.x, candidate.y, false, longLinkDistance, ref totalCrossings, ref totalLongLinks);

                if (pairs.Count == beforeCount)
                    continue;

                int newlyConnected = connected.Contains(candidate.x) ? candidate.y : candidate.x;
                connected.Add(newlyConnected);
                unconnected.Remove(newlyConnected);
                added = true;
                break;
            }

            if (added)
                continue;

            Vector2Int fallback = candidates[0];
            AddPairIfAllowed(pairs, connectionCounts, stones, fallback.x, fallback.y, true, longLinkDistance, ref totalCrossings, ref totalLongLinks);

            int fallbackConnected = connected.Contains(fallback.x) ? fallback.y : fallback.x;
            connected.Add(fallbackConnected);
            unconnected.Remove(fallbackConnected);
        }
    }

    private void BuildFourColorCore(List<CrystalInteract> stones, List<Vector2Int> pairs, int[] connectionCounts, float longLinkDistance, ref int totalCrossings, ref int totalLongLinks)
    {
        List<CoreCandidate> candidates = new List<CoreCandidate>();

        for (int a = 0; a < stones.Count - 3; a++)
        {
            for (int b = a + 1; b < stones.Count - 2; b++)
            {
                for (int c = b + 1; c < stones.Count - 1; c++)
                {
                    for (int d = c + 1; d < stones.Count; d++)
                    {
                        int[] indexes = { a, b, c, d };
                        candidates.Add(new CoreCandidate(indexes, ScoreFourColorCore(stones, indexes, longLinkDistance)));
                    }
                }
            }
        }

        candidates.Sort((left, right) => left.score.CompareTo(right.score));
        int pickCount = Mathf.Min(5, candidates.Count);
        CoreCandidate chosen = candidates[Random.Range(0, pickCount)];

        AddPairIfAllowed(pairs, connectionCounts, stones, chosen.indexes[0], chosen.indexes[1], true, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        AddPairIfAllowed(pairs, connectionCounts, stones, chosen.indexes[0], chosen.indexes[2], true, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        AddPairIfAllowed(pairs, connectionCounts, stones, chosen.indexes[0], chosen.indexes[3], true, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        AddPairIfAllowed(pairs, connectionCounts, stones, chosen.indexes[1], chosen.indexes[2], true, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        AddPairIfAllowed(pairs, connectionCounts, stones, chosen.indexes[1], chosen.indexes[3], true, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        AddPairIfAllowed(pairs, connectionCounts, stones, chosen.indexes[2], chosen.indexes[3], true, longLinkDistance, ref totalCrossings, ref totalLongLinks);
    }

    private float ScoreFourColorCore(List<CrystalInteract> stones, int[] indexes, float longLinkDistance)
    {
        List<Vector2Int> corePairs = new List<Vector2Int>
        {
            new Vector2Int(indexes[0], indexes[1]),
            new Vector2Int(indexes[0], indexes[2]),
            new Vector2Int(indexes[0], indexes[3]),
            new Vector2Int(indexes[1], indexes[2]),
            new Vector2Int(indexes[1], indexes[3]),
            new Vector2Int(indexes[2], indexes[3])
        };

        float totalDistance = 0f;
        int crossings = 0;
        int nearMisses = 0;
        int longLinks = 0;

        for (int i = 0; i < corePairs.Count; i++)
        {
            Vector2Int pair = corePairs[i];
            float distance = Vector3.Distance(stones[pair.x].LineAnchorPosition, stones[pair.y].LineAnchorPosition);
            totalDistance += distance;

            if (distance > longLinkDistance)
                longLinks++;

            if (PassesTooCloseToUnrelatedStone(stones, pair))
                nearMisses++;

            for (int j = i + 1; j < corePairs.Count; j++)
            {
                if (SharesEndpoint(pair, corePairs[j]))
                    continue;

                if (SegmentsCrossXZ(
                    stones[pair.x].LineAnchorPosition,
                    stones[pair.y].LineAnchorPosition,
                    stones[corePairs[j].x].LineAnchorPosition,
                    stones[corePairs[j].y].LineAnchorPosition))
                {
                    crossings++;
                }
            }
        }

        return totalDistance + crossings * 10f + nearMisses * 40f + longLinks * 8f + Random.value * 2f;
    }

    private void TryAddChallengePairs(List<CrystalInteract> stones, List<Vector2Int> pairs, int[] connectionCounts, List<Vector2Int> candidates, int targetCount, float longLinkDistance, ref int totalCrossings, ref int totalLongLinks)
    {
        if (!allowSmallCrossingChallenges)
            return;

        foreach (Vector2Int pair in candidates)
        {
            if (pairs.Count >= targetCount)
                return;

            int crossings = CountCrossingsWithExistingPairs(pairs, stones, pair);
            if (crossings <= 0)
                continue;

            AddPairIfAllowed(pairs, connectionCounts, stones, pair.x, pair.y, false, longLinkDistance, ref totalCrossings, ref totalLongLinks);
        }
    }

    private void SortPairsByReadability(List<CrystalInteract> stones, List<Vector2Int> existingPairs, List<Vector2Int> candidates, float longLinkDistance)
    {
        candidates.Sort((left, right) =>
        {
            float leftScore = GetPairReadabilityScore(stones, existingPairs, left, longLinkDistance);
            float rightScore = GetPairReadabilityScore(stones, existingPairs, right, longLinkDistance);
            return leftScore.CompareTo(rightScore);
        });
    }

    private float GetPairReadabilityScore(List<CrystalInteract> stones, List<Vector2Int> existingPairs, Vector2Int pair, float longLinkDistance)
    {
        float distance = Vector3.Distance(stones[pair.x].LineAnchorPosition, stones[pair.y].LineAnchorPosition);
        int crossings = CountCrossingsWithExistingPairs(existingPairs, stones, pair);
        bool passesNearStone = PassesTooCloseToUnrelatedStone(stones, pair);
        float longPenalty = distance > longLinkDistance ? 0.6f : 0f;
        float nearStonePenalty = passesNearStone ? 100f : 0f;
        float challengeBonus = crossings > 0 && distance <= longLinkDistance ? -0.25f : 0f;

        return distance + crossings * 4f + longPenalty + nearStonePenalty + challengeBonus + Random.value * 6f;
    }

    private void AddPairIfAllowed(List<Vector2Int> pairs, int[] connectionCounts, List<CrystalInteract> stones, int a, int b, bool force, float longLinkDistance, ref int totalCrossings, ref int totalLongLinks)
    {
        if (a == b)
            return;

        Vector2Int normalizedPair = a < b ? new Vector2Int(a, b) : new Vector2Int(b, a);

        int maxPerStone = GetMaxConnectionsPerStone();
        if (!force && (connectionCounts[normalizedPair.x] >= maxPerStone || connectionCounts[normalizedPair.y] >= maxPerStone))
            return;

        bool isLongLink = Vector3.Distance(stones[normalizedPair.x].LineAnchorPosition, stones[normalizedPair.y].LineAnchorPosition) > longLinkDistance;
        if (!force && isLongLink && totalLongLinks >= Mathf.Max(0, maxLongChallengeLinks))
            return;

        if (!force && PassesTooCloseToUnrelatedStone(stones, normalizedPair))
            return;

        int newCrossings = CountCrossingsWithExistingPairs(pairs, stones, normalizedPair);
        if (!force && avoidCrossingLines && newCrossings > 0)
            return;

        if (!force && allowSmallCrossingChallenges)
        {
            int maxLineCrossings = Mathf.Max(0, maxCrossingsPerLine);
            int maxGraphCrossings = Mathf.Max(0, maxTotalCrossings);

            if (newCrossings > maxLineCrossings || totalCrossings + newCrossings > maxGraphCrossings)
                return;
        }

        foreach (Vector2Int pair in pairs)
        {
            if (pair == normalizedPair)
                return;
        }

        pairs.Add(normalizedPair);
        connectionCounts[normalizedPair.x]++;
        connectionCounts[normalizedPair.y]++;
        totalCrossings += newCrossings;

        if (isLongLink)
            totalLongLinks++;
    }

    private float GetLongLinkDistance(List<CrystalInteract> stones)
    {
        if (stones.Count < 2)
            return 0f;

        List<float> distances = new List<float>();
        for (int a = 0; a < stones.Count; a++)
        {
            for (int b = a + 1; b < stones.Count; b++)
                distances.Add(Vector3.Distance(stones[a].LineAnchorPosition, stones[b].LineAnchorPosition));
        }

        distances.Sort();
        int index = Mathf.Clamp(Mathf.RoundToInt(distances.Count * 0.65f), 0, distances.Count - 1);
        return distances[index];
    }

    private int GetMaxConnectionsPerStone()
    {
        return requireFourColors ? Mathf.Max(4, maxConnectionsPerStone) : Mathf.Max(2, maxConnectionsPerStone);
    }

    private struct CoreCandidate
    {
        public readonly int[] indexes;
        public readonly float score;

        public CoreCandidate(int[] indexes, float score)
        {
            this.indexes = indexes;
            this.score = score;
        }
    }

    private int CountCrossingsWithExistingPairs(List<Vector2Int> pairs, List<CrystalInteract> stones, Vector2Int newPair)
    {
        int crossings = 0;

        foreach (Vector2Int pair in pairs)
        {
            if (SharesEndpoint(pair, newPair))
                continue;

            if (SegmentsCrossXZ(
                stones[pair.x].LineAnchorPosition,
                stones[pair.y].LineAnchorPosition,
                stones[newPair.x].LineAnchorPosition,
                stones[newPair.y].LineAnchorPosition))
            {
                crossings++;
            }
        }

        return crossings;
    }

    private bool PassesTooCloseToUnrelatedStone(List<CrystalInteract> stones, Vector2Int pair)
    {
        float minimumDistance = Mathf.Max(0f, minDistanceFromUnrelatedStone);
        if (minimumDistance <= 0f)
            return false;

        Vector2 start = ToXZ(stones[pair.x].LineAnchorPosition);
        Vector2 end = ToXZ(stones[pair.y].LineAnchorPosition);

        for (int i = 0; i < stones.Count; i++)
        {
            if (i == pair.x || i == pair.y)
                continue;

            Vector2 point = ToXZ(stones[i].LineAnchorPosition);
            if (DistancePointToSegment(point, start, end) < minimumDistance)
                return true;
        }

        return false;
    }

    private Vector2 ToXZ(Vector3 position)
    {
        return new Vector2(position.x, position.z);
    }

    private float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;

        if (lengthSquared <= Mathf.Epsilon)
            return Vector2.Distance(point, start);

        float t = Vector2.Dot(point - start, segment) / lengthSquared;
        t = Mathf.Clamp01(t);
        return Vector2.Distance(point, start + segment * t);
    }

    private bool SharesEndpoint(Vector2Int a, Vector2Int b)
    {
        return a.x == b.x || a.x == b.y || a.y == b.x || a.y == b.y;
    }

    private bool SegmentsCrossXZ(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Vector2 a2 = new Vector2(a.x, a.z);
        Vector2 b2 = new Vector2(b.x, b.z);
        Vector2 c2 = new Vector2(c.x, c.z);
        Vector2 dPoint = new Vector2(d.x, d.z);

        float direction1 = Direction(a2, b2, c2);
        float direction2 = Direction(a2, b2, dPoint);
        float direction3 = Direction(c2, dPoint, a2);
        float direction4 = Direction(c2, dPoint, b2);

        return ((direction1 > 0f && direction2 < 0f) || (direction1 < 0f && direction2 > 0f)) &&
               ((direction3 > 0f && direction4 < 0f) || (direction3 < 0f && direction4 > 0f));
    }

    private float Direction(Vector2 a, Vector2 b, Vector2 c)
    {
        return (c.x - a.x) * (b.y - a.y) - (b.x - a.x) * (c.y - a.y);
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private string GetPairSignature(List<Vector2Int> pairs)
    {
        List<string> parts = new List<string>();
        foreach (Vector2Int pair in pairs)
            parts.Add(pair.x + "-" + pair.y);

        parts.Sort();
        return string.Join(",", parts);
    }

    private void ClearGeneratedConnections()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}

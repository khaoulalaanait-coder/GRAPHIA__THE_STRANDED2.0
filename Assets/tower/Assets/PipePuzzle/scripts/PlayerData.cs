using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData instance;
    public static bool puzzleSolved = false;
    public static bool hasFuel = false;
    public static int boatPiecesCollected = 0;
    public static int journalUnlockedPages = 1;

    private static readonly HashSet<string> collectedBoatPieceIds = new HashSet<string>();

    public static bool HasAllBoatPieces
    {
        get { return boatPiecesCollected >= 4; }
    }

    public static int BoatPiecesCollectedCount
    {
        get { return collectedBoatPieceIds.Count; }
    }

    public static bool IsBoatPieceCollected(string pieceId)
    {
        return !string.IsNullOrWhiteSpace(pieceId) && collectedBoatPieceIds.Contains(pieceId);
    }

    public static bool MarkBoatPieceCollected(string pieceId)
    {
        if (string.IsNullOrWhiteSpace(pieceId))
            return false;

        if (!collectedBoatPieceIds.Add(pieceId))
            return false;

        boatPiecesCollected = Mathf.Clamp(collectedBoatPieceIds.Count, 0, 4);
        return true;
    }

    public static void UnlockJournalPage(int pageNumber)
    {
        journalUnlockedPages = Mathf.Clamp(Mathf.Max(journalUnlockedPages, pageNumber), 1, 7);
    }

    public static void ResetGameProgress()
    {
        puzzleSolved = false;
        hasFuel = false;
        boatPiecesCollected = 0;
        journalUnlockedPages = 1;
        collectedBoatPieceIds.Clear();
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}

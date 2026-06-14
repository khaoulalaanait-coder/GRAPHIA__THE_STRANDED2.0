using UnityEngine;

public class ProgressionGate : MonoBehaviour
{
    [SerializeField] private int requiredJournalPage = 2;
    [SerializeField] private int requiredBoatPieces = -1;

    private void Update()
    {
        if (ShouldUnlock())
            gameObject.SetActive(false);
    }

    private bool ShouldUnlock()
    {
        if ((gameObject.name == "BlockRuins" || gameObject.name == "BlockRuins(1)") &&
            PlayerData.BoatPiecesCollectedCount >= 2)
            return true;

        if (gameObject.name == "BlockCliff" && PlayerData.BoatPiecesCollectedCount >= 1)
            return true;

        if (gameObject.name == "BlockStones" && PlayerData.BoatPiecesCollectedCount >= 3)
            return true;

        if (PlayerData.journalUnlockedPages >= requiredJournalPage)
            return true;

        if (requiredBoatPieces >= 0)
            return PlayerData.BoatPiecesCollectedCount >= requiredBoatPieces;

        if (requiredJournalPage <= 2)
            return false;

        int inferredBoatPieces = requiredJournalPage - 2;
        return PlayerData.BoatPiecesCollectedCount >= inferredBoatPieces;
    }
}

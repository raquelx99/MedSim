using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int score;
    public int correctPoints = +1;
    public int wrongPoints = -1;

    public void Add(bool correct)
    {
        score += (correct ? correctPoints : wrongPoints);
    }
}

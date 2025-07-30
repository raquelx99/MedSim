using UnityEngine;

[CreateAssetMenu(menuName = "VRClinic/ExamType")]
public class ExamTypeSO : ScriptableObject
{
    public string examID;
    public string examName;

    public bool isImaging;
    public string defaultNormalResultText;
    public string defaultAbnormalResultText;

    public Sprite normalImageResult;
    public Sprite abnormalImageResult;
}

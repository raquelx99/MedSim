using UnityEngine;

[CreateAssetMenu(menuName = "VRClinic/ExamType")]
public class ExamTypeSO : ScriptableObject
{
    public string examID;
    public string examName;

    public bool isImaging;
    public Sprite[] defaultNormalResult;
    public Sprite[] defaultAbnormalResult;

    public Sprite normalImageResult;
    public Sprite abnormalImageResult;
}

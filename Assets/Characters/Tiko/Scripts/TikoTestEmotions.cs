using UnityEngine;

public class TikoTestEmotions : MonoBehaviour
{
    [SerializeField] private TikoFaceController face;

    [Header("Trage fiecare emotie aici")]
    [SerializeField] private RobotEmotion neutral;
    [SerializeField] private RobotEmotion curious;
    [SerializeField] private RobotEmotion scared;
    [SerializeField] private RobotEmotion happy;
    [SerializeField] private RobotEmotion sad;
    [SerializeField] private RobotEmotion angry;
    [SerializeField] private RobotEmotion surprised;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) face.SetEmotion(neutral);
        if (Input.GetKeyDown(KeyCode.Alpha2)) face.SetEmotion(curious);
        if (Input.GetKeyDown(KeyCode.Alpha3)) face.SetEmotion(scared);
        if (Input.GetKeyDown(KeyCode.Alpha4)) face.SetEmotion(happy);
        if (Input.GetKeyDown(KeyCode.Alpha5)) face.SetEmotion(sad);
        if (Input.GetKeyDown(KeyCode.Alpha6)) face.SetEmotion(angry);
        if (Input.GetKeyDown(KeyCode.Alpha7)) face.SetEmotion(surprised);
    }
}
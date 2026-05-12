#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CameraZone))]
public class CameraZonePreview : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CameraZone zone = (CameraZone)target;

        if (zone.settings == null)
        {
            EditorGUILayout.HelpBox(
                "Assignează un CameraZoneSettings preset!",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Preview Info ──", EditorStyles.boldLabel);

        CameraZoneSettings s = zone.settings;

        EditorGUILayout.LabelField("FOV",             $"{s.fieldOfView}°");
        EditorGUILayout.LabelField("Smooth Speed",    $"{s.smoothSpeed}");
        EditorGUILayout.LabelField("Offset",          s.offset.ToString());
        EditorGUILayout.LabelField("Camera Rotation", s.cameraRotation.ToString());
        EditorGUILayout.LabelField("Bounds X",
            $"{s.boundsExtentX.x}  →  {s.boundsExtentX.y}  (relativ la zona)");
        EditorGUILayout.LabelField("Bounds Y",
            $"{s.boundsExtentY.x}  →  {s.boundsExtentY.y}  (relativ la zona)");
        EditorGUILayout.LabelField("Look Ahead",      $"{s.lookAheadDistance}");
        EditorGUILayout.LabelField("Dead Zone X/Y",   $"{s.deadZoneX} / {s.deadZoneY}");

        EditorGUILayout.Space(4);
        GUI.color = s.lockX ? Color.red : Color.green;
        EditorGUILayout.LabelField("Lock X", s.lockX ? "🔒 Blocat" : "✅ Liber");
        GUI.color = s.lockY ? Color.red : Color.green;
        EditorGUILayout.LabelField("Lock Y", s.lockY ? "🔒 Blocat" : "✅ Liber");
        GUI.color = s.lockZ ? Color.red : Color.green;
        EditorGUILayout.LabelField("Lock Z", s.lockZ ? "🔒 Blocat" : "✅ Liber");
        GUI.color = Color.white;

        EditorGUILayout.Space(8);

        if (GUILayout.Button("📷 Snap Scene View la aceasta Camera", GUILayout.Height(35)))
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.LookAt(
                    zone.transform.position,
                    Quaternion.Euler(s.cameraRotation),
                    Mathf.Abs(s.offset.z)
                );
                sv.orthographic = false;
                sv.Repaint();
            }
        }

        if (GUILayout.Button("⚙️ Deschide Settings Asset", GUILayout.Height(28)))
        {
            Selection.activeObject = zone.settings;
            EditorGUIUtility.PingObject(zone.settings);
        }
    }
}
#endif
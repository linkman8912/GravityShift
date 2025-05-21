using UnityEngine;
using UnityEditor;

public class GrappleLevelEditorWindow : EditorWindow
{
    private GrappleLevelManager manager;
    private SerializedObject so;
    private SerializedProperty pairsProp;
    private SerializedProperty centerLayerProp;
    private SerializedProperty lineMatProp;

    [MenuItem("Tools/Grapple Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<GrappleLevelEditorWindow>("Grapple Level Editor");
    }

    private void OnEnable()
    {
        // Find or create the manager in the scene
        manager = FindObjectOfType<GrappleLevelManager>();
        if (manager == null)
        {
            var go = new GameObject("GrappleLevelManager");
            manager = go.AddComponent<GrappleLevelManager>();
        }

        so = new SerializedObject(manager);
        pairsProp = so.FindProperty("pairs");
        centerLayerProp = so.FindProperty("centerGrappleLayer");
        lineMatProp = so.FindProperty("lineMaterial");
    }

    private void OnGUI()
    {
        so.Update();

        EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(centerLayerProp, new GUIContent("Center Grapple Layer"));
        EditorGUILayout.PropertyField(lineMatProp, new GUIContent("Line Material"));
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Pre-configured Grapple Pairs", EditorStyles.boldLabel);

        // List all pairs
        for (int i = 0; i < pairsProp.arraySize; i++)
        {
            var elem = pairsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("objectA"), new GUIContent("Object A"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("objectB"), new GUIContent("Object B"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("springForce"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("damper"));
            if (GUILayout.Button("Remove This Pair"))
            {
                pairsProp.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add New Pair"))
        {
            pairsProp.InsertArrayElementAtIndex(pairsProp.arraySize);
            var newElem = pairsProp.GetArrayElementAtIndex(pairsProp.arraySize - 1);
            newElem.FindPropertyRelative("objectA").objectReferenceValue = null;
            newElem.FindPropertyRelative("objectB").objectReferenceValue = null;
            newElem.FindPropertyRelative("springForce").floatValue = manager.pairs.Count > 0
                ? manager.pairs[0].springForce
                : 100f;
            newElem.FindPropertyRelative("damper").floatValue = manager.pairs.Count > 0
                ? manager.pairs[0].damper
                : 10f;
        }

        if (so.ApplyModifiedProperties())
        {
            // Mark scene dirty so changes will save
            EditorUtility.SetDirty(manager);
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }
}

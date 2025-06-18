using UnityEngine;
using UnityEditor;

// This attribute tells Unity that this script is a custom editor for the NixieNavigation component.
[CustomEditor(typeof(NixieNavigation))]
public class NixieNavigationEditor : Editor
{
    // OnSceneGUI is the magic method that lets us draw custom handles and controls in the Scene View.
    protected virtual void OnSceneGUI()
    {
        // Get a reference to the component we are inspecting.
        NixieNavigation nixieNav = (NixieNavigation)target;

        // If the patrol node list is null or empty, there's nothing to draw.
        if (nixieNav.PatrolNodes == null || nixieNav.PatrolNodes.Count == 0)
        {
            return;
        }

        // Set the color for the handles.
        Handles.color = Color.green;

        // Loop through all the patrol nodes in the list.
        for (int i = 0; i < nixieNav.PatrolNodes.Count; i++)
        {
            Transform node = nixieNav.PatrolNodes[i];

            // If a slot in the list is empty, skip it.
            if (node == null)
            {
                continue;
            }

            // This is the core logic.
            // We check for any changes the user makes with the handle.
            EditorGUI.BeginChangeCheck();

            // Draw the position handle (the standard move gizmo) at the node's current position.
            Vector3 newPosition = Handles.PositionHandle(node.position, Quaternion.identity);

            // If the user dragged the handle (a change was detected)...
            if (EditorGUI.EndChangeCheck())
            {
                // Record the change for Undo/Redo functionality.
                Undo.RecordObject(node, "Move Nixie Patrol Node");

                // Apply the new position to the node's transform.
                node.position = newPosition;
            }
        }
    }

    // BONUS: This part ensures your default inspector still shows up.
    // Without this, the inspector for NixieNavigation would be blank.
    public override void OnInspectorGUI()
    {
        // "serializedObject" is the modern way to handle inspector fields,
        // it automatically handles Undo and marking the scene as dirty.
        serializedObject.Update();

        // Draw all the fields from NixieNavigation automatically, except the PatrolNodes list.
        DrawPropertiesExcluding(serializedObject, "PatrolNodes");

        // Draw the PatrolNodes list field specifically. This ensures it behaves correctly.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PatrolNodes"), true);

        // Apply any changes made in the inspector.
        serializedObject.ApplyModifiedProperties();
    }
}
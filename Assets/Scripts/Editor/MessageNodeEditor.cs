using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MessageNode))]
public class MessageNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MessageNode node = (MessageNode)target;

        EditorGUILayout.LabelField("Path", node.PathId);
        EditorGUILayout.LabelField("Message so far", string.IsNullOrEmpty(node.SentText) ? "(empty)" : node.SentText);
        EditorGUILayout.Space();

        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(node.IsRoot))
        {
            if (GUILayout.Button("Select Parent") && node.Parent != null)
            {
                Selection.activeObject = node.Parent;
            }
        }

        if (node.IsComplete)
            return;

        EditorGUILayout.LabelField("Next Choices", EditorStyles.boldLabel);
        for (int i = 0; i < MessageNode.ChoiceCount; i++)
        {
            MessageNode child = node.GetChoice(i);
            if (child == null)
            {
                EditorGUILayout.LabelField($"{i + 1}.", "missing - run Generate or Repair Tree");
                continue;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                string word = EditorGUILayout.TextField($"{i + 1}.", child.Word);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(child, "Edit Choice Word");
                    child.EditorSetWord(word);
                    EditorUtility.SetDirty(child);
                }

                if (GUILayout.Button("Open", GUILayout.Width(50f)))
                {
                    Selection.activeObject = child;
                }
            }
        }
    }
}

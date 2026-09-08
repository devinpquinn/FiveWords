using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// One reachable state of the player's message: the words chosen so far, the three words that can
/// come next, and the reply received if the message is sent from here.
/// </summary>
[CreateAssetMenu(fileName = "MessageNode", menuName = "Five Words/Message Node")]
public class MessageNode : ScriptableObject
{
    public const int ChoiceCount = 3;
    public const int MaxWords = 5;

    [Tooltip("The word this choice adds, written as it appears mid-message (lower case unless it's a proper noun).")]
    [SerializeField] private string word;

    [SerializeField] private MessageNode parent;
    [SerializeField] private MessageNode[] choices = new MessageNode[ChoiceCount];

    [Tooltip("The incoming messages received after sending this message, one bubble per entry.")]
    [TextArea(1, 4)]
    [SerializeField] private string[] response = Array.Empty<string>();

    public string Word => word;
    public MessageNode Parent => parent;
    public IReadOnlyList<string> Response => response;
    public bool IsRoot => parent == null;

    public MessageNode GetChoice(int index)
    {
        return index >= 0 && index < choices.Length ? choices[index] : null;
    }

    public int Depth
    {
        get
        {
            int depth = 0;
            for (MessageNode node = parent; node != null; node = node.parent)
            {
                depth++;
            }
            return depth;
        }
    }

    public bool IsComplete => Depth >= MaxWords;

    /// <summary>Word as shown on a choice button; only the message's first word is capitalized.</summary>
    public string DisplayWord => Depth == 1 ? Capitalize(word) : word;

    /// <summary>The message as composed so far, walking back up to the root.</summary>
    public string MessageText
    {
        get
        {
            List<string> words = new List<string>();
            for (MessageNode node = this; node != null && !node.IsRoot; node = node.parent)
            {
                words.Insert(0, node.word);
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(i == 0 ? Capitalize(words[i]) : words[i]);
            }
            return builder.ToString();
        }
    }

    /// <summary>Stable identifier such as "2-1-3", describing the choice indices taken from the root.</summary>
    public string PathId
    {
        get
        {
            List<string> parts = new List<string>();
            for (MessageNode node = this; node.parent != null; node = node.parent)
            {
                parts.Insert(0, (Array.IndexOf(node.parent.choices, node) + 1).ToString());
            }
            return parts.Count == 0 ? "root" : string.Join("-", parts);
        }
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

#if UNITY_EDITOR
    public void EditorSetParent(MessageNode value) => parent = value;

    public void EditorSetChoice(int index, MessageNode value) => choices[index] = value;

    public void EditorClearChoices() => choices = new MessageNode[ChoiceCount];

    public void EditorSetWord(string value) => word = value;

    public void EditorSetResponse(string[] value) => response = value ?? Array.Empty<string>();
#endif
}

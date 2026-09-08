using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates, validates and bulk-edits the full five-word message tree.
/// </summary>
public static class MessageTreeTools
{
    private const string TreeFolder = "Assets/MessageTree";
    private const string RootAssetPath = TreeFolder + "/Root.asset";
    private const string ResponseSeparator = "|";

    [MenuItem("Five Words/Message Tree/Generate or Repair Tree")]
    public static void GenerateOrRepair()
    {
        EnsureFolder(TreeFolder);
        for (int depth = 1; depth <= MessageNode.MaxWords; depth++)
        {
            EnsureFolder($"{TreeFolder}/Depth{depth}");
        }

        MessageNode root = AssetDatabase.LoadAssetAtPath<MessageNode>(RootAssetPath);
        if (root == null)
        {
            root = ScriptableObject.CreateInstance<MessageNode>();
            AssetDatabase.CreateAsset(root, RootAssetPath);
        }
        root.EditorSetParent(null);

        int created = 0;
        Build(root, ref created);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Message tree ready: {CountNodes(root) - 1} message nodes ({created} newly created). Root: {RootAssetPath}", root);
    }

    [MenuItem("Five Words/Message Tree/Validate")]
    public static void Validate()
    {
        MessageNode root = LoadRootOrWarn();
        if (root == null)
            return;

        List<string> missingWords = new List<string>();
        List<string> missingResponses = new List<string>();
        List<string> duplicateSiblings = new List<string>();

        foreach (MessageNode node in Walk(root))
        {
            if (!node.IsRoot && string.IsNullOrWhiteSpace(node.Word))
            {
                missingWords.Add(node.PathId);
            }
            if (!node.IsRoot && node.Response.Count == 0)
            {
                missingResponses.Add($"{node.PathId} ({node.MessageText})");
            }

            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < MessageNode.ChoiceCount; i++)
            {
                MessageNode child = node.GetChoice(i);
                if (child == null || string.IsNullOrWhiteSpace(child.Word))
                    continue;
                if (!seen.Add(child.Word.Trim().ToLowerInvariant()))
                {
                    duplicateSiblings.Add($"{node.PathId} -> '{child.Word}'");
                }
            }
        }

        Debug.Log(
            $"Message tree validation:\n" +
            $"  nodes: {CountNodes(root) - 1}\n" +
            $"  missing words: {missingWords.Count}{Preview(missingWords)}\n" +
            $"  missing responses: {missingResponses.Count}{Preview(missingResponses)}\n" +
            $"  duplicate sibling words: {duplicateSiblings.Count}{Preview(duplicateSiblings)}",
            root);
    }

    [MenuItem("Five Words/Message Tree/Export CSV...")]
    public static void ExportCsv()
    {
        MessageNode root = LoadRootOrWarn();
        if (root == null)
            return;

        string path = EditorUtility.SaveFilePanel("Export message tree", "", "MessageTree.csv", "csv");
        if (string.IsNullOrEmpty(path))
            return;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("path,depth,word,message,punctuation,response");
        foreach (MessageNode node in Walk(root))
        {
            if (node.IsRoot)
                continue;

            builder.AppendLine(string.Join(",", new[]
            {
                Escape(node.PathId),
                node.Depth.ToString(),
                Escape(node.Word),
                Escape(node.MessageText),
                Escape(node.EditorEndPunctuation),
                Escape(string.Join(ResponseSeparator, node.Response))
            }));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        Debug.Log($"Exported message tree to {path}");
    }

    [MenuItem("Five Words/Message Tree/Import CSV...")]
    public static void ImportCsv()
    {
        MessageNode root = LoadRootOrWarn();
        if (root == null)
            return;

        string path = EditorUtility.OpenFilePanel("Import message tree", "", "csv");
        if (string.IsNullOrEmpty(path))
            return;

        Dictionary<string, MessageNode> byPath = new Dictionary<string, MessageNode>();
        foreach (MessageNode node in Walk(root))
        {
            byPath[node.PathId] = node;
        }

        int updated = 0;
        int unmatched = 0;
        string[] lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            List<string> fields = ParseCsvLine(lines[i]);
            if (fields.Count < 3)
                continue;

            if (!byPath.TryGetValue(fields[0], out MessageNode node))
            {
                unmatched++;
                continue;
            }

            Undo.RecordObject(node, "Import Message Tree");
            node.EditorSetWord(fields[2]);
            if (fields.Count > 4 && !string.IsNullOrEmpty(fields[4]))
            {
                node.EditorSetEndPunctuation(fields[4]);
            }
            if (fields.Count > 5)
            {
                node.EditorSetResponse(string.IsNullOrEmpty(fields[5])
                    ? new string[0]
                    : fields[5].Split(new[] { ResponseSeparator }, System.StringSplitOptions.None));
            }
            EditorUtility.SetDirty(node);
            updated++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Imported message tree: {updated} nodes updated, {unmatched} rows with unknown paths.");
    }

    private static void Build(MessageNode node, ref int created)
    {
        if (node.Depth >= MessageNode.MaxWords)
        {
            node.EditorClearChoices();
            EditorUtility.SetDirty(node);
            return;
        }

        string folder = $"{TreeFolder}/Depth{node.Depth + 1}";
        for (int i = 0; i < MessageNode.ChoiceCount; i++)
        {
            MessageNode child = node.GetChoice(i);
            if (child == null)
            {
                child = ScriptableObject.CreateInstance<MessageNode>();
                child.EditorSetParent(node);
                string childPath = node.IsRoot ? (i + 1).ToString() : $"{node.PathId}-{i + 1}";
                AssetDatabase.CreateAsset(child, $"{folder}/N_{childPath}.asset");
                node.EditorSetChoice(i, child);
                EditorUtility.SetDirty(node);
                created++;
            }
            else
            {
                child.EditorSetParent(node);
            }

            EditorUtility.SetDirty(child);
            Build(child, ref created);
        }
    }

    private static IEnumerable<MessageNode> Walk(MessageNode node)
    {
        yield return node;
        for (int i = 0; i < MessageNode.ChoiceCount; i++)
        {
            MessageNode child = node.GetChoice(i);
            if (child == null)
                continue;
            foreach (MessageNode descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }

    private static int CountNodes(MessageNode root)
    {
        int count = 0;
        foreach (MessageNode _ in Walk(root))
        {
            count++;
        }
        return count;
    }

    private static MessageNode LoadRootOrWarn()
    {
        MessageNode root = AssetDatabase.LoadAssetAtPath<MessageNode>(RootAssetPath);
        if (root == null)
        {
            Debug.LogWarning($"No message tree found at {RootAssetPath}. Run Five Words/Message Tree/Generate or Repair Tree first.");
        }
        return root;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    private static string Preview(List<string> items)
    {
        if (items.Count == 0)
            return "";
        int shown = Mathf.Min(items.Count, 10);
        return " -> " + string.Join(", ", items.GetRange(0, shown)) + (items.Count > shown ? ", ..." : "");
    }

    private static string Escape(string value)
    {
        value ??= "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder field = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());
        return fields;
    }
}

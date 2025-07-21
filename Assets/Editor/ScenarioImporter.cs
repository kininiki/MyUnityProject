using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using Fungus;
using System.Globalization;
using UnityEngine.UI;

// Editor utility to import a scenario text file exported from the VBA macro.
// Each line in the file should have the format:
// NameKey:Position:Emotion:Command:LocalizationKey:Russian:English:Params
// "Params" is an optional list of key=value pairs separated by semicolons.
// Example: duration=3;dialogueBoxName=Main
// A new Flowchart with blocks containing the corresponding commands will be created.
// Use the menu item Tools/Scenario/Import From File to run the importer.
public class ScenarioImporter : EditorWindow
{
    private struct PreCommand
    {
        public System.Type CommandType;
        public System.Collections.Generic.Dictionary<string, string> Parameters;
        public PreCommand(System.Type t, System.Collections.Generic.Dictionary<string, string> p)
        {
            CommandType = t;
            Parameters = p;
        }
    }
    [MenuItem("Tools/Scenario/Import From File")]
    public static void ImportScenario()
    {
        string path = EditorUtility.OpenFilePanel("Import Scenario", Application.dataPath, "txt");
        if (string.IsNullOrEmpty(path))
            return;

        var lines = File.ReadAllLines(path);
        GameObject flowchartObject = new GameObject("ImportedFlowchart");
        Undo.RegisterCreatedObjectUndo(flowchartObject, "Create Flowchart");
        Flowchart flowchart = flowchartObject.AddComponent<Flowchart>();

        float blockSpacing = 300f;
        int blockIndex = 0;
        Vector2 position = Vector2.zero;
        Block block = flowchart.CreateBlock(position);
        block.BlockName = "ImportedBlock";

        int lineIndex = 0;
        bool lanaActive = false;
        bool secondarySpriteActive = false;
        bool secondaryDialogSpoken = false;
        bool prevDialogueBox4 = false;
        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;
            string line = rawLine.Trim();
            string[] parts = line.Split(':');
            if (parts.Length < 7)
            {
                Debug.LogWarning(string.Format("Invalid line {0}: {1}", lineIndex + 1, line));
                lineIndex++;
                continue;
            }
            string characterKey = parts[0];
            string pos = parts[1];
            string emotion = parts[2];
            string commandName = parts[3];
            string locKey = parts[4];
            string ruText = parts[5];
            string enText = parts[6];
            string paramString = parts.Length > 7 ? parts[7] : string.Empty;

            // Handle block separator lines
            if (string.Equals(commandName, "Блок", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(commandName, "Block", System.StringComparison.OrdinalIgnoreCase))
            {
                blockIndex++;
                position = new Vector2(blockSpacing * blockIndex, 0f);
                block = flowchart.CreateBlock(position);
                block.BlockName = string.IsNullOrEmpty(paramString) ? "Block" + blockIndex : paramString.Trim();
                continue;
            }


            // Collect parameters in a dictionary so we can customize defaults
            var paramDict = new System.Collections.Generic.Dictionary<string, string>();
            if (!string.IsNullOrEmpty(paramString))
            {
                string[] paramPairs = paramString.Split(';');
                foreach (var pair in paramPairs)
                {
                    if (string.IsNullOrWhiteSpace(pair))
                        continue;
                    string[] kv = pair.Split('=');
                    if (kv.Length != 2)
                        continue;
                    string fieldName = kv[0].Trim();
                    string valueStr = kv[1].Trim();
                    if (fieldName == "Строки")
                        fieldName = "generalTextStyleIndex";
                    paramDict[fieldName] = valueStr;
                }
            }
            // Preserve keys for localization and character so helpers can access them
            paramDict["localizationKey"] = locKey;
            paramDict["characterKey"] = characterKey;


            System.Type commandType = GetCommandType(commandName);
            if (commandType == null)
            {
                Debug.LogWarning(string.Format("Unknown command '{0}' on line {1}", commandName, lineIndex + 1));
                lineIndex++;
                continue;
            }
            bool isWarning = false;
            string trimmedParam = paramString.Trim();
            if (commandType == typeof(ShowDialogueBoxCommand) &&
                (string.Equals(trimmedParam, "Большое", System.StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(trimmedParam, "Маленькое", System.StringComparison.OrdinalIgnoreCase)))
            {
                isWarning = true;
            }

        // Determine if a camera move command should be inserted before this command
            var preCommands = new System.Collections.Generic.List<PreCommand>();
            if (commandType == typeof(SetCharacterSpriteCommand))
            {
                var dict = new System.Collections.Generic.Dictionary<string, string>();
                dict["positionNumber"] = "3";
                preCommands.Add(new PreCommand(GetCommandType("CameraMove.MoveCameraCommand"), dict));
                // emotion column is mapped automatically and positionIndex forced to 2
                if (!string.IsNullOrEmpty(emotion))
                    paramDict["emotion"] = emotion;
                paramDict["positionIndex"] = "2";
            }
            else if (commandType == typeof(SetCharacterSpriteCommand2))
            {
                var dict = new System.Collections.Generic.Dictionary<string, string>();
                dict["positionNumber"] = "5";
                preCommands.Add(new PreCommand(GetCommandType("CameraMove.MoveCameraCommand"), dict));
                paramDict["characterName"] = characterKey;
                if (!string.IsNullOrEmpty(emotion))
                    paramDict["emotionKey"] = emotion;
                paramDict["positionIndex"] = "5";
                paramDict["baseSpriteIndex"] = "0";
                paramDict["dressKey"] = "0";
                secondarySpriteActive = true;
                secondaryDialogSpoken = false;
            }
            else if (commandType == typeof(ShowDialogueBoxCommand) && characterKey == "Empty")
            {
                var dict = new System.Collections.Generic.Dictionary<string, string>();
                dict["positionNumber"] = "4";
                preCommands.Add(new PreCommand(GetCommandType("CameraMove.MoveCameraCommand"), dict));
            }
            else if (isWarning && !prevDialogueBox4)
            {
                var dict = new System.Collections.Generic.Dictionary<string, string>();
                dict["positionNumber"] = "4";
                preCommands.Add(new PreCommand(GetCommandType("CameraMove.MoveCameraCommand"), dict));
            }



            // Additional defaults for ShowDialogueBoxCommand
            if (commandType == typeof(ShowDialogueBoxCommand))
            {
                if (!paramDict.ContainsKey("dialogueBoxName"))
                {
                    if (characterKey == "Lana")
                        paramDict["dialogueBoxName"] = "31";
                    else if (characterKey == "Empty")
                        paramDict["dialogueBoxName"] = "4";
                    else
                        paramDict["dialogueBoxName"] = "52";
                }
                if (!paramDict.ContainsKey("nameTextStyleIndex"))
                    paramDict["nameTextStyleIndex"] = "0";
                if (!paramDict.ContainsKey("hideOnTap"))
                    paramDict["hideOnTap"] = "true";

                if (isWarning)
                {
                    if (string.Equals(trimmedParam, "Большое", System.StringComparison.OrdinalIgnoreCase))
                        paramDict["dialogueBoxName"] = "441";
                    else if (string.Equals(trimmedParam, "Маленькое", System.StringComparison.OrdinalIgnoreCase))
                        paramDict["dialogueBoxName"] = "442";
                    if (!paramDict.ContainsKey("generalTextStyleIndex"))
                        paramDict["generalTextStyleIndex"] = "0";
                    if (!paramDict.ContainsKey("nameTextStyleIndex"))
                        paramDict["nameTextStyleIndex"] = "0";
                    paramDict["characterKey"] = "Continue";
                }


                if (secondarySpriteActive)
                    secondaryDialogSpoken = true;
            };
            // Defaults for NotificationCommand
            if (commandType == typeof(NotificationCommand))
            {
                if (!paramDict.ContainsKey("generalTextStyleIndex"))
                    paramDict["generalTextStyleIndex"] = "0";
                if (!paramDict.ContainsKey("nameTextStyleIndex"))
                    paramDict["nameTextStyleIndex"] = "0";
                if (!paramDict.ContainsKey("duration"))
                    paramDict["duration"] = "5";

                // Name text should use the "Empty" localization key
                paramDict["characterKey"] = "Empty";

                if (!paramDict.ContainsKey("dialogueBoxName"))
                {
                    if (string.Equals(pos, "Лево", System.StringComparison.OrdinalIgnoreCase))
                        paramDict["dialogueBoxName"] = "33";
                    else if (string.Equals(pos, "Центр", System.StringComparison.OrdinalIgnoreCase))
                        paramDict["dialogueBoxName"] = "43";
                }
            }

            // Defaults for background commands
            if (commandType == typeof(SetBackgroundCommand))
            {
                string size = paramString.Trim();
                if (string.Equals(size, "Большой", System.StringComparison.OrdinalIgnoreCase))
                    paramDict["targetImage"] = "BG_big";
                else if (string.Equals(size, "Маленький", System.StringComparison.OrdinalIgnoreCase))
                    paramDict["targetImage"] = "BG_small";
                if (!paramDict.ContainsKey("fadeDuration"))
                    paramDict["fadeDuration"] = "1";
                if (!paramDict.ContainsKey("useFade"))
                    paramDict["useFade"] = "true";
            }
            else if (commandType == typeof(FadeOutBackgroundCommand))
            {
                string size = paramString.Trim();
                if (string.Equals(size, "Большой", System.StringComparison.OrdinalIgnoreCase))
                    paramDict["targetImage"] = "BG_big";
                else if (string.Equals(size, "Маленький", System.StringComparison.OrdinalIgnoreCase))
                    paramDict["targetImage"] = "BG_small";
                if (!paramDict.ContainsKey("fadeDuration"))
                    paramDict["fadeDuration"] = "1";
            }
            else if (commandType == typeof(PlayMusicCommand))
            {
                string keyWord = paramString.Trim();
                if (!paramDict.ContainsKey("volume"))
                    paramDict["volume"] = "1";
                if (!paramDict.ContainsKey("fadeInDuration"))
                    paramDict["fadeInDuration"] = "2";
                if (string.Equals(keyWord, "Фоновая", System.StringComparison.OrdinalIgnoreCase))
                {
                    paramDict["audioSource"] = "Audio Source";
                    if (!paramDict.ContainsKey("loop"))
                        paramDict["loop"] = "true";
                }
                else if (string.Equals(keyWord, "Звук", System.StringComparison.OrdinalIgnoreCase))
                {
                    paramDict["audioSource"] = "Audio Source Sound";
                    if (!paramDict.ContainsKey("loop"))
                        paramDict["loop"] = "false";
                }
            }
            else if (commandType == typeof(StopMusicCommand))
            {
                string keyWord = paramString.Trim();
                if (!paramDict.ContainsKey("fadeOutDuration"))
                    paramDict["fadeOutDuration"] = "2";
                if (string.Equals(keyWord, "Фоновая", System.StringComparison.OrdinalIgnoreCase))
                {
                    paramDict["audioSource"] = "Audio Source";
                }
                else if (string.Equals(keyWord, "Звук", System.StringComparison.OrdinalIgnoreCase))
                {
                    paramDict["audioSource"] = "Audio Source Sound";
                }
            }



            // Insert a fade-out before automatic camera move if Lana is still active
            if (lanaActive)
            {
                System.Type camType = GetCommandType("CameraMove.MoveCameraCommand");
                for (int i = 0; i < preCommands.Count; i++)
                {
                    if (preCommands[i].CommandType == camType)
                    {
                        var fadeDict = new System.Collections.Generic.Dictionary<string, string>();
                        fadeDict["positionIndex"] = "2";
                        fadeDict["fadeDuration"] = "0.03";
                        preCommands.Insert(i, new PreCommand(GetCommandType("FadeOutCharacterCommand"), fadeDict));
                        lanaActive = false;
                        break;
                    }
                }
            }
            // Insert FadeOutCharacterCommand2 when secondary dialogue ended
            if (secondaryDialogSpoken)
            {
                System.Type camType = GetCommandType("CameraMove.MoveCameraCommand");
                for (int i = 0; i < preCommands.Count; i++)
                {
                    if (preCommands[i].CommandType == camType)
                    {
                        var fadeDict = new System.Collections.Generic.Dictionary<string, string>();
                        fadeDict["positionIndex"] = "2";
                        fadeDict["duration"] = "0";
                        preCommands.Insert(i, new PreCommand(GetCommandType("FadeOutCharacterCommand2"), fadeDict));
                        secondaryDialogSpoken = false;
                        secondarySpriteActive = false;
                        break;
                    }
                }
            }

            // Create any pre-inserted commands
            foreach (var pre in preCommands)
            {
                if (pre.CommandType == null) continue;
                Command preCmd = CreateCommand(block, flowchart, pre.CommandType);
                if (preCmd != null)
                {
                    ApplyParameters(preCmd, pre.Parameters);
                }
            }

            Command command = CreateCommand(block, flowchart, commandType);
            if (command == null)
            {
                lineIndex++;
                continue;
            }

            // Apply parameters from the dictionary using reflection
            ApplyParameters(command, paramDict);

            if (commandType == typeof(ShowDialogueBoxCommand) && characterKey == "Lana")
            {
                lanaActive = true;
            }
            if (commandType == typeof(ShowDialogueBoxCommand) && secondarySpriteActive)
            {
                secondaryDialogSpoken = true;
            }

            if (commandType == typeof(ShowDialogueBoxCommand))
            {
                string dbName = "";
                if (paramDict.TryGetValue("dialogueBoxName", out string temp))
                    dbName = temp;
                prevDialogueBox4 = dbName == "4";
            }
            else
            {
                prevDialogueBox4 = false;
            }


            // Other command types can be extended here as needed
            lineIndex++;
        }
        EditorUtility.SetDirty(flowchartObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("Scenario imported. Created Flowchart: " + flowchartObject.name);
    }

    private static System.Type GetCommandType(string name)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType(name);
            if (type == null && name.Contains("."))
            {
                // Try again assuming the command is a nested class
                string nested = name.Replace('.', '+');
                type = asm.GetType(nested);
            }
            if (type != null && type.IsSubclassOf(typeof(Command)))
                return type;
        }
        return null;
    }

    private static Character FindCharacter(string key)
    {
        Character[] characters = Resources.FindObjectsOfTypeAll<Character>();
        foreach (var ch in characters)
        {
            if (string.Equals(ch.name, key, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ch.NameText, key, System.StringComparison.OrdinalIgnoreCase))
            {
                return ch;
            }
        }
        return null;
    }

    private static object ConvertToType(string value, System.Type type)
    {
        try
        {
            if (type == typeof(string))
                return value;
            if (type == typeof(int))
                return int.Parse(value);
            if (type == typeof(float))
                return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(bool))
                return bool.Parse(value);
            if (type.IsEnum)
                return System.Enum.Parse(type, value);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var objects = Resources.FindObjectsOfTypeAll(type);
                foreach (var obj in objects)
                {
                    if (obj != null && obj.name == value)
                        return obj;
                }
            }
        }
        catch
        {
            Debug.LogWarning(string.Format("Failed to convert '{0}' to {1}", value, type.Name));
        }
        return null;
    }

    private static void AssignIfNull(object obj, string fieldName, object value)
    {
        if (obj == null || value == null)
            return;
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var current = field.GetValue(obj);
            if (current == null)
                field.SetValue(obj, value);
        }
    }

    private static Command CreateCommand(Block block, Flowchart flowchart, System.Type commandType)
    {
        if (commandType == null)
            return null;
        Command command = Undo.AddComponent(block.gameObject, commandType) as Command;
        if (command == null)
        {
            Debug.LogWarning("Failed to add command component " + commandType.Name);
            return null;
        }

        command.ParentBlock = block;
        command.ItemId = flowchart.NextItemId();
        Undo.RecordObject(block, "Add Command");
        block.CommandList.Add(command);
        command.OnCommandAdded(block);
        PrefabUtility.RecordPrefabInstancePropertyModifications(block);
        return command;
    }

    private static void ApplyParameters(Command command, System.Collections.Generic.Dictionary<string, string> paramDict)
    {
        if (command == null || paramDict == null)
            return;

        System.Type commandType = command.GetType();
        foreach (var kvp in paramDict)
        {
            string fieldName = kvp.Key;
            string valueStr = kvp.Value;
            var field = commandType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                object val = ConvertToType(valueStr, field.FieldType);
                if (val != null)
                    field.SetValue(command, val);
                continue;
            }
            var prop = commandType.GetProperty(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                object val = ConvertToType(valueStr, prop.PropertyType);
                if (val != null)
                    prop.SetValue(command, val);
            }
        }

        // Assign localized text and default references for known commands
        ShowDialogueBoxCommand dlg = command as ShowDialogueBoxCommand;
        if (dlg != null)
        {
            dlg.generalText.TableReference = "DialogueStrings_Lana1";
            dlg.generalText.TableEntryReference = paramDict.ContainsKey("localizationKey") ? paramDict["localizationKey"] : dlg.generalText.TableEntryReference;
            dlg.nameText.TableReference = "DialogueStrings_Lana1";
            dlg.nameText.TableEntryReference = paramDict.ContainsKey("characterKey") ? paramDict["characterKey"] : dlg.nameText.TableEntryReference;
            AssignIfNull(dlg, "dialogueManager", Object.FindObjectOfType<DialogueManager>());
        }

        NotificationCommand notif = command as NotificationCommand;
        if (notif != null)
        {
            notif.generalText.TableReference = "DialogueStrings_Lana1";
            notif.generalText.TableEntryReference = paramDict.ContainsKey("localizationKey") ? paramDict["localizationKey"] : notif.generalText.TableEntryReference;
            notif.nameText.TableReference = "DialogueStrings_Lana1";
            notif.nameText.TableEntryReference = paramDict.ContainsKey("characterKey") ? paramDict["characterKey"] : notif.nameText.TableEntryReference;
            AssignIfNull(notif, "dialogueManager", Object.FindObjectOfType<DialogueManager>());
        }

        ForChoicesCommand choices = command as ForChoicesCommand;
        if (choices != null)
        {
            choices.generalText.TableReference = "DialogueStrings_Lana1";
            choices.generalText.TableEntryReference = paramDict.ContainsKey("localizationKey") ? paramDict["localizationKey"] : choices.generalText.TableEntryReference;
            choices.nameText.TableReference = "DialogueStrings_Lana1";
            choices.nameText.TableEntryReference = paramDict.ContainsKey("characterKey") ? paramDict["characterKey"] : choices.nameText.TableEntryReference;
            AssignIfNull(choices, "dialogueManager", Object.FindObjectOfType<DialogueManager>());
        }

        SetBackgroundCommand bg = command as SetBackgroundCommand;
        if (bg != null)
        {
            if (paramDict.TryGetValue("targetImage", out string bgName))
            {
                var images = Resources.FindObjectsOfTypeAll<Image>();
                foreach (var img in images)
                {
                    if (img.name == bgName)
                    {
                        var field = bg.GetType().GetField("targetImage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                            field.SetValue(bg, img);
                        break;
                    }
                }
            }
            else
            {
                AssignIfNull(bg, "targetImage", Object.FindObjectOfType<Image>());
            }
        }

        FadeOutBackgroundCommand fadeBg = command as FadeOutBackgroundCommand;
        if (fadeBg != null)
        {
            if (paramDict.TryGetValue("targetImage", out string fbName))
            {
                var images = Resources.FindObjectsOfTypeAll<Image>();
                foreach (var img in images)
                {
                    if (img.name == fbName)
                    {
                        var field = fadeBg.GetType().GetField("targetImage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                            field.SetValue(fadeBg, img);
                        break;
                    }
                }
            }
            else
            {
                AssignIfNull(fadeBg, "targetImage", Object.FindObjectOfType<Image>());
            }
        }

        SetVideoBackgroundCommand videoBg = command as SetVideoBackgroundCommand;
        if (videoBg != null)
        {
            AssignIfNull(videoBg, "targetImage", Object.FindObjectOfType<UnityEngine.UI.RawImage>());
            AssignIfNull(videoBg, "videoPlayer", Object.FindObjectOfType<UnityEngine.Video.VideoPlayer>());
        }

        PlayMusicCommand musicCmd = command as PlayMusicCommand;
        if (musicCmd != null)
        {
            if (musicCmd.audioSource == null)
                musicCmd.audioSource = Object.FindObjectOfType<UnityEngine.AudioSource>();
        }


        StopMusicCommand stopMusic = command as StopMusicCommand;
        if (stopMusic != null)
        {
            if (stopMusic.audioSource == null)
                stopMusic.audioSource = Object.FindObjectOfType<UnityEngine.AudioSource>();
        }
    }
}
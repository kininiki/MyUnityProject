using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class AutoBackup
{
    static AutoBackup()
    {
        EditorApplication.quitting += OnEditorQuitting;
        Debug.Log("AutoBackup initialized"); // Добавим для проверки
    }

    [MenuItem("Tools/Manual Fungus Backup")] // Добавим ручное управление
    static void ManualBackup()
    {
        BackupFungusCommands();
    }

    static void OnEditorQuitting()
    {
        BackupFungusCommands();
    }

    static void BackupFungusCommands()
    {
        Debug.Log("Starting backup..."); // Отладочное сообщение

        // Используем более надёжный путь
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string backupPath = Path.Combine(projectRoot, "FungusBackups");

        Debug.Log($"Backup path: {backupPath}"); // Проверим путь

        // Создаём структуру папок
        try
        {
            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
            string backupFolder = Path.Combine(backupPath, timestamp);
            
            // Проверяем путь к Fungus
            string fungusSourcePath = Path.Combine(Application.dataPath, "Fungus");
            Debug.Log($"Fungus source path: {fungusSourcePath}"); // Проверим путь

            if (Directory.Exists(fungusSourcePath))
            {
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                // Копируем все файлы вместо всей директории
                foreach (string dirPath in Directory.GetDirectories(fungusSourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(fungusSourcePath, Path.Combine(backupFolder, "Fungus")));
                }
                foreach (string filePath in Directory.GetFiles(fungusSourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(filePath, filePath.Replace(fungusSourcePath, Path.Combine(backupFolder, "Fungus")), true);
                }

                Debug.Log($"Backup created successfully at: {backupFolder}");
            }
            else
            {
                Debug.LogError($"Fungus folder not found at: {fungusSourcePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create backup: {e.Message}\n{e.StackTrace}");
        }
    }
}
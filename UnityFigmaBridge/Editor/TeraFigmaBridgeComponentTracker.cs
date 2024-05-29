using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityFigmaBridge.Editor.Nodes;
using UnityFigmaBridge.Editor.Utils;

public static class TeraFigmaBridgeComponentTracker
{
    /// <summary>A reference to the root game-object of all UI Screens before update </summary>
    private static List<GameObject> m_OldPrefabs = new List<GameObject>();


    [MenuItem("Figma Bridge/Connect prefab-component tracker")]
    private static void ConnectTeraComponentTracker()
    {
        FigmaAssetGenerator.OnScreenPrefabToBeCreated -= CopyChangesFromOldPrefabToNew;
        FigmaAssetGenerator.OnScreenPrefabToBeCreated += CopyChangesFromOldPrefabToNew;
        FigmaPaths.OnPrefabScreenFileDeletion -= OnScreenPrefabDelete;
        FigmaPaths.OnPrefabScreenFileDeletion += OnScreenPrefabDelete;
    }
    [MenuItem("Figma Bridge/Disconnect prefab-component tracker")]
    private static void DisconnectTeraComponentTracker()
    {
        FigmaAssetGenerator.OnScreenPrefabToBeCreated -= CopyChangesFromOldPrefabToNew;
        FigmaPaths.OnPrefabScreenFileDeletion -= OnScreenPrefabDelete;
    }
    private static void OnScreenPrefabDelete(FileInfo file)
    {
        if (file.Name.EndsWith(".meta"))
        {
            return;
        }
        string path = FigmaPaths.FigmaScreenPrefabFolder + "/" + file.Name;
        GameObject memoryObject = GameObject.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path));
        memoryObject.name = memoryObject.name.Replace("(Clone)", "");
        m_OldPrefabs.Add(memoryObject);
    }

    private static void CopyChangesFromOldPrefabToNew(GameObject newInstanceRoot)
    {
        if (newInstanceRoot == null)
        {
            UnityEngine.Debug.LogError($"Requested deserialization for null game-object");
            return;
        }

        if (m_OldPrefabs == null || m_OldPrefabs.Count == 0)
        {
            UnityEngine.Debug.LogWarning($"No saved roots for <b>{newInstanceRoot.name}</b>");
            return;
        }
        for (int i = 0; i < m_OldPrefabs.Count; i++)
        {
            string debug = $"Deserializing: {newInstanceRoot?.name}\n";
            if (m_OldPrefabs[i] == null)
            {
                debug += $"--Hierarchy at index: {i} is {m_OldPrefabs[i]?.name}\n";
                Debug.Log(debug);
                continue;
            }

            GameObject oldInstanceRoot = m_OldPrefabs[i];
            if (oldInstanceRoot.name == newInstanceRoot.name)
            {
                debug += $"-Root at index {i} is: <b>{oldInstanceRoot?.name}</b>\n";
                CopyFields(ref debug, newInstanceRoot, oldInstanceRoot);
                m_OldPrefabs.Remove(oldInstanceRoot);
                UnityEditor.Editor.DestroyImmediate(oldInstanceRoot);

            }
            Debug.Log(debug);

        }
    }
    private static void CopyFields(ref string debug, GameObject newInstanceRoot, GameObject oldInstanceRoot)
    {

        UnityEngine.Object[] newInstanceObjects = EditorUtility.CollectDeepHierarchy(new UnityEngine.Object[] { newInstanceRoot });
        UnityEngine.Object[] oldInstanceObjects = EditorUtility.CollectDeepHierarchy(new UnityEngine.Object[] { oldInstanceRoot });
        for (int j = 0; j < oldInstanceObjects.Length; j++)
        {

            bool found = false;
            GameObject go = null;
            debug += $"Old component: {oldInstanceObjects[j].GetType().Name}\n";
            for (int i = 0; i < newInstanceObjects.Length; i++)
            {
                // debug += $"--Checking old: <b>{oldInstanceObjects[j].name}</b> of type: {oldInstanceObjects[j].GetType()} " +
                //  $" to <b>{newInstanceObjects[i].name}</b> with type: {newInstanceObjects[i].GetType()} \n\n";
                if (newInstanceObjects[i].name == oldInstanceObjects[j].name && newInstanceObjects[i].GetType() == oldInstanceObjects[j].GetType())
                {
                    if (newInstanceObjects[i] is GameObject)
                    {
                        go = newInstanceObjects[i] as GameObject;
                        found = true;
                    }
                    else
                    {
                        try
                        {
                            debug += $"--Copying <b>{newInstanceObjects[i].GetType().Name}</b> from <b>{oldInstanceObjects[j].name}</b> to <b>{newInstanceObjects[i].name}</b>\n\n";
                            EditorUtility.CopySerializedIfDifferent(oldInstanceObjects[j], newInstanceObjects[i]);
                            found = true;

                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error while copying from: <b>{oldInstanceObjects[j].name}</b> to <b>{newInstanceObjects[i].name}</b>\n\n{ex.Message}");
                        }
                    }
                }
            }
            if (!found)
            {
                if (go == null)
                {
                    go = GetChildGameObject(newInstanceRoot, oldInstanceObjects[j].name);
                }
                if (go != null)
                {
                    UnityEngine.Component component = go.AddComponent(oldInstanceObjects[j].GetType());
                    debug += $"--Adding <b>{oldInstanceObjects[j].GetType().Name}</b> from <b>{oldInstanceObjects[j].name}</b> to <b>{go.name}</b>\n\n";
                    if (component != null)
                    {
                        EditorUtility.CopySerializedIfDifferent(oldInstanceObjects[j], component);
                    }
                }
                else
                {
                    Debug.LogError($"Could not find gameobject to add compoenent: {oldInstanceObjects[j].name} of type: {oldInstanceObjects[j].GetType().Name} as child of {newInstanceRoot.name}");
                }
            }
        }
    }
    private static GameObject GetChildGameObject(GameObject fromGameObject, string withName)
    {
        var allKids = fromGameObject.GetComponentsInChildren<Transform>();
        var kid = allKids.FirstOrDefault(k => k.gameObject.name == withName);
        if (kid == null) return null;
        return kid.gameObject;
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class CTool
{
    public static GameObject Find(GameObject root, string name)
    {
        return Find0(Dfs, root, name);
    }
    public static T Find<T>(GameObject root, string name) where T : Component
    {
        return Find0<T>(Dfs, root, name);
    }

    private static GameObject Find0(Search search, GameObject root, string name)
    {
        var found = search(root, name);
        if (!found)
            return null;
        return found;
    }

    private static T Find0<T>(Search search, GameObject root, string name) where T : Component
    {
        var found = search(root, name);
        if (!found)
            return default(T);
        return found.GetComponent<T>();
    }

    internal delegate GameObject Search(GameObject root, string name);
    private static GameObject Dfs(GameObject root, string name)
    {
        return Dfs0(root, name);
    }

    private static GameObject Dfs0(GameObject root, string name)
    {
        if (root.name == name)
            return root;
        foreach (Transform t in root.transform)
        {
            GameObject child = Dfs0(t.gameObject, name);
            if (child)
                return child;
        }
        return null;
    }

    public static GameObject Click(GameObject go, Action ac)
    {
        var button = go.GetComponent<Button>();
        if (!button)
        {
            button = go.AddComponent<Button>();
        }
        // if (go.GetComponent<Animation>() == null)
        // go.AddComponent<ButtonHandler>();
        button.onClick.AddListener(() =>
        {
            ac();
        });
        return go;
    }
    public static GameObject Click(GameObject go, string targetName, Action ac)
    {
        var go2 = Find(go, targetName);
        var button = go2.GetComponent<Button>();
        if (!button)
        {
            button = go2.AddComponent<Button>();
        }
        // if (go2.GetComponent<Animation>() == null)
        // go2.AddComponent<ButtonHandler>();
        button.onClick.AddListener(() =>
        {
            ac();

        });
        return go2;
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

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

}

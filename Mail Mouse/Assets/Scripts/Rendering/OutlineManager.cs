using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages outline effects for any GameObject.
/// Provides a simple static interface to enable/disable outlines on any object.
/// </summary>
public static class OutlineManager
{
    private static Dictionary<Renderer, GameObject> outlineObjects = new Dictionary<Renderer, GameObject>();

    /// <summary>
    /// Enables outline on a GameObject and all its child renderers.
    /// </summary>
    public static void EnableOutline(GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Cannot enable outline: targetObject is null");
            return;
        }

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
        Debug.Log($"Enabling outline on {targetObject.name} with {renderers.Length} renderers");

        foreach (Renderer renderer in renderers)
        {
            if (outlineObjects.ContainsKey(renderer))
                continue; // Already has an outline

            // Create outline object
            GameObject outlineObj = new GameObject("Outline_" + renderer.gameObject.name);
            outlineObj.transform.SetParent(renderer.transform);
            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;
            outlineObj.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f); // 5% larger to show outline

            // Copy mesh filter
            if (renderer.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                var outlineMF = outlineObj.AddComponent<MeshFilter>();
                outlineMF.mesh = meshFilter.mesh;
            }

            // Create a material using a guaranteed shader so builds do not end up with pink materials.
            var outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
            Material outlineMat = CreateOutlineMaterial();
            if (outlineMat != null)
                outlineRenderer.material = outlineMat;

            outlineObjects[renderer] = outlineObj;
            Debug.Log($"  Created outline for {renderer.gameObject.name}");
        }
    }

    private static Material CreateOutlineMaterial()
    {
        Shader outlineShader = Shader.Find("Custom/SimpleOutline");
        if (outlineShader == null)
            outlineShader = Shader.Find("Unlit/Color");
        if (outlineShader == null)
            outlineShader = Shader.Find("Standard");

        if (outlineShader == null)
        {
            Debug.LogWarning("OutlineManager could not find a usable outline shader.");
            return null;
        }

        Material material = new Material(outlineShader);
        material.SetColor("_Color", Color.white);
        material.color = Color.white;
        return material;
    }

    /// <summary>
    /// Disables outline on a GameObject and all its child renderers.
    /// </summary>
    public static void DisableOutline(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
        Debug.Log($"Disabling outline on {targetObject.name}");

        foreach (Renderer renderer in renderers)
        {
            if (outlineObjects.TryGetValue(renderer, out var outlineObj))
            {
                Object.Destroy(outlineObj);
                outlineObjects.Remove(renderer);
                Debug.Log($"  Destroyed outline for {renderer.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Sets outline on a single object, disabling it on any previously outlined object.
    /// </summary>
    public static void SetOutlineExclusive(GameObject targetObject)
    {
        // Clear all existing outlines
        var renderersCopy = new List<Renderer>(outlineObjects.Keys);
        foreach (var renderer in renderersCopy)
        {
            if (renderer != null)
                DisableOutline(renderer.gameObject);
        }

        // Enable outline on new target
        if (targetObject != null)
            EnableOutline(targetObject);
    }

    /// <summary>
    /// Clears all outlines from all objects.
    /// </summary>
    public static void ClearAllOutlines()
    {
        var renderersCopy = new List<Renderer>(outlineObjects.Keys);
        foreach (var renderer in renderersCopy)
        {
            if (renderer != null && renderer.gameObject != null)
                DisableOutline(renderer.gameObject);
        }
        outlineObjects.Clear();
    }
}

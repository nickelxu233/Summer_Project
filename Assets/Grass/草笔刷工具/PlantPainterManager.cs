using UnityEngine;
using System.Collections.Generic;

public class PlantPainterManager : MonoBehaviour
{
    [Header("笔刷设置")]
    public float brushSize = 2f;
    public float brushStrength = 1f;
    
    [Header("物体池")]
    public List<GameObject> placedObjects = new List<GameObject>();
    
    [Header("分组设置")]
    public string parentName = "PaintedObjects";
    public bool groupObjects = true;
    
    private Transform parentTransform;
    
    void Start()
    {
        if (groupObjects)
        {
            GameObject parent = GameObject.Find(parentName);
            if (parent == null)
            {
                parent = new GameObject(parentName);
            }
            parentTransform = parent.transform;
        }
    }
    
    public void AddPlacedObject(GameObject obj)
    {
        placedObjects.Add(obj);
        
        if (groupObjects && parentTransform != null)
        {
            obj.transform.SetParent(parentTransform);
        }
    }
    
    public void ClearAllObjects()
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        placedObjects.Clear();
    }
    
    public void RemoveObject(GameObject obj)
    {
        if (placedObjects.Contains(obj))
        {
            placedObjects.Remove(obj);
            DestroyImmediate(obj);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                Gizmos.DrawWireSphere(obj.transform.position, 0.1f);
            }
        }
    }
}
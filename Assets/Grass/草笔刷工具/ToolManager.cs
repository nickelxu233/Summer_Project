using UnityEditor;
using UnityEngine;

public static class ToolManager
{
    private static Tool previousTool = Tool.None;
    private static bool wasSaved = false;
    
    public static void SaveCurrentTool()
    {
        if (!wasSaved)
        {
            previousTool = Tools.current;
            wasSaved = true;
        }
    }
    
    public static void RestorePreviousTool()
    {
        if (wasSaved)
        {
            Tools.current = previousTool;
            wasSaved = false;
        }
    }
    
    public static bool IsToolSaved()
    {
        return wasSaved;
    }
}
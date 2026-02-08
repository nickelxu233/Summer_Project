using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PlantPainterWindow : EditorWindow
{
    // 工具状态
    private bool isBrushActive = false;
    private bool isPainting = false;
    
    // 笔刷设置
    private float brushSize = 2f;
    private float brushOpacity = 1f;
    private Color brushColor = Color.green;
    
    // 放置设置
    private List<GameObject> prefabsToPlace = new List<GameObject>();
    private int density = 3;
    private float minScale = 0.8f;
    private float maxScale = 1.2f;
    private bool randomRotation = true;
    private float offsetFromSurface = 0.01f;
    
    // 过滤设置
    private LayerMask paintLayer = -1;
    private float maxRayDistance = 100f;
    
    // 预览
    private GameObject brushPreview;
    private Material brushPreviewMaterial;
    
    // 工具栏
    private int selectedToolbarIndex = 0;
    private string[] toolbarOptions = new string[] { "笔刷", "物体", "设置" };
    
    // 笔刷历史
    private Stack<GameObject> placedObjects = new Stack<GameObject>();
    
    [MenuItem("TA/PlantPainter/打开植物笔刷工具")]
    public static void ShowWindow()
    {
        PlantPainterWindow window = GetWindow<PlantPainterWindow>("植物笔刷工具");
        window.minSize = new Vector2(350, 500);
    }
    
    [MenuItem("TA/PlantPainter/快速启动")]
    public static void QuickStart()
    {
        ShowWindow();
        
        // 自动创建笔刷管理器
        GameObject brushManager = new GameObject("PlantPainter_Manager");
        PlantPainterManager manager = brushManager.AddComponent<PlantPainterManager>();
        Selection.activeGameObject = brushManager;
    }
    
    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadSettings();
        CreateBrushPreview();
    }
    
    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyBrushPreview();
        SaveSettings();
    }
    
    void OnGUI()
    {
        DrawToolbar();
        
        EditorGUILayout.Space(10);
        
        switch (selectedToolbarIndex)
        {
            case 0:
                DrawBrushSettings();
                break;
            case 1:
                DrawObjectSettings();
                break;
            case 2:
                DrawGeneralSettings();
                break;
        }
        
        EditorGUILayout.Space(20);
        DrawActionButtons();
    }
    
    void DrawToolbar()
    {
        GUIStyle toolbarStyle = new GUIStyle(EditorStyles.toolbarButton);
        toolbarStyle.fixedHeight = 25;
        
        selectedToolbarIndex = GUILayout.Toolbar(selectedToolbarIndex, toolbarOptions, toolbarStyle, GUILayout.Height(30));
    }
    
    void DrawBrushSettings()
    {
        EditorGUILayout.LabelField("笔刷设置", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        brushSize = EditorGUILayout.Slider("笔刷大小", brushSize, 0.1f, 10f);
        brushOpacity = EditorGUILayout.Slider("笔刷强度", brushOpacity, 0f, 1f);
        brushColor = EditorGUILayout.ColorField("笔刷颜色", brushColor);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("过滤设置", EditorStyles.miniBoldLabel);
        paintLayer = EditorGUILayout.MaskField("绘制层", paintLayer, UnityEditorInternal.InternalEditorUtility.layers);
        maxRayDistance = EditorGUILayout.FloatField("射线距离", maxRayDistance);
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("快捷键", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("• B: 切换笔刷模式");
        EditorGUILayout.LabelField("• F: 聚焦到笔刷");
        EditorGUILayout.LabelField("• +/-: 调整笔刷大小");
        EditorGUILayout.LabelField("• Ctrl+Z: 撤销");
        EditorGUILayout.EndVertical();
    }
    
    void DrawObjectSettings()
    {
        EditorGUILayout.LabelField("物体设置", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // Prefab列表
        EditorGUILayout.LabelField("要放置的Prefab", EditorStyles.miniBoldLabel);
        
        int newCount = EditorGUILayout.IntField("Prefab数量", prefabsToPlace.Count);
        if (newCount != prefabsToPlace.Count)
        {
            while (prefabsToPlace.Count > newCount && prefabsToPlace.Count > 0)
                prefabsToPlace.RemoveAt(prefabsToPlace.Count - 1);
            while (prefabsToPlace.Count < newCount)
                prefabsToPlace.Add(null);
        }
        
        for (int i = 0; i < prefabsToPlace.Count; i++)
        {
            prefabsToPlace[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Prefab {i + 1}", 
                prefabsToPlace[i], 
                typeof(GameObject), 
                false);
        }
        
        EditorGUILayout.Space(10);
        
        // 放置参数
        density = EditorGUILayout.IntSlider("密度", density, 1, 20);
        minScale = EditorGUILayout.Slider("最小缩放", minScale, 0.1f, 2f);
        maxScale = EditorGUILayout.Slider("最大缩放", maxScale, 0.1f, 3f);
        
        EditorGUILayout.MinMaxSlider("缩放范围", ref minScale, ref maxScale, 0.1f, 3f);
        
        randomRotation = EditorGUILayout.Toggle("随机旋转", randomRotation);
        offsetFromSurface = EditorGUILayout.FloatField("表面偏移", offsetFromSurface);
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawGeneralSettings()
    {
        EditorGUILayout.LabelField("通用设置", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("保存/加载", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存预设"))
        {
            SavePreset();
        }
        if (GUILayout.Button("加载预设"))
        {
            LoadPreset();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("场景清理", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择所有已放置物体"))
        {
            SelectAllPlacedObjects();
        }
        if (GUILayout.Button("删除所有已放置物体"))
        {
            DeleteAllPlacedObjects();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawActionButtons()
    {
        GUIStyle bigButtonStyle = new GUIStyle(GUI.skin.button);
        bigButtonStyle.fontSize = 12;
        bigButtonStyle.fixedHeight = 35;
        
        // 笔刷开关按钮
        Color oldColor = GUI.color;
        GUI.color = isBrushActive ? Color.red : Color.green;
        
        if (GUILayout.Button(isBrushActive ? "退出笔刷模式 (B)" : "进入笔刷模式 (B)", bigButtonStyle))
        {
            ToggleBrushMode();
        }
        
        GUI.color = oldColor;
        
        EditorGUILayout.Space(10);
        
        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("撤销 (Ctrl+Z)"))
        {
            UndoLastPaint();
        }
        if (GUILayout.Button("聚焦笔刷 (F)"))
        {
            FocusOnBrush();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    void OnSceneGUI(SceneView sceneView)
    {
        if (!isBrushActive) return;
        
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        
        DrawBrushPreview(sceneView);
        HandleBrushInput(sceneView);
        
        sceneView.Repaint();
    }
    
    void DrawBrushPreview(SceneView sceneView)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRayDistance, paintLayer))
        {
            // 绘制笔刷范围
            Handles.color = new Color(brushColor.r, brushColor.g, brushColor.b, brushOpacity * 0.3f);
            Handles.DrawSolidDisc(hit.point + hit.normal * offsetFromSurface, 
                hit.normal, brushSize);
            
            // 绘制轮廓
            Handles.color = brushColor;
            Handles.DrawWireDisc(hit.point + hit.normal * offsetFromSurface, 
                hit.normal, brushSize);
            
            // 绘制法线指示
            Handles.color = Color.red;
            Handles.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
        }
    }
    
    void HandleBrushInput(SceneView sceneView)
    {
        Event e = Event.current;
        
        // 快捷键
        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.B:
                    ToggleBrushMode();
                    e.Use();
                    break;
                case KeyCode.F:
                    FocusOnBrush();
                    e.Use();
                    break;
                case KeyCode.Plus:
                case KeyCode.Equals:
                    brushSize += 0.5f;
                    e.Use();
                    break;
                case KeyCode.Minus:
                    brushSize = Mathf.Max(0.1f, brushSize - 0.5f);
                    e.Use();
                    break;
            }
        }
        
        // 鼠标操作
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isPainting = true;
            PaintObjects();
            e.Use();
        }
        
        if (e.type == EventType.MouseDrag && e.button == 0 && isPainting)
        {
            PaintObjects();
            e.Use();
        }
        
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isPainting = false;
            e.Use();
        }
        
        // 鼠标滚轮调整笔刷大小
        if (e.type == EventType.ScrollWheel && e.control)
        {
            brushSize = Mathf.Max(0.1f, brushSize + e.delta.y * 0.1f);
            e.Use();
        }
    }
    
    void PaintObjects()
    {
        if (prefabsToPlace.Count == 0)
        {
            Debug.LogWarning("没有设置要放置的Prefab！");
            return;
        }
        
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRayDistance, paintLayer))
        {
            int objectsToPlace = Mathf.RoundToInt(density * brushOpacity);
            
            for (int i = 0; i < objectsToPlace; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * brushSize;
                Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up);
                if (tangent.magnitude < 0.1f)
                    tangent = Vector3.Cross(hit.normal, Vector3.forward);
                
                Vector3 bitangent = Vector3.Cross(hit.normal, tangent);
                Vector3 randomOffset = tangent * randomCircle.x + bitangent * randomCircle.y;
                
                // 检查是否在笔刷范围内
                Vector3 samplePoint = hit.point + randomOffset;
                Ray sampleRay = new Ray(samplePoint + Vector3.up * 5f, Vector3.down);
                RaycastHit sampleHit;
                
                if (Physics.Raycast(sampleRay, out sampleHit, 10f, paintLayer))
                {
                    if (Vector3.Distance(hit.point, sampleHit.point) <= brushSize)
                    {
                        PlaceObject(sampleHit);
                    }
                }
            }
        }
    }
    
    void PlaceObject(RaycastHit hit)
    {
        GameObject prefab = prefabsToPlace[Random.Range(0, prefabsToPlace.Count)];
        if (prefab == null) return;
        
        // 计算位置和旋转
        Vector3 position = hit.point + hit.normal * offsetFromSurface;
        Quaternion rotation = Quaternion.identity;
        
        // 对齐法线
        rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        
        // 随机旋转
        if (randomRotation)
        {
            rotation *= Quaternion.Euler(0, Random.Range(0, 360f), 0);
        }
        
        // 随机缩放
        float scale = Random.Range(minScale, maxScale);
        
        // 实例化物体
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newObject.transform.position = position;
        newObject.transform.rotation = rotation;
        newObject.transform.localScale = Vector3.one * scale;
        
        // 记录历史
        placedObjects.Push(newObject);
        
        // 注册撤销操作
        Undo.RegisterCreatedObjectUndo(newObject, "Paint Object");
    }
    
    void ToggleBrushMode()
    {
        isBrushActive = !isBrushActive;
        SceneView.RepaintAll();
        
        if (isBrushActive)
        {
            Debug.Log("笔刷模式已激活");
            ToolManager.SaveCurrentTool();
            Tools.current = Tool.None;
        }
        else
        {
            Debug.Log("笔刷模式已关闭");
            ToolManager.RestorePreviousTool();
        }
    }
    
    void UndoLastPaint()
    {
        if (placedObjects.Count > 0)
        {
            GameObject lastObject = placedObjects.Pop();
            if (lastObject != null)
            {
                Undo.DestroyObjectImmediate(lastObject);
            }
        }
    }
    
    void FocusOnBrush()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRayDistance, paintLayer))
        {
            SceneView.lastActiveSceneView.Frame(new Bounds(hit.point, Vector3.one * brushSize * 2));
        }
    }
    
    void SelectAllPlacedObjects()
    {
        List<GameObject> allPlaced = new List<GameObject>(placedObjects);
        Selection.objects = allPlaced.ToArray();
    }
    
    void DeleteAllPlacedObjects()
    {
        if (EditorUtility.DisplayDialog("确认删除", 
            $"确定要删除所有已放置的{placedObjects.Count}个物体吗？", 
            "删除", "取消"))
        {
            foreach (GameObject obj in placedObjects)
            {
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }
            placedObjects.Clear();
        }
    }
    
    void CreateBrushPreview()
    {
        // 创建笔刷预览物体（可选）
    }
    
    void DestroyBrushPreview()
    {
        if (brushPreview != null)
        {
            DestroyImmediate(brushPreview);
        }
    }
    
    void SaveSettings()
    {
        // 保存设置到EditorPrefs
        EditorPrefs.SetFloat("PlantPainter_BrushSize", brushSize);
        EditorPrefs.SetFloat("PlantPainter_BrushOpacity", brushOpacity);
        EditorPrefs.SetInt("PlantPainter_Density", density);
        // ... 保存其他设置
    }
    
    void LoadSettings()
    {
        brushSize = EditorPrefs.GetFloat("PlantPainter_BrushSize", 2f);
        brushOpacity = EditorPrefs.GetFloat("PlantPainter_BrushOpacity", 1f);
        density = EditorPrefs.GetInt("PlantPainter_Density", 3);
        // ... 加载其他设置
    }
    
    void SavePreset()
    {
        string path = EditorUtility.SaveFilePanel("保存预设", 
            Application.dataPath, 
            "PlantPainter_Preset", 
            "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            // 序列化并保存预设
        }
    }
    
    void LoadPreset()
    {
        string path = EditorUtility.OpenFilePanel("加载预设", 
            Application.dataPath, 
            "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            // 加载并反序列化预设
        }
    }
}
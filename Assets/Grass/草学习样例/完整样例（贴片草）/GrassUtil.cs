using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassUtil : MonoBehaviour
{
    private static Mesh _grassMesh;
    public static Mesh CreateGrassMesh(){
        var grassMesh = new Mesh { name = "Grass Quad" };
        float width = 1f;
        float height = 1f;
        float halfWidth = width/2;
        grassMesh.SetVertices(new List<Vector3>
        {
            new Vector3(-halfWidth, 0, 0.0f),
            new Vector3(-halfWidth,  height, 0.0f),
            new Vector3(halfWidth, 0, 0.0f),
            new Vector3(halfWidth,  height, 0.0f),
            
        });
        grassMesh.SetUVs(0, new List<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 1),
        });

        grassMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3,}, 
        MeshTopology.Triangles, 0, false);
        grassMesh.RecalculateNormals();
        grassMesh.UploadMeshData(true);
        return grassMesh;
    }

    /// <summary>
    /// 如果没有_grassMesh，就创建一个GrassMesh指定给_grassMesh
    /// </summary>
    public static Mesh unitMesh
    {
        get 
        {
            if (_grassMesh != null){
                return _grassMesh;
            }
            _grassMesh = CreateGrassMesh();
            return _grassMesh;
        }
        //get{}是C# 中属性的读取访问器，定义当读取属性时执行什么代码。没有set只有get的时候就是：只读
    }

    /// <summary>
    /// 三角形内部，取平均分布的随机点  
    /// </summary>
    public static Vector3 RandomPointInsideTriangle(Vector3 p1,Vector3 p2,Vector3 p3){
        var x = Random.Range(0,1f);
        var y = Random.Range(0,1f);
        if(y > 1 - x){
            //如果随机到了右上区域，那么反转到左下
            var temp = y;
            y = 1 - x;
            x = 1 - temp;
        }
        var vx = p2 - p1;
        var vy = p3 - p1;
        return p1 + x * vx + y * vy;
    }

    //计算三角形面积
        public static float GetAreaOfTriangle(Vector3 p1,Vector3 p2,Vector3 p3){
            var vx = p2 - p1;
            var vy = p3 - p1;
            var dotvxy = Vector3.Dot(vx,vy);
            //主要是不想用Cross算，推倒过程在procreate里写了
            var sqrArea = vx.sqrMagnitude * vy.sqrMagnitude -  dotvxy * dotvxy; //.sqrMagnitude是计算向量长度的平方
            return 0.5f * Mathf.Sqrt(sqrArea);
        }
    //计算三角形朝向 
        public static Vector3 GetFaceNormal(Vector3 p1,Vector3 p2,Vector3 p3){
            var vx = p2 - p1;
            var vy = p3 - p1;
            return Vector3.Cross(vx,vy);
        }

}

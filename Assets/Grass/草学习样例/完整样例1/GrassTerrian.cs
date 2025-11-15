using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassTerrian : MonoBehaviour
{
    //HashSet<T> 是 C# 中的高性能集合，只存储不重复的元素。
    private static HashSet<GrassTerrian> _actives = new HashSet<GrassTerrian>();

    //返回活跃的GrassTerrian对象合集
    public static IReadOnlyCollection<GrassTerrian> actives{
        get{
            return _actives;
        }
    }
    
    //可编辑私有字段
    [SerializeField]
    private Material _material;

    [SerializeField]
    private Vector2 _grassQuadSize = new Vector2(0.1f,0.6f);

    [SerializeField]
    private int _grassCountPerMeter = 100;
    
    //外部只能读取不能修改
    public Material material{
        get{
            return _material;
        }
    }

    private int _seed;

    //ComputeBuffer
    private ComputeBuffer _grassBuffer;

    private int _grassCount;

    //获得获取Hash值作为整数，将其作为生成全新的GUID（全局唯一标识符）
    private void Awake() {
        _seed = System.Guid.NewGuid().GetHashCode();
    }

    //封装一下grassCount
    public int grassCount{
        get{
            return _grassCount;
        }
    }

    //
    public ComputeBuffer grassBuffer{
            get{
                if(_grassBuffer != null){
                    return _grassBuffer;
                }
                var filter = GetComponent<MeshFilter>();
                var terrianMesh = filter.sharedMesh;        //拿到地形mesh
                var matrix = transform.localToWorldMatrix;  //将地形从本地坐标转换为世界坐标矩阵
                var grassIndex = 0;                         //草index
                List<GrassInfo> grassInfos = new List<GrassInfo>(); //新建一个GrassInfoList
                var maxGrassCount = 10000;                  //最大草数
                Random.InitState(_seed);                    //设置随机种子为_seed，后续所有Random调用都会产生相同的随机序列

                var indices = terrianMesh.triangles;        //获取地形的三角面
                var vertices = terrianMesh.vertices;        //获取网格的顶点位置数组

                for(var j = 0; j < indices.Length / 3; j ++){
                    var index1 = indices[j * 3];
                    var index2 = indices[j * 3 + 1];
                    var index3 = indices[j * 3 + 2];
                    var v1 = vertices[index1];
                    var v2 = vertices[index2];
                    var v3 = vertices[index3];              //获得一个三角面的三个顶点位置

                    //面得到法向
                    var normal = GrassUtil.GetFaceNormal(v1,v2,v3);

                    //计算up到faceNormal的旋转四元数，也就是从世界"上方向"旋转到"法线方向"所需的旋转。
                    var upToNormal = Quaternion.FromToRotation(Vector3.up,normal);

                    //三角面积
                    var arena = GrassUtil.GetAreaOfTriangle(v1,v2,v3);

                    //计算在该三角面中，需要种植的数量
                    var countPerTriangle = Mathf.Max(1,_grassCountPerMeter * arena);

                    for(var i = 0; i < countPerTriangle; i ++){

                        var positionInTerrian = GrassUtil.RandomPointInsideTriangle(v1,v2,v3);
                        float rot = Random.Range(0,180);
                        var localToTerrian = Matrix4x4.TRS(positionInTerrian,  upToNormal * Quaternion.Euler(0,rot,0) ,Vector3.one);
                        //位置，在转为法线向上方向的基础上，围绕自身y轴旋转随机角度，总之是一种四元数乘法，不满足交换律所以乘法顺序很重要，不能
                        //写成 Quaternion.Euler(0,rot,0) * upToNormal  

                        //每颗草的贴图Tiling信息
                        Vector2 texScale = Vector2.one;
                        Vector2 texOffset = Vector2.zero;
                        Vector4 texParams = new Vector4(texScale.x,texScale.y,texOffset.x,texOffset.y);
                        
                        //在这里将每颗草的位置信息传入shader中
                        var grassInfo = new GrassInfo(){
                            localToTerrian = localToTerrian,
                            texParams = texParams
                        };

                        grassInfos.Add(grassInfo);
                        grassIndex ++;
                        //如果指定草的数量大于设定阈值，停止
                        if(grassIndex >= maxGrassCount){
                            break;
                        }
                    }
                    if(grassIndex >= maxGrassCount){
                        break;
                    }
                }
               
                _grassCount = grassIndex;
                _grassBuffer = new ComputeBuffer(_grassCount,64 + 16);  //a：Count，缓冲区中元素的数量，在这里是储存了总共多少根草的信息
                                                                        //b：Stride，每个元素的大小（字节数）。64通常是一个4x4矩阵的大小
                                                                        // （16 x 4），16字节是一个float4向量的大小（4 x 4）
                _grassBuffer.SetData(grassInfos);                       //使用grassInfos中的值设置该缓冲区。
                return _grassBuffer;
            }
        }

    private MaterialPropertyBlock _materialBlock;
        
    public void UpdateMaterialProperties(){
        materialPropertyBlock.SetMatrix(ShaderProperties.TerrianLocalToWorld,transform.localToWorldMatrix);
        materialPropertyBlock.SetBuffer(ShaderProperties.GrassInfos,grassBuffer);
        materialPropertyBlock.SetVector(ShaderProperties.GrassQuadSize,_grassQuadSize);
    }

    public MaterialPropertyBlock materialPropertyBlock{
        get{
            if(_materialBlock == null){
                _materialBlock = new MaterialPropertyBlock();
            }
            
            return _materialBlock;
        }
    }

    public struct GrassInfo{
        public Matrix4x4 localToTerrian;
        public Vector4 texParams;
    }

    private class ShaderProperties{

        public static readonly int TerrianLocalToWorld = Shader.PropertyToID("_TerrianLocalToWorld");
        public static readonly int GrassInfos = Shader.PropertyToID("_GrassInfos");
        public static readonly int GrassQuadSize = Shader.PropertyToID("_GrassQuadSize");

    }

    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using UnityEngine;
public class FunnyGPUInstance : MonoBehaviour
{
    [SerializeField]
    private GameObject _instanceGo;//需要实例化对象
    [SerializeField]
    private int _instanceCount;//需要实例化个数
    [SerializeField]
    private bool _bRandPos = false;//是否随机的显示对象
    // Start is called before the first frame update

    private MaterialPropertyBlock _mpb = null;//与buffer交换数据

    void Start()
    {
        
        
        for (int i = 0; i < _instanceCount; i++)
        {
            Vector3 pos = new Vector3(i * 1.5f, 0, 0);
            GameObject pGO = GameObject.Instantiate<GameObject>(_instanceGo);   //生成实例化对象
            pGO.transform.SetParent(gameObject.transform);                      //设置父级物体（类型是Transform）

            if(_bRandPos)
            {
                pGO.transform.localPosition = Random.insideUnitSphere * 10.0f;
            } 
            else
            {
                pGO.transform.localPosition = pos;
            }       

            //个性化显示
            SetPropertyBlockByGameObject(pGO);

        }
    }

    private bool SetPropertyBlockByGameObject(GameObject pGameObject)
    {
        if(pGameObject == null)
        {
            return false;
        }
        if(_mpb == null)
        {
            _mpb = new MaterialPropertyBlock();
        }

        //随机每个对象的颜色
        _mpb.SetColor("_Color", new Color(Random.Range(0f, 0f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1.0f));
        _mpb.SetFloat("_Phi", Random.Range(-40f, 40f));

        MeshRenderer meshRenderer = pGameObject.GetComponent<MeshRenderer>();

        if(meshRenderer == null)
        {
            return false;
        }

        meshRenderer.SetPropertyBlock(_mpb);

        return true;
    }
}
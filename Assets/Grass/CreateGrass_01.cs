using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateGrass_01 : MonoBehaviour
{
    [SerializeField]
    private GameObject _instanceGo;//需要实例化对象
    [SerializeField]
    private int _GrassCount;//需要实例化个数

    private MaterialPropertyBlock _mpb = null;//与buffer交换数据

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < _GrassCount; i++)
        {
            
        }
    }

    private bool SetPropertyBlockByGameObject(GameObject Pgo){
        if(Pgo == null)
        {
            return false;
        }
        if(_mpb == null)
        {
            _mpb = new MaterialPropertyBlock();
        }

        _mpb.SetFloat("_Phi", Random.Range(-40f, 40f));

        MeshRenderer meshRenderer = Pgo.GetComponent<MeshRenderer>();

        if(meshRenderer == null)
        {
            return false;
        }

        meshRenderer.SetPropertyBlock(_mpb);

        return true;
    }

    
}

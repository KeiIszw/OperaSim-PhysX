using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BacketRot : MonoBehaviour
{
    [SerializeField] private GameObject _backet;
    [SerializeField] private GameObject targetBacketBone;
    // Start is called before the first frame update
    //外から変更できる定数
    [SerializeField] private  float CofRotation= 0.625f;


    // バケット横のオブジェクトが連動するようになるコード


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //現在のバケットの角度
        Vector3 currentBacketAngle = _backet.transform.localEulerAngles;
        //Debug.Log("currentBacketAngle:"+currentBacketAngle.z);
        float targetY=0;
        if(currentBacketAngle.z>180)
        {
         
        //絶対値に変換
            targetY=Mathf.Abs(360-currentBacketAngle.z)*CofRotation;
        }
        else
        {
            targetY = currentBacketAngle.z * CofRotation*-1;
        }
        //targetBackBoneの角度を変更
        targetBacketBone.transform.localEulerAngles = new Vector3(0,0 , targetY);
        
    }
}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 触碰检测
/// </summary>
public class ViveGrip_TouchDetection : MonoBehaviour {

    //半径
    public float radius = 1f;
    //碰撞物体列表
    private List<GameObject> collidingObjects = new List<GameObject>( );

    void Start( ) {
        //开启碰撞检测
        GetComponent<SphereCollider>( ).isTrigger = true;
    }
    
    // 碰撞物体进入便将其加入碰撞列表
    void OnTriggerEnter(Collider other) {
        collidingObjects.Add(other.gameObject);
    }
    // 碰撞物体移除
    void OnTriggerExit(Collider other) {
        collidingObjects.Remove(other.gameObject);
    }

    /// <summary>
    /// 最近的物体
    /// </summary>
    public GameObject NearestObject( ) {
        float closestDistance = radius + 1f;
        GameObject touchedObject = null;

        foreach (GameObject gameObject in collidingObjects) {
            //无抓取或互动组件则跳过
            if (!ActiveViveGripObject(gameObject)) { continue; }

            //计算距离,小于最近距离则赋值给触摸到的对象,重置最小距离,继续遍历
            float distance = Vector3.Distance(transform.position, gameObject.transform.position);
            if (distance < closestDistance) {
                touchedObject = gameObject;
                closestDistance = distance;
            }
        }
        return touchedObject;
    }

    bool ActiveViveGripObject(GameObject gameObject) {
        if (gameObject == null) { return false; } // 有时正好被摧毁则跳过。Happens with Destroy() sometimes
        ViveGrip_Grabbable grabbable = gameObject.GetComponent<ViveGrip_Grabbable>( );
        ViveGrip_Interactable interactable = gameObject.GetComponent<ViveGrip_Interactable>( );
        bool validGrabbable = grabbable != null && grabbable.enabled;
        bool validInteractable = interactable != null && interactable.enabled;
        return validGrabbable || validInteractable;
    }
}

using UnityEngine;
using System.Collections;

public class ViveGripExample_Button : MonoBehaviour {

    private const float SPEED = 0.1f;
    private float distance;
    private int direction = 1;

    void Start( ) {
        ResetDistance( );
    }

    void ViveGripInteractionStart(ViveGrip_GripPoint gripPoint) {
        // 震动手柄
        gripPoint.controller.Vibrate(25, 0.4f);
        // 触发抓取后关闭组件
        GetComponent<ViveGrip_Interactable>( ).enabled = false;
        StartCoroutine("Move");
    }

    IEnumerator Move( ) {
        while (distance > 0) {
            Increment( );
            yield return null;
        }
        yield return StartCoroutine("MoveBack");
    }

    IEnumerator MoveBack( ) {
        direction *= -1;
        ResetDistance( );
        while (distance > 0) {
            Increment( );
            yield return null;
        }
        direction *= -1;
        ResetDistance( );
        //重置后重新开启互动组件
        GetComponent<ViveGrip_Interactable>( ).enabled = true;
    }

    void Increment( ) {
        float increment = Time.deltaTime * SPEED;
        increment = Mathf.Min(increment, distance);
        transform.Translate(0, 0, increment * direction);
        distance -= increment;
    }

    void ResetDistance( ) {
        distance = 0.03f;
    }

}

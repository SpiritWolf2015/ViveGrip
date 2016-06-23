using UnityEngine;

/// <summary>
/// 可交互
/// </summary>
[DisallowMultipleComponent]
public class ViveGrip_Interactable : ViveGrip_Highlight {

    void Start( ) { }
    //下面这些是脚本被调用的情况
    // These are called this on the scripts of the attached object and children of the controller:

    //当互动按钮被按下，松开的时候调用下面的方法
    // When touched and the interaction button is pressed and released, respectively
    //   void ViveGripInteractionStart(ViveGrip_GripPoint gripPoint) {}
    //   void ViveGripInteractionStop(ViveGrip_GripPoint gripPoint) {}

    //它的方法是通过别的脚本来实现和调用的，例如Vive Grip Example_Button脚本中的。
}

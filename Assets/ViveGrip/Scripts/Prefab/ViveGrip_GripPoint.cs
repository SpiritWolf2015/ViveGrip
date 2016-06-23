using UnityEngine;

/// <summary>
/// 抓取点，该脚本的功能主要是处理抓取的,判断哪个可抓取对象最近，
/// 然后进行细致的抓取操作.当然,除此之外，
/// 被抓取的对象也是需要添加相应的脚本组件的。
/// </summary>
[DisallowMultipleComponent]
public class ViveGrip_GripPoint : MonoBehaviour {

    [Tooltip("你可以抓取物体的距离。The distance at which you can touch objects.")]
    public float touchRadius = 0.2f;

    [Tooltip("自动掉落的距离。The distance at which objects will automatically drop.")]
    public float holdRadius = 0.3f;

    [Tooltip("触摸的半径是否可视化？Is the touch radius visible? (Good for debugging)")]
    public bool visible = false;

    [Tooltip("是否需要抓取开关?Should the button toggle grabbing?")]
    public bool inputIsToggle = false;

    [HideInInspector]
    public ViveGrip_ControllerHandler controller;
    //碰撞检测
    private ViveGrip_TouchDetection touch;
    //高亮颜色
    private Color highlightTint = new Color(0.2f, 0.2f, 0.2f);
    //可配置关节
    private ConfigurableJoint joint;
    private GameObject jointObject;
    private bool anchored = false;
    private Vector3 grabbedAt;
    //上一次触摸的对象
    private GameObject lastTouchedObject;
    private GameObject lastInteractedObject;

    void Start( ) {
        controller = GetComponent<ViveGrip_ControllerHandler>( );
        //初始化触摸圆
        GameObject gripSphere = InstantiateTouchSphere( );
        //添加触摸检测组件
        touch = gripSphere.AddComponent<ViveGrip_TouchDetection>( );
        touch.radius = touchRadius;
    }

    void Update( ) {
        //获取最近的对象
        GameObject touchedObject = touch.NearestObject( );
        //处理高亮
        HandleHighlighting(touchedObject);
        // 处理抓取
        HandleGrabbing(touchedObject);
        //处理互动
        HandleInteraction(touchedObject);
        //处理摸索
        HandleFumbling( );
        lastTouchedObject = touchedObject;
    }

    /// <summary>
    /// 处理抓取
    /// </summary>
    void HandleGrabbing(GameObject touchedObject) {
        //没有抓到则返回
        if (!GrabTriggered( )) { return; }
        //如果已经握住什么东西了则摧毁连结
        if (HoldingSomething( )) {
            if (touchedObject != null) {
                GetHighlight(touchedObject).Highlight(highlightTint);
            }
            DestroyConnection( );

            // 抓取对象非空,且有可抓取组件
        } else if (touchedObject != null && touchedObject.GetComponent<ViveGrip_Grabbable>( ) != null) {
            //获取高亮的对象并移除高亮
            GetHighlight(touchedObject).RemoveHighlighting( );
            //创建连接，发送ViveGripGrabStart消息。
            CreateConnectionTo(touchedObject.GetComponent<Rigidbody>( ));
        }
    }

    /// <summary>
    /// 是否触发抓取,这里的Grab对应手柄输入的grip
    /// </summary>
    bool GrabTriggered( ) {
        if (controller == null) { return false; }
        if (inputIsToggle) {
            return controller.Pressed("grab");
        }
        return HoldingSomething( ) ? controller.Released("grab") : controller.Pressed("grab");
    }

    /// <summary>
    /// 处理互动，发送ViveGripInteractionStart，ViveGripInteractionStop消息。
    /// </summary>
    void HandleInteraction(GameObject touchedObject) {
        if (HoldingSomething( )) {
            touchedObject = joint.connectedBody.gameObject;
        }
        if (touchedObject != null) {
            if (touchedObject.GetComponent<ViveGrip_Interactable>( ) == null) { return; }
            if (controller.Pressed("interact")) {
                lastInteractedObject = touchedObject;
                Message("ViveGripInteractionStart");
            }
        }
        if (controller.Released("interact")) {
            Message("ViveGripInteractionStop", lastInteractedObject);
            lastInteractedObject = null;
        }
    }

    /// <summary>
    /// 高亮处理，发送ViveGripTouchStop，ViveGripTouchStart消息。
    /// </summary>
    void HandleHighlighting(GameObject touchedObject) {
        ViveGrip_Highlight last = GetHighlight(lastTouchedObject);
        ViveGrip_Highlight current = GetHighlight(touchedObject);
        if (last != current) {
            if (last != null) {
                last.RemoveHighlighting( );
                Message("ViveGripTouchStop", last.gameObject);
            }
            if (current != null && !HoldingSomething( )) {
                current.Highlight(highlightTint);
                Message("ViveGripTouchStart");
            }
        }
    }

    /// <summary>
    /// 获取高亮
    /// </summary>
    ViveGrip_Highlight GetHighlight(GameObject touchedObject) {
        if (touchedObject == null) { return null; }
        return touchedObject.GetComponent<ViveGrip_Highlight>( );
    }

    /// <summary>
    /// 处理摸索
    /// </summary>
    void HandleFumbling( ) {
        if (HoldingSomething( )) {
            //获取抓取的距离
            float grabDistance = CalculateGrabDistance( );
            //是否在可握住的半径内
            bool pulledToMiddle = grabDistance < holdRadius;
            anchored = anchored || pulledToMiddle;
            if (anchored && grabDistance > holdRadius) {
                // 有锚点且抓取的距离超出握住则摧毁连接
                DestroyConnection( );
            }
        }
    }

    float CalculateGrabDistance( ) {
        ViveGrip_Grabbable grabbable = joint.connectedBody.gameObject.GetComponent<ViveGrip_Grabbable>( );
        Vector3 grabbedAnchorPosition = grabbable.WorldAnchorPosition( );
        return Vector3.Distance(transform.position, grabbedAnchorPosition);
    }

    /// <summary>
    /// 创建连接，发送ViveGripGrabStart消息。
    /// </summary>
    void CreateConnectionTo(Rigidbody desiredBody) {
        // 实例化关节
        jointObject = InstantiateJointParent( );
        //获取朝向 偏移 
        desiredBody.gameObject.GetComponent<ViveGrip_Grabbable>( ).GrabFrom(transform.position);
        //通过关节组件连结起来
        joint = ViveGrip_JointFactory.JointToConnect(jointObject, desiredBody, transform.rotation);
        Message("ViveGripGrabStart");
    }

    /// <summary>
    /// 摧毁连接，发送ViveGripGrabStop消息。
    /// </summary>
    void DestroyConnection( ) {
        GameObject lastObject = jointObject.GetComponent<ConfigurableJoint>( ).connectedBody.gameObject;
        Destroy(jointObject);
        anchored = false;
        Message("ViveGripGrabStop", lastObject);
    }

    /// <summary>
    /// 实例化关节父类
    /// </summary>
    /// <returns>The joint parent.</returns>
    GameObject InstantiateJointParent( ) {
        GameObject newJointObject = new GameObject("ViveGrip Joint");
        newJointObject.transform.parent = transform;
        newJointObject.transform.localPosition = Vector3.zero;
        newJointObject.transform.localScale = Vector3.one;
        newJointObject.transform.rotation = Quaternion.identity;
        Rigidbody jointRigidbody = newJointObject.AddComponent<Rigidbody>( );
        jointRigidbody.useGravity = false;
        jointRigidbody.isKinematic = true;
        return newJointObject;
    }

    /// <summary>
    /// 实例化触摸圆
    /// </summary>
    GameObject InstantiateTouchSphere( ) {
        GameObject gripSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Renderer sphereRenderer = gripSphere.GetComponent<Renderer>( );
        sphereRenderer.enabled = visible;
        if (visible) {
            sphereRenderer.material = new Material(Shader.Find("ViveGrip/TouchSphere"));
            sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sphereRenderer.receiveShadows = false;
        }
        gripSphere.transform.localScale = Vector3.one * touchRadius;
        gripSphere.transform.position = transform.position;
        gripSphere.transform.SetParent(transform);
        gripSphere.AddComponent<Rigidbody>( ).isKinematic = true;
        gripSphere.name = "ViveGrip Touch Sphere";
        return gripSphere;
    }

    /// <summary>
    /// 握住了东西
    /// </summary>
    public bool HoldingSomething( ) {
        return jointObject != null;
    }

    public bool TouchingSomething( ) {
        return touch.NearestObject( ) != null;
    }

    GameObject TrackedObject( ) {
        return controller.trackedObject.gameObject;
    }

    void Message(string name, GameObject touchedObject = null) {
        TrackedObject( ).BroadcastMessage(name, this, SendMessageOptions.DontRequireReceiver);
        touchedObject = touchedObject ?? touch.NearestObject( );
        if (touchedObject == null) { return; }
        touchedObject.SendMessage(name, this, SendMessageOptions.DontRequireReceiver);
    }
}

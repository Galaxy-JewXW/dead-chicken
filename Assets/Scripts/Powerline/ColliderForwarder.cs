using UnityEngine;

/// <summary>
/// 碰撞器事件转发器
/// 用于将子对象的鼠标事件转发到主PowerlineInteraction组件
/// </summary>
public class ColliderForwarder : MonoBehaviour
{
    [HideInInspector]
    public PowerlineInteraction targetInteraction;
    
    void OnMouseEnter()
    {
        if (targetInteraction != null)
        {
            targetInteraction.OnMouseEnterForwarded();
        }
    }
    
    void OnMouseExit()
    {
        if (targetInteraction != null)
        {
            targetInteraction.OnMouseExitForwarded();
        }
    }
    
    void OnMouseDown()
    {
        // 检查鼠标是否在UI上，如果是则不转发点击事件
        var uiManager = FindObjectOfType<SimpleUIToolkitManager>();
        if (uiManager != null && uiManager.IsMouseOverUI())
        {
            return; // 鼠标在UI上，不处理点击
        }
        
        if (targetInteraction != null)
        {
            targetInteraction.OnMouseDownForwarded();
        }
    }
} 
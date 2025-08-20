using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;

namespace Game.Debugging
{
    [MovedFrom(true, null, null, "EchoInput")]
    public class EchoInput : MonoBehaviour
{
    // Must be public and take InputAction.CallbackContext to appear under the "Dynamic" list.
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed && !ctx.canceled) return;
        Vector2 v = ctx.ReadValue<Vector2>();
        Debug.Log($"[EchoInput] Move: {v} (phase: {ctx.phase})");
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Debug.Log("[EchoInput] Jump pressed");
    }

    public void OnSwitchView(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Debug.Log("[EchoInput] SwitchView pressed");
    }
}
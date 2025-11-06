using UnityEngine;
using UnityEngine.InputSystem;

public class RotateWithMouse : MonoBehaviour
{
    [SerializeField] float sens;

    [SerializeField] bool isEnabled = false;

    public void Step(InputAction.CallbackContext callback)
    {
        if (!isEnabled) return;

        Vector2 dir = callback.ReadValue<Vector2>();
        float x = dir.x;
        float y = dir.y;

        transform.Rotate(Vector3.up, x * sens * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right, -y * sens * Time.deltaTime, Space.Self);
    }

    public void Enable(InputAction.CallbackContext callback)
    {
        var val = callback.ReadValue<float>();
        if (val == 1)
        {
            isEnabled = true;
        }
        if (val == 0)
        {
            isEnabled = false;
        }
    }
}

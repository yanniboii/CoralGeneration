using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    [SerializeField] float moveSpeed;

    public void Step(InputAction.CallbackContext callback)
    {
        Vector2 dir = callback.ReadValue<Vector2>();

        float x = dir.x * moveSpeed;
        float z = dir.y * moveSpeed;

        transform.Translate(new Vector3(x, 0, z), Space.Self);
    }
}

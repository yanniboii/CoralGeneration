using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    [SerializeField] float moveSpeed;
    float x;
    float z;

    private void Update()
    {
        transform.Translate(new Vector3(x, 0, z), Space.Self);
    }

    public void Step(InputAction.CallbackContext callback)
    {
        Vector2 dir = callback.ReadValue<Vector2>();

        x = dir.x * moveSpeed;
        z = dir.y * moveSpeed;

    }
}

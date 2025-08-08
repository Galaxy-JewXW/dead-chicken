using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float lookSpeed = 2f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float upDown = 0f;

        if (Input.GetKey(KeyCode.E)) upDown = 1f;
        if (Input.GetKey(KeyCode.Q)) upDown = -1f;

        Vector3 move = (transform.forward * v + transform.right * h + transform.up * upDown) * moveSpeed * Time.deltaTime;
        transform.position += move;

        if (Input.GetMouseButton(0)) // 只有按住鼠标左键时才旋转
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = -Input.GetAxis("Mouse Y") * lookSpeed;

            transform.Rotate(Vector3.up, mouseX, Space.World);
            transform.Rotate(Vector3.right, mouseY, Space.Self);
        }
    }
} 
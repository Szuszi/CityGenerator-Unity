using UnityEngine;
public class FlyCamera : MonoBehaviour
{
    private Vector3 _angles;
    public float speed = 1.0f;
    public float fastSpeed = 2.0f;
    public float mouseSpeed = 4.0f;
 
    private void OnEnable() {
        _angles = transform.eulerAngles;
        Cursor.lockState = CursorLockMode.Locked;
    }
 
    private void OnDisable() { Cursor.lockState = CursorLockMode.None; }
 
    private void Update() {
        _angles.x -= Input.GetAxis("Mouse Y") * mouseSpeed;
        _angles.y += Input.GetAxis("Mouse X") * mouseSpeed;
        transform.eulerAngles = _angles;
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : speed;
        transform.position +=
            Input.GetAxis("Horizontal") * moveSpeed * transform.right +
            Input.GetAxis("Vertical") * moveSpeed * transform.forward;
    }
}

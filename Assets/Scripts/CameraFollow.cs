using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;                 
    public float smoothSpeed = 0.125f;       

    public Vector3 offset = new Vector3(0, 0, -10);  
    public Vector2 minBounds = new Vector2(0, -10);  
    public Vector2 maxBounds = new Vector2(0, 10);   

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, new Vector3(clampedX, clampedY, desiredPosition.z), smoothSpeed);
        transform.position = smoothedPosition;
    }
}


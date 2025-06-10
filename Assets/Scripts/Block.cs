using UnityEngine;

public class Block : MonoBehaviour
{
    public float moveDistance = 5f; // Moves up and down from center
    public float moveSpeed = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // PingPong moves from 0 to 2 * moveDistance, so we subtract moveDistance to center it
        float offset = Mathf.PingPong(Time.time * moveSpeed, 2f * moveDistance) - moveDistance;
        transform.position = startPosition + new Vector3(0, offset, 0);
    }
}

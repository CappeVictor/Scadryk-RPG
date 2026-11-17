using UnityEngine;

public sealed class ScadrykCameraFollow : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float smoothTime = 0.18f;
    private Transform target;
    private Vector3 velocity;
    public void SetTarget(Transform value) => target = value;
    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = new Vector3(target.position.x, target.position.y, -10f);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}

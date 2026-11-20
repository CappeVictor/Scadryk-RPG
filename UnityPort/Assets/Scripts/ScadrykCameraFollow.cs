using UnityEngine;

public sealed class ScadrykCameraFollow : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float smoothTime = 0.18f;
    private Transform target;
    private Vector3 velocity;
    private float shakeUntil, shakeStrength;
    public void SetTarget(Transform value) => target = value;
    public void Shake(float strength, float duration)
    {
        if (ScadrykSettings.ReduceShake) return;
        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeUntil = Mathf.Max(shakeUntil, Time.time + duration);
    }
    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = new Vector3(target.position.x, target.position.y, -10f);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        if (ScadrykSettings.ReduceShake)
        {
            shakeStrength = 0f;
            shakeUntil = 0f;
        }
        else if (Time.time < shakeUntil)
        {
            float fade = Mathf.Clamp01((shakeUntil - Time.time) / 0.25f);
            transform.position += new Vector3(Mathf.Sin(Time.time * 67f), Mathf.Cos(Time.time * 83f), 0f) * shakeStrength * fade;
        }
        else shakeStrength = 0f;
    }
}

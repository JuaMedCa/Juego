using UnityEngine;

[DisallowMultipleComponent]
public class MenuSceneCameraMotion : MonoBehaviour
{
    [SerializeField] private Transform lookTarget;
    [SerializeField] private bool lockLookAtTarget = true;
    [SerializeField] private bool enableDrift = true;
    [SerializeField] private Vector3 driftAmplitude = new Vector3(0.08f, 0.05f, 0.04f);
    [SerializeField] private Vector3 driftSpeed = new Vector3(0.20f, 0.16f, 0.11f);

    private Vector3 baseLocalPosition;

    public Transform LookTarget
    {
        get => lookTarget;
        set => lookTarget = value;
    }

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        baseLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (enableDrift)
        {
            Vector3 driftOffset = new Vector3(
                Mathf.Sin(Time.unscaledTime * driftSpeed.x) * driftAmplitude.x,
                Mathf.Cos(Time.unscaledTime * driftSpeed.y) * driftAmplitude.y,
                Mathf.Sin(Time.unscaledTime * driftSpeed.z) * driftAmplitude.z);

            transform.localPosition = baseLocalPosition + driftOffset;
        }

        if (lockLookAtTarget && lookTarget != null)
        {
            transform.rotation = Quaternion.LookRotation(lookTarget.position - transform.position, Vector3.up);
        }
    }
}

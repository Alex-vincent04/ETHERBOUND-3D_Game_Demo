using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [SerializeField] private Transform target; // The ship's transform
    [SerializeField] private Vector3 defaultDistance = new Vector3(0f, 2f, -10f);
    [SerializeField] private float distanceDamp = 0.15f; // Lower = faster, higher = smoother

    private Transform myT;
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        myT = transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Calculate the exact point in space we want to move to
        // We multiply the target's rotation by our offset to keep the camera behind the ship's tail
        Vector3 toPos = target.position + (target.rotation * defaultDistance);

        // 2. Smoothly move the camera to that position
        Vector3 curPos = Vector3.SmoothDamp(myT.position, toPos, ref velocity, distanceDamp);
        myT.position = curPos;

        // 3. Keep the camera looking at the ship and maintain the ship's local "Up"
        // This prevents the camera from flipping weirdly during rolls
        myT.LookAt(target, target.up);
    }
}
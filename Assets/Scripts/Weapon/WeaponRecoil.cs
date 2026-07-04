using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [SerializeField] private Transform recoilPivot;
    [SerializeField] private float kickBack = 0.06f;
    [SerializeField] private float kickUp = 3.5f;
    [SerializeField] private float kickSide = 1.2f;
    [SerializeField] private float snappiness = 18f;
    [SerializeField] private float returnSpeed = 14f;
    [SerializeField] private Transform firePoint;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot; // NOU
    private Vector3 targetPos, currentPos;
    private Vector3 targetRot, currentRot;

    private void Awake()
    {
        if (recoilPivot == null) recoilPivot = transform;
        initialLocalPos = recoilPivot.localPosition;
        initialLocalRot = recoilPivot.localRotation; // NOU: salvează rotația originală
    }

    private void LateUpdate()
    {
        targetPos = Vector3.Lerp(targetPos, Vector3.zero, returnSpeed * Time.deltaTime);
        currentPos = Vector3.Lerp(currentPos, targetPos, snappiness * Time.deltaTime);

        targetRot = Vector3.Lerp(targetRot, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRot = Vector3.Lerp(currentRot, targetRot, snappiness * Time.deltaTime);

        recoilPivot.localPosition = initialLocalPos + currentPos;
        recoilPivot.localRotation = initialLocalRot * Quaternion.Euler(currentRot); // FIX: adună, nu suprascrie
    }

    public void FireKick()
    {
        float directionSign = Vector3.Dot(recoilPivot.forward, firePoint.forward) >= 0 ? 1f : -1f;
        targetPos += new Vector3(0f, 0f, -kickBack * directionSign);
        targetRot += new Vector3(
            -kickUp,
            Random.Range(-kickSide, kickSide),
            Random.Range(-kickSide * 0.4f, kickSide * 0.4f)
        );
    }
}
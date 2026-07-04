using UnityEngine;

public class Weapon1Toggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject weaponObject; // arma child pe WeaponSocket
    [SerializeField] private Animator animator;       // optional

    [Header("Animator Param (optional)")]
    [SerializeField] private string hasWeaponParam = "HasWeapon";

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    [Header("State")]
    [SerializeField] private bool hasWeapon = false;

    private int hasWeaponHash;
    private bool hasAnimatorParam;

    private void Awake()
    {
        hasWeaponHash = Animator.StringToHash(hasWeaponParam);
        hasAnimatorParam = animator != null && HasParameter(animator, hasWeaponParam);
    }

    private void Start()
    {
        ApplyStateImmediate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            hasWeapon = !hasWeapon;
            ApplyStateImmediate();
        }
    }

    private void ApplyStateImmediate()
    {
        if (weaponObject != null)
            weaponObject.SetActive(hasWeapon);

        if (hasAnimatorParam)
            animator.SetBool(hasWeaponHash, hasWeapon);
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (var p in anim.parameters)
            if (p.name == paramName) return true;
        return false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TikoWeaponFormController : MonoBehaviour
{
    [Header("Base Player Refs")]
    [SerializeField] private PlayerController25D_Anim playerController;
    [SerializeField] private GameObject tikoBaseVisual;


    [Header("Weapon Deploy (separate FBX)")]
    [SerializeField] private GameObject weaponDeployVisual;
    [SerializeField] private Animator weaponDeployAnimator;
    [SerializeField] private string deployStateName = "WeaponDeploy";
    [SerializeField] private float deployDuration = 0.6f;


    private List<Renderer> deployVisualRenderers = new List<Renderer>();


    [Header("Active Weapon")]
    [SerializeField] private GameObject activeWeapon;


    [Header("Face Screen (stays visible always)")]
    [SerializeField] private GameObject faceScreen;


    [Header("Base Animator (optional bool)")]
    [SerializeField] private Animator baseAnimator;
    [SerializeField] private string hasWeaponParam = "HasWeapon";


    [Header("Input (Debug Only)")]
    [SerializeField] private bool allowManualToggle = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;



    [Header("Flash VFX")]
    [SerializeField] private FlashVFXController flashVFXController;
    [SerializeField] private float swapHoldExtra = 0.03f;
    [SerializeField] private float swapCoverDuration = 0.05f;


    private int hasWeaponHash;
    private bool hasAnimatorParam;
    private bool hasWeapon;
    private bool isDeploying;
    private Coroutine deployRoutine;


    private List<Renderer> baseVisualRenderers = new List<Renderer>();


    private void Awake()
    {
        hasWeaponHash = Animator.StringToHash(hasWeaponParam);
        hasAnimatorParam = baseAnimator != null && HasParameter(baseAnimator, hasWeaponParam);


        CacheBaseVisualRenderers();


        deployVisualRenderers.Clear();
        deployVisualRenderers.AddRange(weaponDeployVisual.GetComponentsInChildren<Renderer>(true));
        SetRenderersEnabled(deployVisualRenderers, false);


        weaponDeployVisual.SetActive(true);
        activeWeapon.SetActive(false);
        tikoBaseVisual.SetActive(true);
        SetBaseVisualRenderersEnabled(true);
    }


    private void SetRenderersEnabled(List<Renderer> renderers, bool state)
    {
        foreach (Renderer r in renderers)
            if (r != null) r.enabled = state;
    }


    private void CacheBaseVisualRenderers()
    {
        baseVisualRenderers.Clear();


        Renderer[] allRenderers = tikoBaseVisual.GetComponentsInChildren<Renderer>(true);
        Renderer faceRenderer = faceScreen != null ? faceScreen.GetComponent<Renderer>() : null;


        foreach (Renderer r in allRenderers)
        {
            if (faceRenderer != null && r == faceRenderer)
                continue;


            baseVisualRenderers.Add(r);
        }
    }


    private void SetBaseVisualRenderersEnabled(bool state)
    {
        foreach (Renderer r in baseVisualRenderers)
        {
            if (r != null)
                r.enabled = state;
        }
    }


    private void Update()
    {
        {
            if (allowManualToggle && Input.GetKeyDown(toggleKey) && !isDeploying)
            {
                if (hasWeapon)
                    UnequipWeapon();
                else
                    EquipWeapon();
            }
        }
    }


    public void EquipWeapon()
    {
        if (hasWeapon || isDeploying) return;


        if (deployRoutine != null)
            StopCoroutine(deployRoutine);


        deployRoutine = StartCoroutine(EquipRoutine());
    }


    public void UnequipWeapon()
    {
        if (!hasWeapon || isDeploying) return;


        activeWeapon.SetActive(false);
        hasWeapon = false;


        if (hasAnimatorParam)
            baseAnimator.SetBool(hasWeaponHash, false);
    }


   private IEnumerator EquipRoutine()
    {
        isDeploying = true;
        if (playerController != null)
            playerController.IsBusy = true;


        float totalDuration = 0.08f + 0.12f + deployDuration + 0.08f + 0.12f;


        if (flashVFXController != null)
        {
            flashVFXController.StartContinuousShake(totalDuration);
            flashVFXController.StartSustainedScreenFlash(totalDuration);
            flashVFXController.PlayFlashInSound();
            flashVFXController.PlayTransformSound(deployDuration);
        }


        yield return StartCoroutine(FlashAndSwap(hideBase: true));


        SetRenderersEnabled(deployVisualRenderers, true);
        weaponDeployAnimator.Play(deployStateName, 0, 0f);


        yield return new WaitForSeconds(deployDuration);


        yield return StartCoroutine(FlashAndSwap(hideBase: false));


        if (flashVFXController != null)
            flashVFXController.PlayFlashOutSound();


        SetRenderersEnabled(deployVisualRenderers, false);
        activeWeapon.SetActive(true);


        hasWeapon = true;
        isDeploying = false;
        if (playerController != null)
            playerController.IsBusy = false;


        if (hasAnimatorParam)
            baseAnimator.SetBool(hasWeaponHash, true);
    }


    private IEnumerator FlashAndSwap(bool hideBase)
    {
        float riseTime = flashVFXController != null ? flashVFXController.ScreenFlashRise : 0.05f;


        if (flashVFXController != null)
            yield return StartCoroutine(flashVFXController.PlayFullFlash());


        yield return new WaitForSeconds(riseTime + swapHoldExtra);


        SetBaseVisualRenderersEnabled(!hideBase);


        yield return new WaitForSeconds(swapCoverDuration);
    }


    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (var p in anim.parameters)
            if (p.name == paramName) return true;
        return false;
    }
}
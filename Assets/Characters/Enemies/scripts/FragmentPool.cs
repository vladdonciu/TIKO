using UnityEngine;
using System.Collections.Generic;

public class FragmentPool : MonoBehaviour
{
    public static FragmentPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 60;
    [SerializeField] private Color defaultFragmentColor = new Color(1f, 0f, 0.19f);

    private Queue<GameObject> availableFragments = new Queue<GameObject>();
    private Material sharedFragmentMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        sharedFragmentMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewFragment();
        }
    }

    private GameObject CreateNewFragment()
    {
        GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragment.name = "PooledFragment";
        fragment.transform.SetParent(transform);

        Renderer fragRenderer = fragment.GetComponent<Renderer>();
        fragRenderer.material = new Material(sharedFragmentMaterial);

        fragment.AddComponent<Rigidbody>();
        fragment.AddComponent<FragmentFade>();

        fragment.SetActive(false);
        availableFragments.Enqueue(fragment);

        return fragment;
    }

    public GameObject GetFragment()
    {
        if (availableFragments.Count == 0)
        {
            CreateNewFragment();
        }

        GameObject fragment = availableFragments.Dequeue();
        fragment.SetActive(true);

        return fragment;
    }

    public void ReturnFragment(GameObject fragment)
    {
        fragment.SetActive(false);
        fragment.transform.SetParent(transform);
        availableFragments.Enqueue(fragment);
    }
}
using UnityEngine;

/// <summary>Sorts top-down actors and props by their ground position.</summary>
public sealed class ScadrykDepthSort : MonoBehaviour
{
    [SerializeField] private int sortingBias;
    private SpriteRenderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void SetBias(int bias) => sortingBias = bias;

    private void LateUpdate()
    {
        if (renderers == null) return;
        int order = 100 + sortingBias - Mathf.RoundToInt(transform.position.y * 10f);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].sortingOrder = order + i;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Sirenix.OdinInspector;

[ExecuteInEditMode]
[RequireComponent(typeof(Mesh))]
public class ObjectBoundsGradient : MonoBehaviour
{
    // private GameObject gameObject;
    private MaterialPropertyBlock _matPropBlock;
    private MeshRenderer meshRenderer;
    // private MeshFilter meshFilter;
    private Mesh mesh;

    [SerializeField] public bool ShowBounds;

    // bounds in local space not world space
    private Bounds bounds => this.mesh.bounds;
    private Vector3 bounds_Extents => bounds.extents; // bounds.size / 2
    private Vector3 bounds_Size => bounds.size;
    private Vector3 bounds_Center => bounds.center;
    private Vector3 bounds_Min => bounds.min;
    private Vector3 bounds_Max => bounds.max;

    private static readonly int BoundsMin = Shader.PropertyToID("bounds_Min");
    private static readonly int BoundsMax = Shader.PropertyToID("bounds_Max");
    private static readonly int BoundsSize = Shader.PropertyToID("bounds_Size");
    private static readonly int ID = Shader.PropertyToID("OutlineID");

    private void SetBounds()
    {
        if (_matPropBlock == null)
        {
            _matPropBlock = new MaterialPropertyBlock();
        }

        meshRenderer = GetComponent<MeshRenderer>();

        // Vector3 bounds_Min = this.mesh.bounds.min;
        // Vector3 bounds_Max = this.mesh.bounds.max;
        // Vector3 bounds_Size = this.mesh.bounds.size;
        float OutlineID = Random.value;

        _matPropBlock.SetVector(BoundsMin, bounds_Min);
        _matPropBlock.SetVector(BoundsMax, bounds_Max);
        _matPropBlock.SetVector(BoundsSize, bounds_Size);
        _matPropBlock.SetFloat(ID, OutlineID);

        meshRenderer.SetPropertyBlock(_matPropBlock);
    }
    
    private void OnTransformParentChanged()
    {
        this.mesh.RecalculateBounds();
        this.SetBounds();
    }

    private void OnDrawGizmosSelected()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3 scaledBounds_Size = new Vector3(bounds_Size.x * transform.localScale.x, bounds_Size.y * transform.localScale.y, bounds_Size.z * transform.localScale.z);
        // Draw a yellow cube at the transform position
        Gizmos.color = Color.yellow;
        if (ShowBounds)
        {           
            Gizmos.DrawWireCube(transform.position, scaledBounds_Size);
        }
    }
}
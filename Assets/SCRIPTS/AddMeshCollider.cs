using UnityEngine;

public class AddMeshColliders : MonoBehaviour
{
    [ContextMenu("Add Mesh Colliders To All Children")]
    void AddColliders()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.GetComponent<MeshCollider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }
        Debug.Log("Done! Added colliders to " + meshFilters.Length + " meshes.");
    }
}
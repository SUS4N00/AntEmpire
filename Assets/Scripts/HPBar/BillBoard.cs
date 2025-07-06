using UnityEngine;

public class BillBoard : MonoBehaviour
{
    public Transform cam;
    public Transform target;
    public Vector3 offset = new Vector3();

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main.transform;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.rotation = cam.rotation;
        transform.position = target.position + offset;
    }
}

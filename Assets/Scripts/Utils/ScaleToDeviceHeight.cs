using UnityEngine;

public class ScaleToDeviceHeight : MonoBehaviour
{
    void Start()
    {
        var myHeight = transform.GetComponent<RectTransform>().rect.height;
        var canvasHeight = transform.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.height; 
        var scale = (canvasHeight / myHeight);
        transform.localScale = new Vector3(scale, scale, transform.localScale.z);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapObjectMap : MonoBehaviour
{
    private GameObject snapObject;

    [SerializeField]
    float forwardOffSet = 0.1f;

    [SerializeField]
    Vector2 panelSize;

    private void OnCollisionEnter(Collision collision)
    {
        SnapObject _snapObject = collision.gameObject.GetComponent<SnapObject>();

        if (_snapObject != null)
            _snapObject.SnapSetting(transform.position + (transform.forward * forwardOffSet), transform.rotation, panelSize);
    }

    private void OnTriggerEnter(Collider other)
    {
        SnapObject _snapObject = other.gameObject.GetComponent<SnapObject>();

        if (_snapObject != null)
            _snapObject.SnapSetting(transform.position + (transform.forward * forwardOffSet), transform.rotation, panelSize);
    }

    private void OnTriggerExit(Collider other)
    {
        SnapObject _snapObject = other.gameObject.GetComponent<SnapObject>();

        if (_snapObject != null)
            _snapObject.ScaleRevert();
    }
}

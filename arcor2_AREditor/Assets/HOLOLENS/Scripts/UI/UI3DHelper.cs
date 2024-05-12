/*
 Author: Josef Kníže
*/

using UnityEngine;

public static class UI3DHelper
{
    public static void PlaceOnClosestCollisionPoint(HInteractiveObject interactiveObject, Vector3 source, Transform movedObject)
    {
        var collidCubeGameObject = interactiveObject.transform.Find("Visual").Find("CollidCube");
        var collider = collidCubeGameObject.GetComponent<BoxCollider>();
        var wasColliderActive = collider.transform.gameObject.activeSelf;
        collider.transform.gameObject.SetActive(true);
        var closestPoint = collider.ClosestPoint(source);
        collider.transform.gameObject.SetActive(wasColliderActive);
        movedObject.position = closestPoint;
    }

    public static void PlaceOnClosestCollisionPointInMiddle(HInteractiveObject interactiveObject, Vector3 source, Transform movedObject)
    {
        var collidCubeGameObject = interactiveObject.transform.Find("Visual").Find("CollidCube");
        var collider = collidCubeGameObject.GetComponent<BoxCollider>();
        var wasColliderActive = collider.transform.gameObject.activeSelf;
        collider.transform.gameObject.SetActive(true);

        source.y = collider.bounds.center.y;

        var closestPoint = collider.ClosestPoint(source);
        collider.transform.gameObject.SetActive(wasColliderActive);
        movedObject.position = closestPoint;
    }

    public static void PlaceOnClosestCollisionPointAtBottom(HInteractiveObject interactiveObject, Vector3 source, Transform movedObject)
    {
        var collidCubeGameObject = interactiveObject.transform.Find("Visual").Find("CollidCube");
        var collider = collidCubeGameObject.GetComponent<BoxCollider>();
        var wasColliderActive = collider.transform.gameObject.activeSelf;
        collider.transform.gameObject.SetActive(true);

        //source.y = collider.bounds.center.y;
        source.y = collider.bounds.min.y;

        var closestPoint = collider.ClosestPoint(source);
        collider.transform.gameObject.SetActive(wasColliderActive);
        movedObject.position = closestPoint;
    }
}

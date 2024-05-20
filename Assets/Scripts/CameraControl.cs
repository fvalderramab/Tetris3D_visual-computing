using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private float distanceToTarget = 15;
    private Vector3 previousPosition;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Cuando detecta el click pone esta como la posición previa para luego comparar con la nueva y detectar la dirección a la que se quiere mover la cámara
        {
            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0)) // Si está arrastrando el click, primero se revisa la dirección y luego permite rotar solo en X y Y (moviendose en Z) alrededor del centro del juego
        {
            Vector3 newPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;
            // Se establece que lo máximo a rotar de extremo a extremo de la pantalla son 180 grados vertical y horizontal
            float rotationAroundYAxis = -direction.x * 180;
            float rotationAroundXAxis = direction.y * 180;
            
            cam.transform.position = new Vector3((float)1.5, 6, (float)1.5);

            cam.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
            cam.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

            cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));

            previousPosition = newPosition;
        }
    }
}
using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    public Transform player; //get player component
    public Transform planet; //get planet component
    public float sensitivity = 5f; //camera rotation sensitivity
    public float distanceFromPlayer = 5f; //distance from player
    public float minY = 10f; 
    public float maxY = 80f;

    public float zoomSpeed = 2f; //zoom speed
    public float smoothZoomSpeed = 5f; //smooth zoom speed
    public float minDistance = 3f; 
    public float maxDistance = 10f; 

    private float rotationX = 0f;  
    private float rotationY = 45f;

    private float targetDistanceFromPlayer;  //target distance for smooth zooming
    private float currentDistanceFromPlayer;  //current distance for smooth zooming

    void Start()
    {
        targetDistanceFromPlayer = distanceFromPlayer; //Set target distance to initial distance
        currentDistanceFromPlayer = distanceFromPlayer;
    }

    void Update()
    {
        if (Input.GetMouseButton(1)) 
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity; //get mouse input for X axis
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity; //get mouse input for Y axis

            rotationX += mouseX; //add mouse input to X rotation
            rotationY -= mouseY; //add mouse input to Y rotation

            rotationY = Mathf.Clamp(rotationY, minY, maxY);// clamp Y rotation to prevent flipping
        }

        float scrollInput = Input.GetAxis("Mouse ScrollWheel"); //get scroll wheel input
        targetDistanceFromPlayer -= scrollInput * zoomSpeed;  //adjust target distance based on scroll input
        targetDistanceFromPlayer = Mathf.Clamp(targetDistanceFromPlayer, minDistance, maxDistance); //clamp target distance to allow rotation around the player
        currentDistanceFromPlayer = Mathf.Lerp(currentDistanceFromPlayer, targetDistanceFromPlayer, smoothZoomSpeed * Time.deltaTime); // smoothly adjust current distance to target distance using lerp (liner interpolation) for smoothing

        Vector3 gravityDirection = (player.position - planet.position).normalized; //get gravity direction

        Quaternion playerRotation = Quaternion.FromToRotation(Vector3.up, gravityDirection); //get player rotation
        Quaternion finalRotation = playerRotation * Quaternion.Euler(rotationY, rotationX, 0); //get final rotation

        Vector3 cameraOffset = finalRotation * Vector3.back * currentDistanceFromPlayer; //get camera offset
        transform.position = player.position + cameraOffset; //set camera position based of the players position and camera offset

        transform.LookAt(player, gravityDirection); //look at the player
    }
}

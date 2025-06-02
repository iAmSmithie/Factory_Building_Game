using UnityEngine;

public class Movement : MonoBehaviour
{
    public float speed = 5.0f;

    public Transform cameraTransform;
    public Transform planet;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); //get the rigidbody component
        rb.useGravity = false; //disable plane gravity
    }

    void Update()
    {
        //functions for movement
        applyGravity();
        movePlayer();
    }
    void applyGravity()
    {
        Vector3 gravityDirection = (planet.position - transform.position).normalized; //get gravity direction from the player to the planet
        rb.AddForce(gravityDirection * 9.81f, ForceMode.Acceleration); //apply the gravity force to the player

        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, -gravityDirection) * transform.rotation; //get the target rotation based on the gravity direction
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f); //rotate the player towards the gravity direction

        //Debug.DrawRay(transform.position, (planet.position - transform.position).normalized * 10, Color.red);
        //Debug.Log($"Gravity Direction: {(planet.position - transform.position).normalized}");

    }
    void movePlayer()
    {
        float moveX = Input.GetAxis("Horizontal"); //get input for X axis
        float moveZ = Input.GetAxis("Vertical"); //get input for Z axis

        Vector3 gravityDirection = (planet.position - transform.position).normalized; //get gravity direction
        Vector3 forward = cameraTransform.forward; //get forward direction from camera
        Vector3 right = cameraTransform.right; //get right direction from camera

        forward = Vector3.ProjectOnPlane(forward, transform.up).normalized; //project the forward direction on the plane of the player
        right = Vector3.ProjectOnPlane(right, transform.up).normalized; //project the right direction on the plane of the player

        Vector3 move = (forward * moveZ + right * moveX) * speed; //calculate the move direction
        rb.linearVelocity = move + Vector3.Project(rb.linearVelocity, gravityDirection); //set the velocity of the player
    }
}
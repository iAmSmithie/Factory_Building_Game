using UnityEngine;

public class PlanetGravity : MonoBehaviour
{
    public float gravity = 9.81f;

    public void Attract(Transform player)
    {
        //set gravity direction
        Vector3 gravityDirection = (transform.position - player.position).normalized;
        player.GetComponent<Rigidbody>().AddForce(gravityDirection * gravity, ForceMode.Acceleration);

        //rotate the player around the planet on movement
        Quaternion targetRotation = Quaternion.FromToRotation(player.up, -gravityDirection) * player.rotation;
        player.rotation = Quaternion.Slerp(player.rotation, targetRotation, Time.deltaTime * 5f);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Attract(GameObject.Find("Player").transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    public float speed = 5f;
    public float max_speed = 7f;
    private Rigidbody2D rb;
    void Start()
    {
        Application.targetFrameRate = 30;
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        float inp_x = Input.GetAxis("Horizontal");
        float inp_y = Input.GetAxis("Vertical");
        
        Vector2 movement = speed * new Vector2(inp_x, inp_y);
        
        rb.linearVelocity = movement;
    }
}

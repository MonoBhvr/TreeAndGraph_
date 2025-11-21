using UnityEngine;

public class Treat_Test : MonoBehaviour
{
    public float timer;

    private Material _m;
    private float _duration = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _m = GetComponent<SpriteRenderer>().material;
        _duration = _m.GetFloat("_duration");
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        _m.SetFloat("_timer", timer);
        if (timer > _duration + 3)
        {
            Destroy(gameObject);
        }
    }
}

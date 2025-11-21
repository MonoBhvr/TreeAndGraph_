using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PathFollower), typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float speed = 5f;
    public Transform target;
    public float find_distance = 10f;
    public bool found_player = false;
    public GameObject Threat;

    private PathFollower pathFollower;
    private Rigidbody2D rb;

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        rb = GetComponent<Rigidbody2D>();
        pathFollower.SetTarget(target);
        StartCoroutine(ThreatBehaviour());
    }

    void FixedUpdate()
    {
        if (found_player)
        {
            Vector2 moveDir = pathFollower.GetDesiredVelocity(speed).normalized;
            rb.linearVelocity = moveDir * speed;
        }
        else if (pathFollower.GetRemainingDistance() <= find_distance)
            found_player = true;
    }
    
    private IEnumerator ThreatBehaviour()
    {
        while (true)
        {
            float interval = 0.3f; // 초기 간격
            for (int i = 0; i < 5; i++) // 5-6번 반복
            {
                // 유리함수 그래프에 따라 간격 조정 (예: 1 / (i + 1))
                interval = 1.23f / (i + 2) - 0.124f;

                GameObject i_ = Instantiate(Threat, transform.position, Quaternion.identity);
                i_.GetComponent<Rigidbody2D>().AddForce(-rb.linearVelocity * 0.1f, ForceMode2D.Impulse);
                i_.transform.localScale = Vector3.one * Random.Range(2.5f, 4.0000f);

                // 간격만큼 대기
                yield return new WaitForSeconds(interval);
            }

            // 일정 시간 멈춤
            yield return new WaitForSeconds(Random.Range(1.5f, 3f));
        }
    }
}
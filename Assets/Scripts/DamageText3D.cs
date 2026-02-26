using TMPro;
using UnityEngine;

public class DamageText3D : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float lifetime = 0.7f;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float randomX = 0.25f;

    private Camera cam;
    private float t;
    private Vector3 drift;

    public void Init(int damage, Camera camera)
    {
        if (text == null) text = GetComponent<TMP_Text>();
        text.text = damage.ToString();

        cam = camera != null ? camera : Camera.main;

        drift = new Vector3(Random.Range(-randomX, randomX), 0f, Random.Range(-randomX, randomX));
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        t += Time.deltaTime;

        transform.position += (Vector3.up * floatSpeed + drift) * Time.deltaTime;

        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = transform.position - cam.transform.position;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        if (t >= lifetime) Destroy(gameObject);
    }
}

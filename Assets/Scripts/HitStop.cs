using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    private Coroutine routine;

    public void DoHitStop(float duration, float timeScale) //control in the code
    {
        if(routine != null) StopCoroutine(routine);
        routine = StartCoroutine(HitStopRoutine(duration, timeScale));
    }

    public void DoHitStopEvent(float duration) //Animation event
    {
        DoHitStop(duration, 0f);
    }
    
    private IEnumerator HitStopRoutine(float duration, float timeScale)
    {
        float prev = Time.timeScale;
        Time.timeScale = timeScale;
        
        yield return new WaitForSecondsRealtime(duration);
        
        Time.timeScale = prev;
        routine = null;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

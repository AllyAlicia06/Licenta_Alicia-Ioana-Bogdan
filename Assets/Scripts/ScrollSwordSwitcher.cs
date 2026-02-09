using System.Collections.Generic;
using UnityEngine;

public class ScrollSwordSwitcher : MonoBehaviour
{
    [SerializeField] private List<GameObject> swords = new();

    [Header("Scroll")] 
    [SerializeField] private bool wrap = true;
    [SerializeField] private float scrollStepCooldown = 0.12f;
    [SerializeField] private bool invertScroll = true;
    
    [Header("Startup")]
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool deactivateAllOnStart = true;
    
    private int currentIndex = -1;
    private float nextAllowedTime;

    private void Awake()
    {
        if (deactivateAllOnStart)
            for (int i = 0; i < swords.Count; i++)
                if(swords[i] != null) swords[i].SetActive(false);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (swords.Count == 0) return;
        startIndex = Mathf.Clamp(startIndex, 0, swords.Count - 1);
        Equip(startIndex);
    }

    // Update is called once per frame
    void Update()
    {
        if (swords.Count == 0) return;
        if (Time.unscaledTime < nextAllowedTime) return;
        
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;
        
        if(invertScroll) scroll =  -scroll;
        
        int dir = scroll > 0 ? 1 : -1;
        int next = GetNextIndex(dir);
        Equip(next);
        
        nextAllowedTime = Time.unscaledTime + scrollStepCooldown;
    }

    private int GetNextIndex(int dir)
    {
        int count = swords.Count;
        int next = currentIndex + dir;
        
        if(wrap) return (next % count + count) % count;
        
        return Mathf.Clamp(next, 0, count - 1);
    }

    private void Equip(int index)
    {
        if (index < 0 || index >= swords.Count) return;
        if (index == currentIndex) return;
        
        if(currentIndex >= 0 && currentIndex < swords.Count && swords[currentIndex] != null)
            swords[currentIndex].SetActive(false);
        
        var sword = swords[index];
        if (sword == null) return;
        
        sword.SetActive(true);
        currentIndex = index;
    }
}

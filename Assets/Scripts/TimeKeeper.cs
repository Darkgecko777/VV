using UnityEngine;
using UnityEngine.Events;

public class TimeKeeper : MonoBehaviour
{
    [SerializeField] private float tickRate = 0.5f; // Time between ticks in seconds
    public UnityEvent<int> OnTick = new UnityEvent<int>();
    private float timer = 0f;
    private int currentTick = 0;
    private bool isRunning = false;

    void Start()
    {
        StartCombat();
    }

    void Update()
    {
        if (!isRunning) return;

        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            currentTick++;
            OnTick.Invoke(currentTick);
            timer = 0f;
        }
    }

    public void StartCombat()
    {
        isRunning = true;
    }

    public void StopCombat()
    {
        Debug.Log("TimeKeeper: Stopping combat");
        isRunning = false;
    }

    public float GetTickRate()
    {
        return tickRate;
    }
}
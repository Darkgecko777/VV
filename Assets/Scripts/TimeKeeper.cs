using UnityEngine;
using UnityEngine.Events;

public class TimeKeeper : MonoBehaviour
{
    [SerializeField] private float tickRate = 0.8f; // Default: Standard (0.8s)
    private float[] tickRates = { 0.8f, 0.4f, 0.2f }; // Standard, Fast, Fastest
    private int currentSpeedIndex = 0; // 0 = Standard, 1 = Fast, 2 = Fastest
    private int currentTick = 0;
    private float timer = 0f;
    public UnityEvent<int> OnTick = new UnityEvent<int>();

    public void SetSpeed(int speedIndex)
    {
        if (speedIndex >= 0 && speedIndex < tickRates.Length)
        {
            currentSpeedIndex = speedIndex;
            tickRate = tickRates[speedIndex];
        }
    }

    public float GetTickRate()
    {
        return tickRate;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            currentTick++;
            OnTick.Invoke(currentTick);
            timer = 0f;
        }
    }

    public int GetCurrentTick()
    {
        return currentTick;
    }
}
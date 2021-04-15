using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class AsyncComputeBuffer
{
    public static implicit operator ComputeBuffer(AsyncComputeBuffer buffer) => buffer.m_buffer;

    public int Count => m_buffer.count;

    public bool HasError => m_request.hasError;

    private readonly ComputeBuffer m_buffer;
    private AsyncGPUReadbackRequest m_request;
    private bool m_retrievingData;

    public AsyncComputeBuffer(int count, int stride, ComputeBufferType type = ComputeBufferType.Default)
    {
        m_buffer = new ComputeBuffer(count, stride, type);
    }

    public void RequestData()
    {
        m_request = AsyncGPUReadback.Request(m_buffer);
        m_retrievingData = true;
    }

    public void RequestData(int count, int offset = 0)
    {
        m_request = AsyncGPUReadback.Request(m_buffer, count * m_buffer.stride, offset * m_buffer.stride);
        m_retrievingData = true;
    }

    public bool IsDataAvailable(bool blocking = false)
    {
        if (!m_retrievingData)
        {
            return false;
        }

        if (blocking)
        {
            m_request.WaitForCompletion();
        }

        if (HasError)
        {
            m_retrievingData = false;
        }

        return m_request.done;
    }

    public NativeArray<T> GetData<T>() where T : struct
    {
        NativeArray<T> data = m_request.GetData<T>();
        m_retrievingData = false;

        return data;
    }

    public void SetCounterValue(uint counterValue) => m_buffer.SetCounterValue(counterValue);

    public void Release() => m_buffer.Release();
}

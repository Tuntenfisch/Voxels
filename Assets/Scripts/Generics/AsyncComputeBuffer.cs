using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tuntenfisch.Generics
{
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
            RequestData(m_buffer.count);
        }

        public void RequestData(int count)
        {
            if (count > m_buffer.count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Requested number of elements is larger than the total buffer's number of elements!");
            }

            m_request = AsyncGPUReadback.Request(m_buffer, count * m_buffer.stride, 0);
            m_retrievingData = true;
        }

        public bool IsDataAvailable()
        {
            if (!m_retrievingData)
            {
                return false;
            }

            m_retrievingData = !HasError;

            return m_request.done;
        }

        public NativeArray<T> GetData<T>() where T : struct
        {
            if (HasError)
            {
                throw new InvalidOperationException("An error was encountered during readback! Retrieving the data is not possible!");
            }

            NativeArray<T> data = m_request.GetData<T>();
            m_retrievingData = false;

            return data;
        }

        public void SetCounterValue(uint counterValue) => m_buffer.SetCounterValue(counterValue);

        public void Release() => m_buffer.Release();
    }
}
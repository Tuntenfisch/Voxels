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
        public int Stride => m_buffer.stride;
        public bool HasError => m_request.hasError;
        public bool ReadbackInProgress => (m_flags & AsyncComputeBufferFlags.ReadbackInProgress) == AsyncComputeBufferFlags.ReadbackInProgress;

        private readonly ComputeBuffer m_buffer;
        private AsyncGPUReadbackRequest m_request;
        private int m_requestedCount;
        private AsyncComputeBufferFlags m_flags;

        public AsyncComputeBuffer(int count, int stride, ComputeBufferType type = ComputeBufferType.Default) => m_buffer = new ComputeBuffer(count, stride, type);

        public void StartReadback() => StartReadback(m_buffer.count);

        public void StartReadback(int count)
        {
            ValidateCountAndStateForDataRequest(count);
            m_requestedCount = count;
            m_request = AsyncGPUReadback.Request(m_buffer, count * m_buffer.stride, 0);
            m_flags |= AsyncComputeBufferFlags.ReadbackInProgress;
        }

        public void StartReadbackNonAlloc<T>(ref NativeArray<T> array) where T : struct => StartReadbackNonAlloc(ref array, m_buffer.count);

        public void StartReadbackNonAlloc<T>(ref NativeArray<T> array, int count) where T : struct
        {
            ValidateCountAndStateForDataRequest(count);

            if (!array.IsCreated)
            {
                throw new ObjectDisposedException($"Parameter {nameof(array)} is disposed.");
            }

            if (count > array.Length)
            {
                throw new ArgumentException($"Length of parameter {nameof(array)} is too small to store the readback.");
            }

            m_requestedCount = count;
            m_request = AsyncGPUReadback.RequestIntoNativeArray(ref array, m_buffer, count * m_buffer.stride, 0);
            m_flags |= AsyncComputeBufferFlags.ReadbackInProgress;
        }

        public bool IsDataAvailable()
        {
            if ((m_flags & AsyncComputeBufferFlags.ReadbackInProgress) != AsyncComputeBufferFlags.ReadbackInProgress)
            {
                return false;
            }

            return m_request.done;
        }

        public int EndReadback()
        {
            if ((m_flags & AsyncComputeBufferFlags.ReadbackInProgress) != AsyncComputeBufferFlags.ReadbackInProgress)
            {
                throw new InvalidOperationException($"No readback is currently in progress. Did you forget to start one?");
            }

            m_request.WaitForCompletion();
            m_flags &= ~AsyncComputeBufferFlags.ReadbackInProgress;

            return m_requestedCount;
        }

        public NativeArray<T> GetData<T>() where T : struct
        {
            if (HasError)
            {
                throw new InvalidOperationException("An error was encountered during readback. Retrieving the data is not possible.");
            }

            return m_request.GetData<T>();
        }

        public void SetCounterValue(uint counterValue) => m_buffer.SetCounterValue(counterValue);

        public void Release() => m_buffer.Release();

        private void ValidateCountAndStateForDataRequest(int count)
        {
            if ((m_flags & AsyncComputeBufferFlags.ReadbackInProgress) == AsyncComputeBufferFlags.ReadbackInProgress)
            {
                throw new InvalidOperationException($"A readback is already in progress. Did you forget to call {nameof(EndReadback)}?");
            }

            if (count > m_buffer.count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, $"Parameter {nameof(count)} is larger than the buffer's total number of elements.");
            }
        }

        [Flags]
        private enum AsyncComputeBufferFlags
        {
            ReadbackInProgress = 1
        }
    }
}
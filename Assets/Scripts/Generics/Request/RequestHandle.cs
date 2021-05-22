namespace Tuntenfisch.Generics.Request
{
    public class RequestHandle
    {
        public bool Canceled => m_canceled;

        private readonly IRequest m_request;
        private bool m_canceled;

        public RequestHandle(IRequest request)
        {
            m_request = request;
            m_canceled = false;
        }

        public void Cancel()
        {
            if (m_request != null)
            {
                m_request.Cancel();
            }
            m_canceled = true;
        }
    }
}
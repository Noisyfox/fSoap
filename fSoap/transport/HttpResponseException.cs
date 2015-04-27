using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.transport
{
    public class HttpResponseException : IOException
    {
        private int statusCode;
        private List<HeaderProperty> responseHeaders;


        public HttpResponseException(int statusCode)
        {
            this.statusCode = statusCode;
        }

        public HttpResponseException(String detailMessage, int statusCode)
            : base(detailMessage)
        {
            this.statusCode = statusCode;
        }

        public HttpResponseException(String detailMessage, int statusCode, List<HeaderProperty> responseHeaders)
            : base(detailMessage)
        {
            this.statusCode = statusCode;
            this.responseHeaders = responseHeaders;
        }

        public HttpResponseException(String message, Exception cause, int statusCode)
            : base(message, cause)
        {
            this.statusCode = statusCode;
        }

        public HttpResponseException(Exception cause, int statusCode)
            : base(null, cause)
        {
            this.statusCode = statusCode;
        }

        /**
         * Returns the unexpected Http response code
         *
         * @return response code
         */

        public int getStatusCode()
        {
            return statusCode;
        }

        /**
         * Returns all http headers from this response
         *
         * @return response code
         */

        public List<HeaderProperty> getResponseHeaders()
        {
            return responseHeaders;
        }
    }
}

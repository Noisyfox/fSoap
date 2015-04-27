using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.transport
{
    public class ServiceConnectionSE : ServiceConnection
    {
        private HttpClient client;
        private HttpRequestMessage request;
        private Task<HttpResponseMessage> responseTask;
        private HttpResponseMessage response;

        //private HttpWebRequest connection;
        //private HttpWebResponse response;
        private Stream inStream;
        private int timeout;
        private int fixedLength = -1;
        private MediaTypeHeaderValue contentType;
        

        private void checkRespond()
        {
            if (response == null)
            {
                if (!responseTask.IsCompleted)
                {
                    if (!responseTask.Wait(timeout))
                    {
                        throw new TimeoutException();
                    }
                }
                response = responseTask.Result;
            }
        }

        /**
     * Constructor taking the url to the endpoint for this soap communication
     * @param url the url to open the connection to.
     * @throws IOException
     */

        public ServiceConnectionSE(String url)
            : this(null, url, ServiceConnection.DEFAULT_TIMEOUT)
        {
        }

        public ServiceConnectionSE(IWebProxy proxy, String url)
            : this(proxy, url, ServiceConnection.DEFAULT_TIMEOUT)
        {

        }

        /**
     * Constructor taking the url to the endpoint for this soap communication
     * @param url the url to open the connection to.
     * @param timeout the connection and read timeout for the http connection in milliseconds
     * @throws IOException                            // 20 seconds
     */

        public ServiceConnectionSE(String url, int timeout)
            : this(null, url, timeout)
        {
        }

        public ServiceConnectionSE(IWebProxy proxy, String url, int timeout)
        {
            this.timeout = timeout;
            HttpClientHandler handler = new HttpClientHandler();
            handler.Proxy = proxy;
            client = new HttpClient(handler);
            request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);

            //connection = WebRequest.CreateHttp(url);
            //connection.Proxy = proxy;
            //connection.ContinueTimeout
            //connection.setUseCaches(false);
            //connection.setDoOutput(true);
            //connection.setDoInput(true);
            //connection.setConnectTimeout(timeout);
            //connection.setReadTimeout(timeout);
            // even if we connect fine we want to time out if we cant read anything..
        }

        public override void connect()
        {
            responseTask = client.SendAsync(request);
        }

        public override void disconnect()
        {
            client.Dispose();
        }

        public override List<HeaderProperty> getResponseProperties()
        {
            checkRespond();

            List<HeaderProperty> prop1 = (from httpResponseHeader in response.Headers from s in httpResponseHeader.Value select new HeaderProperty(httpResponseHeader.Key, s)).ToList();
            List<HeaderProperty> prop2 = (from httpContentHeader in response.Content.Headers from s in httpContentHeader.Value select new HeaderProperty(httpContentHeader.Key, s)).ToList();

            prop1.AddRange(prop2);

            return prop1;
        }

        public override int getResponseCode()
        {
            checkRespond();

            return Convert.ToInt32(response.StatusCode);
        }

        public override void setRequestProperty(string propertyName, string value)
        {
            string low = propertyName.ToLower();
            if ("content-type".Equals(low))
            {
                contentType = MediaTypeHeaderValue.Parse(value);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(propertyName, value);
            }
        }

        public override void setRequestMethod(string requestMethodType)
        {
            request.Method = new HttpMethod(requestMethodType);
        }

        /**
         * If the length of a HTTP request body is known ahead, sets fixed length 
         * to enable streaming without buffering. Sets after connection will cause an exception.
         *
         * @param contentLength the fixed length of the HTTP request body
         * @see http://developer.android.com/reference/java/net/HttpURLConnection.html
         **/

        public override void setFixedLengthStreamingMode(int contentLength)
        {
            fixedLength = contentLength;
        }

        public override void setChunkedStreamingMode()
        {
            // Do nothing
        }

        /*public override Stream openOutputStream()
        {
            if (outStream != null)
            {
                return outStream;
            }

            outStream = fixedLength > 0 ? new MemoryStream(fixedLength) : new MemoryStream();
            HttpContent metaDataContent = new StreamContent(outStream);
            request.Content = metaDataContent;
            connect();

            return outStream;
        }*/

        public override void sendData(byte[] data)
        {
            //HttpContent content = new ByteArrayContent((byte[])data.Clone());
            HttpContent ctx = new StreamContent(new MemoryStream((byte[])data.Clone()));
            ctx.Headers.ContentType = contentType;
            request.Content = ctx;
            connect();
        }

        public override Stream openInputStream()
        {
            if (inStream != null)
            {
                return inStream;
            }

            checkRespond();
            Task<Stream> stream = response.Content.ReadAsStreamAsync();
            if (stream.Wait(timeout))
            {
                inStream = stream.Result;
                return inStream;
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public override Stream getErrorStream()
        {
            throw new NotImplementedException();
        }

        public override string getHost()
        {
            return request.RequestUri.Host;
        }

        public override int getPort()
        {
            return request.RequestUri.Port;
        }

        public override string getPath()
        {
            return request.RequestUri.AbsolutePath;
        }
    }
}

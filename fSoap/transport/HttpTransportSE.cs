using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using cn.noisyfox.fsoap.serialization;

namespace cn.noisyfox.fsoap.transport
{
    public class HttpTransportSE : Transport
    {

        /**
         * Creates instance of HttpTransportSE with set url
         * 
         * @param url
         *            the destination to POST SOAP data
         */

        public HttpTransportSE(String url)
            : base(null, url)
        {
        }

        /**
         * Creates instance of HttpTransportSE with set url and defines a
         * proxy server to use to access it
         * 
         * @param proxy
         * Proxy information or <code>null</code> for direct access
         * @param url
         * The destination to POST SOAP data
         */

        public HttpTransportSE(IWebProxy proxy, String url)
            : base(proxy, url)
        {
        }

        /**
         * Creates instance of HttpTransportSE with set url
         * 
         * @param url
         *            the destination to POST SOAP data
         * @param timeout
         *   timeout for connection and Read Timeouts (milliseconds)
         */

        public HttpTransportSE(String url, int timeout)
            : base(url, timeout)
        {
        }

        public HttpTransportSE(IWebProxy proxy, String url, int timeout)
            : base(proxy, url, timeout)
        {
        }

        /**
         * Creates instance of HttpTransportSE with set url
         * 
         * @param url
         *            the destination to POST SOAP data
         * @param timeout
         *   timeout for connection and Read Timeouts (milliseconds)
         * @param contentLength
         *   Content Lenght in bytes if known in advance
         */

        public HttpTransportSE(String url, int timeout, int contentLength)
            : base(url, timeout)
        {
        }

        public HttpTransportSE(IWebProxy proxy, String url, int timeout, int contentLength)
            : base(proxy, url, timeout)
        {
        }

        /**
     * set the desired soapAction header field
     * 
     * @param soapAction
     *            the desired soapAction
     * @param envelope
     *            the envelope containing the information for the soap call.
     * @throws HttpResponseException
     * @throws IOException
     * @throws XmlPullParserException
     */

        public override void call(String soapAction, SoapEnvelope envelope)
        {
            call(soapAction, envelope, null);
        }

        public override List<HeaderProperty> call(string soapAction, SoapEnvelope envelope, List<HeaderProperty> headers)
        {
            return call(soapAction, envelope, headers, null);
        }

        public override List<HeaderProperty> call(string soapAction, SoapEnvelope envelope, List<HeaderProperty> headers,
            FileOutputStream outputFile)
        {
            if (soapAction == null)
            {
                soapAction = "\"\"";
            }

            byte[] requestData = createRequestData(envelope, "UTF-8");

            requestDump = debug ? Encoding.UTF8.GetString(requestData, 0, requestData.Length) : null;
            responseDump = null;

            ServiceConnection connection = getServiceConnection();

            connection.setRequestProperty("User-Agent", USER_AGENT);
            // SOAPAction is not a valid header for VER12 so do not add
            // it
            // @see "http://code.google.com/p/ksoap2-android/issues/detail?id=67
            if (envelope.version != SoapSerializationEnvelope.VER12)
            {
                connection.setRequestProperty("SOAPAction", soapAction);
            }

            if (envelope.version == SoapSerializationEnvelope.VER12)
            {
                connection.setRequestProperty("Content-Type", CONTENT_TYPE_SOAP_XML_CHARSET_UTF_8);
            }
            else
            {
                connection.setRequestProperty("Content-Type", CONTENT_TYPE_XML_CHARSET_UTF_8);
            }

            // this seems to cause issues so we are removing it
            //connection.setRequestProperty("Connection", "close");
            connection.setRequestProperty("Accept-Encoding", "gzip");


            // Pass the headers provided by the user along with the call
            if (headers != null)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    HeaderProperty hp = headers[i];
                    connection.setRequestProperty(hp.getKey(), hp.getValue());
                }
            }

            connection.setRequestMethod("POST");
            sendData(requestData, connection, envelope);
            requestData = null;
            Stream input = null;
            List<HeaderProperty> retHeaders = null;
            int contentLength = 8192; // To determine the size of the response and adjust buffer size
            bool gZippedContent = false;
            bool xmlContent = false;
            int status = connection.getResponseCode();

            try
            {
                retHeaders = connection.getResponseProperties();

                for (int i = 0; i < retHeaders.Count; i++)
                {
                    HeaderProperty hp = (HeaderProperty) retHeaders[i];
                    // HTTP response code has null key
                    if (null == hp.getKey())
                    {
                        continue;
                    }

                    // If we know the size of the response, we should use the size to initiate vars
                    if (String.Equals(hp.getKey(), "content-length", StringComparison.OrdinalIgnoreCase))
                    {
                        if (hp.getValue() != null)
                        {
                            try
                            {
                                contentLength = int.Parse(hp.getValue());
                            }
                            catch (Exception nfe)
                            {
                                contentLength = 8192;
                            }
                        }
                    }


                    // Check the content-type header to see if we're getting back XML, in case of a
                    // SOAP fault on 500 codes
                    if (String.Equals(hp.getKey(), "Content-Type", StringComparison.OrdinalIgnoreCase)
                        && hp.getValue().Contains("xml"))
                    {
                        xmlContent = true;
                    }


                    // ignoring case since users found that all smaller case is used on some server
                    // and even if it is wrong according to spec, we rather have it work..
                    if (String.Equals(hp.getKey(), "Content-Encoding", StringComparison.OrdinalIgnoreCase)
                        && String.Equals(hp.getValue(), "gzip", StringComparison.OrdinalIgnoreCase))
                    {
                        gZippedContent = true;
                    }
                }

                //first check the response code....
                if (status != 200)
                {
                    //throw new IOException("HTTP request failed, HTTP status: " + status);
                    throw new HttpResponseException("HTTP request failed, HTTP status: " + status, status, retHeaders);
                }

                if (contentLength > 0)
                {
                    if (gZippedContent)
                    {
                        //input = getUnZippedInputStream(
                        //        new BufferedInputStream(connection.openInputStream(), contentLength));
                        input = getUnZippedInputStream(connection.openInputStream()); // TODO: need buffered
                    }
                    else
                    {
                        //input = new BufferedInputStream(connection.openInputStream(), contentLength);
                        input = connection.openInputStream();
                    }
                }
            }
            catch (Exception e)
            {
                //throw e;
                if (contentLength > 0)
                {
                    if (gZippedContent)
                    {
                        //input = getUnZippedInputStream(
                        //        new BufferedInputStream(connection.openInputStream(), contentLength));
                        input = getUnZippedInputStream(connection.openInputStream()); // TODO: need buffered
                    }
                    else
                    {
                        //input = new BufferedInputStream(connection.openInputStream(), contentLength);
                        input = connection.openInputStream();
                    }
                }

                if (e is HttpResponseException)
                {
                    if (!xmlContent)
                    {
                        if (debug && input != null)
                        {
                            //go ahead and read the error stream into the debug buffers/file if needed.
                            readDebug(input, contentLength, outputFile);
                        }

                        //we never want to drop through to attempting to parse the HTTP error stream as a SOAP response.
                        connection.disconnect();
                        throw e;
                    }
                }
                // * */
            }

            if (debug)
            {
                input = readDebug(input, contentLength, outputFile);
            }

            parseResponse(envelope, input, retHeaders);

            // release all resources 
            // input stream is will be released inside parseResponse
            input = null;
            //This fixes Issue 173 read my explanation here: https://code.google.com/p/ksoap2-android/issues/detail?id=173
            connection.disconnect();
            connection = null;
            return retHeaders;
        }

        protected void sendData(byte[] requestData, ServiceConnection connection, SoapEnvelope envelope)
        {
            connection.setRequestProperty("Content-Length", "" + requestData.Length);
            connection.setFixedLengthStreamingMode(requestData.Length);

            //Stream os = connection.openOutputStream();
            //os.Write(requestData, 0, requestData.Length);
            connection.sendData(requestData);
            //os.Flush();
            //os.Dispose();
        }


        protected void parseResponse(SoapEnvelope envelope, Stream input, List<HeaderProperty> returnedHeaders)

        {
            parseResponse(envelope, input);
        }

        private Stream readDebug(Stream input, int contentLength, FileOutputStream outputFile)
        {
            Stream bos;
            //if (outputFile != null) {
            //    bos = outputFile;
            //} else {
            // If known use the size if not use default value
            bos = new MemoryStream((contentLength > 0) ? contentLength : 256*1024);
            //}

            byte[] buf = new byte[256];

            while (true)
            {
                int rd = input.Read(buf, 0, 256);
                if (rd == 0)
                {
                    break;
                }
                bos.Write(buf, 0, rd);
            }

            bos.Flush();
            if (bos is MemoryStream)
            {
                buf = ((MemoryStream) bos).ToArray();
            }
            bos = null;
            responseDump = Encoding.UTF8.GetString(buf, 0, buf.Length);
            input.Dispose();

            //if (outputFile != null) {
            //   return new FileInputStream(outputFile);
            //} else {
            return new MemoryStream(buf);
            //}
        }

        private Stream getUnZippedInputStream(Stream inputStream)
        {
            GZipStream s = inputStream as GZipStream ?? new GZipStream(inputStream, CompressionMode.Decompress);

            return s;
        }

        public override ServiceConnection getServiceConnection()
        {
            return new ServiceConnectionSE(proxy, url, timeout);
        }
    }
}

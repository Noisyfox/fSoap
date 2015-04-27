using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using cn.noisyfox.fxml.io;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fsoap.transport
{
    public abstract class Transport
    {

        /**
     * Added to enable web service interactions on the emulator to be debugged
     * with Fiddler2 (Windows) but provides utility for other proxy
     * requirements.
     */
        protected IWebProxy proxy;
        protected String url;
        protected int timeout = ServiceConnection.DEFAULT_TIMEOUT;
        /** Set to true if debugging */
        public bool debug;
        /** String dump of request for debugging. */
        public String requestDump;
        /** String dump of response for debugging */
        public String responseDump;
        private String xmlVersionTag = "";

        protected const String CONTENT_TYPE_XML_CHARSET_UTF_8 = "text/xml;charset=utf-8";
        protected const String CONTENT_TYPE_SOAP_XML_CHARSET_UTF_8 = "application/soap+xml;charset=utf-8";
        protected const String USER_AGENT = "ksoap2-android/2.6.0+";

        private int bufferLength = ServiceConnection.DEFAULT_BUFFER_SIZE;

        private Dictionary<object, object> prefixes = new Dictionary<object, object>();


        public Dictionary<object, object> getPrefixes()
        {
            return prefixes;
        }

        public Transport()
        {
        }

        public Transport(String url) : this(null, url)
        {

        }

        public Transport(String url, int timeout)
        {
            this.url = url;
            this.timeout = timeout;
        }

        public Transport(String url, int timeout, int bufferLength)
        {
            this.url = url;
            this.timeout = timeout;
            this.bufferLength = bufferLength;
        }

        /**
         * Construct the transport object
         * 
         * @param proxy
         *            Specifies the proxy server to use for accessing the web
         *            service or <code>null</code> if a direct connection is
         *            available
         * @param url
         *            Specifies the web service url
         * 
         */

        public Transport(IWebProxy proxy, String url)
        {
            this.proxy = proxy;
            this.url = url;
        }

        public Transport(IWebProxy proxy, String url, int timeout)
        {
            this.proxy = proxy;
            this.url = url;
            this.timeout = timeout;
        }

        public Transport(IWebProxy proxy, String url, int timeout, int bufferLength)
        {
            this.proxy = proxy;
            this.url = url;
            this.timeout = timeout;
            this.bufferLength = bufferLength;
        }

        /**
     * Sets up the parsing to hand over to the envelope to deserialize.
     */

        protected void parseResponse(SoapEnvelope envelope, Stream inputStream)
        {
            XmlPullParser xp = new FXmlParser();
            xp.setFeature(XmlPullParser.FEATURE_PROCESS_NAMESPACES, true);
            xp.setInput(inputStream, null);
            envelope.parse(xp);
            /*
         * Fix memory leak when running on android in strict mode. Issue 133
         */
            inputStream.Dispose();
        }

        /**
     * Serializes the request.
     */

        protected byte[] createRequestData(SoapEnvelope envelope, String encoding)
        {
            Encoding e = Encoding.GetEncoding(encoding);
            MemoryStream bos = new MemoryStream(bufferLength);

            byte[] tagBytes = Encoding.UTF8.GetBytes(xmlVersionTag);
            bos.Write(tagBytes, 0, tagBytes.Length);
            XmlSerializer xw = new FXmlSerializer();

            xw.setOutput(bos, e);
            foreach (KeyValuePair<object, object> keyValuePair in prefixes)
            {
                String key = (String)keyValuePair.Key;
                xw.setPrefix(key, (String)keyValuePair.Value);
            }
            envelope.write(xw);
            xw.flush();
            bos.WriteByte((byte) '\r');
            bos.WriteByte((byte) '\n');
            bos.Flush();
            byte[] result = bos.ToArray();
            xw = null;
            bos = null;
            return result;
        }

        /**
     * Serializes the request.
     */

        protected byte[] createRequestData(SoapEnvelope envelope)
        {
            return createRequestData(envelope, null);
        }

        /**
     * Set the target url.
     * 
     * @param url
     *            the target url.
     */

        public void setUrl(String url)
        {
            this.url = url;
        }

        public String getUrl()
        {
            return url;
        }


        /**
     * Sets the version tag for the outgoing soap call. Example <?xml
     * version=\"1.0\" encoding=\"UTF-8\"?>
     * 
     * @param tag
     *            the xml string to set at the top of the soap message.
     */

        public void setXmlVersionTag(String tag)
        {
            xmlVersionTag = tag;
        }

        /**
     * Attempts to reset the connection.
     */

        public void reset()
        {
        }

        /**
     * Perform a soap call with a given namespace and the given envelope
     * providing any extra headers that the user requires such as cookies.
     * Headers that are returned by the web service will be returned to the
     * caller in the form of a <code>List</code> of <code>HeaderProperty</code>
     * instances.
     * 
     * @param soapAction
     *            the namespace with which to perform the call in.
     * @param envelope
     *            the envelope the contains the information for the call.
     * @param headers
     *            <code>List</code> of <code>HeaderProperty</code> headers to
     *            send with the SOAP request.
     * 
     * @return Headers returned by the web service as a <code>List</code> of
     *         <code>HeaderProperty</code> instances.
     */

        public abstract List<HeaderProperty> call(String soapAction, SoapEnvelope envelope,
            List<HeaderProperty> headers);

        /**
     * Perform a soap call with a given namespace and the given envelope
     * providing any extra headers that the user requires such as cookies.
     * Headers that are returned by the web service will be returned to the
     * caller in the form of a <code>List</code> of <code>HeaderProperty</code>
     * instances.
     * 
     * @param soapAction
     *            the namespace with which to perform the call in.
     * @param envelope
     *            the envelope the contains the information for the call.
     * @param headers
     *            <code>List</code> of <code>HeaderProperty</code> headers to
     *            send with the SOAP request.
     * @param outputFile
     *            a file to stream the response into rather than parsing it,
     *            streaming happens when file is not null
     * 
     * @return Headers returned by the web service as a <code>List</code> of
     *         <code>HeaderProperty</code> instances.
     */

        public abstract List<HeaderProperty> call(String soapAction, SoapEnvelope envelope,
            List<HeaderProperty> headers, FileOutputStream outputFile);

        /**
     * Perform a soap call with a given namespace and the given envelope.
     * 
     * @param soapAction
     *            the namespace with which to perform the call in.
     * @param envelope
     *            the envelope the contains the information for the call.
     */

        public virtual void call(String soapAction, SoapEnvelope envelope)
        {
            call(soapAction, envelope, null);
        }

        /**
     * Return the name of the host that is specified as the web service target
     * 
     * @return Host name
     */

        public String getHost()
        {
            return new Uri(url).Host;
        }

        /**
     * Return the port number of the host that is specified as the web service
     * target
     * 
     * @return Port number
     */

        public int getPort()
        {
            return new Uri(url).Port;
        }

        /**
     * Return the path to the web service target
     * 
     * @return The URL's path
     */

        public String getPath()
        {
            return new Uri(url).AbsolutePath;
        }

        public abstract ServiceConnection getServiceConnection();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.transport
{

    /**
     * Interface to allow the abstraction of the raw transport information
     */

    public abstract class ServiceConnection
    {
        public const int DEFAULT_TIMEOUT = 20000; // 20 seconds
        public const int DEFAULT_BUFFER_SIZE = 256*1024; // 256 Kb


        /**
     * Make an outgoing connection.
     * 
     * @exception IOException
     */
        public abstract void connect();

        /**
     * Disconnect from the outgoing connection
     * 
     * @exception IOException
     */
        public abstract void disconnect();

        /**
     * Returns to the caller all of the headers that were returned with the
     * response to the SOAP request. Primarily this gives the caller an 
     * opportunity to save the cookies for later use.
     * 
     * @return List of HeaderProperty instances that were returned as part of the http response as http header
     * properties
     * 
     * @exception IOException
     */
        public abstract List<HeaderProperty> getResponseProperties();

        /**
     * Returns the numerical HTTP status to the caller
     * @return an integer status value
     * @throws IOException
     */
        public abstract int getResponseCode();

        /**
     * Set properties on the outgoing connection.
     * 
     * @param propertyName
     *            the name of the property to set. For HTTP connections these
     *            are the request properties in the HTTP Header.
     * @param value
     *            the string to set the property header to.
     * @exception IOException
     */
        public abstract void setRequestProperty(string propertyName, string value);

        /**
     * Sets how to make the requests. For HTTP this is typically POST or GET.
     * 
     * @param requestMethodType
     *            the type of request method to make the soap call with.
     * @exception IOException
     */
        public abstract void setRequestMethod(string requestMethodType);

        /**
     * If the length of a HTTP request body is known ahead, sets fixed length 
     * to enable streaming without buffering. Sets after connection will cause an exception.
     *
     * @param contentLength the fixed length of the HTTP request body
     * @see http://developer.android.com/reference/java/net/HttpURLConnection.html
     **/
        public abstract void setFixedLengthStreamingMode(int contentLength);

        public abstract void setChunkedStreamingMode();

        /**
     * Open and return the outputStream to the endpoint.
     * 
     * @exception IOException
     * @return the output stream to write the soap message to.
     */
        //public abstract Stream openOutputStream();

        /**
     * Opens and returns the inputstream from which to parse the result of the
     * soap call.
     * 
     * @exception IOException
     * @return the inputstream containing the xml to parse the result from the
     *         call from.
     */
        public abstract Stream openInputStream();

        /**
     * @return the error stream for the call.
     */
        public abstract Stream getErrorStream();

        public abstract void sendData(byte[] data);

        /**
     * Return the name of the host that is specified as the web service target
     *
     * @return Host name
     */
        public abstract string getHost();

        /**
     * Return the port number of the host that is specified as the web service target
     *
     * @return Port number
     */
        public abstract int getPort();

        /**
     * Return the path to the web service target
     *
     * @return The URL's path
     */
        public abstract string getPath();
    }
}

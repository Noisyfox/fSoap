using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.serialization
{

    /**
 * Provides get and set methods for properties. Can be used to replace
 * reflection (to some extend) for "serialization-aware" classes. Currently used
 * in kSOAP and the RMS based kobjects object repository
 */
    public interface FvmSerializable
    {
        /**
 * Get the property at the given index
 */
        Object getProperty(int index);

        /**
         * @return the number of serializable properties
         */
        int getPropertyCount();

        /**
         * Sets the property with the given index to the given value.
         *
         * @param index the index to be set
         * @param value the value of the property
         */
        void setProperty(int index, Object value);

        /**
         * Fills the given property info record.
         *
         * @param index      the index to be queried
         * @param properties information about the (de)serializer.  Not frequently used.
         * @param info       The return parameter, to be filled with information about the
         *                   property with the given index.
         */
        void getPropertyInfo(int index, Dictionary<object, object> properties, PropertyInfo info);
    }
}

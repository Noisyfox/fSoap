using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.serialization
{

    /**
 * Interface for classes requiring inner text of xml  tags
 *
 * @author satansly
 */
    public interface HasInnerText
    {
        /**
* Gets the inner text of xml tags
*/
        String getInnerText();

        /**
         * @param s String to be set as inner text for an outgoing soap object
         */
        void setInnerText(String s);
    }
}

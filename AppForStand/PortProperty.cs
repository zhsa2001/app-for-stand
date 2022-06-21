using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppForStand
{
    internal class PortProperty
    {
        public Device TargetDevice { get; set; }
        public string Property { get; set; }
        public override string ToString()
        {
            return TargetDevice.ToString()+" "+Property;
        }

        public override bool Equals(object obj)
        {
            if(obj is PortProperty)
            {
                if (((PortProperty)obj).Property.Equals(this.Property) && ((PortProperty)obj).TargetDevice.Equals(this.TargetDevice))
                    return true;
                else 
                    return false;
            }
            else
                return false;
        }
    }
}

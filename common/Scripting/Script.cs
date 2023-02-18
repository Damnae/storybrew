using System;
using System.Runtime.Remoting.Lifetime;

namespace StorybrewCommon.Scripting
{
    ///<summary> Base class for all scripts. </summary>
    public abstract class Script : MarshalByRefObject
    {
        string identifier;

        ///<summary> Script name </summary>
        public string Identifier
        {
            get => identifier;
            set
            {
                if (identifier != null) throw new InvalidOperationException("This script already has an identifier");
                identifier = value;
            }
        }
        ///<summary/>
        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(15);
                lease.RenewOnCallTime = TimeSpan.FromMinutes(15);
            }
            return lease;
        }
    }
}
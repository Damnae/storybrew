using System;
using System.Runtime.Remoting.Lifetime;

namespace StorybrewCommon.Scripting
{
    /// <summary>
    /// Base class for all scripts
    /// </summary>
    public abstract class Script : MarshalByRefObject
    {
        private string identifier;
        public string Identifier
        {
            get { return identifier; }
            set
            {
                if (identifier != null) throw new InvalidOperationException("This script already has an identifier");
                identifier = value;
            }
        }

        public override object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(15);
                //lease.SponsorshipTimeout = TimeSpan.FromSeconds(2);
                lease.RenewOnCallTime = TimeSpan.FromMinutes(15);
            }
            return lease;
        }
    }
}

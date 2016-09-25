using System;

namespace StorybrewCommon.Scripting
{
    /// <summary>
    /// Base class for all scripts
    /// </summary>
    public abstract class Script : MarshalByRefObject
    {
        public abstract string Identifier { get; }

        /*
        public override object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromSeconds(2);
                lease.SponsorshipTimeout = TimeSpan.FromSeconds(2);
                lease.RenewOnCallTime = TimeSpan.FromSeconds(2);
            }
            return lease;
        }
        */
    }
}

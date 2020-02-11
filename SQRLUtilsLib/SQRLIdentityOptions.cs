using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLUtilsLib
{
    /// <summary>
    /// Represents the 16 binary "option flags" of a SQRL
    /// type 1 identity block.
    /// </summary>
    [Serializable]
    public class SQRLIdentityOptions
    {
        /// <summary>
        /// Instanciates a new <c>SQRLIdentityOptions</c> object and 
        /// initializes the options according to the given <paramref name="optionFlags"/>.
        /// </summary>
        /// <param name="optionFlags">The 16 binary option flags represented as an unsigned short.
        /// Defaults to 0x1F3 (499).</param>
        public SQRLIdentityOptions(ushort optionFlags = 0x1F3)
        {
            this.FlagsValue = optionFlags;
        }

        /// <summary>
        /// The 16 binary option flags represented as an unsigned short.
        /// </summary>
        public ushort FlagsValue { get; set; }

        /// <summary>
        /// This requests, and gives the SQRL client permission, to briefly 
        /// check-in with its publisher to see whether any updates to this 
        /// software have been made available.
        /// </summary>
        public bool CheckForUpdates
        {
            get { return (FlagsValue & 0x0001) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0001) : (ushort)(FlagsValue & 0xFFFE); }
        }

        /// <summary>
        /// This requests, and gives the SQRL client permission, to automatically 
        /// replace itself with the latest version when it discovers that a newer
        /// version is available.
        /// </summary>
        public bool UpdateAutonomously
        {
            get { return (FlagsValue & 0x0002) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0002) : (ushort)(FlagsValue & 0xFFFD); }
        }

        /// <summary>
        /// This adds the “option=sqrlonly” string to every client transaction.The 
        /// or lack of presence of this option string in any properly signed client 
        /// transaction is used to push an update of a server-stored flag that, 
        /// when set, will subsequently disable all traditional non-SQRL account logon 
        /// authentication such as username and password.
        /// </summary>
        public bool RequestSQRLOnlyLogin
        {
            get { return (FlagsValue & 0x0004) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0004) : (ushort)(FlagsValue & 0xFFFA); }
        }

        /// <summary>
        /// This adds the “option=hardlock” string to every client transaction.The 
        /// presence or lack of presence of this option string in any properly 
        /// signed client transaction is used to push an update of a server-stored flag that,
        /// when set, will subsequently disable all “out of band” (non-SQRL) account identity 
        /// recovery options such as “what was your favorite pet's name.” 
        /// </summary>
        public bool RequestNoSQRLBypass
        {
            get { return (FlagsValue & 0x0008) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0008) : (ushort)(FlagsValue & 0xFFF7); }
        }

        /// <summary>
        /// When set, this bit instructs the SQRL client to notify its user when the web 
        /// server indicates that an IP address mismatch exists between the entity that 
        /// requested the initial logon web page containing the SQRL link URL(and probably 
        /// encoded into the SQRL link URL's “nut”) and the IP address from which the SQRL 
        /// client's query was received for this reply. 
        /// </summary>
        public bool EnableMITMAttackWarning
        {
            get { return (FlagsValue & 0x0010) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0010) : (ushort)(FlagsValue & 0xFFEF); }
        }

        /// <summary>
        /// When set, this bit instructs the SQRL client to wash any existing local password 
        /// and QuickPass data from RAM upon notification that the system is going to sleep 
        /// in any way such that it cannot be used.This would include sleeping, hibernating, 
        /// screen blanking, etc.
        /// </summary>
        public bool ClearQuickPassOnSleep
        {
            get { return (FlagsValue & 0x0020) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0020) : (ushort)(FlagsValue & 0xFFDF); }
        }

        /// <summary>
        /// When set, this bit instructs the SQRL client to wash any existing local password 
        /// and QuickPass data from RAM upon notification that the current user is being switched.
        /// </summary>
        public bool ClearQuickPassOnSwitchingUser
        {
            get { return (FlagsValue & 0x0040) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0040) : (ushort)(FlagsValue & 0xFFAF); }
        }

        /// <summary>
        /// When set, this bit instructs the SQRL client to wash any existing local password 
        /// and QuickPass data from RAM when the system has been user-idle (no mouse or keyboard 
        /// activity) for the number of minutes specified by the two-byte idle timeout.
        /// </summary>
        public bool ClearQuickPassOnIdle
        {
            get { return (FlagsValue & 0x0080) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0080) : (ushort)(FlagsValue & 0xFF7F); }
        }

        /// <summary>
        /// When set, this bit instructs the SQRL client to notify its user whenever a non-CPS 
        /// authentication is attempted. Since CPS provides extremely strong protection against 
        /// website spoofing—which is a particularly significant concern for SQRL due to its high 
        /// degree of automation and presumed automatic provision of security—this flag must 
        /// default to enabled.
        /// </summary>
        public bool EnableNoCPSWarning
        {
            get { return (FlagsValue & 0x0100) != 0; }
            set { FlagsValue = value ? (ushort)(FlagsValue | 0x0100) : (ushort)(FlagsValue & 0xFEFF); }
        }
    }
}

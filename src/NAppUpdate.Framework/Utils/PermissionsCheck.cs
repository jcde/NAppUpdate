using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace NAppUpdate.Framework.Utils
{
    public static class PermissionsCheck
    {
        private static readonly IdentityReferenceCollection groups;
        private static readonly string sidCurrentUser;

        static PermissionsCheck()
        {
            if (PlatformCheck.CurrentlyRunningInWindows())
                groups = WindowsIdentity.GetCurrent().Groups;
            else
                groups = new IdentityReferenceCollection();

            if (PlatformCheck.CurrentlyRunningInWindows())
                sidCurrentUser = WindowsIdentity.GetCurrent().User.Value;
        }

        public static bool IsDirectory(string path)
        {
            if (!Directory.Exists(path)) return false;
            FileAttributes attr = File.GetAttributes(path);
            return ((attr & FileAttributes.Directory) == FileAttributes.Directory);
        }

        public static bool HaveWritePermissionsForFolder(string path)
        {
            if (!PlatformCheck.CurrentlyRunningInWindows())
                return true;

            string folder = IsDirectory(path) ? path : Path.GetDirectoryName(path);
            return HaveWritePermissionsForFileOrFolder(folder);
        }

        public static bool HaveWritePermissionsForFileOrFolder(string path)
        {
            if (!PlatformCheck.CurrentlyRunningInWindows())
                return true;

            AuthorizationRuleCollection rules = Directory.GetAccessControl(path)
                .GetAccessRules(true, true, typeof(SecurityIdentifier));

            bool allowwrite = false, denywrite = false;
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType == AccessControlType.Deny &&
                    (rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData &&
                    (groups.Contains(rule.IdentityReference) || rule.IdentityReference.Value == sidCurrentUser)
                    )
                {
                    denywrite = true;
                }
                if (rule.AccessControlType == AccessControlType.Allow &&
                    (rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData &&
                    (groups.Contains(rule.IdentityReference) || rule.IdentityReference.Value == sidCurrentUser)
                    )
                {
                    allowwrite = true;
                }
            }

            // If we have both allow and deny permissions, the deny takes precident.
            if (allowwrite && !denywrite)
                return true;

            return false;
        }
    }
}
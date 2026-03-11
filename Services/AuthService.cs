using Novell.Directory.Ldap;
using ServerCRM.Models;
using ServerCRM.Models.LogIn;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using LdapConnection = Novell.Directory.Ldap.LdapConnection;
using LdapException = Novell.Directory.Ldap.LdapException;


namespace ServerCRM.Services
{
    public class AuthService
    {

        public async Task<bool> AuthenticateUser(string username, string password)
        {
          
            var domainServerMap = new Dictionary<string, string>
                                {
                                    { "onepointone.in", "192.168.0.110" },
                                    { "1point1.in", "192.168.0.150" }
                                };

            foreach (var kvp in domainServerMap)
            {
                string domain = kvp.Key;
                string server = kvp.Value;

                try
                {
                    using (var connection = new LdapConnection())
                    {
                        await connection.ConnectAsync(server, 389);

                       
                        await connection.BindAsync($"{username}@{domain}", password);
                        return true; 
                    }
                }
                catch (Novell.Directory.Ldap.LdapException ex)
                {
                    Console.WriteLine($"Failed {username}@{domain} via {server}: {ex.Message}");
                   
                }
            }

            return false; 
        }


        public static string GetUsername(string usernameDomain)
        {
            if (string.IsNullOrWhiteSpace(usernameDomain))
                throw new ArgumentException("Username cannot be null or empty.", nameof(usernameDomain));

            if (usernameDomain.Contains("\\"))
            {
                var index = usernameDomain.IndexOf("\\", StringComparison.Ordinal);
                return usernameDomain[(index + 1)..];
            }

            if (usernameDomain.Contains("@"))
            {
                var index = usernameDomain.IndexOf("@", StringComparison.Ordinal);
                return usernameDomain[..index];
            }

            return usernameDomain;
        }

        public static string GetUsernames(string usernameDomain)
        {
            if (string.IsNullOrEmpty(usernameDomain))
            {
                throw (new ArgumentException("Argument can't be null.", "usernameDomain"));
            }
            if (usernameDomain.Contains("\\"))
            {
                int index = usernameDomain.IndexOf("\\");
                return usernameDomain.Substring(index + 1);
            }
            else if (usernameDomain.Contains("@"))
            {
                int index = usernameDomain.IndexOf("@");
                return usernameDomain.Substring(0, index);
            }
            else
            {
                return usernameDomain;
            }
        }
        //public static bool checkdomain(string empcode)
        //{

        //    IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

        //    // Get the domain name
        //    string domainName = properties.DomainName;


        //    string userName = GetUsernames(empcode);
        //    string DOMAIN = "";

        //    DOMAIN = domainName;

        //    IntPtr token = IntPtr.Zero;
        //    PrincipalContext ctx = new PrincipalContext(ContextType.Domain, DOMAIN);
        //    UserPrincipal user = UserPrincipal.FindByIdentity(ctx, userName);

        //    if (user == null)
        //    {


        //    }

        //    bool isValid = ctx.ValidateCredentials(userName, "");
        //    user.Enabled = false;

        //    if (isValid == false)
        //    {

        //        return false;
        //    }
        //    else
        //    {
        //        DateTime dt = (DateTime)user.LastPasswordSet;
        //        dt = dt.AddMinutes(330);
        //        System.TimeSpan diffResult = Convert.ToDateTime(dt.AddDays(45).ToShortDateString()).Subtract(Convert.ToDateTime(DateTime.Now.ToShortDateString()));

        //        return isValid;
        //    }
        //}




    }
}

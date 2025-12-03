using System;
using System.Net.Mail;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MuhasebeAPI.Helpers
{
    public static class EmailValidator
    {
        public static bool IsValidEmailFormat(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> HasValidMxRecord(string domain)
        {
            try
            {
                var lookup = new DnsClient.LookupClient();
                var result = await lookup.QueryAsync(domain, DnsClient.QueryType.MX);
                return result.Answers.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

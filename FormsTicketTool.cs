// FormsTicketTool.cs
// Author: Axura
// Purpose: decrypt/encrypt/create ASP.NET FormsAuth tickets using machineKey in exe.config
// Build: .NET Framework 4.8. Reference: System.Web

using System;
using System.IO;
using System.Web.Security;

namespace FormsTicketTool
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0) { Usage(); return 1; }

            string cmd = args[0].ToLowerInvariant();
            try
            {
                switch (cmd)
                {
                    case "decrypt":
                        // decrypt <COOKIE> [--json] [--utc]
                        if (args.Length < 2) { Console.WriteLine("Please supply encrypted forms ticket"); return 2; }
                        bool asJson = HasFlag(args, "--json");
                        bool asUtc = HasFlag(args, "--utc");
                        return CmdDecrypt(args[1], asJson, asUtc);

                    case "encrypt":
                        // encrypt <EXISTING_COOKIE> <NEW_USER> <USERDATA> <MINUTES_VALID>
                        if (args.Length < 5) { Console.WriteLine("Usage: FormsTicketTool.exe encrypt <existing_cookie> <new_user> <userData> <minutes_valid>"); return 3; }
                        return CmdEncrypt(args[1], args[2], args[3], args[4]);

                    case "create":
                        // create <USERNAME> <USERDATA> <MINUTES_VALID> [isPersistent]
                        if (args.Length < 4) { Console.WriteLine("Usage: FormsTicketTool.exe create <username> <userData> <minutes_valid> [isPersistent]"); return 4; }
                        bool isPersistent = false;
                        if (args.Length >= 5) bool.TryParse(args[4], out isPersistent);
                        return CmdCreate(args[1], args[2], args[3], isPersistent);

                    case "gen-config":
                        // gen-config <decryptionKey> <validationKey> [compatibilityMode] [outfile]
                        if (args.Length < 3) { Console.WriteLine("Usage: FormsTicketTool.exe gen-config <decryptionKey> <validationKey> [compatibilityMode] [outfile]"); return 5; }
                        string compat = (args.Length >= 4 ? args[3] : null);
                        string outfile = (args.Length >= 5 ? args[4] : null);
                        return CmdGenConfig(args[1], args[2], compat, outfile);

                    case "info":
                        Console.WriteLine(Environment.Version);
                        return 0;

                    default:
                        Usage(); return 6;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 9;
            }
        }

        static void Usage()
        {
            Console.WriteLine("FormsTicketTool");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  FormsTicketTool.exe decrypt <COOKIE> [--json] [--utc]");
            Console.WriteLine("  FormsTicketTool.exe encrypt <EXISTING_COOKIE> <NEW_USER> <USERDATA> <MINUTES_VALID>");
            Console.WriteLine("  FormsTicketTool.exe create  <USERNAME> <USERDATA> <MINUTES_VALID> [isPersistent]");
            Console.WriteLine("  FormsTicketTool.exe gen-config <decryptionKey> <validationKey> [compatibilityMode] [outfile]");
            Console.WriteLine("  FormsTicketTool.exe info");
            Console.WriteLine();
            Console.WriteLine("Note: Put an App.config (<exe>.config) with the exact <machineKey> beside the binary.");
        }

        // ----- commands -----

        // decrypt <COOKIE> [--json] [--utc]
        static int CmdDecrypt(string encryptedTicket, bool asJson, bool asUtc)
        {
            FormsAuthenticationTicket t = FormsAuthentication.Decrypt(encryptedTicket);
            if (t == null) { Console.WriteLine("Decrypt failed."); return 10; }

            if (asJson)
            {
                PrintTicketJson(t, asUtc);
            }
            else
            {
                PrintTicketPretty(t, asUtc, header: "ASP.NET FormsAuthentication Ticket");
            }
            return 0;
        }

        // encrypt <EXISTING_COOKIE> <NEW_USER> <USERDATA> <MINUTES_VALID>
        static int CmdEncrypt(string existingCookie, string newUser, string userData, string minutesStr)
        {
            if (!int.TryParse(minutesStr, out int minutes))
            {
                Console.WriteLine("Invalid minutes value.");
                return 12;
            }

            FormsAuthenticationTicket old = FormsAuthentication.Decrypt(existingCookie);
            if (old == null) { Console.WriteLine("Cannot decrypt existing cookie."); return 13; }

            var ticket = new FormsAuthenticationTicket(
                1,
                newUser,
                DateTime.Now,
                DateTime.Now.AddMinutes(minutes),
                old.IsPersistent,          // preserve original persistence
                userData,
                "/"
            );

            string encrypted = FormsAuthentication.Encrypt(ticket);
            Console.WriteLine(encrypted);
            Console.WriteLine();
            Console.WriteLine(".ASPXAUTH=" + encrypted);

            // pretty summary of what we just minted
            PrintTicketPretty(ticket, asUtc: false, header: "New Ticket (summary)");
            return 0;
        }

        // create <USERNAME> <USERDATA> <MINUTES_VALID> [isPersistent]
        static int CmdCreate(string username, string userData, string minutesStr, bool isPersistent)
        {
            if (!int.TryParse(minutesStr, out int minutes))
            {
                Console.WriteLine("Invalid minutes value.");
                return 15;
            }

            var ticket = new FormsAuthenticationTicket(
                1,
                username,
                DateTime.Now,
                DateTime.Now.AddMinutes(minutes),
                isPersistent,
                userData,
                "/"
            );

            string encrypted = FormsAuthentication.Encrypt(ticket);
            Console.WriteLine(encrypted);
            Console.WriteLine();
            Console.WriteLine(".ASPXAUTH=" + encrypted);

            PrintTicketPretty(ticket, asUtc: false, header: "New Ticket (summary)");
            return 0;
        }

        // gen-config <decryptionKey> <validationKey> [compatibilityMode] [outfile]
        static int CmdGenConfig(string decryptionKey, string validationKey, string compatibilityMode, string outfile)
        {
            if (string.IsNullOrWhiteSpace(decryptionKey) || string.IsNullOrWhiteSpace(validationKey))
            {
                Console.WriteLine("Both keys are required.");
                return 17;
            }

            string compatAttr = string.IsNullOrEmpty(compatibilityMode)
                ? ""
                : $" compatibilityMode=\"{compatibilityMode}\"";

            string xml =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<configuration>\r\n" +
                "  <system.web>\r\n" +
                "    <machineKey decryption=\"AES\" decryptionKey=\"" + decryptionKey +
                "\" validation=\"HMACSHA256\" validationKey=\"" + validationKey + "\"" + compatAttr + " />\r\n" +
                "  </system.web>\r\n" +
                "</configuration>";

            if (!string.IsNullOrEmpty(outfile))
            {
                File.WriteAllText(outfile, xml);
                Console.WriteLine("Wrote: " + outfile);
            }
            else
            {
                Console.WriteLine(xml);
            }
            return 0;
        }

        // ----- formattor -----

        static void PrintTicketPretty(FormsAuthenticationTicket t, bool asUtc, string header)
        {
            DateTime issued = asUtc ? t.IssueDate.ToUniversalTime() : t.IssueDate;
            DateTime expires = asUtc ? t.Expiration.ToUniversalTime() : t.Expiration;
            TimeSpan ttl = expires - DateTime.Now;

            string issuedLbl = asUtc ? "Issued (UTC)" : "Issued (local)";
            string expiresLbl = asUtc ? "Expires (UTC)" : "Expires (local)";

            Console.WriteLine("=== " + header + " ===");
            WriteKV("Version", t.Version.ToString());
            WriteKV("Name", t.Name);
            WriteKV(issuedLbl, issued.ToString("yyyy-MM-dd HH:mm:ss"));
            WriteKV(expiresLbl, expires.ToString("yyyy-MM-dd HH:mm:ss"));
            WriteKV("Persistent", t.IsPersistent.ToString());
            WriteKV("TTL", (ttl < TimeSpan.Zero ? "expired" : ttl.ToString(@"dd\.hh\:mm\:ss")));
            WriteKV("UserData", t.UserData);
            WriteKV("CookiePath", t.CookiePath);
            Console.WriteLine();
        }

        static void PrintTicketJson(FormsAuthenticationTicket t, bool asUtc)
        {
            DateTime issued = asUtc ? t.IssueDate.ToUniversalTime() : t.IssueDate;
            DateTime expires = asUtc ? t.Expiration.ToUniversalTime() : t.Expiration;

            // JSON 
            string json = "{"
                + "\"version\":" + t.Version + ","
                + "\"name\":\"" + EscapeJson(t.Name) + "\","
                + "\"issued\":\"" + issued.ToString("o") + "\","
                + "\"expires\":\"" + expires.ToString("o") + "\","
                + "\"persistent\":" + (t.IsPersistent ? "true" : "false") + ","
                + "\"userdata\":\"" + EscapeJson(t.UserData) + "\","
                + "\"cookiePath\":\"" + EscapeJson(t.CookiePath) + "\""
                + "}";
            Console.WriteLine(json);
        }

        // ----- helpers -----

        static bool HasFlag(string[] args, string flag)
        {
            for (int i = 2; i < args.Length; i++)
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static void WriteKV(string key, string val)
        {
            const int pad = 16; 
            if (key.Length < pad) key = key.PadRight(pad, ' ');
            Console.WriteLine($"{key}: {val}");
        }

        static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}

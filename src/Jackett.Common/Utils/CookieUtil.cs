using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;

namespace Jackett.Common.Utils
{
    public static class CookieUtil
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie
        // NOTE: we are not checking non-ascii characters and we should
        private static readonly Regex _CookieRegex = new Regex(@"([^\(\)<>@,;:\\""/\[\]\?=\{\}\s]+)=([^,;\\""\s]+)");
        private static readonly char[] InvalidKeyChars = { '(', ')', '<', '>', '@', ',', ';', ':', '\\', '"', '/', '[', ']', '?', '=', '{', '}', ' ', '\t', '\n' };
        private static readonly char[] InvalidValueChars = { '"', ',', ';', '\\', ' ', '\t', '\n' };

        public static Dictionary<string, string> CookieHeaderToDictionary(string cookieHeader)
        {
            var cookieDictionary = new Dictionary<string, string>();
            if (cookieHeader == null)
                return cookieDictionary;
            var matches = _CookieRegex.Match(cookieHeader);
            while (matches.Success)
            {
                if (matches.Groups.Count > 2)
                    cookieDictionary[matches.Groups[1].Value] = matches.Groups[2].Value;
                matches = matches.NextMatch();
            }
            return cookieDictionary;
        }

        public static string CookieDictionaryToHeader(Dictionary<string, string> cookieDictionary)
        {
            if (cookieDictionary == null)
                return "";
            foreach (var kv in cookieDictionary)
                if (kv.Key.IndexOfAny(InvalidKeyChars) > -1 || kv.Value.IndexOfAny(InvalidValueChars) > -1)
                    throw new FormatException($"The cookie '{kv.Key}={kv.Value}' is malformed.");
            return string.Join("; ", cookieDictionary.Select(kv => kv.Key + "=" + kv.Value));
        }

        /// <summary>
        /// Remove all the cookies from a CookieContainer. That includes all domains and protocols.
        /// </summary>
        /// <param name="cookieJar">A cookie container</param>
        public static void RemoveAllCookies(CookieContainer cookieJar)
        {
            var table = (Hashtable)cookieJar
                                   .GetType()
                                   .InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField |
                                                                  BindingFlags.Instance, null, cookieJar, new object[] { });
            foreach (var key in table.Keys)
                try
                {
                    foreach (Cookie cookie in cookieJar.GetCookies(new Uri($"http://{key}")))
                        cookie.Expired = true;
                    foreach (Cookie cookie in cookieJar.GetCookies(new Uri($"https://{key}")))
                        cookie.Expired = true;
                }
                catch (Exception)
                {
                    logger.Warn("Unable to delete the cookies of domain: " + key);
                }
        }

    }
}

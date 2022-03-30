using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ThirdPartySeamlessWallet.Extensions
{
    public static class MethodExtension
    {
        public static string GetCurrentMethod()
        {
            return new StackTrace().GetFrame(1)?.GetMethod()?.Name;
        }
        
        public static string GetCurrentAsyncMethod([CallerMemberName] string callerName = "")
        {
            return callerName;
        }
        /// <summary>
        ///     GetEnumDescription
        /// </summary>
        /// <typeparam name="TEnum">Enum</typeparam>
        /// <param name="type">type</param>
        /// <returns>string</returns>
        public static string GetEnumDescription<TEnum>(this TEnum type)
        {
            var fi = type.GetType().GetField(type.ToString());

            if (fi.GetCustomAttribute(typeof(DescriptionAttribute), false) is DescriptionAttribute attributes)
            {
                return attributes.Description;
            }

            return type.ToString();
        }


        public static string CodeToString(this Enum payLoad)
        {
            return ((int)(IConvertible)payLoad).ToString();
        }
    }
}
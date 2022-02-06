using System;
using System.Text.RegularExpressions;
using DotNetPacketCaptor.Models;

namespace DotNetPacketCaptor.Core
{
    public class PacketFilter
    {
        public bool CanFilter { get; set; }

        public bool CheckValidity(string singleFilterItem)
        {
            if (singleFilterItem == "")
                return true;
            var regex = new Regex(@"^[A-Z][a-zA-Z]+\s*=\s*[a-z0-9\%\:\.\/\\]+$");
            /*
             * Don't use * operator because of the case where main string will be matched although there is nothing in front of '='
             * Such as the main string '=sad' and the pattern '[a-z]*\s?=\s?[a-z]+', we can get the result: Successful match!
             */
            return regex.IsMatch(singleFilterItem);
        }

        public void GetFilterLogic(string singleFilterItem, out Predicate<object> @delegate)
        {
            @delegate = o =>
            {
                var property = singleFilterItem.Split('=')[0].Trim();
                var value = singleFilterItem.Split('=')[1].Trim();
                var packet = o as DotNetRawPacket;
                var actualProperty = packet?.GetType().GetProperty(property);
                if (actualProperty == null)
                    return false;
                var actualValue = actualProperty.GetValue(packet);
                return string.Equals(actualValue.ToString(), value, StringComparison.CurrentCultureIgnoreCase);
            };
        }
    }
}
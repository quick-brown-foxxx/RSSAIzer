using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RSSAIzer.Web.Utils;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        var type = enumValue.GetType();
        var member = type.GetMember(enumValue.ToString()).FirstOrDefault();
        if (member == null)
        {
            return enumValue.ToString();
        }

        var attr = member.GetCustomAttribute<DisplayAttribute>();
        return attr != null ? attr.GetName()! : enumValue.ToString();
    }
}

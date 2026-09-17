using System.Reflection;
using MacroTool.Web.Components.Shared;

namespace MacroTool.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void WebComponents_DoNotReference_Infrastructure()
    {
        var infra = typeof(MacroTool.Infrastructure.Storage.TimelineStore).Assembly;
        var violations = new List<string>();

        foreach (var type in typeof(ControlBar).Assembly.GetTypes())
        {
            if (type.Namespace is null ||
                !type.Namespace.StartsWith("MacroTool.Web.Components", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var referenced in ReferencedBy(type))
            {
                if (referenced.Assembly == infra)
                    violations.Add($"{type.FullName} -> {referenced.FullName}");
            }

            foreach (var member in type.GetMembers(
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                foreach (var referenced in ReferencedBy(member))
                {
                    if (referenced.Assembly == infra)
                        violations.Add($"{type.FullName}.{member.Name} -> {referenced.FullName}");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static IEnumerable<Type> ReferencedBy(Type type)
    {
        if (type.BaseType is not null)
            yield return type.BaseType;

        foreach (var iface in type.GetInterfaces())
            yield return iface;

        foreach (var argument in type.GetGenericArguments())
            yield return argument;
    }

    private static IEnumerable<Type> ReferencedBy(MemberInfo member)
    {
        switch (member)
        {
            case FieldInfo field:
                foreach (var t in Unwrap(field.FieldType))
                    yield return t;
                break;
            case PropertyInfo property:
                foreach (var t in Unwrap(property.PropertyType))
                    yield return t;
                break;
            case EventInfo @event when @event.EventHandlerType is not null:
                foreach (var t in Unwrap(@event.EventHandlerType))
                    yield return t;
                break;
            case MethodBase method:
                foreach (var t in Unwrap(method is MethodInfo mi ? mi.ReturnType : typeof(void)))
                    yield return t;
                foreach (var parameter in method.GetParameters())
                    foreach (var t in Unwrap(parameter.ParameterType))
                        yield return t;
                break;
        }
    }

    private static IEnumerable<Type> Unwrap(Type type)
    {
        if (type.HasElementType && type.GetElementType() is { } element)
        {
            foreach (var t in Unwrap(element))
                yield return t;
            yield break;
        }

        yield return type;

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                foreach (var t in Unwrap(argument))
                    yield return t;
        }
    }
}

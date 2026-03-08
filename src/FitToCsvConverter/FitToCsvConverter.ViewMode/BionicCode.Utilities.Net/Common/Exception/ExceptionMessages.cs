[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BionicCode.Controls.Net.Wpf")]
namespace BionicCode.Utilities.Net;

using System;
using System.Collections;
using System.Reflection;

internal static class ExceptionMessages
{
    public static string GetObjectDisposedExceptionMessage(string typeName) => $"The object of type {typeName} has been disposed.";
    public static string GetIndexOutOfRangeExceptionMessage(int index, string collectionName) => $"The index '{index}' is out of range of collection {collectionName}.";
    public static string InvalidOperationExceptionMessage_CollectionEmpty => "The sequence is empty.";
    public static string GetInvalidOperationExceptionMessage_InitializableFailed(Type initializableImplementationType)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(initializableImplementationType);
        return $"Initializable.InitializeAsync for type {initializableImplementationType.FullName}  returned 'false'.";
    }

    public static string GetInvalidOperationExceptionMessage_ItemNotFound(string predicateName) => $"The sequence contains no item that matches the predicate '{predicateName}'.";
    public static string ArgumentNullExceptionMessage_ValidationPropertyName => "Please provide a valid property name. A validation error must always map to a property.";
    public static string ArgumentExceptionMessage_ValidationFailed => "Property validation failed.";
    public static string InvalidOperationExceptionMessage_SetFactoryModeOnScopedFactory => $"Modifying the lifetime of instances produced by a scoped factory is not allowed. Instances are automatically treated as {FactoryMode.Singleton} for the current scope. You can change the IFactory.FactoryMode property after leaving the scope.";
    public static string GetValueNotSupportedExceptionMessage(object value) => $"The {(value is Enum ? "enum " : string.Empty)}value '{(value is Enum enumValue ? $"{enumValue.GetType().FullName}.{enumValue}" : value)}' is not supported.";
    public static string GetModificationOfReadOnlyCollectionNotSupportedExceptionMessage(IEnumerable collection)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(collection);
        return $"The {collection.GetType().FullName} is read-only.";
    }

    public static string GetModificationOfImmutableCollectionNotSupportedExceptionMessage(IEnumerable collection)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(collection);
        return $"The {collection.GetType().FullName} is immutable.";
    }

    public static string GetHandlerDelegateSignatureMismatchExceptionMessage(EventInfo eventInfo, MethodInfo eventHandlerMethodInfo, string because)
    {
            ArgumentNullExceptionAdvanced.ThrowIfNull(eventInfo);
            ArgumentNullExceptionAdvanced.ThrowIfNull(eventHandlerMethodInfo);
            ArgumentNullExceptionAdvanced.ThrowIfNull(because);
        return $"Event handler delegate signature mismatch. Expected signature as required by event source: '{eventInfo.EventHandlerType.FullName}'. Found signature on provided event handler: '{eventHandlerMethodInfo.Name}'. Because: {because}";
    }

    public static string GetTypeMismatchExceptionMessage(Type providedType, string parameterName, Type expectedType, string referenceName)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(providedType);
        ArgumentNullExceptionAdvanced.ThrowIfNull(parameterName);
        ArgumentNullExceptionAdvanced.ThrowIfNull(expectedType);
        ArgumentNullExceptionAdvanced.ThrowIfNull(referenceName);
        return $"Type mismatch. The type of the '{parameterName}' must be assignable to the type of '{referenceName}'. Expected {expectedType.FullName} but found {providedType.FullName}.";
    }

    public static string GetInvalidAccessCollectionEmptyExceptionMessage(string throwingTypeName, string accessedMemberName) => $"The '{throwingTypeName}' is empty. Therefore the '{accessedMemberName}' property is not accessible.";
}

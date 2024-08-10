namespace PSSharp.WindowsUpdate.Commands;

public class WUServerPolicyValueMissingException(Exception? innerException)
    : WindowsUpdateException("WUServer policy value is missing in the registry", innerException) { }

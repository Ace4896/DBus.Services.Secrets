using Tmds.DBus.Protocol;

namespace DBus.Services.Secrets;

/// <summary>
/// Represents a session in the D-Bus secret service.
/// </summary>
public class Session
{
    public ObjectPath SessionPath { get; }

    public Session(ObjectPath sessionPath)
    {
        SessionPath = sessionPath;
    }
}

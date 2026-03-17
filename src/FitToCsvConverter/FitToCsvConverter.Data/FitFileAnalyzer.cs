namespace FitToCsvConverter.Data;

using BionicCode.Utilities.Net;
using Dynastream.Fit;
using DateTime = DateTime;

public static class FitFileAnalyzer
{
    public static DateTime GetSessionDate(string fitFilePath)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(fitFilePath);

        var decoder = new Decode();
        var mesgBroadcaster = new MesgBroadcaster();

        // Connect the the Decode and Message Broadcaster Objects
        decoder.MesgEvent += mesgBroadcaster.OnMesg;
        decoder.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;
        mesgBroadcaster.SessionMesgEvent += OnSessionReady;
        var fitFileStream = new FileStream(fitFilePath, FileMode.Open, FileAccess.Read);
        bool isSuccessful = decoder.Read(fitFileStream);
        return DateTime.Now;
    }

    private static void OnSessionReady(object sender, MesgEventArgs e)
    {
        _ = e.mesg.Fields;
    }
}

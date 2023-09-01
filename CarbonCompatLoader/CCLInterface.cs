using Carbon;
using Carbon.Base;

namespace CarbonCompatLoader;

public class CCLInterface : CarbonModule<CCLConfig, EmptyModuleData>
{
    public static CCLInterface Singleton { get; private set; }
    public override Type Type => typeof(CCLInterface);

    public override bool EnabledByDefault => true;

    public CCLInterface()
    {
        Singleton = this;
    }

    internal static void AttemptModuleInit()
    {
        try
        {
            InitModule();
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to init module: {e}");
        }
    }
    internal static void InitModule()
    {
        if (Singleton != null) return;
        Community.Runtime.ModuleProcessor.Build(typeof(CCLInterface));
    }
    public override string Name => "CCL";
    
    /*[AuthLevel(1)]
    [ConsoleCommand("ccl.loaded")]
    private bool GetLoaded(ConsoleSystem.Arg arg)
    {
        throw new NotImplementedException();
    }*/

    [AuthLevel(1)]
    [ConsoleCommand("ccl.info")]
    private bool Info(ConsoleSystem.Arg arg)
    {
        arg.ReplyWith($"CCL - {MainConverter.BuildConfiguration} - {CCLEntrypoint.CCLVersion.ToString(3)} - {(CCLEntrypoint.bootstrapUsed ? "With bootstrap" : "Without bootstrap")}");
        return true;
    }
}
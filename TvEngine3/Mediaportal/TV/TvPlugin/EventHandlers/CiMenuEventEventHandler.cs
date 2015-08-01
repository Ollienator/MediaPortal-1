using Mediaportal.TV.Server.TVLibrary.Interfaces.CiMenu;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using Mediaportal.TV.Server.TVLibrary.Interfaces.TunerExtension;

namespace Mediaportal.TV.TvPlugin.EventHandlers
{
  /// <summary>
  /// Handler class for gui interactions of ci menu
  /// </summary>
  public class CiMenuEventEventHandler : CiMenuEventCallbackSink
  {
 

    /// <summary>
    /// eventhandler to show CI Menu dialog
    /// </summary>
    /// <param name="menu"></param>
    public override void CiMenuCallback(CiMenu menu)
    {
      try
      {
        this.LogDebug("Callback from tvserver {0}", menu.Title);

        // pass menu to calling dialog
        TvPlugin.TVHome.ProcessCiMenu(menu);
      }
      catch
      {
        menu = new CiMenu("Remoting Exception", "Communication with server failed", string.Empty,
                          CiMenuState.Error);
        // pass menu to calling dialog
        TvPlugin.TVHome.ProcessCiMenu(menu);
      }
    }
  }
}
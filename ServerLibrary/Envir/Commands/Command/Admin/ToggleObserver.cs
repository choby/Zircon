using Library;
using Server.Models;

namespace Server.Envir.Commands.Command.Admin
{
    /// <summary>
    /// Toggles the player being seen by players or monsters
    /// </summary>
    class ToggleObserver : AbstractCommand<IAdminCommand>
    {
        public override string VALUE => "OBSERVER";

        public override void Action(PlayerObject player)
        {
            player.Observer = !player.Observer;
            player.Connection.ReceiveChat($"{VALUE} {(player.Observer ? "已启用" : "已禁用")}", MessageType.System);
            player.AddAllObjects();
            player.RemoveAllObjects();
        }
    }
}

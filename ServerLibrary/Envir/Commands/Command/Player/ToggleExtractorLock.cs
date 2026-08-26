using Library;
using Server.Envir.Commands.Command;
using Server.Envir.Commands.Command.Player;
using Server.Models;

namespace Server.Envir.Commands.Player
{
    class ToggleExtractorLock : AbstractCommand<IPlayerCommand>
    {
        public override string VALUE => "EXTRACTORLOCK";

        public override void Action(PlayerObject player)
        {
            player.ExtractorLock = !player.ExtractorLock;
            player.Connection.ReceiveChat(player.ExtractorLock ? "属性提取已启用" : "属性提取已锁定", MessageType.System);
        }
    }
}

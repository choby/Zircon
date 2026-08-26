using Library;
using Server.DBModels;
using Server.Envir.Commands.Exceptions;
using Server.Models;
using G = Library.Network.GeneralPackets;

namespace Server.Envir.Commands.Command.Admin
{
    class Kick : AbstractParameterizedCommand<IAdminCommand>
    {
        public override string VALUE => "KICK";
        public override int PARAMS_LENGTH => 2;

        public override void Action(PlayerObject player, string[] vals)
        {
            if (vals.Length < PARAMS_LENGTH)
                ThrowNewInvalidParametersException();

            CharacterInfo character = SEnvir.GetCharacter(vals[1]);
            if (character == null)
                throw new UserCommandException(string.Format("找不到玩家：{0}。", vals[1]));

            if (character.Account.Connection == null)
                throw new UserCommandException(string.Format("玩家 {0} 不在线。", vals[1]));

            if (player.Character == character)
                throw new UserCommandException("不能将自己踢下线。");

            character.Account.Connection.SendDisconnect(new G.Disconnect { Reason = DisconnectReason.Kicked });
        }
    }
}

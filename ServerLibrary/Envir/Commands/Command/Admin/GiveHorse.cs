using Library;
using Server.DBModels;
using Server.Envir.Commands.Exceptions;
using Server.Models;
using System;

namespace Server.Envir.Commands.Command.Admin
{
    class GiveHorse : AbstractParameterizedCommand<IAdminCommand>
    {
        public override string VALUE => "GIVEHORSE";
        public override int PARAMS_LENGTH => 2;

        public override void Action(PlayerObject player, string[] vals)
        {
            if (vals.Length < PARAMS_LENGTH)
                ThrowNewInvalidParametersException();

            CharacterInfo character = SEnvir.GetCharacter(vals[1]);
            if (character == null)
                throw new UserCommandException(string.Format("找不到玩家：{0}。", vals[1]));

            if (!Enum.TryParse(vals[2], true, out HorseType type))
                throw new UserCommandException(string.Format("找不到坐骑：{0}。", vals[2]));

            if (character.Player != null)
                character.Player.GiveHorse(type);
            else
                character.Account.Horse = type;

            player.Connection.ReceiveChat(string.Format("[发放坐骑] {0}，类型：{1}", character.CharacterName, type.ToString()), MessageType.System);
        }
    }
}

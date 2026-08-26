using Library.SystemModels;
using Server.Envir.Commands.Exceptions;
using Server.Models;
using System;
using System.Drawing;
using System.Linq;

namespace Server.Envir.Commands.Command.Admin
{
    class MapMove : AbstractParameterizedCommand<IAdminCommand>
    {
        public override string VALUE => "MOVE";
        public override int PARAMS_LENGTH => 2;

        public override void Action(PlayerObject player, string[] vals)
        {
            if (vals.Length < PARAMS_LENGTH)
                ThrowNewInvalidParametersException();

            int x = 0, y = 0;

            if (vals.Length == 4)
            {
                if (!int.TryParse(vals[2], out x))
                    throw new UserCommandException(string.Format("解析 X 坐标失败：{0}", vals[2]));

                if (!int.TryParse(vals[3], out y))
                    throw new UserCommandException(string.Format("解析 Y 坐标失败：{0}", vals[3]));
            }

            MapInfo info = SEnvir.MapInfoList.Binding.FirstOrDefault(x => string.Compare(x.FileName, vals[1], StringComparison.OrdinalIgnoreCase) == 0);
            Map map = SEnvir.GetMap(info);
            if (map == null)
                throw new UserCommandException(string.Format("找不到索引为 {0} 的地图", vals[1]));

            if (x > 0 && y > 0)
            {
                if (x > map.Width || y > map.Height)
                    throw new UserCommandException(string.Format("坐标 {0}:{1} 超出地图边界", x, y));

                player.Teleport(map, new Point(x, y));
                return;
            }

            player.Teleport(map, map.GetRandomLocation());
        }
    }
}

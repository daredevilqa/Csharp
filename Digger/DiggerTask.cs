using System.Windows.Forms;
using Digger.Architecture;

namespace Digger
{
    public class Terrain : ICreature
    {
        public string GetImageFileName() => "Terrain.png";
        public int GetDrawingPriority() => 0;
        public CreatureCommand Act(int x, int y) => new CreatureCommand();
        public bool DeadInConflict(ICreature conflictedObject) => conflictedObject.GetType() == typeof(Player);
    }

    public class Player : ICreature
    {
        public string GetImageFileName() => "Digger.png";
        public int GetDrawingPriority() => 5;

        public CreatureCommand Act(int x, int y)
        {
            var command = new CreatureCommand();
            try {
                switch (Game.KeyPressed) {
                    case Keys.Right:
                        CalculateDeltas(command, x, 1, y, 0);
                        break;
                    case Keys.Left:
                        CalculateDeltas(command, x, -1, y, 0);
                        break;
                    case Keys.Up:
                        CalculateDeltas(command, x, 0, y, -1);
                        break;
                    case Keys.Down:
                        CalculateDeltas(command, x, 0, y, 1);
                        break;
                }
            }
            catch { //ignored
            }
            return command;
        }

        private void CalculateDeltas(CreatureCommand command, int x, int dx, int y, int dy)
        {
            var creature = Game.Map[x + dx, y + dy];
            if (creature != null)
                if (creature.GetType() == typeof(Sack)) return;
            command.DeltaX = dx;
            command.DeltaY = dy;
        }

        public bool DeadInConflict(ICreature conflictedObject) =>
            !(conflictedObject.GetType() == typeof(Terrain) || conflictedObject.GetType() == typeof(Gold)
              && conflictedObject.GetType() != typeof(Monster));
    }

    public class Sack : ICreature
    {
        private int fallingDistance;

        public string GetImageFileName() => "Sack.png";
        public int GetDrawingPriority() => 2;

        public CreatureCommand Act(int x, int y)
        {
            var command = new CreatureCommand();
            if (y == Game.MapHeight - 1) {
                if (fallingDistance > 1)
                    command.TransformTo = new Gold();
                return command;
            }
            if (Game.Map[x, y + 1] == null) {
                command.DeltaY = 1;
                fallingDistance++;
                return command;
            }
            if (fallingDistance > 1 && (Game.Map[x, y + 1] is Player || Game.Map[x, y + 1] is Monster))
                command.DeltaY = 1;
            if (fallingDistance > 1 && !(Game.Map[x, y + 1] is Player || Game.Map[x, y + 1] is Monster))
                command.TransformTo = new Gold();
            return command;
        }

        public bool DeadInConflict(ICreature conflictedObject)
        {
            if (conflictedObject.GetType() == typeof(Player)) {
                Game.IsOver = true;
            }
            return false;
        }
    }

    public class Gold : ICreature
    {
        public string GetImageFileName() => "Gold.png";
        public int GetDrawingPriority() => 1;
        public CreatureCommand Act(int x, int y) => new CreatureCommand();

        public bool DeadInConflict(ICreature conflictedObject)
        {
            if (conflictedObject.GetType() == typeof(Player)) {
                Game.Scores += 10;
                return true;
            }
            return conflictedObject.GetType() == typeof(Monster);
        }
    }

    public class Monster : ICreature
    {
        private int digX, digY;

        private void GetDiggerLocation()
        {
            for (digX = 0; digX < Game.MapWidth; digX++) {
                for (digY = 0; digY < Game.MapHeight; digY++) {
                    var digger = Game.Map[digX, digY];
                    if (digger is Player) {
                        return;
                    }
                }
            }
        }

        public string GetImageFileName() => "Monster.png";
        public int GetDrawingPriority() => 10;

        public CreatureCommand Act(int x, int y)
        {
            GetDiggerLocation();
            var command = new CreatureCommand();

            if (x == digX) {

            }
            if (y == digY) {

            }

            for (var i = x; i < Game.MapWidth; i++) {
                for (var j = y; j < Game.MapHeight; j++) {
                    var creature = Game.Map[i, j];
                }
            }

            return command;
        }

        public bool DeadInConflict(ICreature conflictedObject)
        {
            if (conflictedObject.GetType() == typeof(Player)) {
                Game.IsOver = true;
                return false;
            }

            return conflictedObject.GetType() == typeof(Sack);
        }
    }
}

using WolfShooter.BaseEntities;

namespace WolfShooter.Models
{
    public class Hero
    {
        private readonly Texture fireballTexture;
        private readonly List<Fireball> fireballs;
        public Vector HeroModel { get; }

        public Vector Position { get; private set; }

        public Vector Plane { get; } 
        public double Velocity { get; set; }
        public double RotationSpeed { get; set; }

        public double Health { get; set; }

        public Hero(Vector position, Vector heroModel)
        {
            Position = position;
            HeroModel = heroModel;
            Plane = new Vector(0, 0.66);
            Health = 100.0;
            fireballTexture = new Texture(new Bitmap("Images/fire.png"));
            fireballTexture.InitializeColorArray();
            fireballs = new List<Fireball>();
        }


        public int CastWall(int x, int width, int height, out int mapX, out int mapY, out int texX, out double perpWallDist)
        {
            var side = 0;
            var cameraX = 2 * x / (double)width - 1;
            var rayDir = HeroModel + Plane * cameraX;
            var deltaDist = new Vector(Math.Abs(1 / rayDir.X), Math.Abs(1 / rayDir.Y));

            perpWallDist = 0;
            mapX = (int)Position.X;
            mapY = (int)Position.Y;
            var stepX = rayDir.X < 0 ? -1 : 1;
            var stepY = rayDir.Y < 0 ? -1 : 1;
            var isHitted = false;
            
            var sideDist = GetSideDist(rayDir, mapX, mapY, deltaDist);
            while (!isHitted)
            {
                if (sideDist.X < sideDist.Y)
                {
                    sideDist.X += deltaDist.X;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDist.Y += deltaDist.Y;
                    mapY += stepY;
                    side = 1;
                }
                
                if (Map.MapObjects[mapX][mapY] > 0) isHitted = true;
            }
            texX = 0;
            if (!isHitted)
                return 0;
            if (side == 0)
                perpWallDist = sideDist.X - deltaDist.X;
            else
                perpWallDist = sideDist.Y - deltaDist.Y;
            var hitX = side == 0 ? Position.Y + perpWallDist * rayDir.Y : Position.X + perpWallDist * rayDir.X;
            texX = (int)((hitX - Math.Floor(hitX)) * 64);
            
            
            switch (side)
            {
                case 0 when rayDir.X > 0:
                case 1 when rayDir.Y < 0:
                    hitX = 64 - hitX - 1;
                    break;
            }

            return (int)(height / perpWallDist);

        }

        public void Rotate(double angle)
        {
            Plane.Rotate(angle * RotationSpeed);
            HeroModel.Rotate(angle * RotationSpeed);
        }

        private Vector GetSideDist(Vector rayDir, double mapX, double mapY, Vector deltaDist)
        {
            var sideDist = new Vector(0, 0);
            if (rayDir.X < 0)
                sideDist.X = (Position.X - mapX) * deltaDist.X;
            else
                sideDist.X = (mapX + 1.0 - Position.X) * deltaDist.X;

            if (rayDir.Y < 0)
                sideDist.Y = (Position.Y - mapY) * deltaDist.Y;
            else
                sideDist.Y = (mapY + 1.0 - Position.Y) * deltaDist.Y;
            return sideDist;
        }

        public void Move()
        {
            var newPosition = Position + HeroModel * Velocity;
            if (Map.MapObjects[(int)newPosition.X][(int)newPosition.Y] == 0)
                Position = newPosition;
            foreach (var fireball in fireballs.ToList())
            {
                fireball.Position += fireball.Direction;
                if (!Map.InBounds(fireball.Position))
                    fireballs.Remove(fireball);
                else if (Map.MapObjects[(int)fireball.Position.X][(int)fireball.Position.Y] != 0)
                    fireballs.Remove(fireball);
            }
        }

        public IEnumerable<Fireball> GetFireballs()
        {
            return fireballs.ToList();
        }

        public void RemoveFireball(Fireball fireball)
        {
            fireballs.Remove(fireball);
        }

        public void Fire()
        {
            var fireballSprite = new Sprite(Position, fireballTexture);
            if (!(HeroModel.Length() > 0)) return;
            
            var fireball = new Fireball(fireballSprite, Position + HeroModel)
            {
                Direction = HeroModel * 0.4
            };
            fireballs.Add(fireball);
        }
    }
}

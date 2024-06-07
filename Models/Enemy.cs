using WolfShooter.BaseEntities;

namespace WolfShooter.Models
{
    public class Enemy : ICreature
    {
        public Sprite Sprite => sprite;
        public Vector Position { get => sprite.Position; set => sprite.Position = value; }
        public double Health { get; set; }
        public Vector Direction { get; set; }


        private Sprite sprite;
        private readonly Texture fireballTexture;
        private readonly List<Fireball> fireballs;
        public Enemy(Sprite sprite, Vector position, double health)
        {
            this.sprite = sprite;
            Position = position;
            Health = health;
            Direction = new Vector(0.1, 0.0);
            fireballTexture = new Texture(new Bitmap("Images/fire.png"));
            fireballTexture.InitializeColorArray();
            fireballs = new List<Fireball>();
        }

        public void Move()
        {
            var newPosition = Position + Direction;
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

        public void TakeDamage(double value)
        {
            Health -= value;
        }

        public void Fire()
        {
            var fireballSprite = new Sprite(Position, fireballTexture);
            if (!(Direction.Length() > 0)) return;
            
            var fireball = new Fireball(fireballSprite, Position + Direction)
            {
                Direction = Direction * 2
            };
            fireballs.Add(fireball);
        }

        public IEnumerable<Fireball> GetFireballs()
        {
            return fireballs.ToList();
        }

        public void RemoveFireball(Fireball fireball)
        {
            fireballs.Remove(fireball);
        }
    }
}

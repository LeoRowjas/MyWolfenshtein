using WolfShooter.BaseEntities;

namespace WolfShooter.Models
{
    public class Fireball : ICreature
    {
        public Sprite Sprite => sprite;
        public Vector Position { get => sprite.Position; set => sprite.Position = value; }
        public Vector Direction { get; set; }

        private Sprite sprite;
        
        public Fireball(Sprite sprite, Vector position)
        {
            this.sprite = sprite;
            Position = position;
            Direction = new Vector(0.0, 0.0);
        }

        public void Move()
        {
            Position += Direction;
        }

        public void TakeDamage(double value)
        {
            throw new NotImplementedException();
        }
    }
}

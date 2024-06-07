namespace WolfShooter.BaseEntities
{
    public interface ICreature
    {
        public Sprite Sprite { get; }
        public Vector Position { get; set; }
        public Vector Direction { get; set; }
        private const double MaximumCollideDistance = 0.4;
        
        
        public void Move();
        public void TakeDamage(double value);

        public bool IsCollidedWith(ICreature other)
        {
            var distance = (other.Position - Position).Length();
            return distance < 0.4;
        }

        public bool IsCollidedWith(Sprite other)
        {
            var distance = (other.Position - Position).Length();
            return distance < MaximumCollideDistance;
        }
    }
}

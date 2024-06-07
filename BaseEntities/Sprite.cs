namespace WolfShooter.BaseEntities
{
    public struct Sprite
    {
        public Vector Position;
        public readonly Texture Texture;

        public Sprite(Vector position, Texture texture)
        {
            Texture = texture;
            Position = position;
        }
    }
}

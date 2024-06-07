using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using WolfShooter.BaseEntities;
using WolfShooter.Models;

namespace WolfShooter
{
    public sealed class Form1 : Form
    {
        private System.Windows.Forms.Timer t;
        private System.Windows.Forms.Timer fireTimer;
        private Hero hero;
        private readonly List<Texture> textures = new();
        private readonly List<Texture> weaponTextures = new();
        private readonly List<Enemy> enemies = new();
        private Texture currentWeaponTexture;
        private Color[,] buffer;
        private double[] zBuffer;
        private Label infoLabel;

        private readonly List<Vector> enemiesPositions = new()
        {
            new Vector(2, 2),
            new Vector(7, 2),
            new Vector(11, 2),
            new Vector(6, 5),
            new Vector(10, 5),
            new Vector(15, 5),
            new Vector(15, 15),
            new Vector(15, 10),
            new Vector(4, 18),

        };

        public Form1()
        {
            InitializeFields();
            InitializeTextures(out var redAdidas, out var blueAdidas);
            InitializeHero();
            SpawnEnemies(redAdidas, blueAdidas);
            AddLabel();
            
            t?.Start();
            fireTimer?.Start();
        }

        private void AddLabel()
        {
            infoLabel = new Label
            {
                Text = $"Health: {hero!.Health:0}",
                Location = new Point(10, 520),
                AutoSize = true,
                
            };
            Controls.Add(infoLabel);
        }

        private void SpawnEnemies(Texture redAdidas, Texture blueAdidas)
        {
            var offset = 0;
            foreach (var enemyPosition in enemiesPositions)
            {
                var texture = offset % 2 == 0 ? redAdidas : blueAdidas;
                var enemy = new Enemy(new Sprite(enemyPosition, texture), enemyPosition, 100.0);
                enemies.Add(enemy);
                offset++;
            }
        }

        private void InitializeHero()
        {
            hero = new Hero(new Vector(18, 5), new Vector(-1.0, 0.0));
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
        }

        private void InitializeTextures(out Texture redAdidas, out Texture blueAdidas)
        {
            var basedTexture = new Texture("Images/wolftextures.png");
            for (var i = 0; i < 512; i+=64)
            {
                var clonedRect = new Rectangle(i, 0, 64, 64);
                var clonedImage = basedTexture.Image.Clone(clonedRect, basedTexture.Image.PixelFormat);
                var texture = new Texture(clonedImage);
                texture.InitializeColorArray();
                textures.Add(texture);
            }

            var basedWeaponTexture = new Texture("Images/weapon.png");
            var weaponIdleImage = basedWeaponTexture.Image.Clone(new Rectangle(0, 0, 150, 150), 
                basedWeaponTexture.Image.PixelFormat);
            var weaponIdleTexture = new Texture(weaponIdleImage);
            weaponIdleTexture.InitializeColorArray();
            weaponTextures.Add(weaponIdleTexture);

            blueAdidas = new Texture("Images/adikblue.png");
            redAdidas = new Texture("Images/adikred.png");
            redAdidas.InitializeColorArray();
            blueAdidas.InitializeColorArray();
            
            currentWeaponTexture = weaponTextures[0];
        }

        private void InitializeFields()
        {
            Width = 600;
            Height = 600;
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            buffer = new Color[Width, Height];
            zBuffer = new double[Width];
            DoubleBuffered = true;
            t = new System.Windows.Forms.Timer();
            t.Interval = 30;
            t.Tick += TimerLoop!;
            fireTimer = new System.Windows.Forms.Timer();
            fireTimer.Interval = 2000;
            fireTimer.Tick += FireLoop!;
        }

        private void Form1_KeyUp(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                case Keys.S:
                    hero.Velocity = 0;
                    break;
                case Keys.A:
                case Keys.D:
                    hero.RotationSpeed = 0;
                    break;
            }
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                    hero.Velocity = 0.3;
                    break;
                case Keys.S:
                    hero.Velocity = -0.3;
                    break;
                case Keys.A:
                    hero.RotationSpeed = -1.0;
                    break;
                case Keys.D:
                    hero.RotationSpeed = 1.0;
                    break;
                case Keys.Space:
                    hero.Fire();
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawFloors();
            DrawWalls();
            var sortedSprites = enemies
                .OrderByDescending(x => Math.Abs((hero.Position - x.Sprite.Position).Length()))
                .Where(enemy => (hero.Position - enemy.Position).Length() < 7)
                .Select(x => x.Sprite)
                .ToArray();

            var fireballs = enemies.
                SelectMany(x => x.GetFireballs())
                .Select(fireball => fireball.Sprite)
                .Concat(hero
                    .GetFireballs()
                    .Select(fireball => fireball.Sprite))
                .ToArray();
            
            DrawSprites(sortedSprites);
            DrawSprites(fireballs);
            var bufferImage = BufferToImage(new Bitmap(Width, Height));
            e.Graphics.DrawImage(bufferImage, 0, 0, 600, 580);
        }

        private void DrawWalls()
        {
            Parallel.For(0, Width, (x) =>
            {
                var lineHeight = hero.CastWall(x, Width, Height, out var mapX, out var mapY, out var hitX, out var perpWallDist);
                if (lineHeight <= 0) return;
                
                var drawStart = -lineHeight / 2 + Height / 2;

                if (drawStart < 0) drawStart = 0;
                var drawEnd = lineHeight / 2 + Height / 2;
                if (drawEnd > Height) drawEnd = Height - 1;

                var textureId = Map.MapObjects[mapX][mapY] - 1;

                var step = 64 / ((double)lineHeight + 1);
                var texY = 0.0;

                for (var y = drawStart; y < drawEnd; y++)
                {
                    texY += step;

                    var color = textures[textureId].GetPixel(hitX, (int)texY);

                    buffer[x, y] = color;
                    zBuffer[x] = perpWallDist;
                }
            });
        }

        private void DrawFloors()
        {
            for (var y = 0; y < Height; y++)
            {
                var rayDirLeft = hero.HeroModel - hero.Plane;
                var rayDirRight = hero.HeroModel + hero.Plane;

                var p = y - Height / 2;
                var posZ = 0.5 * Height;
                var rowDistance = posZ / p;

                var floorStepX = rowDistance * (rayDirRight.X - rayDirLeft.X) / Width;
                var floorStepY = rowDistance * (rayDirRight.Y - rayDirLeft.Y) / Width;

                var floorX = hero.Position.X + rowDistance * rayDirLeft.X;
                var floorY = hero.Position.Y + rowDistance * rayDirLeft.Y;

                for (var x = 0; x < Width; ++x)
                {
                    var cellX = (int)floorX;
                    var cellY = (int)floorY;

                    var tx = (int)(64 * (floorX - cellX)) & (64 - 1);
                    var ty = (int)(64 * (floorY - cellY)) & (64 - 1);

                    floorX += floorStepX;
                    floorY += floorStepY;

                    buffer[x, y] = textures[3].GetPixel(tx, ty);
                    buffer[x, Height - y - 1] = textures[4].GetPixel(tx, ty);
                }
            }
        }

        private Bitmap BufferToImage(Bitmap bufferImage)
        {
            var bitmapData = bufferImage.LockBits(new Rectangle(0, 0, bufferImage.Width, bufferImage.Height), ImageLockMode.ReadWrite,
                            bufferImage.PixelFormat);
            var bytesPerPixel = Image.GetPixelFormatSize(bufferImage.PixelFormat) / 8;
            var byteCount = bitmapData.Stride * bufferImage.Height;
            var pixels = new byte[byteCount];
            var ptrFirstPixel = bitmapData.Scan0;
            var heightInPixels = bitmapData.Height;
            var widthInBytes = bitmapData.Width * bytesPerPixel;

            for (var x = 200; x < 200 + currentWeaponTexture.Image.Width; x++)
                for (var y = 425; y < 425 + currentWeaponTexture.Image.Height; y++)
                {
                    var color = currentWeaponTexture.GetPixel(x - 200, y - 425);
                    if (color.R != 0 && color.G != 0 && color.B != 0)
                        buffer[x, y] = color;
                }

            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

            Parallel.For(0, heightInPixels, (y) =>
            {
                var currentLine = y * bitmapData.Stride;
                var buffX = 0;
                for (var x = 0; x < widthInBytes; x += bytesPerPixel)
                {

                    pixels[currentLine + x] = buffer[buffX, y].B;
                    pixels[currentLine + x + 1] = buffer[buffX, y].G;
                    pixels[currentLine + x + 2] = buffer[buffX, y].R;
                    pixels[currentLine + x + 3] = 255;
                    buffX++;
                }
            }

            );

            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bufferImage.UnlockBits(bitmapData);
            return bufferImage;
        }

        private void DrawSprites(Sprite[] sortedSprites)
        {
            foreach (var sprite in sortedSprites)
            {
                var spriteRelatedPosition = sprite.Position - hero.Position;

                var invDet = 1.0 / (hero.Plane.X * hero.HeroModel.Y - hero.HeroModel.X * hero.Plane.Y);
                var transformX = invDet * (hero.HeroModel.Y * spriteRelatedPosition.X
                                           - hero.HeroModel.X * spriteRelatedPosition.Y);
                var transformY = invDet * (-hero.Plane.Y * spriteRelatedPosition.X
                                           + hero.Plane.X * spriteRelatedPosition.Y);

                var spriteScreenX = (int)(Width / 2 * (1 + transformX / transformY));

                var spriteHeight = transformY == 0 ? 0 : Math.Abs((int)(Height / transformY));
                var drawStartY = -spriteHeight / 2 + Height / 2;
                if (drawStartY < 0) drawStartY = 0;
                var drawEndY = spriteHeight / 2 + Height / 2;
                if (drawEndY > Width) drawEndY = Width - 1;

                var spriteWidth = transformY == 0 ? 0 : Math.Abs((int)(Width / transformY));
                var drawStartX = -spriteWidth / 2 + spriteScreenX;
                if (drawStartX < 0) drawStartX = 0;
                var drawEndX = spriteWidth / 2 + spriteScreenX;
                if (drawEndX >= Width) drawEndX = Width - 1;

                for (var stripe = drawStartX; stripe < drawEndX; stripe++)
                {
                    var texX = 256 * (stripe
                                      - (-spriteWidth / 2 + spriteScreenX)) * 64 / spriteWidth / 256;
                    
                    if (!(transformY > 0) || stripe <= 0 || stripe >= Width || !(transformY < zBuffer[stripe]))
                        continue;
                    
                    for (var y = drawStartY; y < drawEndY; y++)
                    {
                        var d = y * 256 - Height * 128 + spriteHeight * 128;
                        var texY = ((d * 64) / spriteHeight) / 256;
                        if (texX < 0 || texY < 0)
                            continue;
                        var color = sprite.Texture.GetPixel(texX, texY);
                        if (color is { R: 0, G: 0, B: 0 })
                            continue;
                        buffer[stripe, y] = color;
                    }
                }
            }
        }

        private void TimerLoop(object sender, EventArgs e)
        {
            if (hero.Health > 0)
            {
                hero.Move();
                hero.Rotate(0.1);
                infoLabel.Text = $"Health: {hero.Health}";
                foreach (var enemy in enemies.ToList())
                {
                    var distVector = hero.Position - enemy.Position;
                    if (distVector.Length() > 2)
                        enemy.Direction = distVector.Normalize() * 0.1;
                    else
                        enemy.Direction = new Vector(0, 0);
                    enemy.Move();
                    foreach (var fireball in enemy.GetFireballs())
                    {
                        var distance = (hero.Position - fireball.Position).Length();
                        if (!(distance < 0.5)) continue;
                        
                        enemy.RemoveFireball(fireball);
                        hero.Health -= 10;
                    }

                    foreach (var fireball in hero.GetFireballs())
                    {
                        var distance = (enemy.Position - fireball.Position).Length();
                        if (!(distance < 1)) continue;
                        
                        hero.RemoveFireball(fireball);
                        enemy.Health -= 50;
                        if (enemy.Health <= 0)
                            enemies.Remove(enemy);
                    }
                }
            }
            else
            {
                infoLabel.Text = "you died!!!";
            }
            Refresh();
        }

        private void FireLoop(object sender, EventArgs e)
        {
            var nearestEnemy = enemies.MinBy(x => (x.Position - hero.Position).Length());
            nearestEnemy?.Fire();
        }
    }
}

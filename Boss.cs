using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public enum BossType
{
    // Tier 1 (floors 1-2)
    Bouncer,    // DVD-logo movement, shoots at player every second
    Gusher,     // Moves on x/y axis only until hitting a wall, chases player on axis (Gusher from Isaac)
    Spawner,    // DVD-logo movement, spawns flying enemies every 5 seconds
    
    // Tier 2 (floors 3-4) — reserved
    // Tier 3 (floors 5-7) — reserved
}

public class Boss
{
    public BossType Type;
    public Vector2 Position;
    public float Size;
    public int Health;
    public int MaxHealth;
    public bool IsAlive;
    public Color Color;

    // DVD bounce movement
    private Vector2 velocity;

    // Shooting
    private float shootTimer;

    // Gusher axis movement
    private enum GusherDir { Up, Down, Left, Right }
    private GusherDir gusherDir;
    private float gusherSpeed;

    // Spawner
    private float spawnTimer;

    // Hit feedback
    private float invincibilityTimer;
    private float hitFlashTimer;
    private const float InvDuration  = 0.2f;
    private const float FlashDuration = 0.1f;

    private Random rand;

    // Room pixel boundaries (set on spawn)
    private float innerLeft, innerRight, innerTop, innerBottom;

    public Boss(BossType type, Vector2 position, float innerLeft, float innerRight, float innerTop, float innerBottom)
    {
        Type = type;
        Position = position;
        IsAlive = true;
        rand = new Random();

        this.innerLeft   = innerLeft;
        this.innerRight  = innerRight;
        this.innerTop    = innerTop;
        this.innerBottom = innerBottom;

        switch (type)
        {
            case BossType.Bouncer:
                MaxHealth = 30;
                Size = 22;
                Color = new Color(180, 50, 220, 255); // purple
                // Random initial diagonal velocity
                float bx = rand.NextDouble() < 0.5 ? 1 : -1;
                float by = rand.NextDouble() < 0.5 ? 1 : -1;
                velocity = new Vector2((float)bx, (float)by) * 120f;
                shootTimer = 1.0f;
                break;

            case BossType.Gusher:
                MaxHealth = 30;
                Size = 22;
                Color = new Color(50, 180, 80, 255); // green
                gusherSpeed = 140f;
                // Start moving in a random cardinal direction
                gusherDir = (GusherDir)rand.Next(4);
                break;

            case BossType.Spawner:
                MaxHealth = 20;
                Size = 22;
                Color = new Color(220, 120, 30, 255); // orange
                float sx = rand.NextDouble() < 0.5 ? 1 : -1;
                float sy = rand.NextDouble() < 0.5 ? 1 : -1;
                velocity = new Vector2((float)sx, (float)sy) * 100f;
                spawnTimer = 5.0f;
                break;
        }

        Health = MaxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (invincibilityTimer > 0) return;
        invincibilityTimer = InvDuration;
        hitFlashTimer = FlashDuration;
        Health -= damage;
        if (Health <= 0) { Health = 0; IsAlive = false; SoundManager.Play("boss_death"); }
        else SoundManager.Play("boss_hurt");
    }

    // Returns list of enemies to spawn (only Spawner uses this)
    public List<Enemy> Update(float dt, Vector2 playerPos, List<Bullet> bullets, float playerSpeed)
    {
        if (!IsAlive) return new List<Enemy>();

        if (invincibilityTimer > 0) invincibilityTimer -= dt;
        if (hitFlashTimer > 0) hitFlashTimer -= dt;

        var spawned = new List<Enemy>();

        switch (Type)
        {
            case BossType.Bouncer:
                UpdateBouncer(dt, playerPos, bullets);
                break;
            case BossType.Gusher:
                UpdateGusher(dt, playerPos);
                break;
            case BossType.Spawner:
                UpdateSpawner(dt, playerPos, spawned, playerSpeed);
                break;
        }

        return spawned;
    }

    private void UpdateBouncer(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        // Move with DVD bounce
        Position += velocity * dt;
        BounceOffWalls();

        // Shoot at player every second
        shootTimer -= dt;
        if (shootTimer <= 0)
        {
            shootTimer = 1.0f;
            Vector2 dir = Vector2.Normalize(playerPos - Position);
            bullets.Add(new Bullet(Position, dir * 160f, 6f, false));
            SoundManager.Play("enemy_shoot");
        }
    }

    private void UpdateGusher(float dt, Vector2 playerPos)
    {
        // Check if player is aligned on current axis — if so, chase on that axis
        bool playerOnX = Math.Abs(playerPos.Y - Position.Y) < Size * 2; // same row
        bool playerOnY = Math.Abs(playerPos.X - Position.X) < Size * 2; // same column

        if (playerOnX)
        {
            // Chase player horizontally
            gusherDir = playerPos.X > Position.X ? GusherDir.Right : GusherDir.Left;
        }
        else if (playerOnY)
        {
            // Chase player vertically
            gusherDir = playerPos.Y > Position.Y ? GusherDir.Down : GusherDir.Up;
        }

        Vector2 move = gusherDir switch
        {
            GusherDir.Up    => new Vector2(0, -gusherSpeed * dt),
            GusherDir.Down  => new Vector2(0,  gusherSpeed * dt),
            GusherDir.Left  => new Vector2(-gusherSpeed * dt, 0),
            GusherDir.Right => new Vector2( gusherSpeed * dt, 0),
            _ => Vector2.Zero
        };

        Vector2 newPos = Position + move;

        // Bounce off walls, pick a new perpendicular direction
        if (newPos.X - Size < innerLeft)  { newPos.X = innerLeft + Size;  gusherDir = GusherDir.Right; }
        if (newPos.X + Size > innerRight) { newPos.X = innerRight - Size; gusherDir = GusherDir.Left; }
        if (newPos.Y - Size < innerTop)   { newPos.Y = innerTop + Size;   gusherDir = GusherDir.Down; }
        if (newPos.Y + Size > innerBottom){ newPos.Y = innerBottom - Size; gusherDir = GusherDir.Up; }

        Position = newPos;
    }

    private void UpdateSpawner(float dt, Vector2 playerPos, List<Enemy> spawned, float playerSpeed)
    {
        // DVD bounce
        Position += velocity * dt;
        BounceOffWalls();

        // Spawn flying enemies every 5 seconds
        spawnTimer -= dt;
        if (spawnTimer <= 0)
        {
            spawnTimer = 5.0f;
            // Spawn 2 flying enemies near boss position with slight offset
            for (int i = 0; i < 2; i++)
            {
                float angle = (float)(rand.NextDouble() * Math.PI * 2);
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (Size + 30);
                spawned.Add(new Enemy(EnemyType.Flying, Position + offset, playerSpeed));
            }
        }
    }

    private void BounceOffWalls()
    {
        if (Position.X - Size < innerLeft)  { Position.X = innerLeft + Size;  velocity.X = Math.Abs(velocity.X); }
        if (Position.X + Size > innerRight) { Position.X = innerRight - Size; velocity.X = -Math.Abs(velocity.X); }
        if (Position.Y - Size < innerTop)   { Position.Y = innerTop + Size;   velocity.Y = Math.Abs(velocity.Y); }
        if (Position.Y + Size > innerBottom){ Position.Y = innerBottom - Size; velocity.Y = -Math.Abs(velocity.Y); }
    }

    public void Draw(Font font)
    {
        if (!IsAlive) return;

        Color drawColor = hitFlashTimer > 0 ? Color.White : Color;
        Raylib.DrawCircleV(Position, Size, drawColor);
        // Outline
        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, (int)Size, Color.White);

        // Health bar
        float barW = Size * 3;
        float barH = 5;
        float pct = (float)Health / MaxHealth;
        Raylib.DrawRectangle((int)(Position.X - barW / 2), (int)(Position.Y - Size - 10), (int)barW, (int)barH, Color.DarkGray);
        Raylib.DrawRectangle((int)(Position.X - barW / 2), (int)(Position.Y - Size - 10), (int)(barW * pct), (int)barH, Color.Red);

        // Boss name label
        string name = Type switch
        {
            BossType.Bouncer => "BOUNCER",
            BossType.Gusher  => "GUSHER",
            BossType.Spawner => "SPAWNER",
            _ => "BOSS"
        };
        Vector2 nameSize = Raylib.MeasureTextEx(font, name, 14, 1);
        Raylib.DrawTextEx(font, name, new Vector2(Position.X - nameSize.X / 2, Position.Y - Size - 24), 14, 1, Color.White);
    }

    public bool IsCollidingWithPlayer(Vector2 playerPos, float playerSize)
    {
        return Vector2.Distance(Position, playerPos) < Size + playerSize / 2;
    }

    public static int GetTierForFloor(int floor) => floor switch
    {
        1 or 2 => 1,
        3 or 4 => 2,
        5 or 6 or 7 => 3,
        _ => 1
    };

    public static BossType[] GetTierBosses(int tier) => tier switch
    {
        1 => new[] { BossType.Bouncer, BossType.Gusher, BossType.Spawner },
        _ => new[] { BossType.Bouncer, BossType.Gusher, BossType.Spawner } // expand for tier 2/3
    };
}
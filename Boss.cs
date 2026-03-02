using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public enum BossType
{
    // Tier 1 (floors 1-2)
    Bouncer,        // DVD bounce, shoots at player every second
    Gusher,         // Axis-locked movement, chases player on alignment
    Spawner,        // DVD bounce, spawns 2 flying enemies every 5s

    // Tier 2 (floors 3-4)
    Chaser,         // Moves toward player, shoots cross pattern, 60 HP
    Orbiter,        // DVD bounce, 4 orbiting flying minions that shoot
    Berserker,      // Every 3s: jump at player OR 5-bullet spread
}

public class OrbitalMinion
{
    public float Angle;       // current angle in radians
    public float ShootTimer;

    public OrbitalMinion(float startAngle)
    {
        Angle = startAngle;
        ShootTimer = startAngle; // stagger shots
    }
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

    // DVD bounce
    private Vector2 velocity;

    // Shooting / attack timer
    private float shootTimer;
    private float attackTimer;

    // Gusher
    private enum GusherDir { Up, Down, Left, Right }
    private GusherDir gusherDir;
    private float gusherSpeed;

    // Spawner
    private float spawnTimer;

    // Chaser
    private float chaserSpeed;

    // Orbiter — 4 orbital minions
    private List<OrbitalMinion> orbitals = new();
    private const float OrbitRadius = 60f;
    private const float OrbitSpeed  = 1.8f; // radians/sec
    private const float OrbitalShootCooldown = 2.0f;

    // Berserker
    private bool berserkerJumping;
    private Vector2 berserkerJumpStart;
    private Vector2 berserkerJumpTarget;
    private float berserkerJumpTimer;
    private const float BerserkerJumpDuration = 0.6f;

    // Hit feedback
    private float invincibilityTimer;
    private float hitFlashTimer;
    private const float InvDuration   = 0.2f;
    private const float FlashDuration = 0.1f;

    private Random rand;
    private float innerLeft, innerRight, innerTop, innerBottom;

    public Boss(BossType type, Vector2 position,
                float innerLeft, float innerRight, float innerTop, float innerBottom)
    {
        Type     = type;
        Position = position;
        IsAlive  = true;
        rand     = new Random();

        this.innerLeft   = innerLeft;
        this.innerRight  = innerRight;
        this.innerTop    = innerTop;
        this.innerBottom = innerBottom;

        switch (type)
        {
            case BossType.Bouncer:
                MaxHealth = 30; Size = 22;
                Color = new Color(180, 50, 220, 255);
                velocity = RandDiag() * 120f;
                shootTimer = 1.0f;
                break;

            case BossType.Gusher:
                MaxHealth = 30; Size = 22;
                Color = new Color(50, 180, 80, 255);
                gusherSpeed = 140f;
                gusherDir = (GusherDir)rand.Next(4);
                break;

            case BossType.Spawner:
                MaxHealth = 20; Size = 22;
                Color = new Color(220, 120, 30, 255);
                velocity = RandDiag() * 100f;
                spawnTimer = 5.0f;
                break;

            case BossType.Chaser:
                MaxHealth = 60; Size = 26;
                Color = new Color(200, 60, 60, 255);
                chaserSpeed = 90f;
                shootTimer = 1.5f;
                break;

            case BossType.Orbiter:
                MaxHealth = 50; Size = 24;
                Color = new Color(60, 120, 220, 255);
                velocity = RandDiag() * 110f;
                // 4 evenly-spaced orbital minions
                for (int i = 0; i < 4; i++)
                    orbitals.Add(new OrbitalMinion((float)(i * Math.PI / 2)));
                break;

            case BossType.Berserker:
                MaxHealth = 70; Size = 28;
                Color = new Color(220, 80, 30, 255);
                attackTimer = 3.0f;
                break;
        }

        Health = MaxHealth;
    }

    private Vector2 RandDiag()
    {
        float x = rand.NextDouble() < 0.5 ? 1f : -1f;
        float y = rand.NextDouble() < 0.5 ? 1f : -1f;
        return Vector2.Normalize(new Vector2(x, y));
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

    // Returns newly spawned enemies (Spawner / Orbiter use this)
    public List<Enemy> Update(float dt, Vector2 playerPos, List<Bullet> bullets, float playerSpeed)
    {
        if (!IsAlive) return new List<Enemy>();

        if (invincibilityTimer > 0) invincibilityTimer -= dt;
        if (hitFlashTimer      > 0) hitFlashTimer      -= dt;

        var spawned = new List<Enemy>();

        switch (Type)
        {
            case BossType.Bouncer:   UpdateBouncer(dt, playerPos, bullets); break;
            case BossType.Gusher:    UpdateGusher(dt, playerPos);           break;
            case BossType.Spawner:   UpdateSpawner(dt, playerPos, spawned, playerSpeed); break;
            case BossType.Chaser:    UpdateChaser(dt, playerPos, bullets);  break;
            case BossType.Orbiter:   UpdateOrbiter(dt, playerPos, bullets); break;
            case BossType.Berserker: UpdateBerserker(dt, playerPos, bullets); break;
        }

        return spawned;
    }

    // ── Tier 1 ──────────────────────────────────────────────────────────────

    private void UpdateBouncer(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        Position += velocity * dt;
        BounceOffWalls();
        shootTimer -= dt;
        if (shootTimer <= 0)
        {
            shootTimer = 1.0f;
            bullets.Add(new Bullet(Position, Vector2.Normalize(playerPos - Position) * 160f, 6f));
            SoundManager.Play("enemy_shoot");
        }
    }

    private void UpdateGusher(float dt, Vector2 playerPos)
    {
        bool playerOnRow = Math.Abs(playerPos.Y - Position.Y) < Size * 2;
        bool playerOnCol = Math.Abs(playerPos.X - Position.X) < Size * 2;

        if (playerOnRow)
            gusherDir = playerPos.X > Position.X ? GusherDir.Right : GusherDir.Left;
        else if (playerOnCol)
            gusherDir = playerPos.Y > Position.Y ? GusherDir.Down : GusherDir.Up;

        Vector2 move = gusherDir switch
        {
            GusherDir.Up    => new Vector2(0, -gusherSpeed * dt),
            GusherDir.Down  => new Vector2(0,  gusherSpeed * dt),
            GusherDir.Left  => new Vector2(-gusherSpeed * dt, 0),
            GusherDir.Right => new Vector2( gusherSpeed * dt, 0),
            _ => Vector2.Zero
        };

        Vector2 np = Position + move;
        if (np.X - Size < innerLeft)  { np.X = innerLeft  + Size; gusherDir = GusherDir.Right; }
        if (np.X + Size > innerRight) { np.X = innerRight - Size; gusherDir = GusherDir.Left;  }
        if (np.Y - Size < innerTop)   { np.Y = innerTop   + Size; gusherDir = GusherDir.Down;  }
        if (np.Y + Size > innerBottom){ np.Y = innerBottom- Size; gusherDir = GusherDir.Up;    }
        Position = np;
    }

    private void UpdateSpawner(float dt, Vector2 playerPos, List<Enemy> spawned, float playerSpeed)
    {
        Position += velocity * dt;
        BounceOffWalls();
        spawnTimer -= dt;
        if (spawnTimer <= 0)
        {
            spawnTimer = 5.0f;
            for (int i = 0; i < 2; i++)
            {
                float a = (float)(rand.NextDouble() * Math.PI * 2);
                Vector2 off = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * (Size + 30);
                spawned.Add(new Enemy(EnemyType.Flying, Position + off, playerSpeed));
            }
        }
    }

    // ── Tier 2 ──────────────────────────────────────────────────────────────

    private void UpdateChaser(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        // Move toward player
        Vector2 dir = playerPos - Position;
        if (dir.Length() > Size)
            Position += Vector2.Normalize(dir) * chaserSpeed * dt;

        ClampToRoom();

        // Shoot cross pattern (4 cardinal directions) periodically
        shootTimer -= dt;
        if (shootTimer <= 0)
        {
            shootTimer = 1.5f;
            float[] angles = { 0, (float)Math.PI / 2, (float)Math.PI, (float)(3 * Math.PI / 2) };
            foreach (float a in angles)
                bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 150f, 6f));
            SoundManager.Play("enemy_shoot");
        }
    }

    private void UpdateOrbiter(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        Position += velocity * dt;
        BounceOffWalls();

        foreach (var orb in orbitals)
        {
            orb.Angle += OrbitSpeed * dt;
            orb.ShootTimer -= dt;
            if (orb.ShootTimer <= 0)
            {
                orb.ShootTimer = OrbitalShootCooldown;
                Vector2 orbPos = GetOrbitalPos(orb);
                Vector2 shootDir = Vector2.Normalize(playerPos - orbPos);
                bullets.Add(new Bullet(orbPos, shootDir * 140f, 5f));
                SoundManager.Play("enemy_shoot");
            }
        }
    }

    private void UpdateBerserker(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        if (berserkerJumping)
        {
            berserkerJumpTimer += dt;
            float t = Math.Min(berserkerJumpTimer / BerserkerJumpDuration, 1f);
            Position = Vector2.Lerp(berserkerJumpStart, berserkerJumpTarget, t);
            if (t >= 1f)
            {
                berserkerJumping = false;
                berserkerJumpTimer = 0;
                attackTimer = 3.0f;
            }
        }
        else
        {
            attackTimer -= dt;
            if (attackTimer <= 0)
            {
                // 50/50: jump at player or spread shot
                if (rand.NextDouble() < 0.5)
                {
                    // Jump toward player — land near them
                    berserkerJumpStart  = Position;
                    berserkerJumpTarget = playerPos + Vector2.Normalize(Position - playerPos) * Size;
                    berserkerJumpTarget = ClampVec(berserkerJumpTarget);
                    berserkerJumping    = true;
                    berserkerJumpTimer  = 0;
                }
                else
                {
                    // 5 bullets in 30° spread toward player
                    float baseAngle = (float)Math.Atan2(playerPos.Y - Position.Y, playerPos.X - Position.X);
                    float spread = (float)(30 * Math.PI / 180);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = baseAngle - spread / 2 + spread * (i / 4f);
                        bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 160f, 6f));
                    }
                    SoundManager.Play("enemy_shoot");
                    attackTimer = 3.0f;
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Vector2 GetOrbitalPos(OrbitalMinion orb)
        => Position + new Vector2((float)Math.Cos(orb.Angle), (float)Math.Sin(orb.Angle)) * OrbitRadius;

    private void BounceOffWalls()
    {
        if (Position.X - Size < innerLeft)  { Position.X = innerLeft  + Size; velocity.X =  Math.Abs(velocity.X); }
        if (Position.X + Size > innerRight) { Position.X = innerRight - Size; velocity.X = -Math.Abs(velocity.X); }
        if (Position.Y - Size < innerTop)   { Position.Y = innerTop   + Size; velocity.Y =  Math.Abs(velocity.Y); }
        if (Position.Y + Size > innerBottom){ Position.Y = innerBottom- Size; velocity.Y = -Math.Abs(velocity.Y); }
    }

    private void ClampToRoom()
    {
        Position.X = Math.Clamp(Position.X, innerLeft + Size, innerRight  - Size);
        Position.Y = Math.Clamp(Position.Y, innerTop  + Size, innerBottom - Size);
    }

    private Vector2 ClampVec(Vector2 v) => new Vector2(
        Math.Clamp(v.X, innerLeft + Size, innerRight  - Size),
        Math.Clamp(v.Y, innerTop  + Size, innerBottom - Size));

    // ── Draw ─────────────────────────────────────────────────────────────────

    public void Draw(Font font)
    {
        if (!IsAlive) return;

        // Draw orbital minions
        if (Type == BossType.Orbiter)
        {
            foreach (var orb in orbitals)
            {
                Vector2 op = GetOrbitalPos(orb);
                Raylib.DrawCircleV(op, 9f, new Color(100, 160, 255, 255));
                Raylib.DrawCircleLines((int)op.X, (int)op.Y, 9, Color.White);
            }
        }

        // Draw jump shadow for Berserker
        if (Type == BossType.Berserker && berserkerJumping)
            Raylib.DrawCircleV(berserkerJumpTarget, Size, new Color(0, 0, 0, 80));

        Color drawColor = hitFlashTimer > 0 ? Color.White : Color;
        Raylib.DrawCircleV(Position, Size, drawColor);
        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, (int)Size, Color.White);

        // Health bar
        float barW = Size * 3;
        float pct  = (float)Health / MaxHealth;
        Raylib.DrawRectangle((int)(Position.X - barW/2), (int)(Position.Y - Size - 10), (int)barW, 5, Color.DarkGray);
        Raylib.DrawRectangle((int)(Position.X - barW/2), (int)(Position.Y - Size - 10), (int)(barW * pct), 5, Color.Red);

        string name = Type switch
        {
            BossType.Bouncer   => "BOUNCER",
            BossType.Gusher    => "GUSHER",
            BossType.Spawner   => "SPAWNER",
            BossType.Chaser    => "CHASER",
            BossType.Orbiter   => "ORBITER",
            BossType.Berserker => "BERSERKER",
            _ => "BOSS"
        };
        Vector2 ns = Raylib.MeasureTextEx(font, name, 14, 1);
        Raylib.DrawTextEx(font, name, new Vector2(Position.X - ns.X/2, Position.Y - Size - 24), 14, 1, Color.White);
    }

    public bool IsCollidingWithPlayer(Vector2 playerPos, float playerSize)
        => Vector2.Distance(Position, playerPos) < Size + playerSize / 2;

    // Check if any orbital minion hits the player
    public bool OrbitalCollidesWithPlayer(Vector2 playerPos, float playerSize)
    {
        if (Type != BossType.Orbiter) return false;
        foreach (var orb in orbitals)
            if (Vector2.Distance(GetOrbitalPos(orb), playerPos) < 9f + playerSize / 2)
                return true;
        return false;
    }

    public static int GetTierForFloor(int floor) => floor switch
    {
        1 or 2       => 1,
        3 or 4       => 2,
        5 or 6 or 7  => 3,
        _ => 1
    };

    public static BossType[] GetTierBosses(int tier) => tier switch
    {
        1 => new[] { BossType.Bouncer,  BossType.Gusher,    BossType.Spawner },
        2 => new[] { BossType.Chaser,   BossType.Orbiter,   BossType.Berserker },
        _ => new[] { BossType.Chaser,   BossType.Orbiter,   BossType.Berserker } // tier 3 TBD
    };
}
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

    // Tier 3 (floors 5-6)
    Ghost,          // Disappears every 5-10s, reappears randomly, shoots bullets
    Leaper,         // Jumps every 3s, shoots 4 beams on landing
    Serpent,        // Snake with 10 sections, each section takes 10 hp to destroy

    // Tier 4 (floor 7 only)
    FinalBoss,      // Stationary: rotating cross beam, spawn 5 flying, 5-bullet spread
}

public class OrbitalMinion
{
    public float Angle;
    public float ShootTimer;
    public OrbitalMinion(float startAngle) { Angle = startAngle; ShootTimer = startAngle; }
}

public class SerpentSection
{
    public Vector2 Position;
    public bool Alive;
    public int Health;
    public SerpentSection(Vector2 pos) { Position = pos; Alive = true; Health = 10; }
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

    // Orbiter
    private List<OrbitalMinion> orbitals = new();
    private const float OrbitRadius = 60f;
    private const float OrbitSpeed  = 1.8f;
    private const float OrbitalShootCooldown = 2.0f;

    // Berserker
    private bool berserkerJumping;
    private Vector2 berserkerJumpStart;
    private Vector2 berserkerJumpTarget;
    private float berserkerJumpTimer;
    private const float BerserkerJumpDuration = 0.6f;

    // Ghost
    private bool ghostVisible;
    private float ghostTimer;       // countdown to next teleport
    private float ghostShootTimer;
    private float ghostInvisTimer;  // how long to stay invisible before reappearing

    // Leaper
    private bool leaperJumping;
    private Vector2 leaperJumpStart;
    private Vector2 leaperJumpTarget;
    private float leaperJumpTimer;
    private float leaperLandTimer;  // brief pause after landing before shooting
    private bool leaperShooting;
    private const float LeaperJumpDuration = 0.5f;

    // Serpent
    private List<SerpentSection> serpentSections = new();
    private float serpentMoveTimer;
    private float serpentShootTimer;
    private const int SerpentSectionCount = 10;
    private const float SerpentSectionSize = 14f;
    private const float SerpentSpacing = 18f;

    // FinalBoss
    private float finalBeamAngle;
    private float finalBeamShootTimer;
    private float finalAttackTimer;
    private int   finalAttackIndex;

    // Hit feedback
    private float invincibilityTimer;
    private float hitFlashTimer;
    private const float InvDuration   = 0.2f;
    private const float FlashDuration = 0.1f;

    private Random rand;
    private float difficultyBulletMult = 1.0f;
    private float innerLeft, innerRight, innerTop, innerBottom;

    public void SetDifficulty(int difficulty)
    {
        difficultyBulletMult = difficulty switch { 0 => 0.60f, 1 => 0.80f, _ => 1.0f };
    }

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
                for (int i = 0; i < 4; i++)
                    orbitals.Add(new OrbitalMinion((float)(i * Math.PI / 2)));
                break;

            case BossType.Berserker:
                MaxHealth = 70; Size = 28;
                Color = new Color(220, 80, 30, 255);
                attackTimer = 3.0f;
                break;

            case BossType.Ghost:
                MaxHealth = 80; Size = 22;
                Color = new Color(180, 180, 255, 255);
                ghostVisible = true;
                ghostTimer = RandGhostInterval();
                ghostShootTimer = 1.5f;
                break;

            case BossType.Leaper:
                MaxHealth = 100; Size = 26;
                Color = new Color(80, 200, 120, 255);
                attackTimer = 3.0f;
                break;

            case BossType.Serpent:
                MaxHealth = SerpentSectionCount * 10; Size = SerpentSectionSize;
                Color = new Color(60, 180, 60, 255);
                // Head starts at center, sections trail behind
                for (int i = 0; i < SerpentSectionCount; i++)
                    serpentSections.Add(new SerpentSection(new Vector2(position.X - i * SerpentSpacing, position.Y)));
                velocity = new Vector2(80f, 0f);
                serpentMoveTimer = 2.0f;
                serpentShootTimer = 2.5f;
                break;

            case BossType.FinalBoss:
                MaxHealth = 200; Size = 30;
                Color = new Color(180, 20, 20, 255);
                finalBeamShootTimer = 0.5f;
                finalAttackTimer = 4.0f;
                break;
        }

        Health = MaxHealth;
    }

    private float RandGhostInterval() => 5f + (float)(rand.NextDouble() * 5f);

    private Vector2 RandDiag()
    {
        float x = rand.NextDouble() < 0.5 ? 1f : -1f;
        float y = rand.NextDouble() < 0.5 ? 1f : -1f;
        return Vector2.Normalize(new Vector2(x, y));
    }

    private Vector2 RandRoomPos()
    {
        float x = innerLeft + Size + (float)(rand.NextDouble() * (innerRight  - innerLeft  - Size * 2));
        float y = innerTop  + Size + (float)(rand.NextDouble() * (innerBottom - innerTop   - Size * 2));
        return new Vector2(x, y);
    }

    public void TakeDamage(int damage)
    {
        if (Type == BossType.Serpent) { TakeSerpentDamage(damage); return; }
        if (invincibilityTimer > 0) return;
        invincibilityTimer = InvDuration;
        hitFlashTimer = FlashDuration;
        Health -= damage;
        if (Health <= 0) { Health = 0; IsAlive = false; }
    }

    private void TakeSerpentDamage(int damage)
    {
        if (invincibilityTimer > 0) return;
        invincibilityTimer = InvDuration;
        hitFlashTimer = FlashDuration;
        // Damage goes to the last alive section from the tail
        for (int i = serpentSections.Count - 1; i >= 0; i--)
        {
            if (serpentSections[i].Alive)
            {
                serpentSections[i].Health -= damage;
                if (serpentSections[i].Health <= 0)
                {
                    serpentSections[i].Alive = false;
                    Health -= 10;
                }
                break;
            }
        }
        // Check if all sections dead
        if (serpentSections.All(s => !s.Alive) || Health <= 0)
        {
            Health = 0;
            IsAlive = false;
        }
    }

    public List<Enemy> Update(float dt, Vector2 playerPos, List<Bullet> bullets, float playerSpeed)
    {
        if (!IsAlive) return new List<Enemy>();

        if (invincibilityTimer > 0) invincibilityTimer -= dt;
        if (hitFlashTimer      > 0) hitFlashTimer      -= dt;

        var spawned = new List<Enemy>();

        switch (Type)
        {
            case BossType.Bouncer:   UpdateBouncer(dt, playerPos, bullets);  break;
            case BossType.Gusher:    UpdateGusher(dt, playerPos);             break;
            case BossType.Spawner:   UpdateSpawner(dt, playerPos, spawned, playerSpeed); break;
            case BossType.Chaser:    UpdateChaser(dt, playerPos, bullets);    break;
            case BossType.Orbiter:   UpdateOrbiter(dt, playerPos, bullets);   break;
            case BossType.Berserker: UpdateBerserker(dt, playerPos, bullets); break;
            case BossType.Ghost:     UpdateGhost(dt, playerPos, bullets);     break;
            case BossType.Leaper:    UpdateLeaper(dt, playerPos, bullets);    break;
            case BossType.Serpent:   UpdateSerpent(dt, playerPos, bullets);   break;
            case BossType.FinalBoss: UpdateFinalBoss(dt, playerPos, bullets, spawned, playerSpeed); break;
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
        Vector2 dir = playerPos - Position;
        if (dir.Length() > Size)
            Position += Vector2.Normalize(dir) * chaserSpeed * dt;
        ClampToRoom();

        shootTimer -= dt;
        if (shootTimer <= 0)
        {
            shootTimer = 1.5f;
            float[] angles = { 0, (float)Math.PI / 2, (float)Math.PI, (float)(3 * Math.PI / 2) };
            foreach (float a in angles)
                bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 150f, 6f));
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
                bullets.Add(new Bullet(orbPos, Vector2.Normalize(playerPos - orbPos) * 140f, 5f));
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
            if (t >= 1f) { berserkerJumping = false; berserkerJumpTimer = 0; attackTimer = 3.0f; }
        }
        else
        {
            attackTimer -= dt;
            if (attackTimer <= 0)
            {
                if (rand.NextDouble() < 0.5)
                {
                    berserkerJumpStart  = Position;
                    berserkerJumpTarget = ClampVec(playerPos + Vector2.Normalize(Position - playerPos) * Size);
                    berserkerJumping    = true;
                    berserkerJumpTimer  = 0;
                }
                else
                {
                    float baseAngle = (float)Math.Atan2(playerPos.Y - Position.Y, playerPos.X - Position.X);
                    float spread = (float)(30 * Math.PI / 180);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = baseAngle - spread / 2 + spread * (i / 4f);
                        bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 160f, 6f));
                    }
                    attackTimer = 3.0f;
                }
            }
        }
    }

    // ── Tier 3 ──────────────────────────────────────────────────────────────

    private void UpdateGhost(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        // Shoot at player while visible
        if (ghostVisible)
        {
            ghostShootTimer -= dt;
            if (ghostShootTimer <= 0)
            {
                ghostShootTimer = 1.5f;
                // 3-way spread toward player
                float baseAngle = (float)Math.Atan2(playerPos.Y - Position.Y, playerPos.X - Position.X);
                for (int i = -1; i <= 1; i++)
                {
                    float a = baseAngle + i * (float)(15 * Math.PI / 180);
                    bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 150f * difficultyBulletMult, 6f));
                }
            }
        }

        // Teleport timer
        ghostTimer -= dt;
        if (ghostTimer <= 0)
        {
            if (ghostVisible)
            {
                // Go invisible, schedule reappearance
                ghostVisible = false;
                ghostInvisTimer = 0.8f; // brief invisible pause before reappearing
            }
        }

        if (!ghostVisible)
        {
            ghostInvisTimer -= dt;
            if (ghostInvisTimer <= 0)
            {
                // Reappear at random location
                Position = RandRoomPos();
                ghostVisible = true;
                ghostTimer = RandGhostInterval();
                ghostShootTimer = 0.3f; // shoot quickly on reappear
            }
        }
    }

    private void UpdateLeaper(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        if (leaperJumping)
        {
            leaperJumpTimer += dt;
            float t = Math.Min(leaperJumpTimer / LeaperJumpDuration, 1f);
            Position = Vector2.Lerp(leaperJumpStart, leaperJumpTarget, t);
            if (t >= 1f)
            {
                leaperJumping   = false;
                leaperJumpTimer = 0;
                leaperLandTimer = 0.2f; // brief pause before beams
                leaperShooting  = true;
            }
        }
        else if (leaperShooting)
        {
            leaperLandTimer -= dt;
            if (leaperLandTimer <= 0)
            {
                // Fire 4 beams (bullets) in cardinal directions
                float[] angles = { 0, (float)Math.PI/2, (float)Math.PI, (float)(3*Math.PI/2) };
                foreach (float a in angles)
                    bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 180f * difficultyBulletMult, 7f));
                leaperShooting = false;
                attackTimer = 3.0f;
            }
        }
        else
        {
            attackTimer -= dt;
            if (attackTimer <= 0)
            {
                leaperJumpStart  = Position;
                leaperJumpTarget = RandRoomPos();
                leaperJumping    = true;
                leaperJumpTimer  = 0;
            }
        }
    }

    private void UpdateSerpent(float dt, Vector2 playerPos, List<Bullet> bullets)
    {
        // Head moves via bounce, sections follow
        Position += velocity * dt;
        BounceOffWalls();

        // Sections follow the one ahead of them
        serpentSections[0].Position = Position;
        for (int i = 1; i < serpentSections.Count; i++)
        {
            if (!serpentSections[i].Alive) continue;
            Vector2 target = serpentSections[i - 1].Position;
            // Find the previous alive section as the leader
            for (int j = i - 1; j >= 0; j--)
            {
                if (serpentSections[j].Alive) { target = serpentSections[j].Position; break; }
            }
            Vector2 diff = serpentSections[i].Position - target;
            if (diff.Length() > SerpentSpacing)
                serpentSections[i].Position = target + Vector2.Normalize(diff) * SerpentSpacing;
        }

        // Occasionally change direction
        serpentMoveTimer -= dt;
        if (serpentMoveTimer <= 0)
        {
            serpentMoveTimer = 1.5f + (float)(rand.NextDouble() * 1.5f);
            velocity = RandDiag() * 80f;
        }

        // Shoot from head
        serpentShootTimer -= dt;
        if (serpentShootTimer <= 0)
        {
            serpentShootTimer = 2.5f;
            bullets.Add(new Bullet(Position, Vector2.Normalize(playerPos - Position) * 150f * difficultyBulletMult, 6f));
        }
    }

    // ── Tier 4 ──────────────────────────────────────────────────────────────

    private void UpdateFinalBoss(float dt, Vector2 playerPos, List<Bullet> bullets, List<Enemy> spawned, float playerSpeed)
    {
        finalBeamAngle += 0.4f * dt;
        finalBeamShootTimer -= dt;
        if (finalBeamShootTimer <= 0)
        {
            finalBeamShootTimer = 2.0f;
            for (int i = 0; i < 4; i++)
            {
                float a = finalBeamAngle + i * (float)(Math.PI / 2);
                bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 80f * difficultyBulletMult, 7f));
            }
            float ra = (float)(rand.NextDouble() * Math.PI * 2);
            bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(ra), (float)Math.Sin(ra)) * 60f * difficultyBulletMult, 5f));
        }

        finalAttackTimer -= dt;
        if (finalAttackTimer <= 0)
        {
            finalAttackIndex = (finalAttackIndex + 1) % 3;
            finalAttackTimer = 4.0f;

            if (finalAttackIndex == 1)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = (float)(i * Math.PI * 2 / 5);
                    Vector2 off = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * (Size + 40);
                    var e = new Enemy(EnemyType.Flying, Position + off, playerSpeed);
                    e.SetDifficulty((int)Math.Round((difficultyBulletMult - 0.6f) / 0.2f));
                    spawned.Add(e);
                }
            }
            else if (finalAttackIndex == 2)
            {
                float baseAngle = (float)Math.Atan2(playerPos.Y - Position.Y, playerPos.X - Position.X);
                float spread = (float)(30 * Math.PI / 180);
                for (int i = 0; i < 5; i++)
                {
                    float a = baseAngle - spread / 2 + spread * (i / 4f);
                    bullets.Add(new Bullet(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 160f * difficultyBulletMult, 6f));
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Vector2 RayToWall(Vector2 origin, Vector2 dir)
    {
        float tMin = float.MaxValue;
        if (Math.Abs(dir.X) > 0.0001f)
        {
            float t1 = (innerLeft   - origin.X) / dir.X;
            float t2 = (innerRight  - origin.X) / dir.X;
            if (t1 > 0) tMin = Math.Min(tMin, t1);
            if (t2 > 0) tMin = Math.Min(tMin, t2);
        }
        if (Math.Abs(dir.Y) > 0.0001f)
        {
            float t3 = (innerTop    - origin.Y) / dir.Y;
            float t4 = (innerBottom - origin.Y) / dir.Y;
            if (t3 > 0) tMin = Math.Min(tMin, t3);
            if (t4 > 0) tMin = Math.Min(tMin, t4);
        }
        if (tMin == float.MaxValue) tMin = 400f;
        return origin + dir * tMin;
    }

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

        // Orbiter minions
        if (Type == BossType.Orbiter)
            foreach (var orb in orbitals)
            {
                Vector2 op = GetOrbitalPos(orb);
                Raylib.DrawCircleV(op, 9f, new Color(100, 160, 255, 255));
                Raylib.DrawCircleLines((int)op.X, (int)op.Y, 9, Color.White);
            }

        // Berserker jump shadow
        if (Type == BossType.Berserker && berserkerJumping)
            Raylib.DrawCircleV(berserkerJumpTarget, Size, new Color(0, 0, 0, 80));

        // Leaper jump shadow
        if (Type == BossType.Leaper && leaperJumping)
            Raylib.DrawCircleV(leaperJumpTarget, Size, new Color(0, 0, 0, 80));

        // FinalBoss rotating beams
        if (Type == BossType.FinalBoss)
            for (int i = 0; i < 4; i++)
            {
                float a = finalBeamAngle + i * (float)(Math.PI / 2);
                Vector2 tip = RayToWall(Position, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)));
                Raylib.DrawLineEx(Position, tip, 3f, new Color((byte)220, (byte)50, (byte)50, (byte)200));
                Raylib.DrawCircleV(tip, 5f, new Color((byte)255, (byte)80, (byte)80, (byte)255));
            }

        // Serpent sections (draw tail to head so head is on top)
        if (Type == BossType.Serpent)
        {
            for (int i = serpentSections.Count - 1; i >= 1; i--)
            {
                if (!serpentSections[i].Alive) continue;
                Color sc = hitFlashTimer > 0 ? Color.White : new Color((byte)40, (byte)160, (byte)40, (byte)255);
                Raylib.DrawCircleV(serpentSections[i].Position, SerpentSectionSize, sc);
                Raylib.DrawCircleLines((int)serpentSections[i].Position.X, (int)serpentSections[i].Position.Y, (int)SerpentSectionSize, Color.White);
            }
        }

        // Ghost: skip drawing body when invisible
        if (Type == BossType.Ghost && !ghostVisible) goto SkipBody;

        {
            Color drawColor = hitFlashTimer > 0 ? Color.White : Color;
            // Ghost gets translucent tint
            if (Type == BossType.Ghost)
                drawColor = hitFlashTimer > 0 ? Color.White : new Color((byte)180, (byte)180, (byte)255, (byte)200);
            Raylib.DrawCircleV(Position, Size, drawColor);
            Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, (int)Size, Color.White);
        }

        SkipBody:

        // Health bar (skip ghost when invisible)
        if (Type == BossType.Ghost && !ghostVisible) goto SkipBar;

        {
            float barW = Size * 3;
            float pct  = (float)Health / MaxHealth;
            Raylib.DrawRectangle((int)(Position.X - barW/2), (int)(Position.Y - Size - 10), (int)barW, 5, Color.DarkGray);
            Raylib.DrawRectangle((int)(Position.X - barW/2), (int)(Position.Y - Size - 10), (int)(barW * pct), 5, Color.Red);
        }

        SkipBar:

        // Serpent: draw head health bar above head position
        if (Type == BossType.Serpent)
        {
            Color headColor = hitFlashTimer > 0 ? Color.White : Color;
            Raylib.DrawCircleV(Position, Size, headColor);
            Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, (int)Size, Color.Yellow);
            float barW = Size * 3;
            float pct  = (float)Health / MaxHealth;
            Raylib.DrawRectangle((int)(Position.X - barW/2), (int)(Position.Y - Size - 10), (int)barW, 5, Color.DarkGray);
            Raylib.DrawRectangle((int)(Position.X - barW/2), (int)(Position.Y - Size - 10), (int)(barW * pct), 5, Color.Red);
        }

        string name = Type switch
        {
            BossType.Bouncer   => "BOUNCER",
            BossType.Gusher    => "GUSHER",
            BossType.Spawner   => "SPAWNER",
            BossType.Chaser    => "CHASER",
            BossType.Orbiter   => "ORBITER",
            BossType.Berserker => "BERSERKER",
            BossType.Ghost     => "GHOST",
            BossType.Leaper    => "LEAPER",
            BossType.Serpent   => "SERPENT",
            BossType.FinalBoss => "THE END",
            _ => "BOSS"
        };

        if (Type == BossType.Ghost && !ghostVisible) return;
        Vector2 ns = Raylib.MeasureTextEx(font, name, 14, 1);
        Raylib.DrawTextEx(font, name, new Vector2(Position.X - ns.X/2, Position.Y - Size - 24), 14, 1, Color.White);
    }

    public bool IsCollidingWithPlayer(Vector2 playerPos, float playerSize)
    {
        if (Type == BossType.Ghost && !ghostVisible) return false;
        // Serpent: check head and all alive sections
        if (Type == BossType.Serpent)
        {
            foreach (var s in serpentSections)
                if (s.Alive && Vector2.Distance(s.Position, playerPos) < SerpentSectionSize + playerSize / 2)
                    return true;
            return false;
        }
        return Vector2.Distance(Position, playerPos) < Size + playerSize / 2;
    }

    public bool OrbitalCollidesWithPlayer(Vector2 playerPos, float playerSize)
    {
        if (Type != BossType.Orbiter) return false;
        foreach (var orb in orbitals)
            if (Vector2.Distance(GetOrbitalPos(orb), playerPos) < 9f + playerSize / 2)
                return true;
        return false;
    }

    public bool BeamCollidesWithPlayer(Vector2 playerPos, float playerSize)
    {
        if (Type != BossType.FinalBoss) return false;
        float beamWidth = 6f;
        for (int i = 0; i < 4; i++)
        {
            float a = finalBeamAngle + i * (float)(Math.PI / 2);
            Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
            Vector2 tip = RayToWall(Position, dir);
            Vector2 seg = tip - Position;
            float segLen = seg.Length();
            if (segLen < 0.001f) continue;
            Vector2 segDir = seg / segLen;
            float t = Math.Clamp(Vector2.Dot(playerPos - Position, segDir), 0f, segLen);
            Vector2 closest = Position + segDir * t;
            if (Vector2.Distance(closest, playerPos) < beamWidth + playerSize / 2)
                return true;
        }
        return false;
    }

    public static int GetTierForFloor(int floor) => floor switch
    {
        1 or 2      => 1,
        3 or 4      => 2,
        5 or 6      => 3,
        _ => 1
    };

    public static BossType[] GetTierBosses(int tier) => tier switch
    {
        1 => new[] { BossType.Bouncer,   BossType.Gusher,    BossType.Spawner },
        2 => new[] { BossType.Chaser,    BossType.Orbiter,   BossType.Berserker },
        3 => new[] { BossType.Ghost,     BossType.Leaper,    BossType.Serpent },
        _ => new[] { BossType.Bouncer,   BossType.Gusher,    BossType.Spawner }
    };
}
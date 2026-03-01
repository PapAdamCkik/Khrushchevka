using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public enum EnemyType
{
    // Stage 1 (all floors)
    Flying,      // Flies over obstacles, slower
    Walking,     // Walks around obstacles, faster
    Shooter,     // Shoots at player, slow
    
    // Stage 2 (floor 2+)
    Stationary,  // Shoots 5 random bullets
    Reviver,     // Walking enemy that revives
    
    // Stage 3 (floor 5+)
    Jumper,      // Jumping stationary shooter
    Beamer       // Shoots beam through walls
}

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Size;
    public bool Active;
    public bool PierceWalls;
    
    public Bullet(Vector2 pos, Vector2 vel, float size, bool pierceWalls = false)
    {
        Position = pos;
        Velocity = vel;
        Size = size;
        Active = true;
        PierceWalls = pierceWalls;
    }
    
    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
    }
}

public class Enemy
{
    public EnemyType Type;
    public Vector2 Position;
    public float Size;
    public int Health;
    public int MaxHealth;
    public bool IsAlive;
    public Color Color;
    
    // Movement
    private float speed;
    private List<Vector2> path;
    private int pathIndex;
    private float pathRecalcTimer;
    
    // Shooting
    private float shootTimer;
    private float shootCooldown;
    
    // Reviver specific
    private bool isStunned;
    private float stunnedTimer;
    private float reviveTime = 5.0f;
    
    // Jumper specific
    private bool isJumping;
    private float jumpTimer;
    private Vector2 jumpTarget;
    private Vector2 jumpStart;
    
    // Beamer specific
    private float beamTimer;
    private bool isFiringBeam;
    private float beamDuration;
    private Vector2 beamDirection;
    
    // Hit feedback
    private float invincibilityTimer;
    private float hitFlashTimer;
    private const float InvincibilityDuration = 0.2f;
    private const float HitFlashDuration = 0.1f;
    
    // Contact damage cooldown
    private float contactCooldown;
    
    // Knockback from sword
    private Vector2 knockbackVelocity;
    
    // Reviver specific (reviving dead allies)
    private Enemy? reviveTarget;
    private float reviveCastTimer;
    
    private Random rand;
    
    public Enemy(EnemyType type, Vector2 position, float playerSpeed)
    {
        Type = type;
        Position = position;
        Size = 15;
        IsAlive = true;
        path = new List<Vector2>();
        pathIndex = 0;
        pathRecalcTimer = 0;
        rand = new Random();
        
        // Set properties based on type
        switch (type)
        {
            case EnemyType.Flying:
                MaxHealth = 15;
                Health = MaxHealth;
                speed = playerSpeed * 0.7f;
                Color = Color.Purple;
                break;
                
            case EnemyType.Walking:
                MaxHealth = 15;
                Health = MaxHealth;
                speed = playerSpeed * 0.9f;
                Color = Color.Red;
                break;
                
            case EnemyType.Shooter:
                MaxHealth = 15;
                Health = MaxHealth;
                speed = playerSpeed * 0.4f;
                shootCooldown = 2.0f;
                shootTimer = shootCooldown;
                Color = Color.Orange;
                break;
                
            case EnemyType.Stationary:
                MaxHealth = 25;
                Health = MaxHealth;
                speed = 0;
                shootCooldown = 2.0f;
                shootTimer = shootCooldown;
                Color = Color.DarkBlue;
                break;
                
            case EnemyType.Reviver:
                MaxHealth = 25;
                Health = MaxHealth;
                speed = playerSpeed * 0.9f;
                Color = Color.Green;
                isStunned = false;
                stunnedTimer = 0;
                break;
                
            case EnemyType.Jumper:
                MaxHealth = 40;
                Health = MaxHealth;
                speed = 0;
                shootCooldown = 2.0f;
                shootTimer = shootCooldown;
                isJumping = false;
                jumpTimer = 0;
                Color = Color.SkyBlue;
                break;
                
            case EnemyType.Beamer:
                MaxHealth = 40;
                Health = MaxHealth;
                speed = playerSpeed * 0.3f;
                beamTimer = 15.0f;
                isFiringBeam = false;
                beamDuration = 0;
                Color = Color.Magenta;
                break;
        }
    }
    
    public void TakeDamage(int damage)
    {
        // Ignore damage during invincibility window
        if (invincibilityTimer > 0) return;
        
        invincibilityTimer = InvincibilityDuration;
        hitFlashTimer = HitFlashDuration;
        
        Health -= damage;
        
        if (Health <= 0)
        {
            if (Type == EnemyType.Reviver && !isStunned)
            {
                isStunned = true;
                stunnedTimer = reviveTime;
                Health = 1;
                Color = Color.DarkGray;
            }
            else
            {
                Health = 0;
                IsAlive = false;
            }
        }
    }
    
    public bool CanContactDamage()
    {
        if (contactCooldown > 0) return false;
        contactCooldown = 0.5f;
        return true;
    }
    
    public void ApplyKnockback(Vector2 direction, float force)
    {
        knockbackVelocity = direction * force;
    }
    
    public void Update(float deltaTime, Vector2 playerPos, int[,] roomLayout, int tileSize, int offsetX, int offsetY, List<Bullet> bullets, List<Enemy> allEnemies)
    {
        if (!IsAlive) return;
        
        // Tick hit timers
        if (invincibilityTimer > 0) invincibilityTimer -= deltaTime;
        if (hitFlashTimer > 0) hitFlashTimer -= deltaTime;
        if (contactCooldown > 0) contactCooldown -= deltaTime;
        
        // Apply knockback
        if (knockbackVelocity.Length() > 0.5f)
        {
            Vector2 kbX = new Vector2(knockbackVelocity.X * deltaTime, 0);
            Vector2 kbY = new Vector2(0, knockbackVelocity.Y * deltaTime);
            Vector2 tryX = Position + kbX;
            Vector2 tryY = Position + kbY;
            if (!CheckWallCollision(tryX, roomLayout, tileSize, offsetX, offsetY)) Position = tryX;
            if (!CheckWallCollision(tryY, roomLayout, tileSize, offsetX, offsetY)) Position = tryY;
            knockbackVelocity *= 0.75f;
        }
        else knockbackVelocity = Vector2.Zero;
        
        if (Type == EnemyType.Reviver && isStunned)
        {
            stunnedTimer -= deltaTime;
            if (stunnedTimer <= 0)
            {
                Health = MaxHealth;
                isStunned = false;
                Color = Color.Green;
            }
            return;
        }
        
        switch (Type)
        {
            case EnemyType.Flying:
                UpdateFlying(playerPos, deltaTime, allEnemies);
                break;
                
            case EnemyType.Walking:
                UpdateWalking(playerPos, roomLayout, tileSize, offsetX, offsetY, deltaTime, allEnemies);
                break;
                
            case EnemyType.Reviver:
                UpdateReviver(playerPos, roomLayout, tileSize, offsetX, offsetY, deltaTime, allEnemies);
                break;
                
            case EnemyType.Shooter:
                UpdateShooter(playerPos, roomLayout, tileSize, offsetX, offsetY, deltaTime, bullets, allEnemies);
                break;
                
            case EnemyType.Stationary:
                UpdateStationary(deltaTime, bullets);
                break;
                
            case EnemyType.Jumper:
                UpdateJumper(deltaTime, bullets, roomLayout, tileSize, offsetX, offsetY);
                break;
                
            case EnemyType.Beamer:
                UpdateBeamer(playerPos, deltaTime, bullets, allEnemies);
                break;
        }
    }
    
    private void UpdateFlying(Vector2 playerPos, float deltaTime, List<Enemy> allEnemies)
    {
        Vector2 direction = playerPos - Position;
        float distance = direction.Length();
        
        if (distance > 1)
        {
            direction = Vector2.Normalize(direction);
            Vector2 newPos = Position + direction * speed * deltaTime * 60;
            if (!CheckEnemyCollision(newPos, allEnemies))
                Position = newPos;
        }
    }
    
    private void UpdateWalking(Vector2 playerPos, int[,] roomLayout, int tileSize, int offsetX, int offsetY, float deltaTime, List<Enemy> allEnemies)
    {
        pathRecalcTimer += deltaTime;
        
        // Recalculate path periodically or if too close to player
        if (path.Count == 0 || pathRecalcTimer > 0.5f)
        {
            float distToPlayer = Vector2.Distance(Position, playerPos);
            
            // If very close to player, try to maintain some distance
            Vector2 targetPos = playerPos;
            if (distToPlayer < Size * 2.5f)
            {
                // Move to a position near player but not on top
                Vector2 awayFromPlayer = Vector2.Normalize(Position - playerPos);
                targetPos = playerPos + awayFromPlayer * Size * 2.5f;
            }
            
            path = FindPath(Position, targetPos, roomLayout, tileSize, offsetX, offsetY);
            pathIndex = 0;
            pathRecalcTimer = 0;
        }
        
        if (path.Count > 0 && pathIndex < path.Count)
        {
            Vector2 target = path[pathIndex];
            Vector2 direction = target - Position;
            float distance = direction.Length();
            
            if (distance < 5)
            {
                pathIndex++;
            }
            else
            {
                direction = Vector2.Normalize(direction);
                Vector2 newPos = Position + direction * speed * deltaTime * 60;
                
                // Check collision with walls/obstacles
                if (!CheckWallCollision(newPos, roomLayout, tileSize, offsetX, offsetY))
                {
                    // Check collision with other enemies
                    if (!CheckEnemyCollision(newPos, allEnemies))
                    {
                        Position = newPos;
                    }
                }
            }
        }
    }
    
    private void UpdateShooter(Vector2 playerPos, int[,] roomLayout, int tileSize, int offsetX, int offsetY, float deltaTime, List<Bullet> bullets, List<Enemy> allEnemies)
    {
        UpdateWalking(playerPos, roomLayout, tileSize, offsetX, offsetY, deltaTime, allEnemies);
        
        shootTimer -= deltaTime;
        if (shootTimer <= 0)
        {
            shootTimer = shootCooldown;
            ShootAtPlayer(playerPos, bullets, 150f, false);
        }
    }
    
    private void UpdateStationary(float deltaTime, List<Bullet> bullets)
    {
        shootTimer -= deltaTime;
        if (shootTimer <= 0)
        {
            shootTimer = shootCooldown;
            
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)(rand.NextDouble() * Math.PI * 2);
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                bullets.Add(new Bullet(Position, direction * 100f, 5f, false));
            }
        }
    }
    
    private void UpdateReviver(Vector2 playerPos, int[,] roomLayout, int tileSize, int offsetX, int offsetY, float deltaTime, List<Enemy> allEnemies)
    {
        // Look for a dead ally to revive
        Enemy? target = null;
        float closestDist = float.MaxValue;
        foreach (var other in allEnemies)
        {
            if (other == this || other.IsAlive) continue;
            float d = Vector2.Distance(Position, other.Position);
            if (d < closestDist) { closestDist = d; target = other; }
        }
        
        if (target != null)
        {
            // Walk toward the dead ally
            reviveTarget = target;
            Vector2 dir = target.Position - Position;
            float dist = dir.Length();
            if (dist > Size + target.Size)
            {
                Vector2 newPos = Position + Vector2.Normalize(dir) * speed * deltaTime * 60;
                if (!CheckWallCollision(newPos, roomLayout, tileSize, offsetX, offsetY) &&
                    !CheckEnemyCollision(newPos, allEnemies))
                    Position = newPos;
            }
            else
            {
                // Close enough — cast revive
                reviveCastTimer += deltaTime;
                if (reviveCastTimer >= 1.5f)
                {
                    target.IsAlive = true;
                    target.Health = target.MaxHealth / 2;
                    target.Color = target.Type switch
                    {
                        EnemyType.Flying    => Color.Purple,
                        EnemyType.Walking   => Color.Red,
                        EnemyType.Shooter   => Color.Orange,
                        EnemyType.Stationary => Color.DarkBlue,
                        EnemyType.Jumper    => Color.SkyBlue,
                        EnemyType.Beamer    => Color.Magenta,
                        _ => Color.White
                    };
                    reviveCastTimer = 0;
                    reviveTarget = null;
                }
            }
        }
        else
        {
            // No dead allies — walk toward player normally
            reviveTarget = null;
            reviveCastTimer = 0;
            UpdateWalking(playerPos, roomLayout, tileSize, offsetX, offsetY, deltaTime, allEnemies);
        }
    }
    
    private void UpdateJumper(float deltaTime, List<Bullet> bullets, int[,] roomLayout, int tileSize, int offsetX, int offsetY)
    {
        if (!isJumping)
        {
            shootTimer -= deltaTime;
            if (shootTimer <= 0)
            {
                shootTimer = shootCooldown;
                for (int i = 0; i < 5; i++)
                {
                    float angle = (float)(rand.NextDouble() * Math.PI * 2);
                    Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    bullets.Add(new Bullet(Position, direction * 100f, 5f, false));
                }
            }
            
            if (rand.NextDouble() < 0.01)
                StartJump(roomLayout, tileSize, offsetX, offsetY);
        }
        else
        {
            jumpTimer += deltaTime;
            float t = Math.Min(jumpTimer / 0.4f, 1.0f); // lerp over 0.4 seconds
            Position = Vector2.Lerp(jumpStart, jumpTarget, t);
            
            if (t >= 1.0f)
            {
                Position = jumpTarget;
                isJumping = false;
                jumpTimer = 0;
            }
        }
    }
    
    private void StartJump(int[,] roomLayout, int tileSize, int offsetX, int offsetY)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int x = rand.Next(1, 12);
            int y = rand.Next(1, 6);
            
            if (roomLayout[y, x] == 0)
            {
                jumpStart = Position;
                jumpTarget = new Vector2(
                    offsetX + (x + 1) * tileSize + tileSize / 2,
                    offsetY + (y + 1) * tileSize + tileSize / 2
                );
                isJumping = true;
                jumpTimer = 0;
                break;
            }
        }
    }
    
    private void UpdateBeamer(Vector2 playerPos, float deltaTime, List<Bullet> bullets, List<Enemy> allEnemies)
    {
        Vector2 direction = playerPos - Position;
        float distance = direction.Length();
        
        // Try to maintain distance but don't get on top of player
        if (distance > 120)
        {
            direction = Vector2.Normalize(direction);
            Vector2 newPos = Position + direction * speed * deltaTime * 60;
            
            if (!CheckEnemyCollision(newPos, allEnemies))
            {
                Position = newPos;
            }
        }
        else if (distance < 80)
        {
            // Too close, back away
            direction = Vector2.Normalize(direction);
            Vector2 newPos = Position - direction * speed * deltaTime * 60;
            
            if (!CheckEnemyCollision(newPos, allEnemies))
            {
                Position = newPos;
            }
        }
        
        if (!isFiringBeam)
        {
            beamTimer -= deltaTime;
            if (beamTimer <= 0)
            {
                beamTimer = 15.0f;
                isFiringBeam = true;
                beamDuration = 2.0f;
                beamDirection = Vector2.Normalize(playerPos - Position);
            }
        }
        else
        {
            beamDuration -= deltaTime;
            if (beamDuration <= 0)
            {
                isFiringBeam = false;
            }
        }
    }
    
    private bool CheckEnemyCollision(Vector2 newPos, List<Enemy> allEnemies)
    {
        foreach (var other in allEnemies)
        {
            if (other == this || !other.IsAlive)
                continue;
            
            // Skip collision with jumping enemies
            if (other.Type == EnemyType.Jumper && other.isJumping)
                continue;
            
            float distance = Vector2.Distance(newPos, other.Position);
            if (distance < Size + other.Size)
            {
                return true; // Collision detected
            }
        }
        
        return false;
    }
    
    private bool CheckWallCollision(Vector2 newPos, int[,] roomLayout, int tileSize, int offsetX, int offsetY)
    {
        int gridX = (int)((newPos.X - offsetX) / tileSize) - 1;
        int gridY = (int)((newPos.Y - offsetY) / tileSize) - 1;
        
        if (gridX < 0 || gridX >= 13 || gridY < 0 || gridY >= 7)
            return true;
        
        if (roomLayout[gridY, gridX] == 2)
            return true;
        
        return false;
    }
    
    private void ShootAtPlayer(Vector2 playerPos, List<Bullet> bullets, float bulletSpeed, bool pierceWalls)
    {
        Vector2 direction = Vector2.Normalize(playerPos - Position);
        bullets.Add(new Bullet(Position, direction * bulletSpeed, 5f, pierceWalls));
    }
    
    private List<Vector2> FindPath(Vector2 start, Vector2 end, int[,] roomLayout, int tileSize, int offsetX, int offsetY)
    {
        List<Vector2> path = new List<Vector2>();
        
        int startX = (int)((start.X - offsetX) / tileSize) - 1;
        int startY = (int)((start.Y - offsetY) / tileSize) - 1;
        int endX = (int)((end.X - offsetX) / tileSize) - 1;
        int endY = (int)((end.Y - offsetY) / tileSize) - 1;
        
        startX = Math.Clamp(startX, 0, 12);
        startY = Math.Clamp(startY, 0, 6);
        endX = Math.Clamp(endX, 0, 12);
        endY = Math.Clamp(endY, 0, 6);
        
        if (IsLineOfSightClear(startX, startY, endX, endY, roomLayout))
        {
            path.Add(end);
            return path;
        }
        
        path.Add(new Vector2(
            offsetX + (endX + 1) * tileSize + tileSize / 2,
            offsetY + (endY + 1) * tileSize + tileSize / 2
        ));
        
        return path;
    }
    
    private bool IsLineOfSightClear(int x1, int y1, int x2, int y2, int[,] roomLayout)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            if (x1 < 0 || x1 >= 13 || y1 < 0 || y1 >= 7)
                return false;
                
            if (roomLayout[y1, x1] == 2)
                return false;
            
            if (x1 == x2 && y1 == y2)
                break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
        
        return true;
    }
    
    public void Draw()
    {
        if (!IsAlive) return;
        
        // Show jump shadow at target
        if (Type == EnemyType.Jumper && isJumping)
            Raylib.DrawCircleV(jumpTarget, Size, new Color(0, 0, 0, 100));
        
        // Show revive cast beam
        if (Type == EnemyType.Reviver && reviveTarget != null)
            Raylib.DrawLineEx(Position, reviveTarget.Position, 2f, new Color(100, 255, 100, 180));
        
        // Flash white on hit, otherwise use normal color
        Color drawColor = hitFlashTimer > 0 ? Color.White : Color;
        Raylib.DrawCircleV(Position, Size, drawColor);
        
        // Health bar
        float healthBarWidth = Size * 2;
        float healthBarHeight = 3;
        float healthPercent = (float)Health / MaxHealth;
        Raylib.DrawRectangle((int)(Position.X - healthBarWidth / 2), (int)(Position.Y - Size - 5), (int)healthBarWidth, (int)healthBarHeight, Color.Red);
        Raylib.DrawRectangle((int)(Position.X - healthBarWidth / 2), (int)(Position.Y - Size - 5), (int)(healthBarWidth * healthPercent), (int)healthBarHeight, Color.Green);
        
        // Beam
        if (Type == EnemyType.Beamer && isFiringBeam)
            Raylib.DrawLineEx(Position, Position + beamDirection * 1000f, 8f, new Color(255, 0, 255, 150));
    }
    
    public bool IsFiringBeam()
    {
        return Type == EnemyType.Beamer && isFiringBeam;
    }
    
    public Vector2 GetBeamDirection()
    {
        return beamDirection;
    }
}
using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;
    public float MaxLifetime;
    public Color Color;
    public float Size;
    
    public Particle(Vector2 pos, Vector2 vel, float lifetime, Color color, float size)
    {
        Position = pos;
        Velocity = vel;
        Lifetime = lifetime;
        MaxLifetime = lifetime;
        Color = color;
        Size = size;
    }
    
    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        Lifetime -= deltaTime;
    }
    
    public bool IsAlive()
    {
        return Lifetime > 0;
    }
    
    public float GetAlpha()
    {
        return Lifetime / MaxLifetime;
    }
}

public class Sword
{
    private Vector2 playerPos;
    private float range;
    private int damage;
    private float attackSpeed;
    private float attackCooldown;
    private bool isAttacking;
    private float attackAnimTimer;
    private float attackAnimDuration = 0.2f;
    private Vector2 attackDirection;
    
    // Charge / boomerang attack
    private bool isBoomerang;
    private Vector2 boomerangPos;
    private Vector2 boomerangDir;
    private float boomerangSpeed;
    private bool boomerangReturning;
    private float boomerangMaxDist;
    private float boomerangTravelled;
    private float boomerangAngle; // rotation for visual spin
    
    // Charge input tracking
    
    // Particle system
    private List<Particle> particles;
    private Random rand;
    
    public Sword(int damage, float attackSpeed, float range)
    {
        this.damage = damage;
        this.attackSpeed = attackSpeed;
        this.range = range;
        this.attackCooldown = 0;
        this.isAttacking = false;
        this.attackAnimTimer = 0;
        this.particles = new List<Particle>();
        this.rand = new Random();
        this.isBoomerang = false;
        this.boomerangReturning = false;
    }
    
    public void Update(float deltaTime, Vector2 playerPosition, Vector2 mousePosition, float tileSize)
    {
        playerPos = playerPosition;
        
        if (attackCooldown > 0)
            attackCooldown -= deltaTime;
        
        // Update normal swing animation
        if (isAttacking)
        {
            attackAnimTimer -= deltaTime;
            if (attackAnimTimer <= 0)
                isAttacking = false;
        }
        
        // Update boomerang
        if (isBoomerang)
        {
            boomerangAngle += deltaTime * 720f; // spin fast
            
            if (!boomerangReturning)
            {
                float step = boomerangSpeed * deltaTime;
                boomerangPos += boomerangDir * step;
                boomerangTravelled += step;
                
                if (boomerangTravelled >= boomerangMaxDist)
                    boomerangReturning = true;
            }
            else
            {
                // Return toward player
                Vector2 toPlayer = playerPos - boomerangPos;
                float dist = toPlayer.Length();
                if (dist < boomerangSpeed * deltaTime + 8f)
                {
                    // Caught — end boomerang
                    isBoomerang = false;
                    boomerangReturning = false;
                    attackCooldown = 1.0f / attackSpeed;
                }
                else
                {
                    boomerangPos += Vector2.Normalize(toPlayer) * boomerangSpeed * deltaTime;
                }
            }
        }
        
        // Update particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            particles[i].Update(deltaTime);
            if (!particles[i].IsAlive())
                particles.RemoveAt(i);
        }
    }
    
    public void Attack(Vector2 direction)
    {
        if (attackCooldown <= 0 && !isAttacking && !isBoomerang)
        {
            isAttacking = true;
            attackAnimTimer = attackAnimDuration;
            attackCooldown = 1.0f / attackSpeed;
            attackDirection = direction.Length() > 0 ? Vector2.Normalize(direction) : new Vector2(0, -1);
            SpawnWindParticles();
            SoundManager.Play("sword_swing");
        }
    }
    
    public void ChargeAttack(Vector2 direction)
    {
        if (attackCooldown <= 0 && !isAttacking && !isBoomerang)
        {
            boomerangDir = direction.Length() > 0 ? Vector2.Normalize(direction) : new Vector2(0, -1);
            boomerangPos = playerPos;
            boomerangTravelled = 0;
            boomerangMaxDist = range * 32f * 5f;
            boomerangSpeed = 500f;
            boomerangReturning = false;
            boomerangAngle = 0;
            isBoomerang = true;
            SoundManager.Play("sword_throw");
        }
    }
    
    private void SpawnWindParticles()
    {
        // Spawn particles in an arc along the swing direction
        int particleCount = 15;
        float arcAngle = 90f; // degrees
        
        for (int i = 0; i < particleCount; i++)
        {
            // Calculate angle for this particle
            float angle = (float)(Math.Atan2(attackDirection.Y, attackDirection.X));
            float offset = (i / (float)particleCount - 0.5f) * (arcAngle * (float)Math.PI / 180f);
            float particleAngle = angle + offset;
            
            // Calculate particle position along the arc
            float distance = range * 32f * (0.5f + (i / (float)particleCount) * 0.5f); // range * tileSize
            Vector2 particlePos = playerPos + new Vector2(
                (float)Math.Cos(particleAngle) * distance,
                (float)Math.Sin(particleAngle) * distance
            );
            
            // Calculate particle velocity
            Vector2 particleVel = new Vector2(
                (float)Math.Cos(particleAngle),
                (float)Math.Sin(particleAngle)
            ) * 100f;
            
            // Random color variation (white to light gray)
            int colorValue = 200 + rand.Next(56);
            Color particleColor = new Color(colorValue, colorValue, colorValue, 255);
            
            // Random size
            float size = 2f + (float)rand.NextDouble() * 3f;
            
            // Create particle
            particles.Add(new Particle(particlePos, particleVel, 0.3f, particleColor, size));
        }
    }
    
    public void Draw()
    {
        // Draw particles
        foreach (var particle in particles)
        {
            float alpha = particle.GetAlpha();
            Color drawColor = new Color(particle.Color.R, particle.Color.G, particle.Color.B, (byte)(alpha * 255));
            Raylib.DrawCircleV(particle.Position, particle.Size, drawColor);
        }
        
        // Draw normal sword swing
        if (isAttacking)
        {
            float animProgress = 1.0f - (attackAnimTimer / attackAnimDuration);
            float swingAngle = animProgress * 120f - 60f;
            float baseAngle = (float)Math.Atan2(attackDirection.Y, attackDirection.X);
            float currentAngle = baseAngle + swingAngle * (float)Math.PI / 180f;
            Vector2 swordEnd = playerPos + new Vector2(
                (float)Math.Cos(currentAngle) * range * 32f,
                (float)Math.Sin(currentAngle) * range * 32f);
            Raylib.DrawLineEx(playerPos, swordEnd, 3f, Color.Gray);
            Raylib.DrawCircleV(swordEnd, 4f, Color.LightGray);
        }
        
        // Draw boomerang — same look as sword, spinning around its center
        if (isBoomerang)
        {
            float rad = boomerangAngle * (float)Math.PI / 180f;
            float halfLen = range * 32f / 2f;
            Vector2 tip1 = boomerangPos + new Vector2((float)Math.Cos(rad), (float)Math.Sin(rad)) * halfLen;
            Vector2 tip2 = boomerangPos - new Vector2((float)Math.Cos(rad), (float)Math.Sin(rad)) * halfLen;
            Raylib.DrawLineEx(tip1, tip2, 3f, Color.Gray);
            Raylib.DrawCircleV(tip1, 4f, Color.LightGray); // blade tip
            
            // Trail line from player to boomerang while outgoing
            if (!boomerangReturning)
            {
                Color trailColor = new Color((byte)180, (byte)180, (byte)220, (byte)60);
                Raylib.DrawLineEx(playerPos, boomerangPos, 1f, trailColor);
            }
        }
    }
    
    public bool IsBoomerangActive() => isBoomerang;
    public Vector2 GetBoomerangPos() => boomerangPos;
    public float GetBoomerangHitRadius() => range * 32f / 2f;
    
    public void UpdateStats(int newDamage, float newAttackSpeed, float newRange)
    {
        damage = newDamage;
        attackSpeed = newAttackSpeed;
        range = newRange;
    }
    
    public bool IsAttacking()
    {
        return isAttacking;
    }
    
    // Get the attack hitbox for collision detection with enemies
    public (Vector2 start, Vector2 end, float width) GetAttackHitbox()
    {
        if (!isAttacking)
            return (Vector2.Zero, Vector2.Zero, 0);
        
        Vector2 swordEnd = playerPos + attackDirection * range * 32f;
        return (playerPos, swordEnd, 10f); // 10 pixel width hitbox
    }
}
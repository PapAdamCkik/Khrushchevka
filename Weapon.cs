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
    }
    
    public void Update(float deltaTime, Vector2 playerPosition, Vector2 mousePosition, float tileSize)
    {
        playerPos = playerPosition;
        
        // Update attack cooldown
        if (attackCooldown > 0)
            attackCooldown -= deltaTime;
        
        // Update attack animation
        if (isAttacking)
        {
            attackAnimTimer -= deltaTime;
            if (attackAnimTimer <= 0)
                isAttacking = false;
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
        if (attackCooldown <= 0 && !isAttacking)
        {
            isAttacking = true;
            attackAnimTimer = attackAnimDuration;
            attackCooldown = 1.0f / attackSpeed; // Convert attack speed to cooldown
            
            // Set attack direction
            attackDirection = direction;
            float length = attackDirection.Length();
            if (length > 0)
                attackDirection = Vector2.Normalize(attackDirection);
            else
                attackDirection = new Vector2(0, -1); // Default to up if no direction
            
            // Spawn wind particles
            SpawnWindParticles();
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
            Color drawColor = new Color(
                particle.Color.R,
                particle.Color.G,
                particle.Color.B,
                (byte)(alpha * 255)
            );
            
            Raylib.DrawCircleV(particle.Position, particle.Size, drawColor);
        }
        
        // Draw sword swing animation
        if (isAttacking)
        {
            float animProgress = 1.0f - (attackAnimTimer / attackAnimDuration);
            float swingAngle = animProgress * 120f - 60f; // Swing from -60 to +60 degrees
            
            float baseAngle = (float)Math.Atan2(attackDirection.Y, attackDirection.X);
            float currentAngle = baseAngle + swingAngle * (float)Math.PI / 180f;
            
            // Draw sword as a line
            Vector2 swordEnd = playerPos + new Vector2(
                (float)Math.Cos(currentAngle) * range * 32f,
                (float)Math.Sin(currentAngle) * range * 32f
            );
            
            Raylib.DrawLineEx(playerPos, swordEnd, 3f, Color.Gray);
            Raylib.DrawCircleV(swordEnd, 4f, Color.LightGray);
        }
    }
    
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
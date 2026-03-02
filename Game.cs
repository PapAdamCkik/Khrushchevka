using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace Khrushchevka_RPG;

public enum ItemType
{
    HealthPickup,
    MaxHealthUp,
    SpeedUp,
    DamageUp,
    RangeUp,
    AttackSpeedUp
}

public class PickupItem
{
    public Vector2 Position;
    public ItemType Type;
    public bool Collected;
    public float BobTimer;
    public PickupItem(Vector2 position, ItemType type) { Position = position; Type = type; }
    public Color GetColor() => Type switch
    {
        ItemType.HealthPickup  => new Color(220, 50,  50,  255),
        ItemType.MaxHealthUp   => new Color(255, 100, 100, 255),
        ItemType.SpeedUp       => new Color(100, 220, 100, 255),
        ItemType.DamageUp      => new Color(255, 140, 0,   255),
        ItemType.RangeUp       => new Color(80,  160, 255, 255),
        ItemType.AttackSpeedUp => new Color(220, 220, 50,  255),
        _ => Color.White
    };
    public string GetLabel() => Type switch
    {
        ItemType.HealthPickup  => "+3 HP",
        ItemType.MaxHealthUp   => "+5 Max HP",
        ItemType.SpeedUp       => "+Speed",
        ItemType.DamageUp      => "+Damage",
        ItemType.RangeUp       => "+Range",
        ItemType.AttackSpeedUp => "+Atk Spd",
        _ => "?"
    };
}

class Game
{
    private FloorGenerator floorGenerator;
    private Dictionary<GridPosition, FloorNode> currentFloor;
    private GridPosition currentRoomPos;
    private FloorNode? currentRoomNode;
    
    // Floor tracking
    private int currentFloorNumber;
    
    // Player variables
    private Vector2 playerPos;
    private float playerSize;
    private Vector2 lastMoveDirection;
    
    // Player statistics
    private int maxHealth;
    private int currentHealth;
    private float speed;
    private int damage;
    private float attackSpeed;
    private float range;
    
    // Weapon
    private Sword sword;
    
    // Room rendering variables
    private int tileSize;
    private int offsetX;
    private int offsetY;
    
    // Gateway variables
    private Vector2 gatewayPos;
    private float gatewaySize;
    private bool showGateway;
    
    // Game state
    private bool isDead;
    private int deathSelectedOption;
    private bool hasWon;
    private int wonSelectedOption;
    
    // Pause menu variables
    private bool isPaused;
    private int pauseSelectedOption;
    private Font pauseFont;
    private int currentLanguage;
    
    // Enemy system
    private List<Enemy> enemies;
    private List<Bullet> bullets;
    private float enemySpawnTimer;
    
    // Boss system
    private Boss? currentBoss;
    private HashSet<BossType> usedBosses;
    private int lastBossTier = 0;
    private float bossContactCooldown;
    
    // Knockback
    private Vector2 knockbackVelocity;
    
    // Charge attack input
    private float chargeHoldTimer;
    private Vector2 chargeHoldDir;
    
    // Player iframes
    private float playerInvincTimer;
    private float playerFlashTimer;
    private const float PlayerInvincDuration = 0.5f;
    private const float PlayerFlashDuration  = 0.5f;
    
    // Room locking
    private bool roomLocked;
    private HashSet<GridPosition> clearedRooms;
    private float lockFlashTimer;
    private const float LockFlashDuration = 0.3f;
    
    // Beam damage cooldown
    private float beamDamageCooldown;
    
    // Item system
    private List<PickupItem> activeItems;
    private Random itemRand;
    
    public Game()
    {
        // Initialize room blueprints
        RoomBlueprints.Initialize();
        
        currentFloorNumber = 1;
        
        // Generate first floor
        floorGenerator = new FloorGenerator();
        currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
        currentRoomPos = new GridPosition(0, 0);
        currentRoomNode = currentFloor[currentRoomPos];
        
        // Calculate room layout
        CalculateRoomLayout();
        
        // Initialize player
        playerPos = new Vector2(400, 225);
        playerSize = 20;
        lastMoveDirection = new Vector2(0, -1);
        
        // Initialize player statistics
        maxHealth = 10;
        currentHealth = 10;
        speed = 1.0f;
        damage = 5;
        attackSpeed = 1.0f;
        range = 1.0f;
        
        // Initialize weapon
        sword = new Sword(damage, attackSpeed, range);
        
        // Initialize gateway
        gatewaySize = 40;
        showGateway = false;
        
        // Initialize game state
        isDead = false;
        deathSelectedOption = 0;
        hasWon = false;
        wonSelectedOption = 0;
        isPaused = false;
        pauseSelectedOption = 0;
        currentLanguage = 0;
        
        // Initialize enemy system
        enemies = new List<Enemy>();
        bullets = new List<Bullet>();
        enemySpawnTimer = 0;
        
        // Initialize boss system
        currentBoss = null;
        usedBosses = new HashSet<BossType>();
        bossContactCooldown = 0;
        
        // Initialize room locking
        roomLocked = false;
        clearedRooms = new HashSet<GridPosition>();
        lockFlashTimer = 0;
        
        // Initialize item system
        activeItems = new List<PickupItem>();
        itemRand = new Random();
    }
    
    public void SetFont(Font font)
    {
        pauseFont = font;
    }
    
    public void SetLanguage(int lang)
    {
        currentLanguage = lang;
    }
    
    private void CalculateRoomLayout()
    {
        int tileSizeX = 800 / 15;
        int tileSizeY = 450 / 9;
        tileSize = Math.Min(tileSizeX, tileSizeY);
        
        int totalWidth = tileSize * 15;
        int totalHeight = tileSize * 9;
        offsetX = (800 - totalWidth) / 2;
        offsetY = (450 - totalHeight) / 2;
    }
    
    private void UpdateGateway()
    {
        if (currentRoomNode?.Type == RoomType.Boss)
        {
            showGateway = !roomLocked && enemies.Count == 0;
            if (showGateway)
                gatewayPos = new Vector2(400, 225);
        }
        else
        {
            showGateway = false;
        }
    }
    
    private void CheckGatewayInteraction()
    {
        if (!showGateway) return;
        
        float distance = Vector2.Distance(playerPos, gatewayPos);
        if (distance < (playerSize + gatewaySize) / 2)
        {
            // Check if final floor
            if (currentFloorNumber == 7)
            {
                hasWon = true;
                wonSelectedOption = 0;
            }
            else if (currentFloorNumber < 7)
            {
                GoToNextFloor();
            }
        }
    }
    
    private void RestartGame()
    {
        currentFloorNumber = 1;
        currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
        currentRoomPos = new GridPosition(0, 0);
        currentRoomNode = currentFloor[currentRoomPos];
        playerPos = new Vector2(400, 225);
        lastMoveDirection = new Vector2(0, -1);
        currentHealth = maxHealth;
        isDead = false;
        hasWon = false;
        showGateway = false;
        
        knockbackVelocity = Vector2.Zero;
        playerInvincTimer = 0;
        playerFlashTimer  = 0;
        chargeHoldTimer   = 0;
        chargeHoldDir     = Vector2.Zero;
        
        // Clear enemies
        enemies.Clear();
        bullets.Clear();
        activeItems.Clear();
        currentBoss = null;
        usedBosses.Clear();
        lastBossTier = 0;
        roomLocked = false;
        clearedRooms.Clear();
        lockFlashTimer = 0;
    }
    
    private void SpawnEnemies()
    {
        enemies.Clear();
        bullets.Clear();
        
        if (currentRoomNode?.Type == RoomType.Start ||
            currentRoomNode?.Type == RoomType.Boss  ||
            currentRoomNode?.Type == RoomType.Item)
        {
            roomLocked = false;
            if (currentRoomNode.Type == RoomType.Item)
                TrySpawnItemRoomPedestal();
            if (currentRoomNode.Type == RoomType.Boss && !clearedRooms.Contains(currentRoomPos))
                SpawnBoss();
            return;
        }
        
        if (clearedRooms.Contains(currentRoomPos))
        {
            roomLocked = false;
            return;
        }
        
        Random rand = new Random();
        
        // 50% chance the room is empty — no enemies spawn
        if (rand.NextDouble() < 0.5)
            return;
        
        int enemyCount = 3 + (currentFloorNumber * 2);
        
        for (int i = 0; i < enemyCount; i++)
        {
            // Find random valid spawn position
            Vector2 spawnPos = Vector2.Zero;
            bool validPosition = false;
            
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = rand.Next(1, 12);
                int y = rand.Next(1, 6);
                
                if (currentRoomNode?.RoomData?.Layout[y, x] == 0)
                {
                    spawnPos = new Vector2(
                        offsetX + (x + 1) * tileSize + tileSize / 2,
                        offsetY + (y + 1) * tileSize + tileSize / 2
                    );
                    
                    // Check distance from player
                    if (Vector2.Distance(spawnPos, playerPos) > 100)
                    {
                        validPosition = true;
                        break;
                    }
                }
            }
            
            if (!validPosition) continue;
            
            // Enemy speed scales with floor: starts at 55% of player speed on floor 1,
            // reaches 85% by floor 7. Always slower than the player.
            float floorSpeedMult = 0.55f + (currentFloorNumber - 1) * (0.30f / 6f);
            float enemySpeed = 3.0f * speed * floorSpeedMult;
            
            EnemyType type = DetermineEnemyType(rand);
            enemies.Add(new Enemy(type, spawnPos, enemySpeed));
        }
        
        if (enemies.Count > 0)
        {
            roomLocked = true;
            SoundManager.Play("room_lock");
        }
    }
    
    private void SpawnBoss()
    {
        currentBoss = null;
        int tier = Boss.GetTierForFloor(currentFloorNumber);

        // Clear used list when entering a new tier
        if (tier != lastBossTier)
        {
            usedBosses.Clear();
            lastBossTier = tier;
        }

        BossType[] tierBosses = Boss.GetTierBosses(tier);
        var available = new List<BossType>();
        foreach (var bt in tierBosses)
            if (!usedBosses.Contains(bt)) available.Add(bt);

        // All exhausted — reset for this tier only
        if (available.Count == 0)
        {
            usedBosses.Clear();
            foreach (var bt in tierBosses) available.Add(bt);
        }

        BossType chosen = available[itemRand.Next(available.Count)];
        usedBosses.Add(chosen);

        float il = offsetX + tileSize;
        float ir = offsetX + 14 * tileSize;
        float it = offsetY + tileSize;
        float ib = offsetY + 8 * tileSize;

        currentBoss = new Boss(chosen, new Vector2(400, 225), il, ir, it, ib);
        roomLocked = true;
        SoundManager.Play("room_lock");
        SoundManager.Play("boss_enter");
    }
    
    private void UpdateRoomLock()
    {
        // Normal room unlock
        if (roomLocked && currentBoss == null && enemies.Count(e => e.IsAlive) == 0)
        {
            enemies.Clear();
            roomLocked = false;
            clearedRooms.Add(currentRoomPos);
            lockFlashTimer = LockFlashDuration;
            TryDropHealthPickup();
            SoundManager.Play("room_unlock");
        }
        
        // Boss room unlock
        if (roomLocked && currentBoss != null && !currentBoss.IsAlive)
        {
            currentBoss = null;
            roomLocked = false;
            clearedRooms.Add(currentRoomPos);
            lockFlashTimer = LockFlashDuration;
            TryDropHealthPickup();
            SoundManager.Play("room_unlock");
        }
    }
    
    private void TrySpawnItemRoomPedestal()
    {
        bool alreadyHas = activeItems.Any(i => i.Type != ItemType.HealthPickup && Vector2.Distance(i.Position, new Vector2(400, 225)) < 50);
        if (alreadyHas) return;
        ItemType[] types = { ItemType.MaxHealthUp, ItemType.SpeedUp, ItemType.DamageUp, ItemType.RangeUp, ItemType.AttackSpeedUp };
        activeItems.Add(new PickupItem(new Vector2(400, 225), types[itemRand.Next(types.Length)]));
    }
    
    private void TryDropHealthPickup()
    {
        if (itemRand.NextDouble() < 0.20)
            activeItems.Add(new PickupItem(new Vector2(400, 225), ItemType.HealthPickup));
    }
    
    private void CheckItemPickups()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (Vector2.Distance(playerPos, activeItems[i].Position) < playerSize / 2 + 12f)
            {
                ApplyItem(activeItems[i].Type);
                activeItems.RemoveAt(i);
                SoundManager.Play("item_pickup");
            }
        }
    }
    
    private void ApplyItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.HealthPickup:   Heal(3); break;
            case ItemType.MaxHealthUp:    maxHealth += 5; currentHealth += 5; break;
            case ItemType.SpeedUp:        speed += 0.5f; break;
            case ItemType.DamageUp:       damage += 3; sword.UpdateStats(damage, attackSpeed, range); break;
            case ItemType.RangeUp:        range += 0.5f; sword.UpdateStats(damage, attackSpeed, range); break;
            case ItemType.AttackSpeedUp:  attackSpeed += 0.5f; sword.UpdateStats(damage, attackSpeed, range); break;
        }
    }
    
    private EnemyType DetermineEnemyType(Random rand)
    {
        List<EnemyType> availableTypes = new List<EnemyType>();
        
        // Stage 1 - available on all floors
        availableTypes.Add(EnemyType.Flying);
        availableTypes.Add(EnemyType.Walking);
        availableTypes.Add(EnemyType.Shooter);
        
        // Stage 2 - available from floor 2+
        if (currentFloorNumber >= 2)
        {
            availableTypes.Add(EnemyType.Stationary);
            availableTypes.Add(EnemyType.Reviver);
        }
        
        // Stage 3 - available from floor 5+
        if (currentFloorNumber >= 5)
        {
            availableTypes.Add(EnemyType.Jumper);
            availableTypes.Add(EnemyType.Beamer);
        }
        
        return availableTypes[rand.Next(availableTypes.Count)];
    }
    
    public (Program.GameState newState, bool openSettings) Update(KeyboardKey upKey, KeyboardKey downKey, KeyboardKey leftKey, KeyboardKey rightKey, KeyboardKey actionKey, int difficulty, int language, Dictionary<string, bool> unlocks)
    {
        // Won screen handling
        if (hasWon)
        {
            if (Raylib.IsKeyPressed(downKey))
                wonSelectedOption = (wonSelectedOption + 1) % 2;
            
            if (Raylib.IsKeyPressed(upKey))
                wonSelectedOption = (wonSelectedOption - 1 + 2) % 2;
            
            if (Raylib.IsKeyPressed(actionKey))
            {
                if (wonSelectedOption == 0)
                    RestartGame();
                else
                    return (Program.GameState.MainMenu, false);
            }
            
            return (Program.GameState.Playing, false);
        }
        
        // Death screen handling
        if (isDead)
        {
            if (Raylib.IsKeyPressed(downKey))
                deathSelectedOption = (deathSelectedOption + 1) % 2;
            
            if (Raylib.IsKeyPressed(upKey))
                deathSelectedOption = (deathSelectedOption - 1 + 2) % 2;
            
            if (Raylib.IsKeyPressed(actionKey))
            {
                if (deathSelectedOption == 0)
                    RestartGame();
                else
                    return (Program.GameState.MainMenu, false);
            }
            
            return (Program.GameState.Playing, false);
        }
        
        // Toggle pause
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            isPaused = !isPaused;
            pauseSelectedOption = 0;
        }
        
        if (isPaused)
        {
            if (Raylib.IsKeyPressed(downKey))
                pauseSelectedOption = (pauseSelectedOption + 1) % 3;
            
            if (Raylib.IsKeyPressed(upKey))
                pauseSelectedOption = (pauseSelectedOption - 1 + 3) % 3;
            
            if (Raylib.IsKeyPressed(actionKey))
            {
                switch (pauseSelectedOption)
                {
                    case 0: isPaused = false; break;
                    case 1: return (Program.GameState.Settings, true);
                    case 2: return (Program.GameState.MainMenu, false);
                }
            }
        }
        else
        {
            // Restart game
            if (Raylib.IsKeyPressed(KeyboardKey.R))
                RestartGame();
            
            // Previous floor (testing)
            if (Raylib.IsKeyPressed(KeyboardKey.P) && currentFloorNumber > 1)
                GoToPreviousFloor();
            
            // Next floor (testing)
            if (Raylib.IsKeyPressed(KeyboardKey.N) && currentFloorNumber < 7)
                GoToNextFloor();
            
            // Damage/heal testing
            if (Raylib.IsKeyPressed(KeyboardKey.H))
                TakeDamage(1);
            if (Raylib.IsKeyPressed(KeyboardKey.J))
                Heal(1);
            
            // Player movement — axes checked independently so player slides along walls
            Vector2 moveDirection = Vector2.Zero;
            float moveSpeed = 3.0f * speed;
            if (Raylib.IsKeyDown(upKey))    moveDirection.Y = -1;
            if (Raylib.IsKeyDown(downKey))  moveDirection.Y =  1;
            if (Raylib.IsKeyDown(leftKey))  moveDirection.X = -1;
            if (Raylib.IsKeyDown(rightKey)) moveDirection.X =  1;
            if (moveDirection.Length() > 0)
            {
                lastMoveDirection = Vector2.Normalize(moveDirection);
                SoundManager.PlayIfNotPlaying("player_step", 0.4f);
            }
            else
            {
                SoundManager.Stop("player_step");
            }
            
            Vector2 newPosX = new Vector2(playerPos.X + moveDirection.X * moveSpeed, playerPos.Y);
            if (!CheckCollision(newPosX)) playerPos = newPosX;
            Vector2 newPosY = new Vector2(playerPos.X, playerPos.Y + moveDirection.Y * moveSpeed);
            if (!CheckCollision(newPosY)) playerPos = newPosY;
            
            // Apply knockback (decays quickly, blocked by walls)
            if (knockbackVelocity.Length() > 0.5f)
            {
                float kbDt = Raylib.GetFrameTime();
                Vector2 kbMoveX = new Vector2(knockbackVelocity.X * kbDt, 0);
                Vector2 kbMoveY = new Vector2(0, knockbackVelocity.Y * kbDt);
                if (!CheckCollision(playerPos + kbMoveX)) playerPos += kbMoveX;
                if (!CheckCollision(playerPos + kbMoveY)) playerPos += kbMoveY;
                knockbackVelocity *= 0.75f; // friction
            }
            else
            {
                knockbackVelocity = Vector2.Zero;
            }
            
            // Update sword
            sword.Update(Raylib.GetFrameTime(), playerPos, new Vector2(Raylib.GetMouseX(), Raylib.GetMouseY()), tileSize);
            
            // Attack with arrow keys: tap = normal swing, hold 0.4s = charge boomerang throw
            Vector2 heldDir = Vector2.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.Up))    heldDir = new Vector2(0, -1);
            else if (Raylib.IsKeyDown(KeyboardKey.Down))  heldDir = new Vector2(0,  1);
            else if (Raylib.IsKeyDown(KeyboardKey.Left))  heldDir = new Vector2(-1, 0);
            else if (Raylib.IsKeyDown(KeyboardKey.Right)) heldDir = new Vector2( 1, 0);
            
            if (heldDir.Length() > 0)
            {
                chargeHoldTimer += Raylib.GetFrameTime();
                chargeHoldDir = heldDir;
            }
            else
            {
                // Key released — was it a tap or a charge?
                if (chargeHoldTimer > 0 && chargeHoldTimer < 0.4f)
                    sword.Attack(chargeHoldDir);
                else if (chargeHoldTimer >= 0.4f)
                    sword.ChargeAttack(chargeHoldDir);
                chargeHoldTimer = 0;
                chargeHoldDir = Vector2.Zero;
            }
            
            // Update enemies — keep dead ones in list so Reviver can reach them
            if (currentRoomNode?.RoomData != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    enemies[i].Update(Raylib.GetFrameTime(), playerPos, currentRoomNode.RoomData.Layout, tileSize, offsetX, offsetY, bullets, enemies);
                }
                // Remove permanently dead enemies (IsAlive=false and no Reviver alive to resurrect them)
                bool reviverAlive = enemies.Any(e => e.Type == EnemyType.Reviver && e.IsAlive);
                if (!reviverAlive)
                    enemies.RemoveAll(e => !e.IsAlive);
            }
            
            // Update boss
            if (currentBoss != null && currentBoss.IsAlive)
            {
                float bossDt = Raylib.GetFrameTime();
                var spawned = currentBoss.Update(bossDt, playerPos, bullets, 3.0f * speed);
                enemies.AddRange(spawned);
                
                // Boss contact damage (0.5s cooldown)
                if (bossContactCooldown > 0) bossContactCooldown -= bossDt;
                if (bossContactCooldown <= 0 &&
                    (currentBoss.IsCollidingWithPlayer(playerPos, playerSize) ||
                     currentBoss.OrbitalCollidesWithPlayer(playerPos, playerSize)))
                {
                    TakeDamage(1);
                    bossContactCooldown = 0.5f;
                    Vector2 kbDir = Vector2.Normalize(playerPos - currentBoss.Position);
                    knockbackVelocity = kbDir * 500f;
                }
                
                // Sword hits boss
                if (sword.IsAttacking())
                {
                    var hitbox = sword.GetAttackHitbox();
                    float distToLine = PointToLineDistance(currentBoss.Position, hitbox.start, hitbox.end);
                    if (distToLine < currentBoss.Size + hitbox.width / 2)
                        currentBoss.TakeDamage(damage);
                }
            }
            
            // Update bullets
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update(Raylib.GetFrameTime());
                
                // Check if bullet hit player
                if (Vector2.Distance(bullets[i].Position, playerPos) < playerSize / 2 + bullets[i].Size)
                {
                    TakeDamage(1);
                    bullets.RemoveAt(i);
                    continue;
                }
                
                // Check if bullet is out of bounds or hit wall
                if (!bullets[i].PierceWalls && CheckBulletCollision(bullets[i].Position))
                {
                    bullets.RemoveAt(i);
                    continue;
                }
                
                // Remove if out of screen
                if (bullets[i].Position.X < 0 || bullets[i].Position.X > 800 ||
                    bullets[i].Position.Y < 0 || bullets[i].Position.Y > 450)
                {
                    bullets.RemoveAt(i);
                }
            }
            
            // Check for beam damage (0.5s cooldown between hits)
            if (beamDamageCooldown > 0) beamDamageCooldown -= Raylib.GetFrameTime();
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;
                
                // Contact damage + knockback
                if (Vector2.Distance(enemy.Position, playerPos) < enemy.Size + playerSize / 2 && enemy.CanContactDamage())
                {
                    TakeDamage(1);
                    Vector2 kbDir = Vector2.Normalize(playerPos - enemy.Position);
                    knockbackVelocity = kbDir * 400f;
                }
                
                if (enemy.IsFiringBeam())
                {
                    Vector2 beamDir = enemy.GetBeamDirection();
                    Vector2 toPlayer = playerPos - enemy.Position;
                    float distanceToBeam = PointToLineDistance(playerPos, enemy.Position, enemy.Position + beamDir * 1000f);
                    
                    if (distanceToBeam < playerSize / 2 + 4 && Vector2.Dot(toPlayer, beamDir) > 0 && beamDamageCooldown <= 0)
                    {
                        TakeDamage(1);
                        beamDamageCooldown = 0.5f;
                    }
                }
            }
            
            // Check sword hits on enemies
            if (sword.IsAttacking())
            {
                var hitbox = sword.GetAttackHitbox();
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsAlive) continue;
                    float distToLine = PointToLineDistance(enemy.Position, hitbox.start, hitbox.end);
                    if (distToLine < enemy.Size + hitbox.width / 2)
                    {
                        enemy.TakeDamage(damage);
                        Vector2 kbDir = Vector2.Normalize(enemy.Position - playerPos);
                        enemy.ApplyKnockback(kbDir, 350f);
                        SoundManager.Play("sword_hit");
                    }
                }
            }
            
            // Boomerang hits
            if (sword.IsBoomerangActive())
            {
                Vector2 bPos = sword.GetBoomerangPos();
                float bRadius = sword.GetBoomerangHitRadius();
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsAlive) continue;
                    if (Vector2.Distance(bPos, enemy.Position) < bRadius + enemy.Size)
                    {
                        enemy.TakeDamage(damage);
                        Vector2 kbDir = Vector2.Normalize(enemy.Position - bPos);
                        enemy.ApplyKnockback(kbDir, 300f);
                        SoundManager.Play("sword_hit");
                    }
                }
                if (currentBoss != null && currentBoss.IsAlive)
                {
                    if (Vector2.Distance(bPos, currentBoss.Position) < bRadius + currentBoss.Size)
                        currentBoss.TakeDamage(damage);
                }
            }
            
            CheckRoomTransition();
            UpdateGateway();
            CheckGatewayInteraction();
            UpdateRoomLock();
            CheckItemPickups();
            
            float dt = Raylib.GetFrameTime();
            foreach (var item in activeItems) item.BobTimer += dt;
            if (lockFlashTimer > 0)  lockFlashTimer  -= dt;
            if (playerInvincTimer > 0) playerInvincTimer -= dt;
            if (playerFlashTimer  > 0) playerFlashTimer  -= dt;
        }
        
        return (Program.GameState.Playing, false);
    }
    
    private void TakeDamage(int damageAmount)
    {
        if (playerInvincTimer > 0) return;
        playerInvincTimer = PlayerInvincDuration;
        playerFlashTimer  = PlayerFlashDuration;
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;
            deathSelectedOption = 0;
            SoundManager.Play("player_death");
        }
        else
        {
            SoundManager.Play("player_hurt");
        }
    }
    
    private void Heal(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }
    
    private void GoToNextFloor()
    {
        if (currentFloorNumber < 7)
        {
            SoundManager.Play("floor_transition");
            currentFloorNumber++;
            currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
            currentRoomPos = new GridPosition(0, 0);
            currentRoomNode = currentFloor[currentRoomPos];
            playerPos = new Vector2(400, 225);
            showGateway = false;
            currentHealth = maxHealth;
            enemies.Clear();
            bullets.Clear();
            activeItems.Clear();
            currentBoss = null;
            usedBosses.Clear();
            lastBossTier = 0;
            roomLocked = false;
            clearedRooms.Clear();
            lockFlashTimer = 0;
        }
    }
    
    private void GoToPreviousFloor()
    {
        if (currentFloorNumber > 1)
        {
            currentFloorNumber--;
            currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
            currentRoomPos = new GridPosition(0, 0);
            currentRoomNode = currentFloor[currentRoomPos];
            playerPos = new Vector2(400, 225);
            showGateway = false;
            enemies.Clear();
            bullets.Clear();
            activeItems.Clear();
            currentBoss = null;
            usedBosses.Clear();
            lastBossTier = 0;
            roomLocked = false;
            clearedRooms.Clear();
            lockFlashTimer = 0;
        }
    }
    
    private void CheckRoomTransition()
    {
        if (currentRoomNode == null) return;
        if (roomLocked) return;
        
        int roomLeft = offsetX + tileSize;
        int roomRight = offsetX + 14 * tileSize;
        int roomTop = offsetY + tileSize;
        int roomBottom = offsetY + 8 * tileSize;
        
        int topDoorLeft = offsetX + 7 * tileSize;
        int topDoorRight = offsetX + 8 * tileSize;
        int topDoorY = offsetY;
        
        int bottomDoorLeft = offsetX + 7 * tileSize;
        int bottomDoorRight = offsetX + 8 * tileSize;
        int bottomDoorY = offsetY + 8 * tileSize;
        
        int leftDoorX = offsetX;
        int leftDoorTop = offsetY + 4 * tileSize;
        int leftDoorBottom = offsetY + 5 * tileSize;
        
        int rightDoorX = offsetX + 14 * tileSize;
        int rightDoorTop = offsetY + 4 * tileSize;
        int rightDoorBottom = offsetY + 5 * tileSize;
        
        if (currentRoomNode.HasTopDoor && playerPos.Y < topDoorY + tileSize / 2 &&
            playerPos.X > topDoorLeft && playerPos.X < topDoorRight)
            TransitionToRoom(0, -1, "bottom");
        
        if (currentRoomNode.HasBottomDoor && playerPos.Y > bottomDoorY + tileSize / 2 &&
            playerPos.X > bottomDoorLeft && playerPos.X < bottomDoorRight)
            TransitionToRoom(0, 1, "top");
        
        if (currentRoomNode.HasLeftDoor && playerPos.X < leftDoorX + tileSize / 2 &&
            playerPos.Y > leftDoorTop && playerPos.Y < leftDoorBottom)
            TransitionToRoom(-1, 0, "right");
        
        if (currentRoomNode.HasRightDoor && playerPos.X > rightDoorX + tileSize / 2 &&
            playerPos.Y > rightDoorTop && playerPos.Y < rightDoorBottom)
            TransitionToRoom(1, 0, "left");
    }
    
    private void TransitionToRoom(int deltaX, int deltaY, string entryDirection)
    {
        GridPosition newRoomPos = new GridPosition(currentRoomPos.X + deltaX, currentRoomPos.Y + deltaY);
        
        if (currentFloor.ContainsKey(newRoomPos))
        {
            currentRoomPos = newRoomPos;
            currentRoomNode = currentFloor[currentRoomPos];
            
            playerPos = entryDirection switch
            {
                "top" => new Vector2(400, offsetY + tileSize + 30),
                "bottom" => new Vector2(400, offsetY + 8 * tileSize - 30),
                "left" => new Vector2(offsetX + tileSize + 30, 225),
                "right" => new Vector2(offsetX + 14 * tileSize - 30, 225),
                _ => playerPos
            };
            
            // Spawn enemies in new room
            SpawnEnemies();
        }
    }
    
    private bool CheckCollision(Vector2 newPos)
    {
        if (currentRoomNode?.RoomData == null) return true;
        
        float left   = newPos.X - playerSize / 2;
        float right  = newPos.X + playerSize / 2;
        float top    = newPos.Y - playerSize / 2;
        float bottom = newPos.Y + playerSize / 2;
        
        // Inner room boundaries (pixel coords of the walkable area edges)
        float innerLeft   = offsetX + tileSize;
        float innerRight  = offsetX + 14 * tileSize;
        float innerTop    = offsetY + tileSize;
        float innerBottom = offsetY + 8 * tileSize;
        
        // Door opening pixel ranges
        float topDoorL  = offsetX + 7 * tileSize;
        float topDoorR  = offsetX + 8 * tileSize;
        float botDoorL  = offsetX + 7 * tileSize;
        float botDoorR  = offsetX + 8 * tileSize;
        float lDoorTop  = offsetY + 4 * tileSize;
        float lDoorBot  = offsetY + 5 * tileSize;
        float rDoorTop  = offsetY + 4 * tileSize;
        float rDoorBot  = offsetY + 5 * tileSize;
        
        bool open = !roomLocked;
        
        if (top < innerTop)
        {
            bool gap = open && currentRoomNode.HasTopDoor && right > topDoorL && left < topDoorR;
            if (!gap) return true;
        }
        if (bottom > innerBottom)
        {
            bool gap = open && currentRoomNode.HasBottomDoor && right > botDoorL && left < botDoorR;
            if (!gap) return true;
        }
        if (left < innerLeft)
        {
            bool gap = open && currentRoomNode.HasLeftDoor && bottom > lDoorTop && top < lDoorBot;
            if (!gap) return true;
        }
        if (right > innerRight)
        {
            bool gap = open && currentRoomNode.HasRightDoor && bottom > rDoorTop && top < rDoorBot;
            if (!gap) return true;
        }
        
        // Interior obstacle tiles
        int[,] layout = currentRoomNode.RoomData.Layout;
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 13; x++)
            {
                if (layout[y, x] == 2)
                {
                    float tL = offsetX + (x + 1) * tileSize;
                    float tR = offsetX + (x + 2) * tileSize;
                    float tT = offsetY + (y + 1) * tileSize;
                    float tB = offsetY + (y + 2) * tileSize;
                    if (right > tL && left < tR && bottom > tT && top < tB)
                        return true;
                }
            }
        }
        
        return false;
    }
    
    private bool CheckBulletCollision(Vector2 pos)
    {
        if (currentRoomNode?.RoomData == null) return true;
        
        // Check walls
        int roomLeft = offsetX + tileSize;
        int roomRight = offsetX + 14 * tileSize;
        int roomTop = offsetY + tileSize;
        int roomBottom = offsetY + 8 * tileSize;
        
        if (pos.X < roomLeft || pos.X > roomRight || pos.Y < roomTop || pos.Y > roomBottom)
            return true;
        
        // Check obstacles
        int[,] layout = currentRoomNode.RoomData.Layout;
        
        int gridX = (int)((pos.X - offsetX) / tileSize) - 1;
        int gridY = (int)((pos.Y - offsetY) / tileSize) - 1;
        
        if (gridX >= 0 && gridX < 13 && gridY >= 0 && gridY < 7)
        {
            if (layout[gridY, gridX] == 2)
                return true;
        }
        
        return false;
    }
    
    private float PointToLineDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.Length();
        
        if (lineLength == 0)
            return Vector2.Distance(point, lineStart);
        
        float t = Math.Max(0, Math.Min(1, Vector2.Dot(point - lineStart, line) / (lineLength * lineLength)));
        Vector2 projection = lineStart + t * line;
        
        return Vector2.Distance(point, projection);
    }
    
    public void Draw()
    {
        // Draw won screen if won
        if (hasWon)
        {
            DrawWonScreen();
            return;
        }
        
        // Draw death screen if dead
        if (isDead)
        {
            DrawDeathScreen();
            return;
        }
        
        if (currentRoomNode != null && currentRoomNode.RoomData != null)
        {
            DrawRoom(currentRoomNode);
            
            if (showGateway)
                DrawGateway();
            
            DrawItems();
            DrawPlayer();
            sword.Draw();
            
            // Draw boss
            currentBoss?.Draw(pauseFont);
            
            // Draw enemies
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive) enemy.Draw();
            }
            
            // Draw bullets
            foreach (var bullet in bullets)
            {
                Raylib.DrawCircleV(bullet.Position, bullet.Size, Color.Yellow);
            }
            
            DrawMinimap();
        }
        
        if (isPaused)
        {
            DrawPauseMenu();
        }
        else
        {
            Raylib.DrawTextEx(pauseFont, $"HP: {currentHealth}/{maxHealth}", new Vector2(10, 10), 20, 1, Color.Red);
            
            if (roomLocked)
            {
                string lockText = currentBoss != null && currentBoss.IsAlive
                    ? $"! BOSS: {currentBoss.Health}/{currentBoss.MaxHealth} HP"
                    : $"! {enemies.Count(e => e.IsAlive)} enemies remaining";
                Vector2 lockSize = Raylib.MeasureTextEx(pauseFont, lockText, 18, 1);
                Raylib.DrawTextEx(pauseFont, lockText, new Vector2((800 - lockSize.X) / 2, 10), 18, 1, new Color(220, 60, 60, 255));
            }
            
            if (showGateway)
                Raylib.DrawTextEx(pauseFont, Localization.Get("ui_gateway", currentLanguage), new Vector2(250, 400), 20, 1, Color.Gold);
        }
    }
    
    private void DrawWonScreen()
    {
        Raylib.DrawRectangle(0, 0, 800, 450, Color.Black);
        Raylib.DrawTextEx(pauseFont, Localization.Get("won_title", currentLanguage), new Vector2(280, 100), 50, 2, Color.Gold);
        
        string[] wonOptions = new[]
        {
            Localization.Get("won_restart", currentLanguage),
            Localization.Get("won_main_menu", currentLanguage)
        };
        
        for (int i = 0; i < 2; i++)
        {
            Rectangle buttonRect = new Rectangle(250, 220 + i * 60, 300, 50);
            Color bgColor = i == wonSelectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == wonSelectedOption ? Color.White : Color.Black;
            
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.Gold);
            
            Vector2 textSize = Raylib.MeasureTextEx(pauseFont, wonOptions[i], 24, 2);
            float textX = buttonRect.X + (buttonRect.Width - textSize.X) / 2;
            float textY = buttonRect.Y + (buttonRect.Height - textSize.Y) / 2;
            
            Raylib.DrawTextEx(pauseFont, wonOptions[i], new Vector2(textX, textY), 24, 2, textColor);
        }
    }
    
    private void DrawDeathScreen()
    {
        Raylib.DrawRectangle(0, 0, 800, 450, Color.Black);
        Raylib.DrawTextEx(pauseFont, Localization.Get("death_title", currentLanguage), new Vector2(250, 100), 50, 2, Color.Red);
        
        string[] deathOptions = new[]
        {
            Localization.Get("death_restart", currentLanguage),
            Localization.Get("death_main_menu", currentLanguage)
        };
        
        for (int i = 0; i < 2; i++)
        {
            Rectangle buttonRect = new Rectangle(250, 220 + i * 60, 300, 50);
            Color bgColor = i == deathSelectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == deathSelectedOption ? Color.White : Color.Black;
            
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.White);
            
            Vector2 textSize = Raylib.MeasureTextEx(pauseFont, deathOptions[i], 24, 2);
            float textX = buttonRect.X + (buttonRect.Width - textSize.X) / 2;
            float textY = buttonRect.Y + (buttonRect.Height - textSize.Y) / 2;
            
            Raylib.DrawTextEx(pauseFont, deathOptions[i], new Vector2(textX, textY), 24, 2, textColor);
        }
    }
    
    private void DrawGateway()
    {
        Raylib.DrawRectangle(
            (int)(gatewayPos.X - gatewaySize / 2),
            (int)(gatewayPos.Y - gatewaySize / 2),
            (int)gatewaySize,
            (int)gatewaySize,
            new Color(139, 69, 19, 255)
        );
        
        Raylib.DrawRectangleLines(
            (int)(gatewayPos.X - gatewaySize / 2),
            (int)(gatewayPos.Y - gatewaySize / 2),
            (int)gatewaySize,
            (int)gatewaySize,
            Color.Gold
        );
    }
    
    private void DrawMinimap()
    {
        int minimapTileSize = 15;
        int minimapX = 800 - 20;
        int minimapY = 20;
        
        int minX = currentFloor.Keys.Min(p => p.X);
        int maxX = currentFloor.Keys.Max(p => p.X);
        int minY = currentFloor.Keys.Min(p => p.Y);
        int maxY = currentFloor.Keys.Max(p => p.Y);
        
        int mapWidth = (maxX - minX + 1) * minimapTileSize + 10;
        int mapHeight = (maxY - minY + 1) * minimapTileSize + 10;
        Raylib.DrawRectangle(minimapX - mapWidth, minimapY, mapWidth, mapHeight, new Color(0, 0, 0, 150));
        
        foreach (var room in currentFloor.Values)
        {
            int roomX = minimapX - mapWidth + 5 + (room.Position.X - minX) * minimapTileSize;
            int roomY = minimapY + 5 + (room.Position.Y - minY) * minimapTileSize;
            
            Color roomColor = room.Type switch
            {
                RoomType.Start => new Color(0, 255, 0, 200),
                RoomType.Item  => new Color(255, 215, 0, 200),
                RoomType.Boss  => new Color(255, 0, 0, 200),
                _ => clearedRooms.Contains(room.Position)
                    ? new Color(80, 80, 80, 200)
                    : new Color(128, 128, 128, 200)
            };
            
            if (room.Position.Equals(currentRoomPos))
                roomColor = new Color(255, 255, 255, 255);
            
            Raylib.DrawRectangle(roomX, roomY, minimapTileSize - 2, minimapTileSize - 2, roomColor);
        }
    }
    
    private void DrawPauseMenu()
    {
        Raylib.DrawRectangle(0, 0, 800, 450, new Color(0, 0, 0, 180));
        Raylib.DrawTextEx(pauseFont, Localization.Get("pause_title", currentLanguage), new Vector2(330, 50), 40, 2, Color.White);
        
        int statsStartY = 110;
        int lineHeight = 22;
        
        Raylib.DrawTextEx(pauseFont, Localization.Get("settings_stats", currentLanguage), new Vector2(320, statsStartY), 20, 1, Color.White);
        Raylib.DrawTextEx(pauseFont, $"{Localization.Get("stats_health", currentLanguage)}: {currentHealth}/{maxHealth}", new Vector2(280, statsStartY + lineHeight), 18, 1, Color.Red);
        Raylib.DrawTextEx(pauseFont, $"{Localization.Get("stats_speed", currentLanguage)}: {speed:F1}", new Vector2(280, statsStartY + lineHeight * 2), 18, 1, Color.Green);
        Raylib.DrawTextEx(pauseFont, $"{Localization.Get("stats_damage", currentLanguage)}: {damage}", new Vector2(280, statsStartY + lineHeight * 3), 18, 1, Color.Orange);
        Raylib.DrawTextEx(pauseFont, $"{Localization.Get("stats_attack_speed", currentLanguage)}: {attackSpeed:F1}", new Vector2(280, statsStartY + lineHeight * 4), 18, 1, Color.Yellow);
        Raylib.DrawTextEx(pauseFont, $"{Localization.Get("stats_range", currentLanguage)}: {range:F1}", new Vector2(280, statsStartY + lineHeight * 5), 18, 1, Color.Blue);
        
        string[] pauseOptions = new[]
        {
            Localization.Get("pause_return", currentLanguage),
            Localization.Get("pause_settings", currentLanguage),
            Localization.Get("pause_main_menu", currentLanguage)
        };
        
        for (int i = 0; i < 3; i++)
        {
            Rectangle buttonRect = new Rectangle(250, 260 + i * 50, 300, 45);
            Color bgColor = i == pauseSelectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == pauseSelectedOption ? Color.White : Color.Black;
            
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.Black);
            
            Vector2 textSize = Raylib.MeasureTextEx(pauseFont, pauseOptions[i], 22, 1);
            float textX = buttonRect.X + (buttonRect.Width - textSize.X) / 2;
            float textY = buttonRect.Y + (buttonRect.Height - textSize.Y) / 2;
            
            Raylib.DrawTextEx(pauseFont, pauseOptions[i], new Vector2(textX, textY), 22, 1, textColor);
        }
    }
    
    private void DrawPlayer()
    {
        // Flash red while taking damage iframes
        Color playerColor = (playerFlashTimer > 0 && (int)(playerFlashTimer * 10) % 2 == 0)
            ? Color.Red : Color.Black;
        Raylib.DrawRectangle(
            (int)(playerPos.X - playerSize / 2),
            (int)(playerPos.Y - playerSize / 2),
            (int)playerSize,
            (int)playerSize,
            playerColor
        );
    }
    
    private void DrawRoom(FloorNode node)
    {
        // Draw walls
        for (int x = 0; x < 15; x++)
        {
            Color wallColor = (x == 7 && node.HasTopDoor) ? Color.Beige : Color.DarkGray;
            Raylib.DrawRectangle(offsetX + x * tileSize, offsetY, tileSize - 1, tileSize - 1, wallColor);
        }
        
        for (int x = 0; x < 15; x++)
        {
            Color wallColor = (x == 7 && node.HasBottomDoor) ? Color.Beige : Color.DarkGray;
            Raylib.DrawRectangle(offsetX + x * tileSize, offsetY + 8 * tileSize, tileSize - 1, tileSize - 1, wallColor);
        }
        
        for (int y = 0; y < 9; y++)
        {
            Color wallColor = (y == 4 && node.HasLeftDoor) ? Color.Beige : Color.DarkGray;
            Raylib.DrawRectangle(offsetX, offsetY + y * tileSize, tileSize - 1, tileSize - 1, wallColor);
        }
        
        for (int y = 0; y < 9; y++)
        {
            Color wallColor = (y == 4 && node.HasRightDoor) ? Color.Beige : Color.DarkGray;
            Raylib.DrawRectangle(offsetX + 14 * tileSize, offsetY + y * tileSize, tileSize - 1, tileSize - 1, wallColor);
        }
        
        // Locked door overlays
        if (roomLocked)
        {
            Color lockColor = new Color(180, 30, 30, 220);
            if (node.HasTopDoor)    Raylib.DrawRectangle(offsetX + 7 * tileSize,      offsetY,              tileSize - 1, tileSize - 1, lockColor);
            if (node.HasBottomDoor) Raylib.DrawRectangle(offsetX + 7 * tileSize,      offsetY + 8 * tileSize, tileSize - 1, tileSize - 1, lockColor);
            if (node.HasLeftDoor)   Raylib.DrawRectangle(offsetX,                     offsetY + 4 * tileSize, tileSize - 1, tileSize - 1, lockColor);
            if (node.HasRightDoor)  Raylib.DrawRectangle(offsetX + 14 * tileSize,     offsetY + 4 * tileSize, tileSize - 1, tileSize - 1, lockColor);
        }
        
        // Unlock flash
        if (lockFlashTimer > 0)
        {
            float alpha = lockFlashTimer / LockFlashDuration;
            Color fc = new Color((byte)255, (byte)255, (byte)100, (byte)(alpha * 180));
            if (node.HasTopDoor)    Raylib.DrawRectangle(offsetX + 7 * tileSize,  offsetY,              tileSize - 1, tileSize - 1, fc);
            if (node.HasBottomDoor) Raylib.DrawRectangle(offsetX + 7 * tileSize,  offsetY + 8 * tileSize, tileSize - 1, tileSize - 1, fc);
            if (node.HasLeftDoor)   Raylib.DrawRectangle(offsetX,                 offsetY + 4 * tileSize, tileSize - 1, tileSize - 1, fc);
            if (node.HasRightDoor)  Raylib.DrawRectangle(offsetX + 14 * tileSize, offsetY + 4 * tileSize, tileSize - 1, tileSize - 1, fc);
        }
        
        // Draw room interior
        int[,] layout = node.RoomData!.Layout;
        
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 13; x++)
            {
                Color tileColor = layout[y, x] switch
                {
                    0 => Color.Beige,
                    2 => Color.Brown,
                    3 => Color.Gold,
                    _ => Color.Gray
                };
                
                Raylib.DrawRectangle(
                    offsetX + (x + 1) * tileSize,
                    offsetY + (y + 1) * tileSize,
                    tileSize - 1,
                    tileSize - 1,
                    tileColor
                );
            }
        }
        
        if (!isPaused)
        {
            Raylib.DrawTextEx(pauseFont, $"{Localization.Get("ui_room", currentLanguage)}: {node.Type}", new Vector2(10, 420), 16, 1, Color.White);
            Raylib.DrawTextEx(pauseFont, $"{Localization.Get("ui_floor", currentLanguage)} {currentFloorNumber}", new Vector2(680, 10), 20, 1, Color.White);
        }
    }
    
    private void DrawItems()
    {
        foreach (var item in activeItems)
        {
            bool isPedestal = item.Type != ItemType.HealthPickup;
            float bob = isPedestal ? (float)Math.Sin(item.BobTimer * 2.5f) * 3f : 0f;
            Vector2 drawPos = item.Position + new Vector2(0, bob);
            float radius = isPedestal ? 10f : 7f;
            if (isPedestal)
                Raylib.DrawRectangle((int)(item.Position.X - 14), (int)(item.Position.Y + 8), 28, 6, new Color(100, 80, 60, 200));
            Raylib.DrawCircleV(drawPos, radius + 2, new Color(0, 0, 0, 80));
            Raylib.DrawCircleV(drawPos, radius, item.GetColor());
            Raylib.DrawCircleLines((int)drawPos.X, (int)drawPos.Y, (int)radius, Color.White);
            string label = item.GetLabel();
            Vector2 ls = Raylib.MeasureTextEx(pauseFont, label, 14, 1);
            Raylib.DrawTextEx(pauseFont, label, new Vector2(drawPos.X - ls.X / 2, drawPos.Y - radius - 18), 14, 1, Color.White);
        }
    }
}